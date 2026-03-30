using System.Net.Mime;
using Microsoft.Extensions.Logging;
using UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration;
using UKHO.Aspire.Configuration.Seeder.Services;
using UKHO.Aspire.Configuration.Seeder.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests.Services
{
    /// <summary>
    /// Verifies that <see cref="LocalSeederService"/> forwards local seeding arguments correctly and always stops the host after completion.
    /// </summary>
    public sealed class LocalSeederServiceTests
    {
        /// <summary>
        /// Verifies that a successful local seeding run resolves local placeholders, forwards the optional additional configuration arguments, and requests host shutdown.
        /// </summary>
        [Fact]
        public async Task StartAsync_WhenSeedingSucceeds_ShouldStopHostAndWriteLocalConfiguration()
        {
            // Arrange a local configuration payload, external-service payload, and optional additional configuration file.
            using var directory = new TemporaryDirectory();
            var configFilePath = directory.CreateFile("config.json", """
            {
              "local": {
                "feature": {
                  "enabled": true
                }
              }
            }
            """);
            var servicesFilePath = directory.CreateFile("services.json", """
            {
              "local": {
                "catalog": {
                  "clientId": "catalog-client",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://{{localseedercatalog}}/api"
                    }
                  ]
                }
              }
            }
            """);
            var additionalConfigurationPath = System.IO.Path.Combine(directory.Path, "additional");
            directory.CreateFile(System.IO.Path.Combine("additional", "nested", "setting.txt"), "forwarded value");

            using var endpointScope = new EnvironmentVariableScope("services__localseedercatalog__https__0", "https://seeded.local.test:9443/");
            var hostLifetime = new TestHostApplicationLifetime();
            var configLogger = new TestLogger<ConfigurationService>();
            var localSeederLogger = new TestLogger<LocalSeederService>();
            var configurationService = new ConfigurationService(configLogger, new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()));
            var configurationClient = new TestConfigurationClient();
            var service = new LocalSeederService(
                hostLifetime,
                configurationService,
                new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()),
                "MySERVICE",
                configurationClient,
                configFilePath,
                servicesFilePath,
                additionalConfigurationPath,
                "additional",
                localSeederLogger);

            // Act by starting the hosted service once, which should run the local seeding flow and then stop the host.
            await service.StartAsync(CancellationToken.None);

            // Assert that the host shutdown was requested exactly once after the successful run.
            Assert.Equal(1, hostLifetime.StopApplicationCallCount);

            // Assert that local endpoint resolution happened by checking the serialized external-service payload.
            var externalServiceWrite = Assert.Single(configurationClient.Writes, write => write.Setting.Key == "externalservice:catalog").Setting;
            Assert.Contains("https://seeded.local.test:9443/api", externalServiceWrite.Value, StringComparison.Ordinal);
            Assert.Equal("myservice", externalServiceWrite.Label);

            // Assert that the optional additional configuration arguments were forwarded into the seeding flow.
            var additionalWrite = Assert.Single(configurationClient.Writes, write => write.Setting.Key == "additional:nested:setting").Setting;
            Assert.Equal("forwarded value", additionalWrite.Value);
            Assert.Equal("myservice", additionalWrite.Label);
            Assert.Equal(MediaTypeNames.Text.Plain, additionalWrite.ContentType);

            // The hosted service should log successful completion and avoid logging an error.
            Assert.Contains(localSeederLogger.Entries, entry => entry.LogLevel == LogLevel.Information && entry.Message.Contains("completed", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(localSeederLogger.Entries, entry => entry.LogLevel == LogLevel.Error);
        }

        /// <summary>
        /// Verifies that a seeding failure is logged, rethrown to the host, and still triggers application shutdown.
        /// </summary>
        [Fact]
        public async Task StartAsync_WhenSeedingFails_ShouldStopHostLogErrorAndRethrow()
        {
            // Arrange the smallest possible local payload while forcing the first configuration write to fail permanently.
            using var directory = new TemporaryDirectory();
            var configFilePath = directory.CreateFile("config.json", """
            {
              "local": {
                "feature": {
                  "enabled": true
                }
              }
            }
            """);
            var servicesFilePath = directory.CreateFile("services.json", """
            {
              "local": {
                "catalog": {
                  "clientId": "catalog-client",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.local.example.test"
                    }
                  ]
                }
              }
            }
            """);

            var hostLifetime = new TestHostApplicationLifetime();
            var localSeederLogger = new TestLogger<LocalSeederService>();
            var configurationService = new ConfigurationService(new TestLogger<ConfigurationService>(), new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()));
            var configurationClient = new TestConfigurationClient((_, _, _) => throw new InvalidOperationException("boom"));
            var service = new LocalSeederService(
                hostLifetime,
                configurationService,
                new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()),
                "MySERVICE",
                configurationClient,
                configFilePath,
                servicesFilePath,
                string.Empty,
                string.Empty,
                localSeederLogger);

            // Act by starting the hosted service and capturing the expected failure from the wrapped configuration service.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));

            // Assert that the original failure was rethrown and that shutdown was still requested in the finally block.
            Assert.Equal("boom", exception.Message);
            Assert.Equal(1, hostLifetime.StopApplicationCallCount);
            Assert.Contains(localSeederLogger.Entries, entry => entry.LogLevel == LogLevel.Error && entry.Exception is InvalidOperationException);
        }

        /// <summary>
        /// Verifies that <see cref="LocalSeederService.StopAsync"/> completes immediately because the hosted service has no asynchronous shutdown work.
        /// </summary>
        [Fact]
        public async Task StopAsync_ShouldReturnCompletedTask()
        {
            // Arrange a service instance using temporary files even though shutdown should not inspect them.
            using var directory = new TemporaryDirectory();
            var configFilePath = directory.CreateFile("config.json", """
            {
              "local": {
                "feature": {
                  "enabled": true
                }
              }
            }
            """);
            var servicesFilePath = directory.CreateFile("services.json", """
            {
              "local": {
                "catalog": {
                  "clientId": "catalog-client",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.local.example.test"
                    }
                  ]
                }
              }
            }
            """);

            var service = new LocalSeederService(
                new TestHostApplicationLifetime(),
                new ConfigurationService(new TestLogger<ConfigurationService>(), new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>())),
                new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()),
                "MySERVICE",
                new TestConfigurationClient(),
                configFilePath,
                servicesFilePath,
                string.Empty,
                string.Empty,
                new TestLogger<LocalSeederService>());

            // Act by invoking the hosted-service shutdown hook and capturing the returned task for inspection.
            var stopTask = service.StopAsync(CancellationToken.None);

            // Assert that shutdown completes synchronously because the implementation has no asynchronous cleanup to perform.
            Assert.True(stopTask.IsCompletedSuccessfully);
            await stopTask;
        }
    }
}
