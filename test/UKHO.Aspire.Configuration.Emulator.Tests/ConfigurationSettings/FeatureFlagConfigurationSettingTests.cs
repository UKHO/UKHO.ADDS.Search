using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.ConfigurationSettings
{
    /// <summary>
    /// Verifies that <see cref="FeatureFlagConfigurationSetting"/> can serialize and deserialize complex filter payloads without losing feature metadata.
    /// </summary>
    public sealed class FeatureFlagConfigurationSettingTests
    {
        /// <summary>
        /// Verifies that constructing a feature flag from strongly typed members serializes the expected JSON payload.
        /// </summary>
        [Fact]
        public void Value_WhenConstructedFromStronglyTypedMembers_ShouldSerializeNestedFilterParameters()
        {
            // Arrange a feature flag with nested filter parameters so serialization must preserve mixed primitive and object values.
            var filters = new List<FeatureFlagFilter>
            {
                new(
                    "Microsoft.TimeWindow",
                    new Dictionary<string, object>
                    {
                        ["threshold"] = 5,
                        ["settings"] = new Dictionary<string, object>
                        {
                            ["enabled"] = true,
                            ["regions"] = new List<object>
                            {
                                "uk",
                                "ie"
                            }
                        }
                    })
            };

            var setting = new FeatureFlagConfigurationSetting(
                id: "catalog",
                enabled: true,
                clientFilters: filters,
                etag: "etag-1",
                key: ".appconfig.featureflag/catalog",
                lastModified: new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                locked: false,
                description: "Catalog feature flag",
                displayName: "Catalog",
                label: "dev",
                contentType: MediaType.FeatureFlag,
                tags: new Dictionary<string, string>
                {
                    ["team"] = "search"
                });

            // Act by reading the computed JSON payload.
            var value = setting.Value;

            // Assert that the payload captures the expected top-level and nested filter information.
            Assert.NotNull(value);
            Assert.Contains("\"id\":\"catalog\"", value, StringComparison.Ordinal);
            Assert.Contains("\"display_name\":\"Catalog\"", value, StringComparison.Ordinal);
            Assert.Contains("\"threshold\":5", value, StringComparison.Ordinal);
            Assert.Contains("\"regions\":[\"uk\",\"ie\"]", value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that assigning the JSON value repopulates the typed feature-flag members, including nested filter parameters.
        /// </summary>
        [Fact]
        public void Value_WhenSetFromJsonPayload_ShouldRoundTripNestedFilterParameters()
        {
            // Arrange a feature flag instance that will hydrate itself from JSON produced by Azure App Configuration style payloads.
            var setting = new FeatureFlagConfigurationSetting(
                id: "placeholder",
                enabled: false,
                clientFilters: [],
                etag: "etag-2",
                key: ".appconfig.featureflag/catalog",
                lastModified: new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                locked: true,
                description: null,
                displayName: null,
                label: "test",
                contentType: MediaType.FeatureFlag,
                tags: null);

            const string payload = """
            {
              "id": "catalog",
              "enabled": true,
              "conditions": {
                "client_filters": [
                  {
                    "name": "Microsoft.TimeWindow",
                    "parameters": {
                      "threshold": 10,
                      "settings": {
                        "enabled": true,
                        "regions": ["uk", "ie"],
                        "window": {
                          "start": "2026-03-30T00:00:00Z"
                        }
                      }
                    }
                  }
                ]
              },
              "description": "Catalog feature flag",
              "display_name": "Catalog"
            }
            """;

            // Act by hydrating the feature flag from the JSON payload and then re-reading the serialized value.
            setting.Value = payload;
            var roundTrippedValue = setting.Value;

            // Assert that the typed feature-flag members and nested filter parameters were preserved.
            Assert.Equal("catalog", setting.Id);
            Assert.True(setting.Enabled);
            Assert.Equal("Catalog feature flag", setting.Description);
            Assert.Equal("Catalog", setting.DisplayName);

            var filter = Assert.Single(setting.ClientFilters);
            Assert.Equal("Microsoft.TimeWindow", filter.Name);
            Assert.Equal(10, Assert.IsType<int>(filter.Parameters["threshold"]));

            var settings = Assert.IsAssignableFrom<IDictionary<string, object>>(filter.Parameters["settings"]);
            Assert.True(Assert.IsType<bool>(settings["enabled"]));

            var regions = Assert.IsAssignableFrom<IList<object?>>(settings["regions"]);
            Assert.Equal(new object?[] { "uk", "ie" }, regions.ToArray());

            var window = Assert.IsAssignableFrom<IDictionary<string, object>>(settings["window"]);
            Assert.Equal("2026-03-30T00:00:00Z", Assert.IsType<string>(window["start"]));

            Assert.NotNull(roundTrippedValue);
            Assert.Contains("\"Microsoft.TimeWindow\"", roundTrippedValue, StringComparison.Ordinal);
            Assert.Contains("\"display_name\":\"Catalog\"", roundTrippedValue, StringComparison.Ordinal);
        }
    }
}
