using Microsoft.Extensions.DependencyInjection;

namespace UKHO.Workbench.Services
{
    /// <summary>
    /// Registers the minimal server-side Workbench services required for the bootstrap slice.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the server-side Workbench service-layer registrations.
        /// </summary>
        /// <param name="services">The service collection that receives the Workbench service registrations.</param>
        /// <returns>The same service collection so registration calls can be chained by the host and outer layers.</returns>
        public static IServiceCollection AddWorkbenchServices(this IServiceCollection services)
        {
            // Guard the composition root against a missing service collection so failures are immediate and descriptive.
            ArgumentNullException.ThrowIfNull(services);

            // The initial Workbench slice has no concrete server services yet.
            // Returning the collection preserves the extension point for later work items without introducing placeholder behaviour.
            return services;
        }
    }
}
