using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.ProviderModel;

namespace UKHO.Search.Services.Ingestion.Providers
{
    public sealed class IngestionProviderStartupValidator : IIngestionProviderStartupValidator
    {
        private readonly ILogger<IngestionProviderStartupValidator> _logger;
        private readonly IngestionProviderOptions _options;
        private readonly IProviderCatalog _providerCatalog;
        private readonly IEnumerable<IIngestionDataProviderFactory> _providers;

        public IngestionProviderStartupValidator(
            IOptions<IngestionProviderOptions> options,
            IProviderCatalog providerCatalog,
            IEnumerable<IIngestionDataProviderFactory> providers,
            ILogger<IngestionProviderStartupValidator> logger)
        {
            ArgumentNullException.ThrowIfNull(options);

            _options = options.Value;
            _providerCatalog = providerCatalog ?? throw new ArgumentNullException(nameof(providerCatalog));
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Validate()
        {
            var configuredProviderNames = GetConfiguredProviderNames();
            var runtimeProviders = BuildRuntimeProvidersByName();
            var enabledProviderNames = configuredProviderNames.Length == 0
                ? runtimeProviders.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
                : configuredProviderNames;

            if (configuredProviderNames.Length == 0)
            {
                _logger.LogInformation("No enabled ingestion providers were configured; defaulting to all registered runtime providers. ProviderCount={ProviderCount}", runtimeProviders.Count);
            }

            foreach (var providerName in enabledProviderNames)
            {
                if (!_providerCatalog.TryGetProvider(providerName, out var descriptor))
                {
                    throw new InvalidOperationException($"Enabled ingestion provider '{providerName}' is not registered in provider metadata.");
                }

                if (!runtimeProviders.ContainsKey(providerName))
                {
                    throw new InvalidOperationException($"Enabled ingestion provider '{descriptor!.Name}' does not have a runtime registration.");
                }
            }

            _logger.LogInformation("Validated ingestion provider enablement. EnabledProviderCount={ProviderCount} Providers={Providers}", enabledProviderNames.Length, enabledProviderNames);
        }

        private string[] GetConfiguredProviderNames()
        {
            return (_options.Providers ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private Dictionary<string, IIngestionDataProviderFactory> BuildRuntimeProvidersByName()
        {
            var runtimeProviders = new Dictionary<string, IIngestionDataProviderFactory>(StringComparer.OrdinalIgnoreCase);

            foreach (var provider in _providers)
            {
                ArgumentNullException.ThrowIfNull(provider);

                if (!runtimeProviders.TryAdd(provider.Name, provider))
                {
                    throw new InvalidOperationException($"Ingestion provider '{provider.Name}' already has a runtime registration.");
                }
            }

            return runtimeProviders;
        }
    }
}
