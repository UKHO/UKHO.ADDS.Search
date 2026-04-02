using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.ProviderModel.Injection;
using Xunit;

namespace UKHO.Search.ProviderModel.Tests
{
    public sealed class ProviderCatalogTests
    {
        [Fact]
        public void GetAllProviders_returns_names_in_deterministic_order()
        {
            var services = new ServiceCollection();
            services.AddProviderDescriptor<TestProviderRegistrationMarkerA>(new ProviderDescriptor("z-provider", "Zed"));
            services.AddProviderDescriptor<TestProviderRegistrationMarkerB>(new ProviderDescriptor("a-provider", "Alpha"));

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IProviderCatalog>();

            catalog.GetAllProviders().Select(x => x.Name).ShouldBe(["a-provider", "z-provider"]);
        }

        [Fact]
        public void GetProvider_is_case_insensitive()
        {
            var services = new ServiceCollection();
            services.AddProviderDescriptor<TestProviderRegistrationMarkerA>(new ProviderDescriptor("file-share", "File Share"));

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IProviderCatalog>();

            catalog.GetProvider("FILE-SHARE").DisplayName.ShouldBe("File Share");
        }

        [Fact]
        public void GetProvider_throws_for_unknown_name()
        {
            var services = new ServiceCollection();
            services.AddProviderDescriptor<TestProviderRegistrationMarkerA>(new ProviderDescriptor("file-share", "File Share"));

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IProviderCatalog>();

            Should.Throw<KeyNotFoundException>(() => catalog.GetProvider("unknown"));
        }

        [Fact]
        public void Catalog_construction_throws_for_duplicate_names()
        {
            var services = new ServiceCollection();
            services.AddProviderDescriptor<TestProviderRegistrationMarkerA>(new ProviderDescriptor("file-share", "File Share"));
            services.AddProviderDescriptor<TestProviderRegistrationMarkerB>(new ProviderDescriptor("file-share", "File Share Duplicate"));

            using var provider = services.BuildServiceProvider();

            Should.Throw<InvalidOperationException>(() => provider.GetRequiredService<IProviderCatalog>());
        }

        [Fact]
        public void AddProviderDescriptor_is_idempotent_for_same_registration_marker()
        {
            var services = new ServiceCollection();
            services.AddProviderDescriptor<TestProviderRegistrationMarkerA>(new ProviderDescriptor("file-share", "File Share"));
            services.AddProviderDescriptor<TestProviderRegistrationMarkerA>(new ProviderDescriptor("file-share", "File Share"));

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IProviderCatalog>();

            catalog.GetAllProviders().Count.ShouldBe(1);
        }

        private sealed class TestProviderRegistrationMarkerA
        {
        }

        private sealed class TestProviderRegistrationMarkerB
        {
        }
    }
}
