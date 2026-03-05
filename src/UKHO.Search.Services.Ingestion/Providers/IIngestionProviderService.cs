namespace UKHO.Search.Services.Ingestion.Providers
{
    using UKHO.Search.Ingestion.Providers;

    public interface IIngestionProviderService
    {
        IEnumerable<IIngestionDataProvider> GetAllProviders();

        IIngestionDataProvider GetProvider(string name);
    }
}
