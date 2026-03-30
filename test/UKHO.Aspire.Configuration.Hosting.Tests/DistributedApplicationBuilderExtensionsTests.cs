using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Projects;
using Shouldly;
using UKHO.Aspire.Configuration;
using UKHO.Aspire.Configuration.Hosting;
using Xunit;

namespace UKHO.Aspire.Configuration.Hosting.Tests
{
    /// <summary>
    /// Verifies that the hosting extension methods add the expected Aspire resources, relationships,
    /// waits, endpoints, and environment variables without changing production behavior.
    /// </summary>
    public sealed class DistributedApplicationBuilderExtensionsTests
    {
        /// <summary>
        /// Validates that `AddConfiguration` creates an Azure App Configuration resource and wires each
        /// configuration-aware project to reference that resource while propagating the selected environment.
        /// </summary>
        /// <returns>A task that completes when the asynchronous environment callback assertions have finished.</returns>
        [Fact]
        public async Task AddConfiguration_WhenCalled_ShouldCreateAppConfigurationAndWireConfigurationAwareProjects()
        {
            // Create a lightweight Aspire builder so the test can inspect the in-memory application model.
            var builder = CreateBuilder();
            var addsEnvironment = builder.AddParameter("adds-environment-parameter", "Prod");
            var emulatorProject = builder.AddProject<UKHO_Aspire_Configuration_Emulator>("config-aware-emulator");
            var seederProject = builder.AddProject<UKHO_Aspire_Configuration_Seeder>("config-aware-seeder");

            // Execute the extension method under test using two representative project resources.
            var appConfiguration = builder.AddConfiguration(
                "app-configuration",
                addsEnvironment,
                [emulatorProject, seederProject]);

            // Confirm the extension returns and registers the expected Azure App Configuration resource.
            appConfiguration.Resource.Name.ShouldBe("app-configuration");
            builder.Resources.OfType<AzureAppConfigurationResource>().Single(resource => resource.Name == "app-configuration")
                   .ShouldBe(appConfiguration.Resource);

            // Validate both projects received the reference relationship and the propagated environment value.
            await AssertProjectReferencesConfigurationAsync(emulatorProject.Resource, appConfiguration.Resource, "Prod");
            await AssertProjectReferencesConfigurationAsync(seederProject.Resource, appConfiguration.Resource, "Prod");
        }

        /// <summary>
        /// Validates that `AddConfigurationEmulator` creates the emulator resource with the expected public HTTP endpoint,
        /// health check metadata, and local-environment settings used during local development.
        /// </summary>
        /// <returns>A task that completes when the asynchronous environment callback assertions have finished.</returns>
        [Fact]
        public async Task AddConfigurationEmulator_WhenCalled_ShouldCreateEmulatorWithEndpointHealthCheckAndLocalEnvironment()
        {
            // Create the builder and deterministic source files required by the seeder setup.
            var builder = CreateBuilder();
            var sourceDirectory = CreateTemporaryDirectory();
            var configurationPath = Path.Combine(sourceDirectory, "configuration.json");
            var externalServicesPath = Path.Combine(sourceDirectory, "external-services.json");

            File.WriteAllText(configurationPath, "{\"setting\":\"value\"}");
            File.WriteAllText(externalServicesPath, "{\"service\":\"value\"}");

            try
            {
                // Use relative paths so the test exercises the content-root-based path resolution in production code.
                var relativeConfigurationPath = Path.GetRelativePath(builder.Environment.ContentRootPath, configurationPath);
                var relativeExternalServicesPath = Path.GetRelativePath(builder.Environment.ContentRootPath, externalServicesPath);

                // Invoke the emulator extension with no dependent projects so the assertions can focus on emulator wiring.
                var emulator = builder.AddConfigurationEmulator(
                    "ukho-search",
                    [],
                    [],
                    relativeConfigurationPath,
                    relativeExternalServicesPath);

                // Confirm the returned resource name matches the well-known configuration service name.
                emulator.Resource.Name.ShouldBe(WellKnownConfigurationName.ConfigurationServiceName);

                // Validate the external HTTP endpoint created by WithExternalHttpEndpoints.
                emulator.Resource.TryGetEndpoints(out var endpoints).ShouldBeTrue();
                endpoints!.Any(endpoint =>
                        endpoint.UriScheme == "http"
                        && endpoint.Transport == "http"
                        && endpoint.IsExternal)
                    .ShouldBeTrue();

                // Validate the health check annotation added for the emulator endpoint.
                emulator.Resource.Annotations.OfType<HealthCheckAnnotation>().ShouldNotBeEmpty();

                // Validate the emulator always runs with the local configuration environment.
                var environmentVariables = await GetEnvironmentVariablesAsync(emulator.Resource);
                environmentVariables[WellKnownConfigurationName.AddsEnvironmentName].ShouldBe(AddsEnvironment.Local.Value);
            }
            finally
            {
                // Ensure the temporary source directory created for the test is removed deterministically.
                DeleteDirectoryIfExists(sourceDirectory);
            }
        }

        /// <summary>
        /// Validates that `AddConfigurationEmulator` copies the supplied configuration inputs to temporary files,
        /// creates the seeder resource, and wires the seeder to both the emulator and supplied mock resources.
        /// </summary>
        /// <returns>A task that completes when the asynchronous environment callback assertions have finished.</returns>
        [Fact]
        public async Task AddConfigurationEmulator_WhenCalled_ShouldCreateSeederWithCopiedInputsAndMockReferences()
        {
            // Create a builder, source files, and a representative external mock resource for the seeder.
            var builder = CreateBuilder();
            var sourceDirectory = CreateTemporaryDirectory();
            var configurationPath = Path.Combine(sourceDirectory, "configuration.json");
            var externalServicesPath = Path.Combine(sourceDirectory, "external-services.json");

            File.WriteAllText(configurationPath, "{\"configuration\":\"content\"}");
            File.WriteAllText(externalServicesPath, "{\"external\":\"content\"}");

            var mockProject = builder.AddProject<UKHO_Aspire_Configuration_Seeder>("mock-service");
            string? copiedConfigurationPath = null;
            string? copiedExternalServicesPath = null;

            try
            {
                // Resolve the paths relative to the builder content root to exercise the production path resolution logic.
                var relativeConfigurationPath = Path.GetRelativePath(builder.Environment.ContentRootPath, configurationPath);
                var relativeExternalServicesPath = Path.GetRelativePath(builder.Environment.ContentRootPath, externalServicesPath);

                // Execute the extension with explicit additional configuration values so their propagation can be asserted.
                var emulator = builder.AddConfigurationEmulator(
                    "ukho-search",
                    [],
                    [mockProject],
                    relativeConfigurationPath,
                    relativeExternalServicesPath,
                    additionalConfigurationPath: "rules-root",
                    additionalConfigurationPrefix: "rules-prefix");

                // Locate the seeder resource registered by the extension.
                var seeder = builder.Resources.OfType<ProjectResource>()
                                    .Single(resource => resource.Name == WellKnownConfigurationName.ConfigurationSeederName);

                // Validate the seeder waits for and references both the emulator and the supplied mock resource.
                seeder.Annotations.OfType<WaitAnnotation>()
                      .Any(annotation => ReferenceEquals(annotation.Resource, emulator.Resource))
                      .ShouldBeTrue();
                seeder.Annotations.OfType<ResourceRelationshipAnnotation>()
                      .Any(annotation => ReferenceEquals(annotation.Resource, emulator.Resource))
                      .ShouldBeTrue();
                seeder.Annotations.OfType<ResourceRelationshipAnnotation>()
                      .Any(annotation => ReferenceEquals(annotation.Resource, mockProject.Resource))
                      .ShouldBeTrue();

                // Evaluate the seeder environment callbacks so the copied file paths and propagated values can be inspected.
                var environmentVariables = await GetEnvironmentVariablesAsync(seeder);
                environmentVariables[WellKnownConfigurationName.AddsEnvironmentName].ShouldBe(AddsEnvironment.Local.Value);
                environmentVariables[WellKnownConfigurationName.ServiceName].ShouldBe("ukho-search");
                environmentVariables[WellKnownConfigurationName.AdditionalConfigurationPath].ShouldBe("rules-root");
                environmentVariables[WellKnownConfigurationName.AdditionalConfigurationPrefix].ShouldBe("rules-prefix");

                copiedConfigurationPath = environmentVariables[WellKnownConfigurationName.ConfigurationFilePath].ShouldBeOfType<string>();
                copiedExternalServicesPath = environmentVariables[WellKnownConfigurationName.ExternalServicesFilePath].ShouldBeOfType<string>();

                // Confirm the seeder received copied files instead of the original source files and preserved their content.
                copiedConfigurationPath.ShouldNotBe(configurationPath);
                copiedExternalServicesPath.ShouldNotBe(externalServicesPath);
                File.ReadAllText(copiedConfigurationPath).ShouldBe(File.ReadAllText(configurationPath));
                File.ReadAllText(copiedExternalServicesPath).ShouldBe(File.ReadAllText(externalServicesPath));
            }
            finally
            {
                // Clean up both the source directory and any copied temp files created by the production method.
                DeleteFileIfExists(copiedConfigurationPath);
                DeleteFileIfExists(copiedExternalServicesPath);
                DeleteDirectoryIfExists(sourceDirectory);
            }
        }

        /// <summary>
        /// Validates that `AddConfigurationEmulator` wires configuration-aware projects to reference the emulator,
        /// wait for both the emulator and seeder, and receive the local environment variable used by local runs.
        /// </summary>
        /// <returns>A task that completes when the asynchronous environment callback assertions have finished.</returns>
        [Fact]
        public async Task AddConfigurationEmulator_WhenCalled_ShouldWireConfigurationAwareProjectsToWaitForSeederAndEmulator()
        {
            // Create the builder, dependent projects, and the source files required by the seeder resource.
            var builder = CreateBuilder();
            var sourceDirectory = CreateTemporaryDirectory();
            var configurationPath = Path.Combine(sourceDirectory, "configuration.json");
            var externalServicesPath = Path.Combine(sourceDirectory, "external-services.json");

            File.WriteAllText(configurationPath, "{\"setting\":\"value\"}");
            File.WriteAllText(externalServicesPath, "{\"service\":\"value\"}");

            var projectOne = builder.AddProject<UKHO_Aspire_Configuration_Emulator>("aware-project-one");
            var projectTwo = builder.AddProject<UKHO_Aspire_Configuration_Seeder>("aware-project-two");
            string? copiedConfigurationPath = null;
            string? copiedExternalServicesPath = null;

            try
            {
                // Resolve the inputs relative to the content root so the test covers the full emulator setup path.
                var relativeConfigurationPath = Path.GetRelativePath(builder.Environment.ContentRootPath, configurationPath);
                var relativeExternalServicesPath = Path.GetRelativePath(builder.Environment.ContentRootPath, externalServicesPath);

                // Execute the extension with two configuration-aware projects so both loop iterations are exercised.
                var emulator = builder.AddConfigurationEmulator(
                    "ukho-search",
                    [projectOne, projectTwo],
                    [],
                    relativeConfigurationPath,
                    relativeExternalServicesPath);

                var seeder = builder.Resources.OfType<ProjectResource>()
                                    .Single(resource => resource.Name == WellKnownConfigurationName.ConfigurationSeederName);

                // Capture the copied file paths so they can be deleted during cleanup.
                var seederEnvironment = await GetEnvironmentVariablesAsync(seeder);
                copiedConfigurationPath = seederEnvironment[WellKnownConfigurationName.ConfigurationFilePath].ShouldBeOfType<string>();
                copiedExternalServicesPath = seederEnvironment[WellKnownConfigurationName.ExternalServicesFilePath].ShouldBeOfType<string>();

                // Validate the first project received the expected waits, reference, and local environment value.
                await AssertProjectWaitsForEmulatorAndSeederAsync(projectOne.Resource, emulator.Resource, seeder);

                // Validate the second project received the same wiring, proving the foreach loop applies to all projects.
                await AssertProjectWaitsForEmulatorAndSeederAsync(projectTwo.Resource, emulator.Resource, seeder);
            }
            finally
            {
                // Remove every temporary artifact created to keep the test deterministic across repeated runs.
                DeleteFileIfExists(copiedConfigurationPath);
                DeleteFileIfExists(copiedExternalServicesPath);
                DeleteDirectoryIfExists(sourceDirectory);
            }
        }

        /// <summary>
        /// Creates a distributed application builder configured for isolated unit-style inspection of the Aspire model.
        /// </summary>
        /// <returns>A distributed application builder with the dashboard disabled for deterministic tests.</returns>
        private static IDistributedApplicationBuilder CreateBuilder()
        {
            // Disable the dashboard because these tests only inspect the in-memory model and do not need runtime hosting.
            return DistributedApplication.CreateBuilder(
                new DistributedApplicationOptions
                {
                    Args = [],
                    DisableDashboard = true,
                });
        }

        /// <summary>
        /// Evaluates the environment callbacks attached to a resource and returns the resolved environment dictionary.
        /// </summary>
        /// <param name="resource">The resource whose environment callbacks should be executed.</param>
        /// <returns>A dictionary containing the environment values produced by the resource annotations.</returns>
        private static async Task<Dictionary<string, object>> GetEnvironmentVariablesAsync(IResource resource)
        {
            // Retrieve the callbacks attached by the Aspire builder extensions.
            resource.TryGetEnvironmentVariables(out var annotations).ShouldBeTrue();

            var environmentVariables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Execute every callback using a run-mode execution context so parameter and literal values resolve normally.
            var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
            var callbackContext = new EnvironmentCallbackContext(executionContext, resource, environmentVariables);

            foreach (var annotation in annotations!)
            {
                // Each annotation contributes environment values into the same dictionary used by the resource model.
                await annotation.Callback(callbackContext);
            }

            return environmentVariables;
        }

        /// <summary>
        /// Verifies that a configuration-aware project references the supplied App Configuration resource and receives
        /// the expected environment value from the adds-environment parameter.
        /// </summary>
        /// <param name="project">The project resource that should have been wired by the extension.</param>
        /// <param name="configurationResource">The App Configuration resource the project should reference.</param>
        /// <param name="expectedEnvironment">The expected resolved adds-environment value.</param>
        /// <returns>A task that completes when the asynchronous environment callback assertion has finished.</returns>
        private static async Task AssertProjectReferencesConfigurationAsync(
            ProjectResource project,
            AzureAppConfigurationResource configurationResource,
            string expectedEnvironment)
        {
            // Verify the reference relationship that WithReference adds to the project resource.
            project.Annotations.OfType<ResourceRelationshipAnnotation>()
                   .Any(annotation => ReferenceEquals(annotation.Resource, configurationResource))
                   .ShouldBeTrue();

            // Verify the environment callback resolved the expected adds-environment value from the parameter.
            var environmentVariables = await GetEnvironmentVariablesAsync(project);
            var resolvedEnvironment = await ResolveEnvironmentValueAsync(environmentVariables[WellKnownConfigurationName.AddsEnvironmentName]);
            resolvedEnvironment.ShouldBe(expectedEnvironment);
        }

        /// <summary>
        /// Verifies that a configuration-aware project waits for the emulator and seeder, references the emulator,
        /// and receives the local adds-environment value required by local emulator runs.
        /// </summary>
        /// <param name="project">The configuration-aware project resource under inspection.</param>
        /// <param name="emulator">The emulator resource that should be referenced and awaited.</param>
        /// <param name="seeder">The seeder resource that should be awaited before the project starts.</param>
        /// <returns>A task that completes when the asynchronous environment callback assertion has finished.</returns>
        private static async Task AssertProjectWaitsForEmulatorAndSeederAsync(
            ProjectResource project,
            ProjectResource emulator,
            ProjectResource seeder)
        {
            // Verify the project references the emulator connection details.
            project.Annotations.OfType<ResourceRelationshipAnnotation>()
                   .Any(annotation => ReferenceEquals(annotation.Resource, emulator))
                   .ShouldBeTrue();

            // Verify the project waits for the emulator before starting.
            project.Annotations.OfType<WaitAnnotation>()
                   .Any(annotation => ReferenceEquals(annotation.Resource, emulator))
                   .ShouldBeTrue();

            // Verify the project also waits for the seeder so configuration data exists before startup.
            project.Annotations.OfType<WaitAnnotation>()
                   .Any(annotation => ReferenceEquals(annotation.Resource, seeder))
                   .ShouldBeTrue();

            // Verify the local environment variable is propagated consistently to every project.
            var environmentVariables = await GetEnvironmentVariablesAsync(project);
            var resolvedEnvironment = await ResolveEnvironmentValueAsync(environmentVariables[WellKnownConfigurationName.AddsEnvironmentName]);
            resolvedEnvironment.ShouldBe(AddsEnvironment.Local.Value);

        }

        /// <summary>
        /// Resolves environment values that may be stored either as direct strings or as Aspire value providers.
        /// </summary>
        /// <param name="environmentValue">The raw value captured from the environment callback dictionary.</param>
        /// <returns>The resolved string value represented by the supplied environment entry.</returns>
        private static async Task<string?> ResolveEnvironmentValueAsync(object environmentValue)
        {
            // Parameter-based environment propagation stores the parameter resource itself in the callback dictionary.
            if (environmentValue is ParameterResource parameterResource)
            {
                return await parameterResource.GetValueAsync(CancellationToken.None);
            }

            // Other Aspire callbacks can surface any generic value provider that resolves lazily at execution time.
            if (environmentValue is IValueProvider valueProvider)
            {
                return await valueProvider.GetValueAsync(CancellationToken.None);
            }

            // Literal values are already in their final form and can be normalized to string directly.
            return environmentValue.ToString();
        }

        /// <summary>
        /// Creates a unique temporary directory for file-backed emulator setup tests.
        /// </summary>
        /// <returns>The full path to the created temporary directory.</returns>
        private static string CreateTemporaryDirectory()
        {
            // Allocate a unique directory under the system temp path so the repository remains untouched.
            var directoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }

        /// <summary>
        /// Deletes the specified file when it exists.
        /// </summary>
        /// <param name="filePath">The file path to delete.</param>
        private static void DeleteFileIfExists(string? filePath)
        {
            // Ignore null and missing paths because cleanup is best-effort and only targets test-created files.
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Deletes the specified directory and all child content when it exists.
        /// </summary>
        /// <param name="directoryPath">The directory path to delete.</param>
        private static void DeleteDirectoryIfExists(string? directoryPath)
        {
            // Ignore null and missing paths because cleanup is best-effort and only targets test-created directories.
            if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }
}
