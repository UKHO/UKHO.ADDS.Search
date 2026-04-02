using Microsoft.Extensions.Configuration;
using UKHO.ADDS.Aspire.Configuration.Emulator.Authentication.Hmac;
using UKHO.Aspire.Configuration.Emulator.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Authentication.Hmac
{
    /// <summary>
    /// Verifies that <see cref="HmacConfigureOptions"/> binds HMAC option values from authentication configuration.
    /// </summary>
    public sealed class HmacConfigureOptionsTests
    {
        /// <summary>
        /// Verifies that named scheme configuration overrides the existing credential and secret values.
        /// </summary>
        [Fact]
        public void Configure_WhenNamedSchemeConfigurationPresent_ShouldBindCredentialAndSecret()
        {
            // Arrange configuration that mirrors the named authentication-scheme layout used by ASP.NET Core.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Schemes:Catalog:Credential"] = "catalog-client",
                    ["Authentication:Schemes:Catalog:Secret"] = "catalog-secret"
                })
                .Build();
            var options = new HmacOptions
            {
                Credential = "existing-credential",
                Secret = "existing-secret"
            };
            var configureOptions = new HmacConfigureOptions(new TestAuthenticationConfigurationProvider(configuration.GetSection("Authentication")));

            // Act by binding the named scheme options.
            configureOptions.Configure("Catalog", options);

            // Assert that the configured values replaced the previous defaults.
            Assert.Equal("catalog-client", options.Credential);
            Assert.Equal("catalog-secret", options.Secret);
        }

        /// <summary>
        /// Verifies that blank names or missing configuration leave the existing option values unchanged.
        /// </summary>
        [Fact]
        public void Configure_WhenNameBlankOrConfigurationMissing_ShouldLeaveExistingValuesUntouched()
        {
            // Arrange an empty authentication configuration and baseline option values.
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var configureOptions = new HmacConfigureOptions(new TestAuthenticationConfigurationProvider(configuration.GetSection("Authentication")));
            var options = new HmacOptions
            {
                Credential = "existing-credential",
                Secret = "existing-secret"
            };

            // Act by attempting to bind both a blank scheme name and a missing scheme section.
            configureOptions.Configure(string.Empty, options);
            configureOptions.Configure("Missing", options);

            // Assert that no binding occurred because no usable scheme configuration was available.
            Assert.Equal("existing-credential", options.Credential);
            Assert.Equal("existing-secret", options.Secret);
        }
    }
}
