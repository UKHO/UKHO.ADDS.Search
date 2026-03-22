using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.ProviderModel;
using UKHO.Search.Studio;
using Xunit;

namespace StudioApiHost.Tests
{
    public sealed class StudioApiHostCompositionTests
    {
        [Fact]
        public async Task BuildApp_registers_provider_and_studio_metadata_without_runtime_factories()
        {
            var app = StudioApiHostApplication.BuildApp(Array.Empty<string>());

            try
            {
                var catalog = app.Services.GetRequiredService<IProviderCatalog>();
                var studioProviderCatalog = app.Services.GetRequiredService<IStudioProviderCatalog>();

                catalog.GetProvider("file-share").DisplayName.ShouldBe("File Share");
                studioProviderCatalog.GetProvider("file-share").ProviderName.ShouldBe("file-share");
                app.Services.GetServices<IIngestionDataProviderFactory>().ShouldBeEmpty();
            }
            finally
            {
                await app.DisposeAsync();
            }
        }
    }
}
