using System.Text.Json;
using Shouldly;
using Xunit;

namespace AppHost.Tests
{
    /// <summary>
    /// Verifies the AppHost contract that keeps the discontinued Studio and Theia workflow detached from the active developer path.
    /// </summary>
    public sealed class PlaceholderSmokeTests
    {
        /// <summary>
        /// Verifies that the checked-in AppHost configuration no longer carries the retired Studio shell settings.
        /// </summary>
        [Fact]
        public void AppHost_configuration_removes_the_retired_studio_shell_settings()
        {
            // Read the checked-in AppHost configuration so the active developer workflow stays free from retired Studio shell settings.
            using var configurationDocument = JsonDocument.Parse(File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "AppHost", "appsettings.json")));

            configurationDocument.RootElement.TryGetProperty("Studio", out _).ShouldBeFalse();
        }

        /// <summary>
        /// Verifies that AppHost source no longer registers the retired Studio API and Theia shell resources.
        /// </summary>
        [Fact]
        public void AppHost_source_removes_the_retired_studio_and_theia_resources()
        {
            // Read the AppHost source so the discontinued Studio and Theia orchestration stays removed where direct execution is impractical.
            var appHostSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "AppHost", "AppHost.cs"));

            appHostSource.ShouldNotContain("AddProject<StudioServiceHost>");
            appHostSource.ShouldNotContain("AddJavaScriptApp(ServiceNames.StudioShell");
            appHostSource.ShouldNotContain(".WithYarn(");
            appHostSource.ShouldNotContain("STUDIO_API_HOST_API_BASE_URL");
            appHostSource.ShouldNotContain("studioApi");
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
