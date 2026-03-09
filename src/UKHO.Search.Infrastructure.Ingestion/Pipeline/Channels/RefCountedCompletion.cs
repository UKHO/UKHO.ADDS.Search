using System.Threading.Channels;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Channels
{
    public sealed class RefCountedCompletion
    {
        private Exception? error;
        private int remaining;

        public RefCountedCompletion(int writers)
        {
            if (writers <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(writers));
            }

            remaining = writers;
        }

        public bool TryComplete<T>(ChannelWriter<T> inner, Exception? completeError = null)
        {
            ArgumentNullException.ThrowIfNull(inner);

            if (completeError is not null)
            {
                Interlocked.CompareExchange(ref error, completeError, null);
            }

            if (Interlocked.Decrement(ref remaining) == 0)
            {
                return inner.TryComplete(error);
            }

            return true;
        }
    }
}