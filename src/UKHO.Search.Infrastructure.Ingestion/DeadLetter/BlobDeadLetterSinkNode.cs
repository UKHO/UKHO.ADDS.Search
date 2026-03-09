using System.Text.Json;
using System.Threading.Channels;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Pipelines.DeadLetter;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.DeadLetter
{
    public sealed class BlobDeadLetterSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
    {
        private readonly string blobPrefix;
        private readonly BlobContainerClient containerClient;
        private readonly SemaphoreSlim ensureContainerSemaphore = new(1, 1);
        private readonly bool fatalIfCannotPersist;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly ILogger? logger;
        private readonly IDeadLetterMetadataProvider metadataProvider;
        private bool containerEnsured;

        public BlobDeadLetterSinkNode(string name, ChannelReader<Envelope<TPayload>> input, BlobServiceClient blobServiceClient, IConfiguration configuration, bool fatalIfCannotPersist = true, string? containerName = null, string? blobPrefix = null, IDeadLetterMetadataProvider? metadataProvider = null, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name,
            input, logger, fatalErrorReporter)
        {
            this.fatalIfCannotPersist = fatalIfCannotPersist;
            this.metadataProvider = metadataProvider ?? new DefaultDeadLetterMetadataProvider();
            this.logger = logger;

            var resolvedContainerName = containerName ?? configuration["ingestion:deadletterContainer"];
            if (string.IsNullOrWhiteSpace(resolvedContainerName))
            {
                throw new InvalidOperationException("Missing required configuration value 'ingestion:deadletterContainer'.");
            }

            this.blobPrefix = blobPrefix ?? configuration["ingestion:deadletterBlobPrefix"] ?? "deadletter";

            containerClient = blobServiceClient.GetBlobContainerClient(resolvedContainerName);

            jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);

            var persisted = false;

            try
            {
                await EnsureContainerExistsAsync(cancellationToken)
                    .ConfigureAwait(false);

                var record = new DeadLetterRecord<TPayload>
                {
                    DeadLetteredAtUtc = DateTimeOffset.UtcNow,
                    NodeName = Name,
                    Envelope = item,
                    Error = item.Error,
                    RawSnapshot = null,
                    Metadata = new DeadLetterMetadata
                    {
                        AppVersion = metadataProvider.AppVersion,
                        CommitId = metadataProvider.CommitId,
                        HostName = metadataProvider.HostName
                    }
                };

                var blobName = CreateBlobName(record.DeadLetteredAtUtc, item);

                BinaryData data;
                try
                {
                    data = BinaryData.FromString(JsonSerializer.Serialize(record, jsonOptions));
                }
                catch (Exception ex)
                {
                    var fallback = new
                    {
                        record.DeadLetteredAtUtc,
                        record.NodeName,
                        Envelope = new
                        {
                            item.TimestampUtc,
                            item.CorrelationId,
                            item.Headers,
                            item.MessageId,
                            item.Key,
                            item.Attempt,
                            item.Status,
                            item.Error,
                            item.Context
                        },
                        SerializationError = ex.ToString()
                    };

                    data = BinaryData.FromString(JsonSerializer.Serialize(fallback, jsonOptions));
                }

                await containerClient.GetBlobClient(blobName)
                                     .UploadAsync(data, true, cancellationToken)
                                     .ConfigureAwait(false);

                persisted = true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Blob dead-letter persist failed in '{NodeName}' for MessageId={MessageId} Key='{Key}' ErrorCode='{ErrorCode}'.", Name, item.MessageId, item.Key, item.Error?.Code);

                if (fatalIfCannotPersist)
                {
                    throw;
                }
            }

            if (!persisted)
            {
                return;
            }

            if (!item.Context.TryGetItem<IQueueMessageAcker>(QueueEnvelopeContextKeys.MessageAcker, out var acker) || acker is null)
            {
                return;
            }

            try
            {
                await acker.DeleteAsync(cancellationToken)
                           .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to delete queue message after blob dead-letter persistence. NodeName={NodeName} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, item.Key, item.MessageId, item.Attempt);
            }
        }

        private async Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
        {
            if (containerEnsured)
            {
                return;
            }

            await ensureContainerSemaphore.WaitAsync(cancellationToken)
                                          .ConfigureAwait(false);
            try
            {
                if (containerEnsured)
                {
                    return;
                }

                await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                                     .ConfigureAwait(false);

                containerEnsured = true;
            }
            finally
            {
                ensureContainerSemaphore.Release();
            }
        }

        private string CreateBlobName(DateTimeOffset deadLetteredAtUtc, Envelope<TPayload> envelope)
        {
            var date = deadLetteredAtUtc.UtcDateTime;

            var prefix = blobPrefix.Trim('/');
            var key = SanitizePathSegment(envelope.Key);
            var messageId = envelope.MessageId.ToString("D");

            return $"{prefix}/{date:yyyy/MM/dd}/{key}/{messageId}.json";
        }

        private static string SanitizePathSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "_";
            }

            return value.Replace("/", "_", StringComparison.Ordinal)
                        .Replace("\\", "_", StringComparison.Ordinal);
        }
    }
}