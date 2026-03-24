using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Studio.Injection;
using UKHO.Search.Studio.Providers;
using UKHO.Search.Studio.Tests.TestDoubles;
using Xunit;

namespace UKHO.Search.Studio.Tests
{
    public sealed class StudioProviderCatalogTests
    {
        [Fact]
        public void GetAllProviders_returns_names_in_deterministic_order()
        {
            var services = new ServiceCollection();
            services.AddStudioProvider<RegistrationMarkerA, ZetaStudioProvider>();
            services.AddStudioProvider<RegistrationMarkerB, AlphaStudioProvider>();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IStudioProviderCatalog>();

            catalog.GetAllProviders().Select(x => x.ProviderName).ShouldBe(["a-provider", "z-provider"]);
        }

        [Fact]
        public void GetProvider_is_case_insensitive()
        {
            var services = new ServiceCollection();
            services.AddStudioProvider<RegistrationMarkerA, FileShareStudioProviderTestDouble>();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IStudioProviderCatalog>();

            catalog.GetProvider("FILE-SHARE").ProviderName.ShouldBe("file-share");
        }

        [Fact]
        public void GetProvider_throws_for_unknown_name()
        {
            var services = new ServiceCollection();
            services.AddStudioProvider<RegistrationMarkerA, FileShareStudioProviderTestDouble>();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IStudioProviderCatalog>();

            Should.Throw<KeyNotFoundException>(() => catalog.GetProvider("unknown"));
        }

        [Fact]
        public void Catalog_construction_throws_for_duplicate_names()
        {
            var services = new ServiceCollection();
            services.AddStudioProvider<RegistrationMarkerA, FileShareStudioProviderTestDouble>();
            services.AddStudioProvider<RegistrationMarkerB, UppercaseFileShareStudioProviderTestDouble>();

            using var provider = services.BuildServiceProvider();

            Should.Throw<InvalidOperationException>(() => provider.GetRequiredService<IStudioProviderCatalog>());
        }

        [Fact]
        public void AddStudioProvider_is_idempotent_for_same_registration_marker()
        {
            var services = new ServiceCollection();
            services.AddStudioProvider<RegistrationMarkerA, FileShareStudioProviderTestDouble>();
            services.AddStudioProvider<RegistrationMarkerA, FileShareStudioProviderTestDouble>();

            using var provider = services.BuildServiceProvider();
            var catalog = provider.GetRequiredService<IStudioProviderCatalog>();

            catalog.GetAllProviders().Count.ShouldBe(1);
        }
    }
}
