using Microsoft.Extensions.DependencyInjection;
using UKHO.Workbench.Output;
using UKHO.Workbench.Services.Output;
using UKHO.Workbench.Services.Shell;

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

            // The bootstrap slice keeps shell orchestration in a singleton manager because the shell itself is singleton-oriented for the first hosted-tool path.
            services.AddSingleton<WorkbenchShellManager>();

            // The output stream is shell-wide for the current session, so the host and shell share one singleton in-memory service.
            services.AddSingleton<IWorkbenchOutputService, WorkbenchOutputService>();

            // Returning the collection preserves the extension point for later work items while centralizing shell orchestration registration here.
            return services;
        }
    }
}
