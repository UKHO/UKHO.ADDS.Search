using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UKHO.ADDS.Aspire.Configuration.Emulator.Authentication.Hmac;
using UKHO.Aspire.Configuration.Emulator.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Emulator.Tests.Authentication.Hmac
{
    /// <summary>
    /// Verifies that the HMAC authentication extension methods register the expected authentication scheme and option services.
    /// </summary>
    public sealed class HmacExtensionsTests
    {
        /// <summary>
        /// Verifies that the default overload registers the production HMAC scheme and supporting option binder.
        /// </summary>
        [Fact]
        public async Task AddHmac_WhenDefaultOverloadUsed_ShouldRegisterDefaultSchemeAndConfigureOptions()
        {
            // Arrange a service collection with the authentication configuration provider dependency required by HMAC options.
            var services = new ServiceCollection();
            services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationConfigurationProvider>(
                new TestAuthenticationConfigurationProvider(new ConfigurationBuilder().AddInMemoryCollection().Build().GetSection("Authentication")));

            // Act by registering HMAC authentication through the default overload and building the provider.
            services.AddAuthentication().AddHmac();
            await using var provider = services.BuildServiceProvider().CreateAsyncScope();

            // Assert that the default HMAC scheme and option binder are both available from the service provider.
            var schemeProvider = provider.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(HmacDefaults.AuthenticationScheme);
            var configureOptions = provider.ServiceProvider.GetServices<IConfigureOptions<HmacOptions>>();

            Assert.NotNull(scheme);
            Assert.Equal(typeof(HmacHandler), scheme!.HandlerType);
            Assert.Contains(configureOptions, item => item is HmacConfigureOptions);
        }

        /// <summary>
        /// Verifies that the custom overload applies the caller's named scheme and option delegate.
        /// </summary>
        [Fact]
        public async Task AddHmac_WhenCustomSchemeAndConfigureDelegateUsed_ShouldRegisterNamedOptions()
        {
            // Arrange a service collection with empty authentication configuration and a custom HMAC registration.
            var services = new ServiceCollection();
            services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationConfigurationProvider>(
                new TestAuthenticationConfigurationProvider(new ConfigurationBuilder().AddInMemoryCollection().Build().GetSection("Authentication")));
            services.AddAuthentication().AddHmac("CatalogHmac", options =>
            {
                // Apply custom values so the resulting named options instance can be asserted directly.
                options.Credential = "catalog-client";
                options.Secret = "catalog-secret";
            });

            // Act by building the service provider and resolving the named scheme and options.
            await using var provider = services.BuildServiceProvider().CreateAsyncScope();
            var schemeProvider = provider.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
            var optionsMonitor = provider.ServiceProvider.GetRequiredService<IOptionsMonitor<HmacOptions>>();
            var scheme = await schemeProvider.GetSchemeAsync("CatalogHmac");
            var options = optionsMonitor.Get("CatalogHmac");

            // Assert that the named scheme exists and the configure delegate populated the expected values.
            Assert.NotNull(scheme);
            Assert.Equal(typeof(HmacHandler), scheme!.HandlerType);
            Assert.Equal("catalog-client", options.Credential);
            Assert.Equal("catalog-secret", options.Secret);
        }
    }
}
