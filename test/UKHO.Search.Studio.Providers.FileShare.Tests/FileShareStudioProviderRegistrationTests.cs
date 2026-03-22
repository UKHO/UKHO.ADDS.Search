using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using UKHO.Search.Studio;
using UKHO.Search.Studio.Providers.FileShare;
using UKHO.Search.Studio.Providers.FileShare.Injection;
using UKHO.Search.Studio.Providers.FileShare.Tests.TestDoubles;
using Xunit;

namespace UKHO.Search.Studio.Providers.FileShare.Tests
{
    public sealed class FileShareStudioProviderRegistrationTests
    {
        [Fact]
        public void AddFileShareStudioProvider_registers_studio_provider_and_validation_succeeds()
        {
            var services = new ServiceCollection();
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger<StudioProviderRegistrationValidator>>(NullLogger<StudioProviderRegistrationValidator>.Instance);
            services.AddProviderDescriptor<FileShareProviderMetadataRegistrationMarker>(new ProviderDescriptor("file-share", "File Share"));

            services.AddFileShareStudioProvider();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IStudioProviderCatalog>();
            var validator = provider.GetRequiredService<IStudioProviderRegistrationValidator>();

            catalog.GetProvider("file-share").ProviderName.ShouldBe("file-share");
            Should.NotThrow(() => validator.Validate());
        }

        [Fact]
        public void AddFileShareStudioProvider_is_idempotent()
        {
            var services = new ServiceCollection();
            services.AddSingleton<Microsoft.Extensions.Logging.ILogger<StudioProviderRegistrationValidator>>(NullLogger<StudioProviderRegistrationValidator>.Instance);
            services.AddProviderDescriptor<FileShareProviderMetadataRegistrationMarker>(new ProviderDescriptor("file-share", "File Share"));

            services.AddFileShareStudioProvider();
            services.AddFileShareStudioProvider();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IStudioProviderCatalog>();

            catalog.GetAllProviders().Count.ShouldBe(1);
            services.Count(x => x.ServiceType == typeof(IStudioProvider) && x.ImplementationType == typeof(FileShareStudioProvider)).ShouldBe(1);
        }
    }
}
