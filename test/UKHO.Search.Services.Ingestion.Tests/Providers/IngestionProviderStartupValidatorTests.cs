using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.ProviderModel;
using UKHO.Search.Services.Ingestion.Providers;
using UKHO.Search.Services.Ingestion.Tests.TestProviders;
using Xunit;

namespace UKHO.Search.Services.Ingestion.Tests.Providers
{
    public sealed class IngestionProviderStartupValidatorTests
    {
        [Fact]
        public void Validate_succeeds_when_enabled_provider_has_metadata_and_runtime_registration()
        {
            var validator = CreateValidator(
                new IngestionProviderOptions
                {
                    Providers = ["FILE-SHARE"]
                },
                [new ProviderDescriptor("file-share", "File Share")],
                [new TestIngestionDataProviderFactory("file-share", "queue-a")]);

            Should.NotThrow(() => validator.Validate());
        }

        [Fact]
        public void Validate_throws_when_enabled_provider_is_missing_from_metadata_catalog()
        {
            var validator = CreateValidator(
                new IngestionProviderOptions
                {
                    Providers = ["file-share"]
                },
                [new ProviderDescriptor("other-provider", "Other")],
                [new TestIngestionDataProviderFactory("file-share", "queue-a")]);

            var exception = Should.Throw<InvalidOperationException>(() => validator.Validate());
            exception.Message.ShouldContain("not registered in provider metadata");
        }

        [Fact]
        public void Validate_throws_when_enabled_provider_has_metadata_but_no_runtime_registration()
        {
            var validator = CreateValidator(
                new IngestionProviderOptions
                {
                    Providers = ["file-share"]
                },
                [new ProviderDescriptor("file-share", "File Share")],
                Array.Empty<IIngestionDataProviderFactory>());

            var exception = Should.Throw<InvalidOperationException>(() => validator.Validate());
            exception.Message.ShouldContain("does not have a runtime registration");
        }

        [Fact]
        public void Validate_throws_when_runtime_provider_names_are_duplicated()
        {
            var validator = CreateValidator(
                new IngestionProviderOptions(),
                [new ProviderDescriptor("file-share", "File Share")],
                [
                    new TestIngestionDataProviderFactory("file-share", "queue-a"),
                    new TestIngestionDataProviderFactory("FILE-SHARE", "queue-b")
                ]);

            var exception = Should.Throw<InvalidOperationException>(() => validator.Validate());
            exception.Message.ShouldContain("already has a runtime registration");
        }

        private static IngestionProviderStartupValidator CreateValidator(
            IngestionProviderOptions options,
            IEnumerable<ProviderDescriptor> descriptors,
            IEnumerable<IIngestionDataProviderFactory> factories)
        {
            return new IngestionProviderStartupValidator(
                Options.Create(options),
                new ProviderCatalog(descriptors),
                factories,
                NullLogger<IngestionProviderStartupValidator>.Instance);
        }
    }
}
