using Azure.Data.AppConfiguration;

namespace UKHO.Aspire.Configuration.Seeder.Tests.TestSupport
{
    /// <summary>
    /// Records one call made to <see cref="TestConfigurationClient"/> so tests can assert the written setting and call metadata.
    /// </summary>
    internal sealed record TestConfigurationWrite
    {
        /// <summary>
        /// Gets or sets the configuration setting passed to the client call.
        /// </summary>
        public ConfigurationSetting Setting { get; init; } = new(string.Empty, string.Empty);

        /// <summary>
        /// Gets or sets a value indicating whether the write required the target setting to be unchanged.
        /// </summary>
        public bool OnlyIfUnchanged { get; init; }

        /// <summary>
        /// Gets or sets the cancellation token forwarded to the client call.
        /// </summary>
        public CancellationToken CancellationToken { get; init; }
    }
}
