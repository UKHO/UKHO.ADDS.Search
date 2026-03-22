using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UKHO.Search.ProviderModel.Injection
{
    public static class ProviderServiceCollectionExtensions
    {
        public static IServiceCollection AddProviderCatalog(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<IProviderCatalog>(sp => new ProviderCatalog(sp.GetServices<ProviderDescriptor>()));

            return services;
        }

        public static IServiceCollection AddProviderDescriptor<TRegistrationMarker>(this IServiceCollection services, ProviderDescriptor descriptor)
            where TRegistrationMarker : class, new()
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(descriptor);

            services.AddProviderCatalog();

            if (services.Any(x => x.ServiceType == typeof(TRegistrationMarker)))
            {
                return services;
            }

            services.AddSingleton(descriptor);
            services.AddSingleton<TRegistrationMarker>();

            return services;
        }
    }
}
