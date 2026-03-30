using System.Net.Mime;
using Azure;
using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Logging;
using UKHO.Aspire.Configuration;
using UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration;
using UKHO.Aspire.Configuration.Seeder.Services;
using UKHO.Aspire.Configuration.Seeder.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests.Services
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationService"/> orchestrates sentinel writes, flattened configuration writes, external service writes, optional additional configuration writes, and retry handling.
    /// </summary>
    public sealed class ConfigurationServiceTests
    {
        /// <summary>
        /// Verifies that the service writes the reload sentinel first, lowercases the label, strips JSON comments, writes flattened values, serializes external services, and then appends additional configuration.
        /// </summary>
        [Fact]
        public async Task SeedConfigurationAsync_WhenInputsValid_ShouldWriteSentinelConfigurationServicesAndAdditionalConfigurationInExpectedOrder()
        {
            // Arrange a complete local seeding scenario so the orchestration flow can be asserted end to end without live Azure dependencies.
            using var directory = new TemporaryDirectory();
            var configFilePath = directory.CreateFile("config.json", """
            {
              // The local environment block should be selected and comments should be ignored.
              "local": {
                "feature": {
                  "enabled": true,
                  /* Preserve the value while discarding this block comment. */
                  "message": "hello world",
                  "secret": { "uri": "https://sample.vault.azure.net/secrets/example" }
                }
              }
            }
            """);
            var servicesFilePath = directory.CreateFile("services.json", """
            {
              "local": {
                // The placeholder should resolve through the Aspire service environment variable convention.
                "catalog": {
                  "clientId": "catalog-client",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://{{seedercatalog}}/api"
                    }
                  ]
                }
              }
            }
            """);
            var additionalConfigurationPath = System.IO.Path.Combine(directory.Path, "additional");
            directory.CreateFile(System.IO.Path.Combine("additional", "nested", "extra.txt"), "extra value");

            using var endpointScope = new EnvironmentVariableScope("services__seedercatalog__https__0", "https://catalog.local.test:8443/");
            var logger = new TestLogger<ConfigurationService>();
            var additionalSeederLogger = new TestLogger<AdditionalConfigurationSeeder>();
            var additionalSeeder = new AdditionalConfigurationSeeder(additionalSeederLogger);
            var service = new ConfigurationService(logger, additionalSeeder);
            var client = new TestConfigurationClient();

            // Act by running the full seeding path with additional configuration enabled.
            await service.SeedConfigurationAsync(
                AddsEnvironment.Local,
                client,
                "MySERVICE",
                configFilePath,
                servicesFilePath,
                additionalConfigurationPath,
                "additional",
                CancellationToken.None);

            // Assert that the reload sentinel is always written first so downstream refresh detection has a stable anchor.
            Assert.NotEmpty(client.Writes);
            var sentinelWrite = client.Writes[0].Setting;
            Assert.Equal(WellKnownConfigurationName.ReloadSentinelKey, sentinelWrite.Key);
            Assert.Equal("change this value to reload all", sentinelWrite.Value);
            Assert.Equal("myservice", sentinelWrite.Label);
            Assert.Equal(MediaTypeNames.Text.Plain, sentinelWrite.ContentType);

            // Assert that the flattened configuration settings preserve JSON order and cleaned values after comment removal.
            var configurationWrites = client.Writes.Skip(1).Take(3).Select(write => write.Setting).ToArray();
            Assert.Collection(
                configurationWrites,
                setting =>
                {
                    Assert.Equal("feature:enabled", setting.Key);
                    Assert.Equal("True", setting.Value);
                    Assert.Equal("myservice", setting.Label);
                    Assert.Equal(MediaTypeNames.Text.Plain, setting.ContentType);
                },
                setting =>
                {
                    Assert.Equal("feature:message", setting.Key);
                    Assert.Equal("hello world", setting.Value);
                    Assert.Equal("myservice", setting.Label);
                    Assert.Equal(MediaTypeNames.Text.Plain, setting.ContentType);
                },
                setting =>
                {
                    Assert.Equal("feature:secret:uri", setting.Key);
                    Assert.Equal("https://sample.vault.azure.net/secrets/example", setting.Value);
                    Assert.Equal("myservice", setting.Label);
                    Assert.Equal(MediaTypeNames.Text.Plain, setting.ContentType);
                });

            // Assert that the external service definition is written after flattened configuration and reflects the resolved local endpoint.
            var externalServiceWrite = client.Writes[4].Setting;
            Assert.Equal($"{WellKnownConfigurationName.ExternalServiceKeyPrefix}:catalog", externalServiceWrite.Key);
            Assert.Equal("myservice", externalServiceWrite.Label);
            Assert.Equal(MediaTypeNames.Text.Plain, externalServiceWrite.ContentType);
            Assert.Contains("catalog-client", externalServiceWrite.Value, StringComparison.Ordinal);
            Assert.Contains("https://catalog.local.test:8443/api", externalServiceWrite.Value, StringComparison.Ordinal);

            // Assert that additional configuration is appended last and uses the supplied prefix with the normalised label.
            var additionalWrite = client.Writes[5].Setting;
            Assert.Equal("additional:nested:extra", additionalWrite.Key);
            Assert.Equal("extra value", additionalWrite.Value);
            Assert.Equal("myservice", additionalWrite.Label);
            Assert.Equal(MediaTypeNames.Text.Plain, additionalWrite.ContentType);

            // The happy path should not emit warnings because no retries or validation failures were required.
            Assert.DoesNotContain(logger.Entries, entry => entry.LogLevel >= LogLevel.Warning);
            Assert.DoesNotContain(additionalSeederLogger.Entries, entry => entry.LogLevel >= LogLevel.Warning);
        }

        /// <summary>
        /// Verifies that blank optional additional-configuration arguments skip the additional seeding branch entirely.
        /// </summary>
        [Fact]
        public async Task SeedConfigurationAsync_WhenAdditionalConfigurationArgumentsBlank_ShouldSkipAdditionalConfigurationWrites()
        {
            // Arrange a minimal non-local scenario so only the optional additional-configuration branch is under test.
            using var directory = new TemporaryDirectory();
            var configFilePath = directory.CreateFile("config.json", """
            {
              "dev": {
                "feature": {
                  "enabled": false
                }
              }
            }
            """);
            var servicesFilePath = directory.CreateFile("services.json", """
            {
              "dev": {
                "catalog": {
                  "clientId": "catalog-client",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.dev.example.test"
                    }
                  ]
                }
              }
            }
            """);

            var logger = new TestLogger<ConfigurationService>();
            var service = new ConfigurationService(logger, new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()));
            var client = new TestConfigurationClient();

            // Act by passing blank optional arguments, which should suppress additional seeding.
            await service.SeedConfigurationAsync(
                AddsEnvironment.Development,
                client,
                "CatalogService",
                configFilePath,
                servicesFilePath,
                string.Empty,
                string.Empty,
                CancellationToken.None);

            // Assert that only the sentinel, flattened configuration, and external service definition were written.
            Assert.Equal(3, client.Writes.Count);
            Assert.DoesNotContain(client.Writes, write => write.Setting.Key.StartsWith("additional", StringComparison.Ordinal));
        }

        /// <summary>
        /// Verifies that a transient <see cref="HttpRequestException"/> triggers one retry and then allows the write to complete.
        /// </summary>
        [Fact]
        public async Task SeedConfigurationAsync_WhenHttpRequestExceptionOccurs_ShouldRetryAndEventuallySucceed()
        {
            // Arrange a client that fails only on the first write attempt so the retry branch can recover.
            using var directory = new TemporaryDirectory();
            var configFilePath = directory.CreateFile("config.json", """
            {
              "dev": {
                "feature": {
                  "enabled": true
                }
              }
            }
            """);
            var servicesFilePath = directory.CreateFile("services.json", """
            {
              "dev": {
                "catalog": {
                  "clientId": "catalog-client",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.dev.example.test"
                    }
                  ]
                }
              }
            }
            """);

            var logger = new TestLogger<ConfigurationService>();
            var service = new ConfigurationService(logger, new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()));
            var attemptCount = 0;
            var client = new TestConfigurationClient((setting, _, _) =>
            {
                // Fail the first attempt only so the service must retry once before continuing normally.
                attemptCount++;
                return attemptCount == 1
                    ? Task.FromException<Response<ConfigurationSetting>>(new HttpRequestException("temporary network issue"))
                    : Task.FromResult(Response.FromValue(setting, new TestResponse(200, "OK")));
            });

            // Act by running the seeding path that will experience one transient network failure.
            await service.SeedConfigurationAsync(
                AddsEnvironment.Development,
                client,
                "CatalogService",
                configFilePath,
                servicesFilePath,
                CancellationToken.None);

            // Assert that the first failing sentinel write was retried and the remaining writes still completed.
            Assert.Equal(4, client.Writes.Count);
            Assert.Equal(1, logger.Entries.Count(entry => entry.LogLevel == LogLevel.Warning));
        }

        /// <summary>
        /// Verifies that retryable Azure App Configuration failures are retried and then complete successfully once the service responds.
        /// </summary>
        [Fact]
        public async Task SeedConfigurationAsync_WhenRequestFailedExceptionIsRetryable_ShouldRetryAndEventuallySucceed()
        {
            // Arrange a client that returns one retryable service-unavailable failure before succeeding.
            using var directory = new TemporaryDirectory();
            var configFilePath = directory.CreateFile("config.json", """
            {
              "dev": {
                "feature": {
                  "enabled": true
                }
              }
            }
            """);
            var servicesFilePath = directory.CreateFile("services.json", """
            {
              "dev": {
                "catalog": {
                  "clientId": "catalog-client",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.dev.example.test"
                    }
                  ]
                }
              }
            }
            """);

            var logger = new TestLogger<ConfigurationService>();
            var service = new ConfigurationService(logger, new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()));
            var attemptCount = 0;
            var client = new TestConfigurationClient((setting, _, _) =>
            {
                // Return a retryable 503 once so the Azure-specific transient-path logic is exercised.
                attemptCount++;
                return attemptCount == 1
                    ? Task.FromException<Response<ConfigurationSetting>>(new RequestFailedException(503, "temporarily unavailable"))
                    : Task.FromResult(Response.FromValue(setting, new TestResponse(200, "OK")));
            });

            // Act by executing the same minimal seeding flow against the transient-failure client.
            await service.SeedConfigurationAsync(
                AddsEnvironment.Development,
                client,
                "CatalogService",
                configFilePath,
                servicesFilePath,
                CancellationToken.None);

            // Assert that the transient failure produced one warning and the operation recovered on retry.
            Assert.Equal(4, client.Writes.Count);
            Assert.Equal(1, logger.Entries.Count(entry => entry.LogLevel == LogLevel.Warning));
        }

        /// <summary>
        /// Verifies that an operation timeout surfaces as <see cref="TaskCanceledException"/>, is treated as transient, and succeeds on the next attempt.
        /// </summary>
        [Fact]
        public async Task SeedConfigurationAsync_WhenOperationTimesOut_ShouldRetryAndEventuallySucceed()
        {
            // Arrange a client that hangs on the first attempt until the service's per-attempt timeout cancels it.
            using var directory = new TemporaryDirectory();
            var configFilePath = directory.CreateFile("config.json", """
            {
              "dev": {
                "feature": {
                  "enabled": true
                }
              }
            }
            """);
            var servicesFilePath = directory.CreateFile("services.json", """
            {
              "dev": {
                "catalog": {
                  "clientId": "catalog-client",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.dev.example.test"
                    }
                  ]
                }
              }
            }
            """);

            var logger = new TestLogger<ConfigurationService>();
            var service = new ConfigurationService(logger, new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()));
            var attemptCount = 0;
            var client = new TestConfigurationClient(async (setting, _, cancellationToken) =>
            {
                // Delay indefinitely on the first attempt so the service's internal timeout cancels the linked token and triggers retry logic.
                attemptCount++;
                if (attemptCount == 1)
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }

                return Response.FromValue(setting, new TestResponse(200, "OK"));
            });

            // Act by executing the seeding flow that will time out once and then recover.
            await service.SeedConfigurationAsync(
                AddsEnvironment.Development,
                client,
                "CatalogService",
                configFilePath,
                servicesFilePath,
                CancellationToken.None);

            // Assert that the timeout path retried once and still completed all expected writes.
            Assert.Equal(4, client.Writes.Count);
            Assert.Equal(1, logger.Entries.Count(entry => entry.LogLevel == LogLevel.Warning));
        }

        /// <summary>
        /// Verifies that retryable failures stop after the configured maximum number of attempts and rethrow the final exception.
        /// </summary>
        [Fact]
        public async Task SeedConfigurationAsync_WhenRetryableFailurePersists_ShouldThrowAfterMaxAttempts()
        {
            // Arrange a client that always returns a retryable failure so the service exhausts its retry budget on the first setting.
            using var directory = new TemporaryDirectory();
            var configFilePath = directory.CreateFile("config.json", """
            {
              "dev": {
                "feature": {
                  "enabled": true
                }
              }
            }
            """);
            var servicesFilePath = directory.CreateFile("services.json", """
            {
              "dev": {
                "catalog": {
                  "clientId": "catalog-client",
                  "endpoints": [
                    {
                      "tag": "",
                      "url": "https://catalog.dev.example.test"
                    }
                  ]
                }
              }
            }
            """);

            var logger = new TestLogger<ConfigurationService>();
            var service = new ConfigurationService(logger, new AdditionalConfigurationSeeder(new TestLogger<AdditionalConfigurationSeeder>()));
            var client = new TestConfigurationClient((_, _, _) => Task.FromException<Response<ConfigurationSetting>>(new RequestFailedException(503, "still unavailable")));

            // Act by executing the seeding flow and capturing the terminal failure after the retry budget is exhausted.
            var exception = await Assert.ThrowsAsync<RequestFailedException>(() => service.SeedConfigurationAsync(
                AddsEnvironment.Development,
                client,
                "CatalogService",
                configFilePath,
                servicesFilePath,
                CancellationToken.None));

            // Assert that the same retryable failure surfaced after all eight attempts of the first write were consumed.
            Assert.Equal(503, exception.Status);
            Assert.Equal(8, client.Writes.Count);
            Assert.Equal(7, logger.Entries.Count(entry => entry.LogLevel == LogLevel.Warning));
        }
    }
}
