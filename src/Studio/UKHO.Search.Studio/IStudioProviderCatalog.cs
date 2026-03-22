namespace UKHO.Search.Studio
{
    public interface IStudioProviderCatalog
    {
        IReadOnlyCollection<IStudioProvider> GetAllProviders();

        IStudioProvider GetProvider(string providerName);

        bool TryGetProvider(string providerName, out IStudioProvider? provider);
    }
}
