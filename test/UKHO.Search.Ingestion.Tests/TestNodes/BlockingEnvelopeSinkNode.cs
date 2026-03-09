using System.Threading.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Ingestion.Tests.TestNodes
{
    public sealed class BlockingEnvelopeSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
    {
        private readonly int blockAfterCount;
        private readonly object gate = new();
        private readonly List<Envelope<TPayload>> items = new();
        private readonly SemaphoreSlim receivedSignal = new(0);
        private readonly TaskCompletionSource releaseGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingEnvelopeSinkNode(string name, ChannelReader<Envelope<TPayload>> input, int blockAfterCount) : base(name, input)
        {
            this.blockAfterCount = blockAfterCount;
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

        public void ReleaseBlocking()
        {
            releaseGate.TrySetResult();
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

        protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            lock (gate)
            {
                items.Add(item);
            }

            receivedSignal.Release();

            if (blockAfterCount > 0)
            {
                var shouldBlock = false;
                lock (gate)
                {
                    shouldBlock = items.Count == blockAfterCount;
                }

                if (shouldBlock)
                {
                    await releaseGate.Task.WaitAsync(cancellationToken)
                                     .ConfigureAwait(false);
                }
            }
        }
    }
}