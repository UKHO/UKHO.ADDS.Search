using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Common
{
    /// <summary>
    /// Verifies parsing and formatting behaviour for <see cref="LinkHeaderValue"/>.
    /// </summary>
    public sealed class LinkHeaderValueTests
    {
        /// <summary>
        /// Verifies that next and previous relations are parsed into the expected URI collections.
        /// </summary>
        [Fact]
        public void Parse_WhenInputContainsNextAndPrevRelations_ShouldExposeUrisByRelation()
        {
            // Arrange a link header that exposes both paging directions.
            const string input = "</kv?after=abc>; rel=\"next\", </kv?before=abc>; rel=\"prev\"";

            // Act by parsing the combined link header value.
            var result = LinkHeaderValue.Parse(input);

            // Assert that each relation can be read independently from the parsed representation.
            var next = Assert.Single(Assert.IsAssignableFrom<IEnumerable<Uri>>(result.Next));
            var previous = Assert.Single(Assert.IsAssignableFrom<IEnumerable<Uri>>(result.Prev));
            Assert.Equal("/kv?after=abc", next.ToString());
            Assert.Equal("/kv?before=abc", previous.ToString());
        }

        /// <summary>
        /// Verifies that formatting preserves multiple URIs that share the same relation.
        /// </summary>
        [Fact]
        public void ToString_WhenValueContainsMultipleNextLinks_ShouldFormatCombinedHeader()
        {
            // Arrange a parsed value that contains two next links so formatting must join them correctly.
            var value = LinkHeaderValue.Parse("</kv?after=abc>; rel=\"next\", </kv?after=def>; rel=\"next\"");

            // Act by converting the parsed value back to its wire-format representation.
            var result = value.ToString();

            // Assert that the next links are emitted in the same combined relation format.
            Assert.Equal("</kv?after=abc>; rel=\"next\", </kv?after=def>; rel=\"next\"", result);
        }
    }
}
