using UKHO.Aspire.Configuration;
using UKHO.Aspire.Configuration.Seeder.Json;
using UKHO.Aspire.Configuration.Seeder.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests.Json
{
    /// <summary>
    /// Verifies that <see cref="ExternalServiceDefinitionParser"/> validates discovery JSON and resolves local placeholders correctly.
    /// </summary>
    public sealed class ExternalServiceDefinitionParserTests
    {
        /// <summary>
        /// Verifies that the requested environment section must exist in the discovery JSON.
        /// </summary>
        [Fact]
        public async Task ParseAndResolveAsync_WhenEnvironmentSectionMissing_ShouldThrowInvalidDataException()
        {
            // Provide only a development section so the local lookup fails immediately.
            var json = """
            {
              "dev":
              {
                "catalog":
                {
                  "clientId": "client-id",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.example.test"
                    }
                  ]
                }
              }
            }
            """;

            // Execute the parser for the missing environment section.
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            // The error should identify the missing environment section.
            Assert.Contains("local", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that each service must declare a client identifier.
        /// </summary>
        [Fact]
        public async Task ParseAndResolveAsync_WhenClientIdMissing_ShouldThrowInvalidDataException()
        {
            // Omit the clientId field while leaving the rest of the service shape valid.
            var json = """
            {
              "local":
              {
                "catalog":
                {
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.example.test"
                    }
                  ]
                }
              }
            }
            """;

            // Parse the invalid payload.
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            // The message should explain that clientId is required.
            Assert.Contains("clientId", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that services with a missing or empty endpoint list are rejected.
        /// </summary>
        /// <param name="endpointsJson">The JSON fragment used for the endpoints property.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("[]")]
        public async Task ParseAndResolveAsync_WhenEndpointsMissingOrEmpty_ShouldThrowInvalidDataException(string? endpointsJson)
        {
            // Compose the service JSON so both missing and empty endpoints scenarios can be exercised by the same theory.
            var endpointsProperty = endpointsJson is null
                ? string.Empty
                : $"\"endpoints\": {endpointsJson},";

            var json = $$"""
            {
              "local":
              {
                "catalog":
                {
                  {{endpointsProperty}}
                  "clientId": "client-id"
                }
              }
            }
            """;

            // Parse the invalid payload.
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            // The error should explain that at least one endpoint is required.
            Assert.Contains("at least one endpoint", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that each service must provide an endpoint with an empty tag to act as the default.
        /// </summary>
        [Fact]
        public async Task ParseAndResolveAsync_WhenDefaultTagMissing_ShouldThrowInvalidDataException()
        {
            // Provide only explicitly tagged endpoints so the parser reaches the default-tag validation branch.
            var json = """
            {
              "local":
              {
                "catalog":
                {
                  "clientId": "client-id",
                  "endpoints": [
                    {
                      "tag": "blue",
                      "url": "https://catalog.example.test"
                    }
                  ]
                }
              }
            }
            """;

            // Parse the invalid payload.
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            // The message should call out the missing default-tag endpoint.
            Assert.Contains("tag=\"\"", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that endpoint URLs must include an HTTP or HTTPS scheme.
        /// </summary>
        [Fact]
        public async Task ParseAndResolveAsync_WhenSchemeInvalid_ShouldThrowInvalidDataException()
        {
            // Use a scheme-less URL so the parser reaches the scheme validation path.
            var json = """
            {
              "local":
              {
                "catalog":
                {
                  "clientId": "client-id",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "catalog.example.test"
                    }
                  ]
                }
              }
            }
            """;

            // Parse the invalid payload.
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            // The error should explain that the scheme is invalid or missing.
            Assert.Contains("scheme", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that non-local environments preserve the original endpoint template without trying to resolve placeholders.
        /// </summary>
        [Fact]
        public async Task ParseAndResolveAsync_WhenEnvironmentNotLocal_ShouldLeaveEndpointUnchanged()
        {
            // Arrange a payload that contains a literal remote URL and a second explicitly tagged endpoint.
            var json = """
            {
              "dev":
              {
                "catalog":
                {
                  "clientId": "client-id",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.dev.example.test"
                    },
                    {
                      "tag": "blue",
                      "url": "http://catalog-blue.dev.example.test"
                    }
                  ]
                }
              }
            }
            """;

            // Parse the non-local discovery JSON.
            var definitions = await ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Development, json);

            // The service definition should preserve its metadata exactly as declared.
            var definition = Assert.Single(definitions);
            Assert.Equal("catalog", definition.Service);
            Assert.Equal("client-id", definition.ClientId);
            Assert.Equal(2, definition.Endpoints.Count);

            var defaultEndpoint = Assert.Single(definition.Endpoints, endpoint => endpoint.Tag == string.Empty);
            Assert.Equal("https", defaultEndpoint.Scheme);
            Assert.Equal("https://catalog.dev.example.test", defaultEndpoint.OriginalTemplate);
            Assert.Equal("https://catalog.dev.example.test", defaultEndpoint.ResolvedUrl);
            Assert.Null(defaultEndpoint.Placeholder);

            var blueEndpoint = Assert.Single(definition.Endpoints, endpoint => endpoint.Tag == "blue");
            Assert.Equal("http", blueEndpoint.Scheme);
            Assert.Equal("http://catalog-blue.dev.example.test", blueEndpoint.ResolvedUrl);
        }

        /// <summary>
        /// Verifies that local placeholder templates resolve through the expected environment-variable convention.
        /// </summary>
        [Fact]
        public async Task ParseAndResolveAsync_WhenLocalPlaceholderPresent_ShouldResolveFromEnvironmentVariables()
        {
            // Set the environment variable that the parser expects for a local HTTPS catalog endpoint.
            using var scope = new EnvironmentVariableScope("services__catalog__https__0", "https://resolved.example.test:8443/");

            var json = """
            {
              "local":
              {
                "catalog":
                {
                  "clientId": "client-id",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://{{catalog}}/api"
                    }
                  ]
                }
              }
            }
            """;

            // Parse and resolve the local discovery JSON.
            var definitions = await ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json);

            // The resolved endpoint should substitute only the host and port from the discovered service URL.
            var definition = Assert.Single(definitions);
            var endpoint = Assert.Single(definition.Endpoints);
            Assert.Equal("catalog", definition.Service);
            Assert.Equal("client-id", definition.ClientId);
            Assert.Equal("https", endpoint.Scheme);
            Assert.Equal("catalog", endpoint.Placeholder);
            Assert.Equal("https://{{catalog}}/api", endpoint.OriginalTemplate);
            Assert.Equal("https://resolved.example.test:8443/api", endpoint.ResolvedUrl);
        }

        /// <summary>
        /// Verifies that local templates with more than one placeholder are rejected.
        /// </summary>
        [Fact]
        public async Task ParseAndResolveAsync_WhenLocalTemplateContainsMultiplePlaceholders_ShouldThrowInvalidDataException()
        {
            // Provide two placeholders because the parser only supports a single local service substitution.
            var json = """
            {
              "local":
              {
                "catalog":
                {
                  "clientId": "client-id",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://{{catalog}}/{{other}}"
                    }
                  ]
                }
              }
            }
            """;

            // Parse the invalid local payload.
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            // The error should explain that multiple placeholders are unsupported.
            Assert.Contains("more than one placeholder", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that local placeholder resolution fails clearly when the required environment variable is absent.
        /// </summary>
        [Fact]
        public async Task ParseAndResolveAsync_WhenLocalEnvironmentVariableMissing_ShouldThrowInvalidOperationException()
        {
            // Clear the environment variable that would normally provide the endpoint resolution.
            using var scope = new EnvironmentVariableScope("services__catalog__https__0", null);

            var json = """
            {
              "local":
              {
                "catalog":
                {
                  "clientId": "client-id",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://{{catalog}}/api"
                    }
                  ]
                }
              }
            }
            """;

            // Parse the payload and expect endpoint lookup to fail.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ExternalServiceDefinitionParser.ParseAndResolveAsync(AddsEnvironment.Local, json));

            // The failure message should point to the missing service and scheme combination.
            Assert.Contains("catalog", exception.Message, StringComparison.Ordinal);
            Assert.Contains("https", exception.Message, StringComparison.Ordinal);
        }
    }
}
