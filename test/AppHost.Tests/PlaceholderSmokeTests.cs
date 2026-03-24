using System.Text.Json;
using Shouldly;
using Xunit;

namespace AppHost.Tests
{
    /// <summary>
    /// Verifies the AppHost contract that keeps the Studio shell wired to the active Theia workspace.
    /// </summary>
    public sealed class PlaceholderSmokeTests
    {
        /// <summary>
        /// Verifies that AppHost still reads the fixed Studio shell port from configuration.
        /// </summary>
        [Fact]
        public void AppHost_configuration_keeps_the_fixed_studio_shell_port()
        {
            // Read the checked-in AppHost configuration so the fixed Studio shell port contract is protected.
            using var configurationDocument = JsonDocument.Parse(File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "AppHost", "appsettings.json")));
            var studioServerPort = configurationDocument.RootElement
                                                        .GetProperty("Studio")
                                                        .GetProperty("Server")
                                                        .GetProperty("Port")
                                                        .GetInt32();

            studioServerPort.ShouldBe(3000);
        }

        /// <summary>
        /// Verifies that AppHost still points Aspire at the fresh Studio shell workspace and preserved environment bridge.
        /// </summary>
        [Fact]
        public void AppHost_source_keeps_the_active_studio_shell_contract()
        {
            // Read the AppHost source so the preserved JavaScript resource contract remains covered where direct execution is impractical.
            var appHostSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "AppHost", "AppHost.cs"));

            appHostSource.ShouldContain("AddJavaScriptApp(ServiceNames.StudioShell, \"../../Studio/Server\", \"start:browser\")");
            appHostSource.ShouldContain(".WithBuildScript(\"build:browser\")");
            appHostSource.ShouldContain(".WithEnvironment(\"STUDIO_API_HOST_API_BASE_URL\", studioApi.GetEndpoint(\"https\"))");
            appHostSource.ShouldContain(".WithHttpEndpoint(targetPort: studioShellPort, port: studioShellPort, env: \"PORT\", isProxied: false)");
        }

        /// <summary>
        /// Resolves a repository-relative file path from the test output directory.
        /// </summary>
        /// <param name="pathSegments">The repository-relative path segments to combine.</param>
        /// <returns>The absolute path to the requested repository file.</returns>
        private static string GetRepositoryFilePath(params string[] pathSegments)
        {
            // Walk up from the test output directory until the repository root marker is found.
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

            while (currentDirectory is not null)
            {
                var solutionPath = Path.Combine(currentDirectory.FullName, "Search.slnx");

                if (File.Exists(solutionPath))
                {
                    return Path.Combine([currentDirectory.FullName, .. pathSegments]);
                }

                currentDirectory = currentDirectory.Parent;
            }

            throw new InvalidOperationException("The repository root could not be located from the test output directory.");
        }
    }
}
