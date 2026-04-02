using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using UKHO.Search.Studio.Injection;
using UKHO.Search.Studio.Providers;
using UKHO.Search.Studio.Tests.TestDoubles;
using Xunit;

namespace UKHO.Search.Studio.Tests
{
    public sealed class StudioProviderRegistrationValidatorTests
    {
        [Fact]
        public void Validate_succeeds_when_studio_provider_has_matching_provider_metadata()
        {
            var services = new ServiceCollection();
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger<StudioProviderRegistrationValidator>>(NullLogger<StudioProviderRegistrationValidator>.Instance);
            services.AddProviderDescriptor<ProviderMetadataRegistrationMarker>(new ProviderDescriptor("file-share", "File Share"));
            services.AddStudioProvider<RegistrationMarkerA, FileShareStudioProviderTestDouble>();

            using var provider = services.BuildServiceProvider();
            var validator = provider.GetRequiredService<IStudioProviderRegistrationValidator>();

            Should.NotThrow(() => validator.Validate());
        }

        [Fact]
        public void Validate_throws_when_studio_provider_is_missing_from_provider_metadata()
        {
            var services = new ServiceCollection();
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger<StudioProviderRegistrationValidator>>(NullLogger<StudioProviderRegistrationValidator>.Instance);
            services.AddProviderDescriptor<ProviderMetadataRegistrationMarker>(new ProviderDescriptor("other-provider", "Other"));
            services.AddStudioProvider<RegistrationMarkerA, FileShareStudioProviderTestDouble>();

            using var provider = services.BuildServiceProvider();
            var validator = provider.GetRequiredService<IStudioProviderRegistrationValidator>();

            var exception = Should.Throw<InvalidOperationException>(() => validator.Validate());
            exception.Message.ShouldContain("not registered in provider metadata");
        }
    }
}
