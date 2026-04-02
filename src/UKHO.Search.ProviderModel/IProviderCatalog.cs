namespace UKHO.Search.ProviderModel
{
    public interface IProviderCatalog
    {
        IReadOnlyCollection<ProviderDescriptor> GetAllProviders();

        ProviderDescriptor GetProvider(string name);

        bool TryGetProvider(string name, out ProviderDescriptor? descriptor);
    }
}
