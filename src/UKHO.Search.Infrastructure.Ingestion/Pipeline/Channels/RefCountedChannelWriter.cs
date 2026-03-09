using System.Threading.Channels;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Channels
{
    public sealed class RefCountedChannelWriter<T> : ChannelWriter<T>
    {
        private readonly RefCountedCompletion completion;
        private readonly ChannelWriter<T> inner;

        public RefCountedChannelWriter(ChannelWriter<T> inner, RefCountedCompletion completion)
        {
            this.inner = inner;
            this.completion = completion;
        }

        public override bool TryComplete(Exception? error = null)
        {
            return completion.TryComplete(inner, error);
        }

        public override bool TryWrite(T item)
        {
            return inner.TryWrite(item);
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        {
            return inner.WaitToWriteAsync(cancellationToken);
        }

        public override ValueTask WriteAsync(T item, CancellationToken cancellationToken = default)
        {
            return inner.WriteAsync(item, cancellationToken);
        }
    }
}