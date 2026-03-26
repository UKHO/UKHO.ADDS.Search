using Microsoft.Extensions.DependencyInjection;
using UKHO.Workbench.Client.Services;

namespace UKHO.Workbench.Client.Infrastructure
{
    /// <summary>
    /// Registers the minimal client-side infrastructure wiring required by the hosted Blazor application.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the client-side Workbench infrastructure registrations and composes the inward service layer.
        /// </summary>
        /// <param name="services">The service collection that receives the Workbench client infrastructure registrations.</param>
        /// <returns>The same service collection so the client bootstrap can continue fluent registration.</returns>
        public static IServiceCollection AddWorkbenchClientInfrastructure(this IServiceCollection services)
        {
            // Guard the extension entry point so client startup failures are clear.
            ArgumentNullException.ThrowIfNull(services);

            // Compose the inward service layer now so future client infrastructure dependencies remain centralized here.
            services.AddWorkbenchClientServices();

            // No concrete infrastructure services are required for the minimal hello experience.
            return services;
        }
    }
}
