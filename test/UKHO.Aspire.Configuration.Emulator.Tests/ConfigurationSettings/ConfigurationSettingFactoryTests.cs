using UKHO.ADDS.Aspire.Configuration.Emulator.Common;
using UKHO.ADDS.Aspire.Configuration.Emulator.ConfigurationSettings;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.ConfigurationSettings
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationSettingFactory"/> chooses the expected configuration-setting model for plain values and feature-flag payloads.
    /// </summary>
    public sealed class ConfigurationSettingFactoryTests
    {
        /// <summary>
        /// Verifies that feature-flag content is materialized as <see cref="FeatureFlagConfigurationSetting"/> so callers can work with the richer model.
        /// </summary>
        [Fact]
        public void Create_WhenFeatureFlagContentTypeProvided_ShouldReturnFeatureFlagConfigurationSetting()
        {
            // Arrange a feature-flag payload that exercises the special-case feature-flag model path.
            var factory = new ConfigurationSettingFactory();
            var tags = new Dictionary<string, string>
            {
                ["service"] = "catalog"
            };

            // Act by creating a configuration setting whose content type identifies it as a feature flag.
            var setting = factory.Create(
                etag: "etag-1",
                key: ".appconfig.featureflag/catalog",
                lastModified: new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                locked: false,
                label: "dev",
                contentType: MediaType.FeatureFlag,
                value: """
                {
                  "id": "catalog",
                  "enabled": true,
                  "conditions": {
                    "client_filters": []
                  }
                }
                """,
                tags: tags);

            // Assert that the feature-flag specific model is returned and the common metadata is preserved.
            var featureFlagSetting = Assert.IsType<FeatureFlagConfigurationSetting>(setting);
            Assert.Equal("catalog", featureFlagSetting.Id);
            Assert.True(featureFlagSetting.Enabled);
            Assert.Equal("dev", featureFlagSetting.Label);
            Assert.Equal(MediaType.FeatureFlag, featureFlagSetting.ContentType);
            Assert.Same(tags, featureFlagSetting.Tags);
        }

        /// <summary>
        /// Verifies that malformed content-type values do not prevent standard configuration-setting creation.
        /// </summary>
        [Fact]
        public void Create_WhenContentTypeInvalid_ShouldFallbackToStandardConfigurationSetting()
        {
            // Arrange a malformed content type so the feature-flag content-type parsing branch throws internally.
            var factory = new ConfigurationSettingFactory();

            // Act by creating the configuration setting with the invalid content type.
            var setting = factory.Create(
                etag: "etag-2",
                key: "catalog:endpoint",
                lastModified: new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                locked: true,
                label: "prod",
                contentType: "not a valid content type",
                value: "https://catalog.example.test",
                tags: null);

            // Assert that the factory safely falls back to the plain configuration-setting model.
            Assert.IsType<ConfigurationSetting>(setting);
            Assert.IsNotType<FeatureFlagConfigurationSetting>(setting);
            Assert.Equal("catalog:endpoint", setting.Key);
            Assert.Equal("https://catalog.example.test", setting.Value);
            Assert.True(setting.Locked);
        }

        /// <summary>
        /// Verifies that non-feature-flag content remains a standard <see cref="ConfigurationSetting"/>.
        /// </summary>
        [Fact]
        public void Create_WhenContentTypeNotFeatureFlag_ShouldReturnStandardConfigurationSetting()
        {
            // Arrange a standard JSON payload whose content type should not trigger feature-flag materialization.
            var factory = new ConfigurationSettingFactory();

            // Act by creating the setting with a non-feature-flag content type.
            var setting = factory.Create(
                etag: "etag-3",
                key: "catalog:settings",
                lastModified: new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero),
                locked: false,
                label: null,
                contentType: MediaType.Json,
                value: "{\"enabled\":true}",
                tags: null);

            // Assert that callers receive the standard configuration-setting representation.
            Assert.IsType<ConfigurationSetting>(setting);
            Assert.IsNotType<FeatureFlagConfigurationSetting>(setting);
            Assert.Equal(MediaType.Json, setting.ContentType);
            Assert.Equal("{\"enabled\":true}", setting.Value);
        }
    }
}
