using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public sealed class IngestionSourceNode : SourceNodeBase<Envelope<IngestionRequest>>
    {
        private readonly IConfiguration configuration;
        private readonly ILogger logger;
        private readonly IIngestionProviderService providerService;
        private readonly IQueueClientFactory queueClientFactory;

        public IngestionSourceNode(string name, ChannelWriter<Envelope<IngestionRequest>> output, IConfiguration configuration, IIngestionProviderService providerService, IQueueClientFactory queueClientFactory, ILogger logger, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, output, logger, fatalErrorReporter)
        {
            this.configuration = configuration;
            this.providerService = providerService;
            this.queueClientFactory = queueClientFactory;
            this.logger = logger;
        }

        protected override async ValueTask ProduceAsync(ChannelWriter<Envelope<IngestionRequest>> output, CancellationToken cancellationToken)
        {
            var providers = providerService.GetAllProviders()
                                           .ToArray();

            if (providers.Length == 0)
            {
                logger.LogWarning("No ingestion providers registered; source node will be idle.");
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken)
                          .ConfigureAwait(false);
                return;
            }

            var tasks = providers.Select(factory => PollProviderQueueAsync(factory, output, cancellationToken))
                                 .ToArray();

            await Task.WhenAll(tasks)
                      .ConfigureAwait(false);
        }

        private async Task PollProviderQueueAsync(IIngestionDataProviderFactory factory, ChannelWriter<Envelope<IngestionRequest>> output, CancellationToken cancellationToken)
        {
            var queueName = factory.QueueName;
            if (string.IsNullOrWhiteSpace(queueName))
            {
                logger.LogWarning("Ingestion provider '{ProviderName}' has an empty queue name; skipping.", factory.Name);
                return;
            }

            var provider = factory.CreateProvider();

            var receiveBatchSize = configuration.GetValue("ingestion:queueReceiveBatchSize", 16);
            var visibilityTimeoutSeconds = configuration.GetValue("ingestion:queueVisibilityTimeoutSeconds", 300);
            var renewalSeconds = configuration.GetValue("ingestion:queueVisibilityRenewalSeconds", 60);
            var pollingIntervalMs = configuration.GetValue("ingestion:queuePollingIntervalMilliseconds", 1000);
            var maxDequeueCount = configuration.GetValue("ingestion:queueMaxDequeueCount", 5);
            var poisonQueueSuffix = configuration["ingestion:poisonQueueSuffix"] ?? "-poison";

            var visibilityTimeout = TimeSpan.FromSeconds(visibilityTimeoutSeconds);
            var renewalInterval = TimeSpan.FromSeconds(renewalSeconds);
            var pollingInterval = TimeSpan.FromMilliseconds(pollingIntervalMs);

            var queue = queueClientFactory.GetQueueClient(queueName);
            var poisonQueue = queueClientFactory.GetQueueClient(queueName + poisonQueueSuffix);

            logger.LogInformation("Ensuring ingestion queues exist. ProviderName={ProviderName} QueueName={QueueName} PoisonQueueName={PoisonQueueName}", factory.Name, queueName, queueName + poisonQueueSuffix);

            await queue.CreateIfNotExistsAsync(cancellationToken)
                       .ConfigureAwait(false);
            await poisonQueue.CreateIfNotExistsAsync(cancellationToken)
                             .ConfigureAwait(false);

            logger.LogInformation("Starting ingestion queue poller. ProviderName={ProviderName} QueueName={QueueName}", factory.Name, queueName);

            while (!cancellationToken.IsCancellationRequested)
            {
                var messages = await queue.ReceiveMessagesAsync(receiveBatchSize, visibilityTimeout, cancellationToken)
                                          .ConfigureAwait(false);

                if (messages.Count == 0)
                {
                    await Task.Delay(pollingInterval, cancellationToken)
                              .ConfigureAwait(false);
                    continue;
                }

                foreach (var message in messages)
                {
                    if (message.DequeueCount > maxDequeueCount)
                    {
                        await MoveToPoisonAsync(queueName, queue, poisonQueue, message, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }

                    IngestionRequest request;
                    try
                    {
                        request = await provider.DeserializeIngestionRequestAsync(message.MessageText, cancellationToken)
                                                .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to deserialize ingestion request. ProviderName={ProviderName} QueueName={QueueName} MessageId={MessageId} DequeueCount={DequeueCount}", factory.Name, queueName, message.MessageId, message.DequeueCount);
                        continue;
                    }

                    var requestId = GetRequestId(request);
                    if (string.IsNullOrWhiteSpace(requestId))
                    {
                        logger.LogWarning("Ingestion request did not contain a valid Id. ProviderName={ProviderName} QueueName={QueueName} MessageId={MessageId}", factory.Name, queueName, message.MessageId);
                        continue;
                    }

                    var envelope = new Envelope<IngestionRequest>(requestId, request);
                    if (envelope.Headers is Dictionary<string, string> headers)
                    {
                        headers["queueName"] = queueName;
                        headers["queueMessageId"] = message.MessageId;
                        headers["dequeueCount"] = message.DequeueCount.ToString();
                        headers["providerName"] = factory.Name;
                    }

                    var acker = new QueueMessageAcker(queue, message.MessageId, message.PopReceipt, message.MessageText, logger);

                    acker.StartVisibilityRenewal(visibilityTimeout, renewalInterval, cancellationToken);

                    envelope.Context.SetItem(QueueEnvelopeContextKeys.MessageAcker, acker);

                    await WriteAsync(output, envelope, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private async ValueTask MoveToPoisonAsync(string queueName, IQueueClient queue, IQueueClient poisonQueue, QueueReceivedMessage message, CancellationToken cancellationToken)
        {
            var poisonBody = JsonSerializer.Serialize(new
            {
                queueName,
                messageId = message.MessageId,
                dequeueCount = message.DequeueCount,
                insertedOnUtc = message.InsertedOnUtc,
                nextVisibleOnUtc = message.NextVisibleOnUtc,
                movedToPoisonUtc = DateTimeOffset.UtcNow,
                body = message.MessageText,
                reason = "MaxDequeueCountExceeded"
            });

            var acker = new QueueMessageAcker(queue, message.MessageId, message.PopReceipt, message.MessageText, logger);

            await acker.MoveToPoisonAsync(poisonQueue, poisonBody, cancellationToken)
                       .ConfigureAwait(false);

            logger.LogWarning("Moved message to poison queue. QueueName={QueueName} PoisonQueueName={PoisonQueueName} MessageId={MessageId} DequeueCount={DequeueCount}", queueName, queueName + (configuration["ingestion:poisonQueueSuffix"] ?? "-poison"), message.MessageId, message.DequeueCount);
        }

        private static string? GetRequestId(IngestionRequest request)
        {
            return request.RequestType switch
            {
                IngestionRequestType.AddItem => request.AddItem?.Id,
                IngestionRequestType.UpdateItem => request.UpdateItem?.Id,
                IngestionRequestType.DeleteItem => request.DeleteItem?.Id,
                IngestionRequestType.UpdateAcl => request.UpdateAcl?.Id,
                var _ => null
            };
        }
    }
}