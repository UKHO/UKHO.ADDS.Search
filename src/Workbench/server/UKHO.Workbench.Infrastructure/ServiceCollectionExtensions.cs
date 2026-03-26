using Microsoft.Extensions.DependencyInjection;
using UKHO.Workbench.Services;

namespace UKHO.Workbench.Infrastructure
{
    /// <summary>
    /// Registers the minimal server-side infrastructure wiring required by the Workbench host.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the server-side Workbench infrastructure registrations and composes the inward service layer.
        /// </summary>
        /// <param name="services">The service collection that receives the Workbench infrastructure registrations.</param>
        /// <returns>The same service collection so the host can continue fluent registration.</returns>
        public static IServiceCollection AddWorkbenchInfrastructure(this IServiceCollection services)
        {
            // Guard the extension entry point so a host configuration error fails fast.
            ArgumentNullException.ThrowIfNull(services);

            // Compose the inward layer now so later infrastructure registrations can remain centralized here.
            services.AddWorkbenchServices();

            // No concrete infrastructure services are required for the hello-world bootstrap slice.
            return services;
        }
    }
}
