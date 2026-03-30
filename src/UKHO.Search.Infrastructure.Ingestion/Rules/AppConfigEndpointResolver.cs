using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class AppConfigEndpointResolver
    {
        private const string ConfigurationServiceName = "adds-configuration";

        private readonly IConfiguration _configuration;
        private readonly ILogger<AppConfigEndpointResolver> _logger;

        public AppConfigEndpointResolver(IConfiguration configuration, ILogger<AppConfigEndpointResolver> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Uri? TryResolveEndpoint()
        {
            // Aspire provides referenced service endpoints via configuration.
            // Prefer HTTPS when available and fall back to HTTP for older local setups.
            var endpointKeys = new[]
            {
                $"services__{ConfigurationServiceName}__https__0",
                $"services__{ConfigurationServiceName}__http__0"
            };

            foreach (var endpointKey in endpointKeys)
            {
                var url = _configuration[NormalizeConfigurationKey(endpointKey)] ?? Environment.GetEnvironmentVariable(endpointKey);
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    _logger.LogWarning("Azure App Configuration endpoint is invalid. Url={Url}", url);
                    continue;
                }

                return uri;
            }

            _logger.LogWarning("Azure App Configuration endpoint not found in configuration. Keys={Keys}", string.Join(", ", endpointKeys));
            return null;
        }

        private static string NormalizeConfigurationKey(string key)
        {
            return key.Replace("__", ":", StringComparison.Ordinal);
        }
    }
}
