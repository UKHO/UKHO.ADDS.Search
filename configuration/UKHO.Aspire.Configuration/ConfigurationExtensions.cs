using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UKHO.Aspire.Configuration.Remote;

namespace UKHO.Aspire.Configuration
{
    public static class ConfigurationExtensions
    {
        public static TBuilder AddConfiguration<TBuilder>(this TBuilder builder, string serviceName, string componentName, string? serviceIdentityId = null, int refreshIntervalSeconds = 30) where TBuilder : IHostApplicationBuilder
        {
            var environment = AddsEnvironment.GetEnvironment();

            if (environment == AddsEnvironment.Local)
            {
                builder.Configuration.AddAzureAppConfiguration(o =>
                {
                    var endpoint = ResolveLocalAppConfigurationEndpoint(builder.Configuration);
                    var connectionString = $"Endpoint={endpoint};Id=aac-credential;Secret=c2VjcmV0;";

                    o.Connect(connectionString)
                     .Select("*", serviceName.ToLowerInvariant())
                     .ConfigureRefresh(refresh =>
                     {
                         refresh.Register(WellKnownConfigurationName.ReloadSentinelKey, refreshAll: true, label: serviceName.ToLowerInvariant())
                                .SetRefreshInterval(TimeSpan.FromSeconds(refreshIntervalSeconds));
                     });
                });
            }
            else
            {
                builder.AddAzureAppConfiguration(componentName.ToLowerInvariant(), null, o =>
                {
                    o.Select("*", serviceName.ToLowerInvariant())
                     .ConfigureRefresh(refresh =>
                     {
                         refresh.Register(WellKnownConfigurationName.ReloadSentinelKey, refreshAll: true, label: serviceName.ToLowerInvariant())
                                .SetRefreshInterval(TimeSpan.FromSeconds(refreshIntervalSeconds));
                     });

                    o.ConfigureKeyVault(keyVaultOptions =>
                    {
                        keyVaultOptions.SetCredential(new ManagedIdentityCredential(serviceIdentityId));
                    });
                });
            }

            builder.Services.AddSingleton<IExternalServiceRegistry, ExternalServiceRegistry>();

            return builder;
        }

        private static string ResolveLocalAppConfigurationEndpoint(IConfiguration configuration)
        {
            var endpointKeys = new[]
            {
                $"services__{WellKnownConfigurationName.ConfigurationServiceName}__https__0",
                $"services__{WellKnownConfigurationName.ConfigurationServiceName}__http__0"
            };

            foreach (var endpointKey in endpointKeys)
            {
                var endpoint = configuration[NormalizeConfigurationKey(endpointKey)] ?? Environment.GetEnvironmentVariable(endpointKey);
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    return endpoint.TrimEnd('/');
                }
            }

            throw new InvalidOperationException($"Azure App Configuration endpoint was not found in configuration. Checked keys: {string.Join(", ", endpointKeys)}");
        }

        private static string NormalizeConfigurationKey(string key)
        {
            return key.Replace("__", ":", StringComparison.Ordinal);
        }

    }
}