using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Ingestion;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Ingestion.Providers.FileShare;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers;
using UKHO.Search.Ingestion.Providers.FileShare.Injection;
using UKHO.Search.ProviderModel;
using Xunit;

namespace UKHO.Search.Ingestion.Providers.FileShare.Tests.Injection
{
    public sealed class FileShareProviderRegistrationTests
    {
        [Fact]
        public void AddFileShareProviderMetadata_registers_catalog_and_descriptor_without_runtime_services()
        {
            var services = new ServiceCollection();

            services.AddFileShareProviderMetadata();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IProviderCatalog>();
            var descriptor = catalog.GetProvider(FileShareIngestionDataProviderFactory.ProviderName);

            descriptor.Name.ShouldBe(FileShareIngestionDataProviderFactory.ProviderName);
            descriptor.DisplayName.ShouldBe("File Share");
            services.Any(x => x.ServiceType == typeof(IIngestionEnricher)).ShouldBeFalse();
        }

        [Fact]
        public void AddFileShareProviderRuntime_registers_metadata_and_runtime_services()
        {
            var services = new ServiceCollection();

            services.AddFileShareProviderRuntime();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IProviderCatalog>();

            catalog.GetProvider(FileShareIngestionDataProviderFactory.ProviderName).DisplayName.ShouldBe("File Share");
            services.Any(x => x.ServiceType == typeof(IIngestionEnricher) && x.ImplementationType == typeof(BasicEnricher)).ShouldBeTrue();
            services.Any(x => x.ServiceType == typeof(IBatchContentHandler) && x.ImplementationType == typeof(S57BatchContentHandler)).ShouldBeTrue();
            services.Any(x => x.ServiceType == typeof(IBatchContentHandler) && x.ImplementationType == typeof(S100BatchContentHandler)).ShouldBeTrue();
            provider.GetRequiredService<IIngestionDataProviderFactory>().Name.ShouldBe(FileShareIngestionDataProviderFactory.ProviderName);
        }

        [Fact]
        public void AddFileShareProviderMetadata_is_idempotent()
        {
            var services = new ServiceCollection();

            services.AddFileShareProviderMetadata();
            services.AddFileShareProviderMetadata();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IProviderCatalog>();

            catalog.GetAllProviders().Count.ShouldBe(1);
        }

        [Fact]
        public void AddFileShareProviderRuntime_is_idempotent()
        {
            var services = new ServiceCollection();

            services.AddFileShareProviderRuntime();
            services.AddFileShareProviderRuntime();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IProviderCatalog>();

            catalog.GetAllProviders().Count.ShouldBe(1);
            services.Count(x => x.ServiceType == typeof(IIngestionDataProviderFactory)).ShouldBe(1);
            services.Count(x => x.ServiceType == typeof(IIngestionEnricher) && x.ImplementationType == typeof(BasicEnricher)).ShouldBe(1);
        }
    }
}
