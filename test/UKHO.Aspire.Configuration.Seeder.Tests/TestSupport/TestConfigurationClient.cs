using Azure;
using Azure.Data.AppConfiguration;

namespace UKHO.Aspire.Configuration.Seeder.Tests.TestSupport
{
    /// <summary>
    /// Captures Azure App Configuration write requests in memory while still satisfying the concrete <see cref="ConfigurationClient"/> dependency used by the seeder code.
    /// </summary>
    internal sealed class TestConfigurationClient : ConfigurationClient
    {
        private readonly Func<ConfigurationSetting, bool, CancellationToken, Task<Response<ConfigurationSetting>>>? _writeHandler;

        /// <summary>
        /// Initializes a new fake configuration client.
        /// </summary>
        /// <param name="writeHandler">An optional callback that can customise the result or failure produced for each write attempt.</param>
        public TestConfigurationClient(Func<ConfigurationSetting, bool, CancellationToken, Task<Response<ConfigurationSetting>>>? writeHandler = null)
        {
            // Store the optional callback so future tests can simulate retries or transient failures without changing production code.
            _writeHandler = writeHandler;
        }

        /// <summary>
        /// Gets the writes captured in the order they were issued by the code under test.
        /// </summary>
        public List<TestConfigurationWrite> Writes { get; } = [];

        /// <summary>
        /// Records a configuration write request and returns a successful fake response unless a custom handler is supplied.
        /// </summary>
        /// <param name="setting">The configuration setting being written.</param>
        /// <param name="onlyIfUnchanged">Indicates whether the write should be conditional on the target being unchanged.</param>
        /// <param name="cancellationToken">The cancellation token associated with the write operation.</param>
        /// <returns>A task containing the fake response returned to the caller.</returns>
        public override Task<Response<ConfigurationSetting>> SetConfigurationSettingAsync(
            ConfigurationSetting setting,
            bool onlyIfUnchanged = false,
            CancellationToken cancellationToken = default)
        {
            // Copy the supplied setting so later assertions are insulated from any caller-side mutation after the write returns.
            var capturedSetting = new ConfigurationSetting(setting.Key, setting.Value, setting.Label)
            {
                ContentType = setting.ContentType
            };

            Writes.Add(new TestConfigurationWrite
            {
                Setting = capturedSetting,
                OnlyIfUnchanged = onlyIfUnchanged,
                CancellationToken = cancellationToken
            });

            // Allow a custom handler to simulate service responses or failures for richer orchestration tests.
            if (_writeHandler is not null)
            {
                return _writeHandler(capturedSetting, onlyIfUnchanged, cancellationToken);
            }

            return Task.FromResult(Response.FromValue(capturedSetting, new TestResponse(200, "OK")));
        }
    }
}
