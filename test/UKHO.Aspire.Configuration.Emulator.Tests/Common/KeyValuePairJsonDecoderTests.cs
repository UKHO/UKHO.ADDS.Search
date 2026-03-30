using System.Text.Json;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Common
{
    /// <summary>
    /// Verifies that <see cref="KeyValuePairJsonDecoder"/> flattens nested JSON content into Azure App Configuration style key-value pairs.
    /// </summary>
    public sealed class KeyValuePairJsonDecoderTests
    {
        /// <summary>
        /// Verifies that nested objects, arrays, and primitive values are flattened with the expected separator and string conversions.
        /// </summary>
        [Fact]
        public void Decode_WhenJsonContainsNestedObjectsAndArrays_ShouldFlattenValuesWithPrefix()
        {
            // Arrange a mixed JSON payload so object nesting, arrays, booleans, numbers, strings, and nulls are all exercised.
            using var document = JsonDocument.Parse("""
            {
              "catalog": {
                "enabled": true,
                "threshold": 2.5,
                "aliases": ["search", "discovery"],
                "description": null
              }
            }
            """);
            var decoder = new KeyValuePairJsonDecoder();

            // Act by flattening the payload with an application-specific prefix.
            var result = decoder.Decode(document, prefix: "app", separator: ":").ToArray();

            // Assert that each primitive leaf is emitted as a separate key-value pair using the expected path segments.
            Assert.Equal(5, result.Length);
            Assert.Contains(result, pair => pair.Key == "app:catalog:enabled" && pair.Value == bool.TrueString);
            Assert.Contains(result, pair => pair.Key == "app:catalog:threshold" && pair.Value == "2.5");
            Assert.Contains(result, pair => pair.Key == "app:catalog:aliases:0" && pair.Value == "search");
            Assert.Contains(result, pair => pair.Key == "app:catalog:aliases:1" && pair.Value == "discovery");
            Assert.Contains(result, pair => pair.Key == "app:catalog:description" && pair.Value is null);
        }
    }
}
