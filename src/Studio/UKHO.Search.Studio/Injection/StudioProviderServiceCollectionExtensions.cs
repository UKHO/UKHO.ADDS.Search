using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UKHO.Search.Studio.Injection
{
    public static class StudioProviderServiceCollectionExtensions
    {
        public static IServiceCollection AddStudioProviderCatalog(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<IStudioProviderCatalog>(sp => new StudioProviderCatalog(sp.GetServices<IStudioProvider>()));
            services.TryAddSingleton<IStudioProviderRegistrationValidator, StudioProviderRegistrationValidator>();

            return services;
        }

        public static IServiceCollection AddStudioProvider<TRegistrationMarker, TStudioProvider>(this IServiceCollection services)
            where TRegistrationMarker : class, new()
            where TStudioProvider : class, IStudioProvider
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddStudioProviderCatalog();

            if (services.Any(x => x.ServiceType == typeof(TRegistrationMarker)))
            {
                return services;
            }

            services.AddSingleton<IStudioProvider, TStudioProvider>();
            services.AddSingleton<TRegistrationMarker>();

            return services;
        }
    }
}
