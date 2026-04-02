using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Common
{
    /// <summary>
    /// Verifies that <see cref="SelectJsonTypeInfoModifier"/> filters JSON properties according to the requested projection.
    /// </summary>
    public sealed class SelectJsonTypeInfoModifierTests
    {
        /// <summary>
        /// Verifies that explicitly selected properties are retained and the special <c>items</c> property remains available for collection results.
        /// </summary>
        [Fact]
        public void Modify_WhenNamesProvided_ShouldKeepSelectedPropertiesAndItems()
        {
            // Arrange an anonymous type that mirrors the shape of list-result payloads used by the emulator.
            var options = new JsonSerializerOptions();
            var resolver = new DefaultJsonTypeInfoResolver();
            var modifier = new SelectJsonTypeInfoModifier(["selected"]);
            var runtimeType = new { items = new[] { "catalog" }, selected = "value", ignored = "skip" }.GetType();
            var typeInfo = resolver.GetTypeInfo(runtimeType, options);

            // Act by applying the property-selection modifier.
            modifier.Modify(typeInfo);

            // Assert that the selected property and mandatory items projection remain while unrelated properties are removed.
            var propertyNames = typeInfo.Properties.Select(property => property.Name).ToArray();
            Assert.Contains("items", propertyNames);
            Assert.Contains("selected", propertyNames);
            Assert.DoesNotContain("ignored", propertyNames);
        }
    }
}
