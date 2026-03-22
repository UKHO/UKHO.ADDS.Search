using Microsoft.Extensions.Options;
using UKHO.Search.Ingestion.Providers;

namespace UKHO.Search.Services.Ingestion.Providers
{
    public sealed class IngestionProviderService : IIngestionProviderService
    {
        private readonly IReadOnlyDictionary<string, IIngestionDataProviderFactory> _providers;

        public IngestionProviderService(IEnumerable<IIngestionDataProviderFactory> providers, IOptions<IngestionProviderOptions> options)
        {
            ArgumentNullException.ThrowIfNull(providers);
            ArgumentNullException.ThrowIfNull(options);

            var providersByName = new Dictionary<string, IIngestionDataProviderFactory>(StringComparer.OrdinalIgnoreCase);

            foreach (var provider in providers)
            {
                ArgumentNullException.ThrowIfNull(provider);

                if (!providersByName.TryAdd(provider.Name, provider))
                {
                    throw new InvalidOperationException($"Ingestion provider '{provider.Name}' is registered more than once.");
                }
            }

            var enabledProviderNames = (options.Value.Providers ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (enabledProviderNames.Length == 0)
            {
                _providers = providersByName.OrderBy(x => x.Key, StringComparer.Ordinal)
                    .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
                return;
            }

            _providers = enabledProviderNames
                .Where(providersByName.ContainsKey)
                .Select(x => providersByName[x])
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<IIngestionDataProviderFactory> GetAllProviders()
        {
            return _providers.Values;
        }

        public IIngestionDataProviderFactory GetProvider(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (!_providers.TryGetValue(name, out var provider))
            {
                throw new KeyNotFoundException($"No ingestion provider registered with name '{name}'.");
            }

            return provider;
        }
    }
}