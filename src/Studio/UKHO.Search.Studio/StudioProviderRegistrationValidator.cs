using Microsoft.Extensions.Logging;
using UKHO.Search.ProviderModel;

namespace UKHO.Search.Studio
{
    public sealed class StudioProviderRegistrationValidator : IStudioProviderRegistrationValidator
    {
        private readonly ILogger<StudioProviderRegistrationValidator> _logger;
        private readonly IProviderCatalog _providerCatalog;
        private readonly IStudioProviderCatalog _studioProviderCatalog;

        public StudioProviderRegistrationValidator(
            IProviderCatalog providerCatalog,
            IStudioProviderCatalog studioProviderCatalog,
            ILogger<StudioProviderRegistrationValidator> logger)
        {
            _providerCatalog = providerCatalog ?? throw new ArgumentNullException(nameof(providerCatalog));
            _studioProviderCatalog = studioProviderCatalog ?? throw new ArgumentNullException(nameof(studioProviderCatalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Validate()
        {
            var studioProviders = _studioProviderCatalog.GetAllProviders();

            foreach (var studioProvider in studioProviders)
            {
                if (!_providerCatalog.TryGetProvider(studioProvider.ProviderName, out var descriptor))
                {
                    throw new InvalidOperationException($"Studio provider '{studioProvider.ProviderName}' is not registered in provider metadata.");
                }

                _logger.LogInformation("Validated studio provider registration. ProviderName={ProviderName} DisplayName={DisplayName}", descriptor!.Name, descriptor.DisplayName);
            }
        }
    }
}
