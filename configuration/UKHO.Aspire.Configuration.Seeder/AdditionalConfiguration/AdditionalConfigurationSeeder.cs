using System.Net.Mime;
using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Logging;

namespace UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration
{
    internal class AdditionalConfigurationSeeder
    {
        private readonly ILogger<AdditionalConfigurationSeeder> _logger;

        public AdditionalConfigurationSeeder(ILogger<AdditionalConfigurationSeeder> logger)
        {
            _logger = logger;
        }

        public async Task SeedAsync(ConfigurationClient configurationClient, string label, string rootPath, string prefix, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(configurationClient);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);
            ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

            if (!Directory.Exists(rootPath))
            {
                _logger.LogWarning("Additional configuration path does not exist: {Path}. Skipping.", rootPath);
                return;
            }

            var files = AdditionalConfigurationFileEnumerator.EnumerateFiles(rootPath);

            foreach (var filePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativeSegments = AdditionalConfigurationFileEnumerator.GetRelativePathSegments(rootPath, filePath);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                // Key format: {prefix}:{path0}:...:{pathN}:{filenameWithoutExtension}
                var key = AdditionalConfigurationKeyBuilder.Build(prefix, relativeSegments, fileNameWithoutExtension);

                var value = await File.ReadAllTextAsync(filePath, cancellationToken);

                var setting = new ConfigurationSetting(key, value, label)
                {
                    ContentType = MediaTypeNames.Text.Plain
                };

                _logger.LogDebug("Writing additional configuration setting {Key} (Label={Label}) from {FilePath}.", key, label, filePath);

                await configurationClient.SetConfigurationSettingAsync(setting, false, cancellationToken);
            }
        }
    }
}
