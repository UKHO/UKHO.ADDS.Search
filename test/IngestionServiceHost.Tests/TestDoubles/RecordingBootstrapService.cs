using UKHO.Search.Infrastructure.Ingestion.Bootstrap;

namespace IngestionServiceHost.Tests.TestDoubles
{
    internal sealed class RecordingBootstrapService : IBootstrapService
    {
        public int CallCount { get; private set; }

        public Task BootstrapAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
