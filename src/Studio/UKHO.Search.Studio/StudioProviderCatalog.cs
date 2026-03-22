namespace UKHO.Search.Studio
{
    public sealed class StudioProviderCatalog : IStudioProviderCatalog
    {
        private readonly IReadOnlyDictionary<string, IStudioProvider> _providersByName;
        private readonly IReadOnlyList<IStudioProvider> _orderedProviders;

        public StudioProviderCatalog(IEnumerable<IStudioProvider> providers)
        {
            ArgumentNullException.ThrowIfNull(providers);

            var orderedProviders = providers.OrderBy(x => x.ProviderName, StringComparer.Ordinal).ToArray();
            var providersByName = new Dictionary<string, IStudioProvider>(StringComparer.OrdinalIgnoreCase);

            foreach (var provider in orderedProviders)
            {
                ArgumentNullException.ThrowIfNull(provider);

                if (!providersByName.TryAdd(provider.ProviderName, provider))
                {
                    throw new InvalidOperationException($"A studio provider with the name '{provider.ProviderName}' is already registered.");
                }
            }

            _orderedProviders = orderedProviders;
            _providersByName = providersByName;
        }

        public IReadOnlyCollection<IStudioProvider> GetAllProviders()
        {
            return _orderedProviders;
        }

        public IStudioProvider GetProvider(string providerName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

            if (!TryGetProvider(providerName, out var provider))
            {
                throw new KeyNotFoundException($"No studio provider registered with name '{providerName}'.");
            }

            return provider!;
        }

        public bool TryGetProvider(string providerName, out IStudioProvider? provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

            return _providersByName.TryGetValue(providerName, out provider);
        }
    }
}
