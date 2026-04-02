using System.Text.Json;
using UKHO.Aspire.Configuration.Seeder.Json;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests.Json
{
    /// <summary>
    /// Verifies that <see cref="JsonStripper"/> removes JSON comments without corrupting legitimate string content.
    /// </summary>
    public sealed class JsonStripperTests
    {
        /// <summary>
        /// Verifies that a null input is rejected immediately.
        /// </summary>
        [Fact]
        public void StripJsonComments_WhenJsonMissing_ShouldThrowArgumentNullException()
        {
            // Invoke the stripper with a null value to protect the public guard clause.
            var exception = Assert.Throws<ArgumentNullException>(() => JsonStripper.StripJsonComments(null!));

            // The exception should identify the missing json parameter.
            Assert.Equal("json", exception.ParamName);
        }

        /// <summary>
        /// Verifies that single-line comments are removed while the remaining JSON stays parseable.
        /// </summary>
        [Fact]
        public void StripJsonComments_WhenSingleLineCommentsPresent_ShouldRemoveThem()
        {
            // Provide JSON containing line comments before and after properties.
            var json = """
            {
              // root comment
              "name": "value", // trailing comment
              "enabled": true
            }
            """;

            // Strip the comments and parse the result to confirm the remaining payload is still valid JSON.
            var stripped = JsonStripper.StripJsonComments(json);
            using var document = JsonDocument.Parse(stripped);

            // The comments should be gone while the original property values remain intact.
            Assert.DoesNotContain("root comment", stripped, StringComparison.Ordinal);
            Assert.DoesNotContain("trailing comment", stripped, StringComparison.Ordinal);
            Assert.Equal("value", document.RootElement.GetProperty("name").GetString());
            Assert.True(document.RootElement.GetProperty("enabled").GetBoolean());
        }

        /// <summary>
        /// Verifies that block comments are removed without disturbing adjacent JSON content.
        /// </summary>
        [Fact]
        public void StripJsonComments_WhenBlockCommentsPresent_ShouldRemoveThem()
        {
            // Provide JSON containing a multi-line block comment between properties.
            var json = """
            {
              "name": "value",
              /*
                 block comment
              */
              "count": 2
            }
            """;

            // Strip the comments and validate the remaining document shape.
            var stripped = JsonStripper.StripJsonComments(json);
            using var document = JsonDocument.Parse(stripped);

            // The block comment should be absent while the surrounding properties remain readable.
            Assert.DoesNotContain("block comment", stripped, StringComparison.Ordinal);
            Assert.Equal(2, document.RootElement.GetProperty("count").GetInt32());
        }

        /// <summary>
        /// Verifies that comment-like sequences inside string literals are preserved.
        /// </summary>
        [Fact]
        public void StripJsonComments_WhenCommentLikeTextInsideStrings_ShouldPreserveStringContent()
        {
            // Supply values that contain // and /* */ sequences that should remain because they are inside quoted strings.
            var json = """
            {
              "url": "https://example.test/api//resource",
              "pattern": "/* not a comment */"
            }
            """;

            // Strip comments and parse the output so the assertions can focus on semantic preservation.
            var stripped = JsonStripper.StripJsonComments(json);
            using var document = JsonDocument.Parse(stripped);

            // Both string values should survive unchanged because the stripper tracks string literal state.
            Assert.Equal("https://example.test/api//resource", document.RootElement.GetProperty("url").GetString());
            Assert.Equal("/* not a comment */", document.RootElement.GetProperty("pattern").GetString());
        }

        /// <summary>
        /// Verifies that escaped content inside strings survives comment removal.
        /// </summary>
        [Fact]
        public void StripJsonComments_WhenStringsContainEscapes_ShouldPreserveEscapedText()
        {
            // Use escaped quotes and slashes inside a string so the state machine must skip over escape sequences correctly.
            var json = """
            {
              "text": "quoted \" // not comment",
              "path": "c:\\temp\\value"
            }
            """;

            // Strip comments and parse the resulting JSON document.
            var stripped = JsonStripper.StripJsonComments(json);
            using var document = JsonDocument.Parse(stripped);

            // The escaped string content should remain exactly as authored.
            Assert.Equal("quoted \" // not comment", document.RootElement.GetProperty("text").GetString());
            Assert.Equal("c:\\temp\\value", document.RootElement.GetProperty("path").GetString());
        }

        /// <summary>
        /// Verifies that mixed commented and uncommented content is normalised into valid JSON.
        /// </summary>
        [Fact]
        public void StripJsonComments_WhenContentMixed_ShouldRetainUncommentedValues()
        {
            // Combine line comments, block comments, arrays, and ordinary properties in one payload.
            var json = """
            {
              "items": [
                1,
                2 // keep second value
              ],
              /* remove this section marker */
              "enabled": false,
              "message": "done"
            }
            """;

            // Strip comments and parse the result so the test proves the overall document remains coherent.
            var stripped = JsonStripper.StripJsonComments(json);
            using var document = JsonDocument.Parse(stripped);

            // The uncommented data should remain available exactly where the caller expects it.
            Assert.Equal(2, document.RootElement.GetProperty("items").GetArrayLength());
            Assert.False(document.RootElement.GetProperty("enabled").GetBoolean());
            Assert.Equal("done", document.RootElement.GetProperty("message").GetString());
        }
    }
}
