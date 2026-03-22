using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Pipeline;
using IngestionServiceHost.Tests.TestDoubles;
using Xunit;

namespace IngestionServiceHost.Tests
{
    public sealed class IngestionPipelineHostedServiceTests
    {
        [Fact]
        public async Task StartAsync_throws_when_provider_validation_fails_and_bootstrap_does_not_run()
        {
            var bootstrapService = new RecordingBootstrapService();
            var validator = new ThrowingIngestionProviderStartupValidator(new InvalidOperationException("Provider validation failed."));
            var hostedService = new IngestionPipelineHostedService(
                new ConfigurationBuilder().Build(),
                new UnusedIngestionProviderService(),
                new UnusedQueueClientFactory(),
                bootstrapService,
                new NoOpHostApplicationLifetime(),
                validator,
                NullLogger<IngestionPipelineHostedService>.Instance);

            var exception = await Should.ThrowAsync<InvalidOperationException>(() => hostedService.StartAsync(CancellationToken.None));

            exception.Message.ShouldBe("Provider validation failed.");
            validator.CallCount.ShouldBe(1);
            bootstrapService.CallCount.ShouldBe(0);
        }
    }
}
