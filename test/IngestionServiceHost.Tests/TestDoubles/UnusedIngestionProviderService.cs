using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Services.Ingestion.Providers;

namespace IngestionServiceHost.Tests.TestDoubles
{
    internal sealed class UnusedIngestionProviderService : IIngestionProviderService
    {
        public IEnumerable<IIngestionDataProviderFactory> GetAllProviders()
        {
            return Array.Empty<IIngestionDataProviderFactory>();
        }

        public IIngestionDataProviderFactory GetProvider(string name)
        {
            throw new NotSupportedException();
        }
    }
}
