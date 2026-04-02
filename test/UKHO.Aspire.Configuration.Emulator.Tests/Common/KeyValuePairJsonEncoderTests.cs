using System.Text.Json;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Common
{
    /// <summary>
    /// Verifies that <see cref="KeyValuePairJsonEncoder"/> reconstructs hierarchical JSON documents from flattened key-value pairs.
    /// </summary>
    public sealed class KeyValuePairJsonEncoderTests
    {
        /// <summary>
        /// Verifies that prefixed keys and array indexes are translated back into the expected JSON hierarchy.
        /// </summary>
        [Fact]
        public void Encode_WhenPairsContainPrefixAndArrayIndexes_ShouldReconstructHierarchy()
        {
            // Arrange flattened settings that model the structure commonly read from Azure App Configuration.
            var pairs = new[]
            {
                new KeyValuePair<string, string?>("app:catalog:enabled", "true"),
                new KeyValuePair<string, string?>("app:catalog:aliases:0", "search"),
                new KeyValuePair<string, string?>("app:catalog:aliases:1", "discovery")
            };
            var encoder = new KeyValuePairJsonEncoder();

            // Act by rebuilding the JSON hierarchy while stripping the known prefix.
            using var document = encoder.Encode(pairs, prefix: "app", separator: ":");

            // Assert that the hierarchy and array order match the flattened source pairs.
            var catalog = document.RootElement.GetProperty("catalog");
            Assert.Equal("true", catalog.GetProperty("enabled").GetString());
            Assert.Equal("search", catalog.GetProperty("aliases")[0].GetString());
            Assert.Equal("discovery", catalog.GetProperty("aliases")[1].GetString());
        }

        /// <summary>
        /// Verifies that a string-only payload can be decoded and re-encoded without changing the reconstructed hierarchy.
        /// </summary>
        [Fact]
        public void Encode_WhenPairsProducedByDecoder_ShouldRoundTripStringLeafHierarchy()
        {
            // Arrange a string-only JSON payload so decoding and re-encoding should preserve both paths and values.
            using var sourceDocument = JsonDocument.Parse("""
            {
              "catalog": {
                "endpoint": "https://catalog.example.test",
                "aliases": ["search", "discovery"]
              }
            }
            """);
            var decoder = new KeyValuePairJsonDecoder();
            var encoder = new KeyValuePairJsonEncoder();

            // Act by flattening the payload and reconstructing it again.
            var pairs = decoder.Decode(sourceDocument, separator: ":").ToArray();
            using var roundTrippedDocument = encoder.Encode(pairs, separator: ":");

            // Assert that the reconstructed hierarchy still contains the original string values.
            var catalog = roundTrippedDocument.RootElement.GetProperty("catalog");
            Assert.Equal("https://catalog.example.test", catalog.GetProperty("endpoint").GetString());
            Assert.Equal("search", catalog.GetProperty("aliases")[0].GetString());
            Assert.Equal("discovery", catalog.GetProperty("aliases")[1].GetString());
        }
    }
}
