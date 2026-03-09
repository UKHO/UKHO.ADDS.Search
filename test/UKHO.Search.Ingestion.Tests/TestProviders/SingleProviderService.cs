using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Ingestion.Tests.TestProviders
{
    public sealed class SingleProviderService : IIngestionProviderService
    {
        private readonly IIngestionDataProviderFactory factory;

        public SingleProviderService(IIngestionDataProviderFactory factory)
        {
            this.factory = factory;
        }

        public IEnumerable<IIngestionDataProviderFactory> GetAllProviders()
        {
            return new[] { factory };
        }

        public IIngestionDataProviderFactory GetProvider(string name)
        {
            return factory;
        }
    }
}