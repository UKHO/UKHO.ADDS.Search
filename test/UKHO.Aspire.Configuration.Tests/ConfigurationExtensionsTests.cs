using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using UKHO.Aspire.Configuration.Remote;
using Xunit;

namespace UKHO.Aspire.Configuration.Tests
{
    /// <summary>
    /// Verifies the public configuration-registration behaviour exposed by <see cref="ConfigurationExtensions"/>.
    /// </summary>
    public sealed class ConfigurationExtensionsTests
    {
        /// <summary>
        /// Verifies that the local endpoint resolver prefers the HTTPS configuration value and trims any trailing slash.
        /// </summary>
        [Fact]
        public void ResolveLocalAppConfigurationEndpoint_WhenHttpsConfigurationPresent_ShouldPreferConfigurationHttpsValue()
        {
            // Build an isolated host builder so the resolver reads only the values this test provides.
            var builder = CreateBuilder();
            builder.Configuration["services:adds-configuration:https:0"] = "https://from-config.test/";
            builder.Configuration["services:adds-configuration:http:0"] = "http://from-http-config.test/";

            // Also provide an environment override to prove configuration wins before environment variables are consulted.
            var endpoint = WithEnvironmentVariables(
                new Dictionary<string, string?>
                {
                    ["services__adds-configuration__https__0"] = "https://ignored-environment.test/"
                },
                () => InvokeResolveLocalAppConfigurationEndpoint(builder.Configuration));

            // The resolver should return the HTTPS configuration entry without the trailing slash.
            endpoint.ShouldBe("https://from-config.test");
        }

        /// <summary>
        /// Verifies that the local endpoint resolver falls back to environment variables when configuration does not contain a value.
        /// </summary>
        [Fact]
        public void ResolveLocalAppConfigurationEndpoint_WhenConfigurationMissing_ShouldFallBackToEnvironmentVariable()
        {
            // Keep the configuration empty so the fallback path is the only source of data.
            var builder = CreateBuilder();

            // Supply the HTTP endpoint through the environment variable name used by Aspire local wiring.
            var endpoint = WithEnvironmentVariables(
                new Dictionary<string, string?>
                {
                    ["services__adds-configuration__http__0"] = "http://from-environment.test/"
                },
                () => InvokeResolveLocalAppConfigurationEndpoint(builder.Configuration));

            // The fallback endpoint should still be normalised by trimming the trailing slash.
            endpoint.ShouldBe("http://from-environment.test");
        }

        /// <summary>
        /// Verifies that the local endpoint resolver fails with a helpful error when no endpoint can be found.
        /// </summary>
        [Fact]
        public void ResolveLocalAppConfigurationEndpoint_WhenNoEndpointConfigured_ShouldThrowInvalidOperationException()
        {
            // Use an empty configuration and clear any matching environment variables.
            var builder = CreateBuilder();

            // Invoking the resolver should explain which keys were checked.
            var exception = WithEnvironmentVariables(
                new Dictionary<string, string?>
                {
                    ["services__adds-configuration__https__0"] = null,
                    ["services__adds-configuration__http__0"] = null
                },
                () => Should.Throw<InvalidOperationException>(() => InvokeResolveLocalAppConfigurationEndpoint(builder.Configuration)));

            exception.Message.ShouldContain("services__adds-configuration__https__0");
            exception.Message.ShouldContain("services__adds-configuration__http__0");
        }

        /// <summary>
        /// Verifies that the local registration path fails before contacting Azure App Configuration when no local endpoint is available.
        /// </summary>
        [Fact]
        public void AddConfiguration_WhenLocalEndpointMissing_ShouldThrowInvalidOperationException()
        {
            // Configure the host to use the local path while omitting all endpoint values.
            var builder = CreateBuilder();

            // The extension should fail with the same endpoint-resolution message the local path depends on.
            var exception = WithEnvironmentVariables(
                new Dictionary<string, string?>
                {
                    [WellKnownConfigurationName.AddsEnvironmentName] = AddsEnvironment.Local.Value,
                    ["services__adds-configuration__https__0"] = null,
                    ["services__adds-configuration__http__0"] = null
                },
                () => Should.Throw<ArgumentException>(() => builder.AddConfiguration("SearchApi", "ConfigComponent")));

            exception.Message.ShouldContain("Azure App Configuration endpoint was not found");
            exception.InnerException.ShouldBeOfType<InvalidOperationException>();
        }

        /// <summary>
        /// Verifies that the non-local registration path adds Azure App Configuration with lower-case labels, refresh sentinel wiring, and the external registry singleton.
        /// </summary>
        [Fact]
        public void AddConfiguration_WhenNonLocal_ShouldRegisterSingletonAndConfigureLowercaseRefreshMetadata()
        {
            // Prepare the builder with the connection metadata consumed by the Aspire Azure App Configuration extension.
            var builder = CreateBuilder();
            builder.Configuration["ConnectionStrings:configcomponent"] = "Endpoint=https://127.0.0.1:1;Id=aac-credential;Secret=c2VjcmV0;";
            builder.Configuration["Aspire:Microsoft:Extensions:Configuration:AzureAppConfiguration:Optional"] = "true";
            builder.Configuration["Aspire:Microsoft:Extensions:Configuration:AzureAppConfiguration:DisableHealthChecks"] = "true";

            // Run the non-local path under a valid environment value so the extension registers the remote provider.
            var returnedBuilder = WithEnvironmentVariables(
                new Dictionary<string, string?>
                {
                    [WellKnownConfigurationName.AddsEnvironmentName] = AddsEnvironment.Development.Value
                },
                () => builder.AddConfiguration("SearchApi", "ConfigComponent", "managed-identity", 42));

            // The extension should be chainable and should add the registry as a singleton service.
            returnedBuilder.ShouldBeSameAs(builder);
            var registryDescriptor = builder.Services.Single(descriptor => descriptor.ServiceType == typeof(IExternalServiceRegistry));
            registryDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
            registryDescriptor.ImplementationType.ShouldNotBeNull();
            registryDescriptor.ImplementationType.FullName.ShouldBe("UKHO.Aspire.Configuration.Remote.ExternalServiceRegistry");

            // Inspect the added Azure App Configuration source so the label and sentinel wiring are protected.
            var azureSource = builder.Configuration.Sources.Single(source => source.IsAzureAppConfigurationSource());
            var azureProvider = ((IConfigurationRoot)builder.Configuration).Providers.Single(provider => provider.GetType().FullName == "Microsoft.Extensions.Configuration.AzureAppConfiguration.AzureAppConfigurationProvider");
            var selectors = GetObjectSequence(FindRequiredMemberValue(azureProvider, "Selectors"));
            var selector = selectors.Single();
            var watchers = GetObjectSequence(FindRequiredMemberValue(azureProvider, "IndividualKvWatchers"));
            var watcher = watchers.Single();

            GetRequiredMemberValue(selector, "KeyFilter").ShouldBe("*");
            GetRequiredMemberValue(selector, "LabelFilter").ShouldBe("searchapi");
            GetRequiredMemberValue(watcher, "Key").ShouldBe(WellKnownConfigurationName.ReloadSentinelKey);
            GetRequiredMemberValue(watcher, "Label").ShouldBe("searchapi");
            GetRequiredMemberValue(watcher, "RefreshAll").ShouldBe(true);
            GetRequiredMemberValue(watcher, "RefreshInterval").ShouldBe(TimeSpan.FromSeconds(42));
            FindRequiredMemberValue(azureProvider, "IsKeyVaultConfigured").ShouldBe(true);

            // Keep the direct source assertion as well so the test still proves the provider was registered through configuration sources.
            azureSource.ShouldNotBeNull();
        }

        /// <summary>
        /// Creates an isolated host builder for configuration-focused unit tests.
        /// </summary>
        /// <returns>A builder with default host wiring disabled so only explicit test data is present.</returns>
        private static IHostApplicationBuilder CreateBuilder()
        {
            // Use a lightweight fake builder because the test project references only the host abstractions package.
            return new TestHostApplicationBuilder();
        }

        /// <summary>
        /// Invokes the private local endpoint resolver through reflection so its behaviour can be tested without altering production visibility.
        /// </summary>
        /// <param name="configuration">The configuration instance that should be evaluated by the resolver.</param>
        /// <returns>The normalised local Azure App Configuration endpoint.</returns>
        private static string InvokeResolveLocalAppConfigurationEndpoint(IConfiguration configuration)
        {
            // Locate the private helper exactly once for the call and fail loudly if the production signature changes.
            var method = typeof(ConfigurationExtensions).GetMethod("ResolveLocalAppConfigurationEndpoint", BindingFlags.Static | BindingFlags.NonPublic);
            method.ShouldNotBeNull();

            try
            {
                // Invoke the real production logic so the test exercises the existing contract rather than a copy.
                return (string)method.Invoke(null, new object[] { configuration })!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                // Preserve the original exception so the tests assert the same failure shape callers would see.
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        /// <summary>
        /// Reads a required member value from an external object using reflection.
        /// </summary>
        /// <param name="instance">The object that should expose the requested member.</param>
        /// <param name="memberName">The field or property name to read.</param>
        /// <returns>The member value.</returns>
        private static object? GetRequiredMemberValue(object instance, string memberName)
        {
            // Search both properties and fields because the package types are external implementation details.
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var property = instance.GetType().GetProperty(memberName, Flags);

            if (property != null)
            {
                return property.GetValue(instance);
            }

            var field = instance.GetType().GetField(memberName, Flags);

            if (field != null)
            {
                return field.GetValue(instance);
            }

            // Fail with the concrete type name so package-shape changes are obvious during test failures.
            throw new InvalidOperationException($"Unable to locate member '{memberName}' on type '{instance.GetType().FullName}'.");
        }

        /// <summary>
        /// Finds a member anywhere within an external object's nested graph so package implementation details can still be asserted safely.
        /// </summary>
        /// <param name="instance">The root object to search.</param>
        /// <param name="memberName">The member name to find.</param>
        /// <returns>The discovered member value.</returns>
        private static object? FindRequiredMemberValue(object instance, string memberName)
        {
            // Track visited objects to avoid cycles while traversing the provider graph.
            var visited = new HashSet<int>();
            var value = FindMemberValueRecursive(instance, memberName, visited, 6);
            value.ShouldNotBeNull();
            return value;
        }

        /// <summary>
        /// Materialises an arbitrary sequence as objects so reflection-based assertions can inspect the entries.
        /// </summary>
        /// <param name="value">The sequence object that should be materialised.</param>
        /// <returns>The sequence of collection items.</returns>
        private static IReadOnlyList<object> GetObjectSequence(object? value)
        {
            // Materialise the sequence so the assertions remain stable even if the underlying collection is lazily evaluated.
            var sequence = value as System.Collections.IEnumerable;
            sequence.ShouldNotBeNull();
            return sequence.Cast<object>().ToList();
        }

        /// <summary>
        /// Walks an object's nested fields, properties, and collection items looking for a named member.
        /// </summary>
        /// <param name="instance">The current object being inspected.</param>
        /// <param name="memberName">The member name to locate.</param>
        /// <param name="visited">The set of already-visited object identities.</param>
        /// <param name="remainingDepth">The remaining recursion depth budget.</param>
        /// <returns>The matching member value when found; otherwise <see langword="null"/>.</returns>
        private static object? FindMemberValueRecursive(object? instance, string memberName, ISet<int> visited, int remainingDepth)
        {
            // Stop recursion when the graph has been exhausted or a leaf/simple value is reached.
            if (instance == null || remainingDepth < 0 || IsSimpleValue(instance.GetType()))
            {
                return null;
            }

            var identity = RuntimeHelpers.GetHashCode(instance);

            if (!visited.Add(identity))
            {
                return null;
            }

            // Check the current object first so exact matches win before deeper traversal starts.
            var directMatch = TryGetMemberValue(instance, memberName);

            if (directMatch.Found)
            {
                return directMatch.Value;
            }

            // Explore collection items because provider implementations often store the interesting values inside nested lists.
            if (instance is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    var nestedCollectionMatch = FindMemberValueRecursive(item, memberName, visited, remainingDepth - 1);

                    if (nestedCollectionMatch != null)
                    {
                        return nestedCollectionMatch;
                    }
                }
            }

            // Traverse child members so the test can tolerate source/provider implementation detail wrappers.
            foreach (var child in GetChildObjects(instance))
            {
                var nestedMatch = FindMemberValueRecursive(child, memberName, visited, remainingDepth - 1);

                if (nestedMatch != null)
                {
                    return nestedMatch;
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to read a named member directly from the supplied object.
        /// </summary>
        /// <param name="instance">The object that should expose the member.</param>
        /// <param name="memberName">The member name to read.</param>
        /// <returns>A tuple describing whether the member existed and, if so, its value.</returns>
        private static (bool Found, object? Value) TryGetMemberValue(object instance, string memberName)
        {
            // Search both properties and fields because the external package uses a mix of shapes across its types.
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var property = instance.GetType().GetProperty(memberName, Flags);

            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return (true, property.GetValue(instance));
            }

            var field = instance.GetType().GetField(memberName, Flags);

            if (field != null)
            {
                return (true, field.GetValue(instance));
            }

            return (false, null);
        }

        /// <summary>
        /// Returns the non-simple child objects exposed by an object's fields and properties.
        /// </summary>
        /// <param name="instance">The object whose children should be enumerated.</param>
        /// <returns>The child objects that are suitable for deeper recursive inspection.</returns>
        private static IEnumerable<object> GetChildObjects(object instance)
        {
            // Enumerate both properties and fields because external package graphs may use either storage strategy.
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var property in instance.GetType().GetProperties(Flags))
            {
                if (property.GetIndexParameters().Length != 0 || IsSimpleValue(property.PropertyType))
                {
                    continue;
                }

                object? value;

                try
                {
                    // Ignore property getters that throw because the tests only need inspectable members.
                    value = property.GetValue(instance);
                }
                catch
                {
                    continue;
                }

                if (value != null)
                {
                    yield return value;
                }
            }

            foreach (var field in instance.GetType().GetFields(Flags))
            {
                if (IsSimpleValue(field.FieldType))
                {
                    continue;
                }

                var value = field.GetValue(instance);

                if (value != null)
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Determines whether a type is simple enough that recursive graph traversal would add no value.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns><see langword="true"/> when the type should be treated as a leaf value; otherwise <see langword="false"/>.</returns>
        private static bool IsSimpleValue(Type type)
        {
            // Treat primitives, common framework value types, delegates, and strings as traversal leaves.
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid)
                || type == typeof(Uri)
                || typeof(Delegate).IsAssignableFrom(type);
        }

        /// <summary>
        /// Temporarily overrides a set of environment variables for the duration of a callback.
        /// </summary>
        /// <typeparam name="TResult">The result type returned by the callback.</typeparam>
        /// <param name="variables">The variables to set or clear while the callback is running.</param>
        /// <param name="action">The callback that should execute under the temporary environment.</param>
        /// <returns>The value returned by <paramref name="action"/>.</returns>
        private static TResult WithEnvironmentVariables<TResult>(IReadOnlyDictionary<string, string?> variables, Func<TResult> action)
        {
            // Capture the original values up front so the environment can be restored even if the callback throws.
            var originalValues = variables.ToDictionary(pair => pair.Key, pair => Environment.GetEnvironmentVariable(pair.Key));

            foreach (var pair in variables)
            {
                // Apply each override before entering the callback so all configuration sources observe a consistent state.
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }

            try
            {
                // Execute the caller's logic while the temporary process environment is active.
                return action();
            }
            finally
            {
                foreach (var pair in originalValues)
                {
                    // Restore the prior state to keep the wider test process deterministic.
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                }
            }
        }
    }
}
