using Microsoft.Extensions.DependencyInjection;

namespace UKHO.Workbench.Client.Services
{
    /// <summary>
    /// Registers the minimal client-side Workbench services required for the bootstrap slice.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the client-side Workbench service-layer registrations.
        /// </summary>
        /// <param name="services">The service collection that receives the Workbench client service registrations.</param>
        /// <returns>The same service collection so outer layers can continue fluent registration.</returns>
        public static IServiceCollection AddWorkbenchClientServices(this IServiceCollection services)
        {
            // Guard the composition root against invalid host startup input.
            ArgumentNullException.ThrowIfNull(services);

            // The bootstrap slice intentionally keeps the client service layer empty until real features are introduced.
            return services;
        }
    }
}
