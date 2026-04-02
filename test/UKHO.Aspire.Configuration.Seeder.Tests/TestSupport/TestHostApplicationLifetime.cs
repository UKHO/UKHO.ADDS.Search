using Microsoft.Extensions.Hosting;

namespace UKHO.Aspire.Configuration.Seeder.Tests.TestSupport
{
    /// <summary>
    /// Captures host-lifetime interactions so hosted-service tests can verify whether shutdown was requested.
    /// </summary>
    internal sealed class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _applicationStartedSource = new();
        private readonly CancellationTokenSource _applicationStoppingSource = new();
        private readonly CancellationTokenSource _applicationStoppedSource = new();

        /// <summary>
        /// Gets the token representing the started state for the fake host lifetime.
        /// </summary>
        public CancellationToken ApplicationStarted => _applicationStartedSource.Token;

        /// <summary>
        /// Gets the token representing the stopping state for the fake host lifetime.
        /// </summary>
        public CancellationToken ApplicationStopping => _applicationStoppingSource.Token;

        /// <summary>
        /// Gets the token representing the stopped state for the fake host lifetime.
        /// </summary>
        public CancellationToken ApplicationStopped => _applicationStoppedSource.Token;

        /// <summary>
        /// Gets the number of times the hosted service requested application shutdown.
        /// </summary>
        public int StopApplicationCallCount { get; private set; }

        /// <summary>
        /// Records a shutdown request and transitions the stopping and stopped tokens.
        /// </summary>
        public void StopApplication()
        {
            // Count the shutdown request first so assertions can distinguish multiple calls if a regression appears.
            StopApplicationCallCount++;

            // Transition both tokens once because the hosted-service tests only need to know that shutdown was requested.
            if (!_applicationStoppingSource.IsCancellationRequested)
            {
                _applicationStoppingSource.Cancel();
            }

            if (!_applicationStoppedSource.IsCancellationRequested)
            {
                _applicationStoppedSource.Cancel();
            }
        }
    }
}
