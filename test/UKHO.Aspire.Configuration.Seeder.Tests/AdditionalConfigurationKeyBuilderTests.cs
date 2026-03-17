using UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests
{
    public class AdditionalConfigurationKeyBuilderTests
    {
        [Fact]
        public void Build_WhenNoPathSegments_ReturnsPrefixAndFileName()
        {
            var key = AdditionalConfigurationKeyBuilder.Build("prefix", Array.Empty<string>(), "file");
            Assert.Equal("prefix:file", key);
        }

        [Fact]
        public void Build_WhenNestedSegments_ReturnsFullKey()
        {
            var key = AdditionalConfigurationKeyBuilder.Build("prefix", new[] { "a", "b" }, "file");
            Assert.Equal("prefix:a:b:file", key);
        }

        [Fact]
        public void Build_IgnoresEmptySegments()
        {
            var key = AdditionalConfigurationKeyBuilder.Build("prefix", new[] { "a", " ", "b" }, "file");
            Assert.Equal("prefix:a:b:file", key);
        }
    }
}
