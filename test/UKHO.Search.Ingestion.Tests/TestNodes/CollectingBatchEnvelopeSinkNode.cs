using System.Threading.Channels;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Ingestion.Tests.TestNodes
{
    public sealed class CollectingBatchEnvelopeSinkNode<TPayload> : SinkNodeBase<BatchEnvelope<TPayload>>
    {
        private readonly object gate = new();
        private readonly List<Envelope<TPayload>> items = new();
        private readonly SemaphoreSlim receivedSignal = new(0);

        public CollectingBatchEnvelopeSinkNode(string name, ChannelReader<BatchEnvelope<TPayload>> input) : base(name, input, cancellationMode: CancellationMode.Drain)
        {
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

        public async Task WaitForCountAsync(int expectedCount, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);

            while (true)
            {
                lock (gate)
                {
                    if (items.Count >= expectedCount)
                    {
                        return;
                    }
                }

                await receivedSignal.WaitAsync(cts.Token)
                                    .ConfigureAwait(false);
            }
        }

        protected override ValueTask HandleItemAsync(BatchEnvelope<TPayload> batch, CancellationToken cancellationToken)
        {
            lock (gate)
            {
                items.AddRange(batch.Items);
            }

            receivedSignal.Release(batch.Items.Count);
            return ValueTask.CompletedTask;
        }
    }
}