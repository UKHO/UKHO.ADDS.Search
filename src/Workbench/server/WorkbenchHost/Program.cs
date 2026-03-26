using UKHO.Workbench.Infrastructure;

namespace WorkbenchHost
{
    /// <summary>
    /// Boots the minimal server host that serves the hosted Workbench Blazor WebAssembly client.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Configures and starts the Workbench host.
        /// </summary>
        /// <param name="args">The command-line arguments supplied to the ASP.NET Core host.</param>
        public static void Main(string[] args)
        {
            // Create the ASP.NET Core host builder that will serve the hosted Blazor WebAssembly client.
            var builder = WebApplication.CreateBuilder(args);

            // Compose the server-side Onion chain from the outer infrastructure layer inward.
            builder.Services.AddWorkbenchInfrastructure();

            var app = builder.Build();

            // Keep the production pipeline minimal while still enforcing HTTPS and basic exception handling.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/");
                app.UseHsts();
            }

            // Redirect plain HTTP requests to the HTTPS endpoint exposed by the host.
            app.UseHttpsRedirection();

            // Expose the hosted Blazor WebAssembly framework assets and the client static web assets.
            // The hosted WebAssembly client relies on the static-file middleware pipeline for the _framework payload.
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            // Route every non-file request to the hosted client so the Workbench hello page is available at '/'.
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
