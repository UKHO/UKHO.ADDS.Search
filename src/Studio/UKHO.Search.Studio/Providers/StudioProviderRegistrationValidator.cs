using Microsoft.Extensions.Logging;
using UKHO.Search.ProviderModel;

namespace UKHO.Search.Studio.Providers
{
    /// <summary>
    /// Verifies that every Studio provider registration has a matching provider metadata registration.
    /// </summary>
    public sealed class StudioProviderRegistrationValidator : IStudioProviderRegistrationValidator
    {
        private readonly ILogger<StudioProviderRegistrationValidator> _logger;
        private readonly IProviderCatalog _providerCatalog;
        private readonly IStudioProviderCatalog _studioProviderCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="StudioProviderRegistrationValidator"/> class.
        /// </summary>
        /// <param name="providerCatalog">The provider metadata catalog that defines known providers.</param>
        /// <param name="studioProviderCatalog">The Studio provider catalog that defines Studio registrations.</param>
        /// <param name="logger">The logger used to record successful validation results.</param>
        public StudioProviderRegistrationValidator(
            IProviderCatalog providerCatalog,
            IStudioProviderCatalog studioProviderCatalog,
            ILogger<StudioProviderRegistrationValidator> logger)
        {
            // Capture the required collaborators up front so validation can compare the two provider catalogs.
            _providerCatalog = providerCatalog ?? throw new ArgumentNullException(nameof(providerCatalog));
            _studioProviderCatalog = studioProviderCatalog ?? throw new ArgumentNullException(nameof(studioProviderCatalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates that each Studio provider is present in provider metadata.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a Studio provider is missing from provider metadata.</exception>
        public void Validate()
        {
            // Read the registered Studio providers once so the validation loop works over a stable snapshot.
            var studioProviders = _studioProviderCatalog.GetAllProviders();

            // Ensure every Studio registration points at known provider metadata before the host starts serving requests.
            foreach (var studioProvider in studioProviders)
            {
                if (!_providerCatalog.TryGetProvider(studioProvider.ProviderName, out var descriptor))
                {
                    throw new InvalidOperationException($"Studio provider '{studioProvider.ProviderName}' is not registered in provider metadata.");
                }

                // Log successful matches to aid startup diagnostics when providers are wired through DI.
                _logger.LogInformation("Validated studio provider registration. ProviderName={ProviderName} DisplayName={DisplayName}", descriptor!.Name, descriptor.DisplayName);
            }
        }
    }
}
