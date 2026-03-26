using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UKHO.Workbench.Client.Infrastructure;

namespace WorkbenchClient
{
    /// <summary>
    /// Bootstraps the hosted Blazor WebAssembly client for the minimal Workbench slice.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Configures the browser host and starts the minimal Workbench client.
        /// </summary>
        /// <param name="args">The command-line arguments supplied by the browser host environment.</param>
        /// <returns>A task that completes when the WebAssembly host shuts down.</returns>
        public static async Task Main(string[] args)
        {
            // Create the WebAssembly host builder that wires the client root components and service registrations.
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Attach the application shell to the DOM so the hosted page can render the Workbench greeting at '/'.
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // Compose the client-side Onion chain from the outer infrastructure layer inward.
            builder.Services.AddWorkbenchClientInfrastructure();

            // Build and run the browser host so the minimal page becomes interactive once loaded.
            await builder.Build()
                         .RunAsync();
        }
    }
}
