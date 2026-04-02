using Microsoft.Extensions.Options;
using Shouldly;
using UKHO.Search.Services.Ingestion.Providers;
using UKHO.Search.Services.Ingestion.Tests.TestProviders;
using Xunit;

namespace UKHO.Search.Services.Ingestion.Tests.Providers
{
    public sealed class IngestionProviderServiceTests
    {
        [Fact]
        public void GetAllProviders_returns_all_registered_providers_when_configuration_is_empty()
        {
            var service = new IngestionProviderService(
                [
                    new TestIngestionDataProviderFactory("file-share", "queue-a"),
                    new TestIngestionDataProviderFactory("other-provider", "queue-b")
                ],
                Options.Create(new IngestionProviderOptions()));

            service.GetAllProviders().Select(x => x.Name).ShouldBe(["file-share", "other-provider"]);
        }

        [Fact]
        public void GetAllProviders_filters_to_enabled_provider_names_case_insensitively()
        {
            var service = new IngestionProviderService(
                [
                    new TestIngestionDataProviderFactory("file-share", "queue-a"),
                    new TestIngestionDataProviderFactory("other-provider", "queue-b")
                ],
                Options.Create(new IngestionProviderOptions
                {
                    Providers = ["FILE-SHARE"]
                }));

            service.GetAllProviders().Select(x => x.Name).ShouldBe(["file-share"]);
            service.GetProvider("file-share").QueueName.ShouldBe("queue-a");
        }

        [Fact]
        public void Constructor_throws_for_duplicate_provider_names()
        {
            Should.Throw<InvalidOperationException>(() => new IngestionProviderService(
                [
                    new TestIngestionDataProviderFactory("file-share", "queue-a"),
                    new TestIngestionDataProviderFactory("FILE-SHARE", "queue-b")
                ],
                Options.Create(new IngestionProviderOptions())));
        }
    }
}
