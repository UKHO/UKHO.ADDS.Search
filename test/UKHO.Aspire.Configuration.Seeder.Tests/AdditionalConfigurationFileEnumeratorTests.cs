using UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests
{
    public class AdditionalConfigurationFileEnumeratorTests
    {
        [Fact]
        public void GetRelativePathSegments_WhenFileAtRoot_ReturnsEmpty()
        {
            var segments = AdditionalConfigurationFileEnumerator.GetRelativePathSegments(
                "C:\\root",
                "C:\\root\\file.json");

            Assert.Empty(segments);
        }

        [Fact]
        public void GetRelativePathSegments_WhenNested_ReturnsSegments()
        {
            var segments = AdditionalConfigurationFileEnumerator.GetRelativePathSegments(
                "C:\\root",
                "C:\\root\\a\\b\\file.json");

            Assert.Equal(new[] { "a", "b" }, segments);
        }
    }
}
