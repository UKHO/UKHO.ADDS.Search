using System.Net.Mime;
using UKHO.Aspire.Configuration;
using UKHO.Aspire.Configuration.Seeder.Json;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests.Json
{
    /// <summary>
    /// Verifies that <see cref="JsonFlattener"/> transforms environment-scoped JSON into the key/value shape expected by Azure App Configuration.
    /// </summary>
    public sealed class JsonFlattenerTests
    {
        /// <summary>
        /// Verifies that the requested environment section must exist in the JSON payload.
        /// </summary>
        [Fact]
        public void Flatten_WhenEnvironmentSectionMissing_ShouldThrowArgumentException()
        {
            // Provide JSON that contains a different environment section so the lookup fails deterministically.
            var json = """
            {
              "dev":
              {
                "name": "value"
              }
            }
            """;

            // Execute the flattening request for a missing environment.
            var exception = Assert.Throws<ArgumentException>(() => JsonFlattener.Flatten(AddsEnvironment.Local, json, "label"));

            // The message should identify the missing environment token.
            Assert.Contains("local", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that nested objects, arrays, labels, and primitive values are flattened correctly.
        /// </summary>
        [Fact]
        public void Flatten_WhenEnvironmentSectionPresent_ShouldFlattenObjectsArraysAndPrimitiveValues()
        {
            // Arrange a representative payload containing the primitive and composite shapes used by the seeder.
            var json = """
            {
              "local":
              {
                "service":
                {
                  "name": "search",
                  "enabled": true,
                  "retries": 3,
                  "optional": null,
                  "endpoints": [
                    "https://one.test",
                    "https://two.test"
                  ],
                  "secret": "{\"uri\":\"https://sample.vault.azure.net/secrets/example\"}"
                }
              }
            }
            """;

            // Flatten the payload using a non-empty label so both key generation and label propagation are exercised.
            var settings = JsonFlattener.Flatten(AddsEnvironment.Local, json, "seeder-label");

            // Nested object properties should become colon-delimited keys that preserve the original hierarchy.
            var nameSetting = Assert.Contains("service:name", settings);
            Assert.Equal("search", nameSetting.Value);
            Assert.Equal("seeder-label", nameSetting.Label);
            Assert.Equal(MediaTypeNames.Text.Plain, nameSetting.ContentType);

            // Boolean, numeric, and null values should be converted to their string equivalents.
            Assert.Equal("True", Assert.Contains("service:enabled", settings).Value);
            Assert.Equal("3", Assert.Contains("service:retries", settings).Value);
            Assert.Equal("null", Assert.Contains("service:optional", settings).Value);

            // Arrays should use zero-based indexes appended to the flattened key.
            Assert.Equal("https://one.test", Assert.Contains("service:endpoints:0", settings).Value);
            Assert.Equal("https://two.test", Assert.Contains("service:endpoints:1", settings).Value);

            // Key Vault references should receive the special content type while preserving the original JSON string value.
            var secretSetting = Assert.Contains("service:secret", settings);
            Assert.Equal("{\"uri\":\"https://sample.vault.azure.net/secrets/example\"}", secretSetting.Value);
            Assert.Equal("application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8", secretSetting.ContentType);
        }

        /// <summary>
        /// Verifies that values from non-target environments are ignored in favour of the requested environment section.
        /// </summary>
        [Fact]
        public void Flatten_WhenMultipleEnvironmentSectionsPresent_ShouldUseRequestedSectionOnly()
        {
            // Arrange separate local and development values so the caller can prove the correct section was selected.
            var json = """
            {
              "local":
              {
                "name": "local-value"
              },
              "dev":
              {
                "name": "dev-value"
              }
            }
            """;

            // Flatten the development section specifically.
            var settings = JsonFlattener.Flatten(AddsEnvironment.Development, json, "dev-label");

            // The output should contain only the requested environment's data.
            var setting = Assert.Single(settings);
            Assert.Equal("name", setting.Key);
            Assert.Equal("dev-value", setting.Value.Value);
            Assert.Equal("dev-label", setting.Value.Label);
        }
    }
}
