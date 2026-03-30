using System.Reflection;
using Microsoft.Extensions.Logging;
using UKHO.Aspire.Configuration;
using UKHO.Aspire.Configuration.Seeder.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests
{
    /// <summary>
    /// Provides best-effort coverage for the accessible startup decisions and validation helpers in the seeder entry point.
    /// </summary>
    public sealed class ProgramTests
    {
        /// <summary>
        /// Verifies that the entry point selects command-line mode when Aspire configuration variables are absent and returns failure for invalid arguments.
        /// </summary>
        [Fact]
        public async Task Main_WhenCommandLineArgumentsInvalid_ShouldReturnMinusOne()
        {
            // Clear the Aspire environment markers so the program follows the command-line parsing branch.
            using var configurationPathScope = new EnvironmentVariableScope(WellKnownConfigurationName.ConfigurationFilePath, null);
            using var environmentScope = new EnvironmentVariableScope(WellKnownConfigurationName.AddsEnvironmentName, null);

            // Invoke the private async entry point with no arguments, which should fail parser validation and return -1.
            var exitCode = await InvokeMainAsync([]);

            // The program should report failure for invalid command-line usage.
            Assert.Equal(-1, exitCode);
        }

        /// <summary>
        /// Verifies that the private file-path validator rejects missing files with a clear exception type.
        /// </summary>
        [Fact]
        public void ValidateFilePath_WhenFileMissing_ShouldThrowFileNotFoundException()
        {
            // Use a guaranteed missing path so the validator reaches its failure branch deterministically.
            var logger = new TestLogger<UKHO.Aspire.Configuration.Seeder.Program>();
            var missingPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            // Invoke the private helper through reflection and capture the validation failure.
            var exception = Assert.Throws<FileNotFoundException>(() => InvokePrivateStaticMethod(
                nameof(ValidateFilePath_WhenFileMissing_ShouldThrowFileNotFoundException),
                "ValidateFilePath",
                logger,
                missingPath,
                "ConfigurationFilePath"));

            // The failure should identify the missing file path for easier diagnostics.
            Assert.Contains(missingPath, exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that the private URI validator rejects malformed absolute URIs.
        /// </summary>
        [Fact]
        public void ValidateUri_WhenUriInvalid_ShouldThrowArgumentException()
        {
            // Supply an invalid URI string so the validator reaches the argument guard.
            var logger = new TestLogger<UKHO.Aspire.Configuration.Seeder.Program>();

            // Invoke the private helper through reflection and capture the validation failure.
            var exception = Assert.Throws<ArgumentException>(() => InvokePrivateStaticMethod(
                nameof(ValidateUri_WhenUriInvalid_ShouldThrowArgumentException),
                "ValidateUri",
                logger,
                "not a uri",
                "AppConfigServiceUrl"));

            // The failure should identify the bad URI value.
            Assert.Contains("not a uri", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that App Configuration endpoint resolution prefers HTTPS over HTTP and trims a trailing slash.
        /// </summary>
        [Fact]
        public void ResolveAppConfigurationEndpoint_WhenBothEndpointsPresent_ShouldPreferHttpsAndTrimTrailingSlash()
        {
            // Populate both candidate environment variables so precedence can be asserted without bootstrapping the full host.
            using var httpsScope = new EnvironmentVariableScope($"services__{WellKnownConfigurationName.ConfigurationServiceName}__https__0", "https://preferred.example.test/");
            using var httpScope = new EnvironmentVariableScope($"services__{WellKnownConfigurationName.ConfigurationServiceName}__http__0", "http://fallback.example.test/");
            var logger = new TestLogger<UKHO.Aspire.Configuration.Seeder.Program>();

            // Invoke the private helper that scans the environment-variable precedence list.
            var endpoint = (string)InvokePrivateStaticMethod(
                nameof(ResolveAppConfigurationEndpoint_WhenBothEndpointsPresent_ShouldPreferHttpsAndTrimTrailingSlash),
                "ResolveAppConfigurationEndpoint",
                logger)!;

            // HTTPS should win over HTTP and any trailing slash should be removed before the connection string is built.
            Assert.Equal("https://preferred.example.test", endpoint);
        }

        /// <summary>
        /// Verifies that the program does nothing when launched by Aspire in a non-local environment.
        /// </summary>
        [Fact]
        public async Task Main_WhenRunningUnderAspireInNonLocalEnvironment_ShouldReturnZero()
        {
            // Set only the environment markers required to reach the non-local short-circuit branch.
            using var configurationPathScope = new EnvironmentVariableScope(WellKnownConfigurationName.ConfigurationFilePath, "present");
            using var environmentScope = new EnvironmentVariableScope(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Development.Value);

            // Invoke the private async entry point. A non-local Aspire execution should no-op and return success.
            var exitCode = await InvokeMainAsync([]);

            // The entry point should complete successfully without starting the local hosted service path.
            Assert.Equal(0, exitCode);
        }

        /// <summary>
        /// Invokes the private async program entry point and awaits its integer exit code.
        /// </summary>
        /// <param name="args">The command-line arguments to supply to the entry point.</param>
        /// <returns>The exit code produced by the private program entry point.</returns>
        private static async Task<int> InvokeMainAsync(string[] args)
        {
            // Locate the compiler-generated private Main method so the test can exercise the real entry-point decisions.
            var mainMethod = typeof(UKHO.Aspire.Configuration.Seeder.Program).GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(mainMethod);

            // Invoke the method, cast the result to the expected task, and await the final exit code.
            var task = (Task<int>)mainMethod!.Invoke(null, [args])!;
            return await task;
        }

        /// <summary>
        /// Invokes one private static helper on <see cref="UKHO.Aspire.Configuration.Seeder.Program"/> and unwraps reflection failures.
        /// </summary>
        /// <param name="assertionContext">The calling test name, captured only to make assertion failures easier to diagnose.</param>
        /// <param name="methodName">The private static method name to invoke.</param>
        /// <param name="parameters">The parameters to pass to the reflected method.</param>
        /// <returns>The reflected method result, if any.</returns>
        private static object? InvokePrivateStaticMethod(string assertionContext, string methodName, params object?[] parameters)
        {
            // Locate the target helper by name so best-effort tests can cover directly accessible logic without changing production visibility.
            var method = typeof(UKHO.Aspire.Configuration.Seeder.Program).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.True(method is not null, $"Expected private method '{methodName}' to exist for {assertionContext}.");

            try
            {
                // Execute the reflected helper and return its result to the calling test.
                return method!.Invoke(null, parameters);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                // Re-throw the real inner failure so the tests assert the production exception rather than reflection plumbing.
                throw exception.InnerException;
            }
        }
    }
}
