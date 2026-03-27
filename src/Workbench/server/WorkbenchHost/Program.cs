using UKHO.Workbench.Infrastructure;
using WorkbenchHost.Components;

namespace WorkbenchHost
{
    /// <summary>
    /// Boots the temporary Workbench host that serves Razor components directly from the server.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Configures and starts the Workbench host.
        /// </summary>
        /// <param name="args">The command-line arguments supplied to the ASP.NET Core host.</param>
        public static void Main(string[] args)
        {
            // Create the ASP.NET Core host builder that will serve the temporary Workbench experience.
            var builder = WebApplication.CreateBuilder(args);

            // Compose the server-side Onion chain from the outer infrastructure layer inward.
            builder.Services.AddWorkbenchInfrastructure();

            // Register the Razor component services required for the host-served Blazor root page.
            builder.Services.AddRazorComponents()
                   .AddInteractiveServerComponents();

            var app = builder.Build();

            // Keep the production pipeline minimal while still enforcing HTTPS and basic exception handling.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/");
                app.UseHsts();
            }

            // Redirect plain HTTP requests to the HTTPS endpoint exposed by the host.
            app.UseHttpsRedirection();

            // Enable the anti-forgery protections expected by interactive server-rendered components.
            app.UseAntiforgery();

            // Expose the component-generated static assets and route the root request through the Razor component app shell.
            app.MapStaticAssets();
            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
