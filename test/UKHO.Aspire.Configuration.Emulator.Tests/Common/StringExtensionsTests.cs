using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Common
{
    /// <summary>
    /// Verifies the escape-removal behaviour provided by <see cref="StringExtensions.Unescape(string)"/>.
    /// </summary>
    public sealed class StringExtensionsTests
    {
        /// <summary>
        /// Verifies that escaped characters are unescaped while preserving the original character sequence.
        /// </summary>
        [Fact]
        public void Unescape_WhenEscapedCharactersPresent_ShouldRemoveEscapeMarkers()
        {
            // Arrange a representative string that includes escaped punctuation and path separators.
            const string value = "catalog\\/service\\:v1\\?enabled\\=true";

            // Act by unescaping the string.
            var result = value.Unescape();

            // Assert that the escape characters were removed without dropping the escaped characters themselves.
            Assert.Equal("catalog/service:v1?enabled=true", result);
        }

        /// <summary>
        /// Verifies that a trailing backslash is preserved because there is no following character to unescape.
        /// </summary>
        [Fact]
        public void Unescape_WhenTrailingBackslashPresent_ShouldKeepTrailingCharacter()
        {
            // Arrange a value whose final character is a backslash so the loop reaches the end-of-string branch.
            const string value = "catalog\\";

            // Act by unescaping the value.
            var result = value.Unescape();

            // Assert that the trailing backslash remains intact.
            Assert.Equal("catalog\\", result);
        }
    }
}
