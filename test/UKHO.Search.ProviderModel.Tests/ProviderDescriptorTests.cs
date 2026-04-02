using Shouldly;
using Xunit;

namespace UKHO.Search.ProviderModel.Tests
{
    public sealed class ProviderDescriptorTests
    {
        [Fact]
        public void Constructor_sets_properties_for_valid_descriptor()
        {
            var descriptor = new ProviderDescriptor("file-share", "File Share", "Imports content from File Share.");

            descriptor.Name.ShouldBe("file-share");
            descriptor.DisplayName.ShouldBe("File Share");
            descriptor.Description.ShouldBe("Imports content from File Share.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("File-Share")]
        [InlineData("file_share")]
        [InlineData("file share")]
        [InlineData("file--share")]
        public void Constructor_throws_for_invalid_name(string? name)
        {
            Should.Throw<ArgumentException>(() => new ProviderDescriptor(name!, "File Share"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_throws_for_missing_display_name(string? displayName)
        {
            Should.Throw<ArgumentException>(() => new ProviderDescriptor("file-share", displayName!));
        }
    }
}
