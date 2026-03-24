namespace StudioServiceHost.Operations
{
    /// <summary>
    /// Reports progress updates synchronously so callers observe state transitions in emission order.
    /// </summary>
    /// <typeparam name="T">The progress payload type.</typeparam>
    internal sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronousProgress{T}"/> class.
        /// </summary>
        /// <param name="handler">The callback that receives each reported progress update.</param>
        public SynchronousProgress(Action<T> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Reports a progress update immediately on the calling thread.
        /// </summary>
        /// <param name="value">The progress update to forward.</param>
        public void Report(T value)
        {
            _handler(value);
        }
    }
}
