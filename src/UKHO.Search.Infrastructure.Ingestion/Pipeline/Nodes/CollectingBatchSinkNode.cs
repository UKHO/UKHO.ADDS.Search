using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes
{
    public sealed class CollectingBatchSinkNode<TPayload> : SinkNodeBase<BatchEnvelope<TPayload>>
    {
        private readonly object gate = new();
        private readonly List<Envelope<TPayload>> items = new();
        private readonly ILogger? logger;

        public CollectingBatchSinkNode(string name, ChannelReader<BatchEnvelope<TPayload>> input, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, logger, fatalErrorReporter)
        {
            this.logger = logger;
        }

        public IReadOnlyList<Envelope<TPayload>> Items
        {
            get
            {
                lock (gate)
                {
                    return items.ToArray();
                }
            }
        }

        protected override ValueTask HandleItemAsync(BatchEnvelope<TPayload> batch, CancellationToken cancellationToken)
        {
            foreach (var envelope in batch.Items)
            {
                envelope.Context.AddBreadcrumb(Name);
                envelope.Context.MarkTimeUtc($"received:{Name}", DateTimeOffset.UtcNow);

                logger?.LogInformation("Stub indexed message. NodeName={NodeName} PartitionId={PartitionId} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, batch.PartitionId, envelope.Key, envelope.MessageId, envelope.Attempt);

                lock (gate)
                {
                    items.Add(envelope);
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}