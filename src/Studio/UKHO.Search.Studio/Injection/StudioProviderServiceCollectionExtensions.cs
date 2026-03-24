using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UKHO.Search.Studio.Providers;

namespace UKHO.Search.Studio.Injection
{
    /// <summary>
    /// Adds Studio provider catalog services and provider registrations to a service collection.
    /// </summary>
    public static class StudioProviderServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the shared Studio provider catalog services when they are not already registered.
        /// </summary>
        /// <param name="services">The service collection being configured.</param>
        /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection AddStudioProviderCatalog(this IServiceCollection services)
        {
            // Guard the extension method because callers are expected to provide an active service collection.
            ArgumentNullException.ThrowIfNull(services);

            // Register the catalog and validator once so every provider registration shares the same lookup infrastructure.
            services.TryAddSingleton<IStudioProviderCatalog>(sp => new StudioProviderCatalog(sp.GetServices<IStudioProvider>()));
            services.TryAddSingleton<IStudioProviderRegistrationValidator, StudioProviderRegistrationValidator>();

            // Return the original service collection to preserve standard DI chaining semantics.
            return services;
        }

        /// <summary>
        /// Registers a Studio provider and uses a marker type to keep the registration idempotent.
        /// </summary>
        /// <typeparam name="TRegistrationMarker">A marker type used to detect whether the provider has already been registered.</typeparam>
        /// <typeparam name="TStudioProvider">The Studio provider implementation to register.</typeparam>
        /// <param name="services">The service collection being configured.</param>
        /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
        public static IServiceCollection AddStudioProvider<TRegistrationMarker, TStudioProvider>(this IServiceCollection services)
            where TRegistrationMarker : class, new()
            where TStudioProvider : class, IStudioProvider
        {
            // Validate the input before composing the shared provider catalog services.
            ArgumentNullException.ThrowIfNull(services);

            // Ensure the shared catalog services are available before registering the concrete provider.
            services.AddStudioProviderCatalog();

            // Use the marker registration to make repeated registrations for the same provider idempotent.
            if (services.Any(x => x.ServiceType == typeof(TRegistrationMarker)))
            {
                return services;
            }

            // Register the provider as part of the catalog and then record the marker that prevents duplicates.
            services.AddSingleton<IStudioProvider, TStudioProvider>();
            services.AddSingleton<TRegistrationMarker>();

            // Return the original service collection to support fluent registration patterns.
            return services;
        }
    }
}
