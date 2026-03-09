using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Pipelines.DeadLetter;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Terminal
{
    public sealed class DeadLetterPersistAndAckSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(StringComparer.OrdinalIgnoreCase);
        private readonly bool fatalIfCannotPersist;

        private readonly string filePath;
        private readonly ILogger? logger;
        private readonly IDeadLetterMetadataProvider metadataProvider;

        public DeadLetterPersistAndAckSinkNode(string name, ChannelReader<Envelope<TPayload>> input, string filePath, bool fatalIfCannotPersist = false, IDeadLetterMetadataProvider? metadataProvider = null, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, logger, fatalErrorReporter)
        {
            this.filePath = filePath;
            this.fatalIfCannotPersist = fatalIfCannotPersist;
            this.metadataProvider = metadataProvider ?? new DefaultDeadLetterMetadataProvider();
            this.logger = logger;
        }

        protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);

            try
            {
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

                var json = JsonSerializer.Serialize(record);
                await AppendLineAsync(json, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Dead-letter persist failed in '{NodeName}' for MessageId={MessageId} Key='{Key}' ErrorCode='{ErrorCode}'.", Name, item.MessageId, item.Key, item.Error?.Code);

                if (fatalIfCannotPersist)
                {
                    throw;
                }
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
                logger?.LogError(ex, "Failed to delete queue message after dead-letter persistence. NodeName={NodeName} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, item.Key, item.MessageId, item.Attempt);
            }
        }

        private async Task AppendLineAsync(string line, CancellationToken cancellationToken)
        {
            var fileLock = FileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
            await fileLock.WaitAsync(cancellationToken)
                          .ConfigureAwait(false);
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true);
                await using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken)
                            .ConfigureAwait(false);
            }
            finally
            {
                fileLock.Release();
            }
        }
    }
}