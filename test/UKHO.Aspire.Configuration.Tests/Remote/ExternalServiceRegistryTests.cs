using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Shouldly;
using UKHO.Aspire.Configuration.Remote;
using Xunit;

namespace UKHO.Aspire.Configuration.Tests.Remote
{
    /// <summary>
    /// Verifies the endpoint-resolution behaviour implemented by the internal external service registry.
    /// </summary>
    public sealed class ExternalServiceRegistryTests
    {
        /// <summary>
        /// Verifies that requesting a missing service definition produces a clear not-found error.
        /// </summary>
        [Fact]
        public void GetServiceEndpoint_WhenServiceDefinitionMissing_ShouldThrowKeyNotFoundException()
        {
            // Create a registry with no external-service entries so the lookup cannot succeed.
            var registry = CreateRegistry(new Dictionary<string, string?>());

            // The registry should report that the service definition is absent.
            var exception = Should.Throw<KeyNotFoundException>(() => registry.GetServiceEndpoint("catalogue"));
            exception.Message.ShouldContain("catalogue");
        }

        /// <summary>
        /// Verifies that the default empty tag selects the endpoint explicitly marked as the default entry.
        /// </summary>
        [Fact]
        public void GetServiceEndpoint_WhenTagOmitted_ShouldReturnDefaultTaggedEndpoint()
        {
            // Provide both a default endpoint and a named endpoint so the empty-tag lookup has a clear target.
            var registry = CreateRegistry(
                new Dictionary<string, string?>
                {
                    ["externalservice:catalogue"] = BuildServiceDefinitionJson(
                        "catalogue-client",
                        (string.Empty, "https://catalogue-default.test/api"),
                        ("internal", "https://catalogue-internal.test/api"))
                });

            // Request the default endpoint by omitting the tag argument.
            var endpoint = registry.GetServiceEndpoint("catalogue");

            // The registry should materialise the default endpoint details without altering the requested tag.
            endpoint.Tag.ShouldBe(string.Empty);
            endpoint.Host.ShouldBe(EndpointHostSubstitution.None);
            endpoint.Uri.ShouldBe(new Uri("https://catalogue-default.test/api"));
        }

        /// <summary>
        /// Verifies that a specific tag selects the matching endpoint definition.
        /// </summary>
        [Fact]
        public void GetServiceEndpoint_WhenSpecificTagRequested_ShouldReturnMatchingEndpoint()
        {
            // Provide a named endpoint alongside the default one so the lookup must honour the requested tag.
            var registry = CreateRegistry(
                new Dictionary<string, string?>
                {
                    ["externalservice:catalogue"] = BuildServiceDefinitionJson(
                        "catalogue-client",
                        (string.Empty, "https://catalogue-default.test/api"),
                        ("internal", "https://catalogue-internal.test/api"))
                });

            // Request the tagged endpoint explicitly.
            var endpoint = registry.GetServiceEndpoint("catalogue", "internal");

            // The resolved endpoint should match the tagged entry rather than the default entry.
            endpoint.Tag.ShouldBe("internal");
            endpoint.Uri.ShouldBe(new Uri("https://catalogue-internal.test/api"));
        }

        /// <summary>
        /// Verifies that requesting an unknown tag produces a clear not-found error.
        /// </summary>
        [Fact]
        public void GetServiceEndpoint_WhenRequestedTagMissing_ShouldThrowKeyNotFoundException()
        {
            // Provide only the default endpoint so a named-tag request cannot be fulfilled.
            var registry = CreateRegistry(
                new Dictionary<string, string?>
                {
                    ["externalservice:catalogue"] = BuildServiceDefinitionJson("catalogue-client", (string.Empty, "https://catalogue-default.test/api"))
                });

            // The registry should explain which tag lookup failed.
            var exception = Should.Throw<KeyNotFoundException>(() => registry.GetServiceEndpoint("catalogue", "public"));
            exception.Message.ShouldContain("public");
        }

        /// <summary>
        /// Verifies that Docker substitution rewrites the URI host to the Docker host bridge while preserving the remainder of the URI.
        /// </summary>
        [Fact]
        public void GetServiceEndpoint_WhenDockerSubstitutionRequested_ShouldReplaceUriHost()
        {
            // Use a localhost-style endpoint so the substitution behaviour is easy to observe.
            var registry = CreateRegistry(
                new Dictionary<string, string?>
                {
                    ["externalservice:catalogue"] = BuildServiceDefinitionJson("catalogue-client", (string.Empty, "https://localhost:8443/api"))
                });

            // Request the endpoint using Docker host substitution.
            var endpoint = registry.GetServiceEndpoint("catalogue", string.Empty, EndpointHostSubstitution.Docker);

            // Only the host should change; the scheme, port, and path should remain intact.
            endpoint.Host.ShouldBe(EndpointHostSubstitution.Docker);
            endpoint.Uri.ShouldBe(new Uri("https://host.docker.internal:8443/api"));
        }

        /// <summary>
        /// Verifies that unsupported host substitution values are rejected rather than silently ignored.
        /// </summary>
        [Fact]
        public void GetServiceEndpoint_WhenHostSubstitutionUnsupported_ShouldThrowArgumentOutOfRangeException()
        {
            // Provide a valid service definition so the host-substitution switch is the only failing branch.
            var registry = CreateRegistry(
                new Dictionary<string, string?>
                {
                    ["externalservice:catalogue"] = BuildServiceDefinitionJson("catalogue-client", (string.Empty, "https://catalogue-default.test/api"))
                });

            // Cast an out-of-range value to prove the switch default path remains protected.
            var exception = Should.Throw<ArgumentOutOfRangeException>(() => registry.GetServiceEndpoint("catalogue", string.Empty, (EndpointHostSubstitution)999));
            exception.ParamName.ShouldBe("host");
        }

        /// <summary>
        /// Creates a registry instance backed by in-memory configuration entries.
        /// </summary>
        /// <param name="values">The configuration values that should back the registry lookup.</param>
        /// <returns>An <see cref="IExternalServiceRegistry"/> that exercises the production implementation.</returns>
        private static IExternalServiceRegistry CreateRegistry(IReadOnlyDictionary<string, string?> values)
        {
            // Build a deterministic configuration root so each test controls the full lookup surface.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
            var registryType = typeof(IExternalServiceRegistry).Assembly.GetType("UKHO.Aspire.Configuration.Remote.ExternalServiceRegistry");

            registryType.ShouldNotBeNull();

            // Construct the internal registry through reflection to avoid changing production visibility just for tests.
            return (IExternalServiceRegistry)Activator.CreateInstance(registryType!, configuration)!;
        }

        /// <summary>
        /// Builds a minimal service-definition payload that matches the registry's expected configuration shape.
        /// </summary>
        /// <param name="clientId">The client identifier that should be carried through to the resolved endpoint.</param>
        /// <param name="endpoints">The endpoint tuples to serialise into the configuration payload.</param>
        /// <returns>A JSON string representing an external service definition.</returns>
        private static string BuildServiceDefinitionJson(string clientId, params (string Tag, string Url)[] endpoints)
        {
            // Serialise the smallest payload shape required by the production JSON codec.
            var payload = new
            {
                Service = "catalogue",
                ClientId = clientId,
                Endpoints = endpoints.Select(endpoint => new
                {
                    Service = "catalogue",
                    endpoint.Tag,
                    Scheme = "https",
                    OriginalTemplate = endpoint.Url,
                    ResolvedUrl = endpoint.Url
                })
            };

            return JsonSerializer.Serialize(payload);
        }
    }
}
