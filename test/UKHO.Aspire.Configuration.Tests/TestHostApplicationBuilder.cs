using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UKHO.Aspire.Configuration.Tests
{
    /// <summary>
    /// Provides a minimal <see cref="IHostApplicationBuilder"/> implementation for exercising configuration extensions without pulling in the full host builder package.
    /// </summary>
    internal sealed class TestHostApplicationBuilder : IHostApplicationBuilder
    {
        /// <summary>
        /// Initialises the fake host builder with empty configuration, service, logging, and metrics collections.
        /// </summary>
        public TestHostApplicationBuilder()
        {
            // Use the standard mutable configuration manager because the production extension mutates sources directly.
            Configuration = new ConfigurationManager();

            // Provide the minimal host abstractions required by the extension under test.
            Services = new ServiceCollection();
            Environment = new TestHostEnvironment();
            Properties = new Dictionary<object, object>();
            Logging = new TestLoggingBuilder(Services);
            Metrics = new TestMetricsBuilder(Services);
        }

        /// <summary>
        /// Gets a shared property bag that mirrors the state bag available on the real host builder.
        /// </summary>
        public IDictionary<object, object> Properties { get; }

        /// <summary>
        /// Gets the mutable configuration manager used by the tests.
        /// </summary>
        public ConfigurationManager Configuration { get; }

        /// <summary>
        /// Exposes the mutable configuration manager through the interface contract expected by the production extension.
        /// </summary>
        IConfigurationManager IHostApplicationBuilder.Configuration => Configuration;

        /// <summary>
        /// Gets the fake host environment instance.
        /// </summary>
        public IHostEnvironment Environment { get; }

        /// <summary>
        /// Gets the fake logging builder instance.
        /// </summary>
        public ILoggingBuilder Logging { get; }

        /// <summary>
        /// Gets the fake metrics builder instance.
        /// </summary>
        public IMetricsBuilder Metrics { get; }

        /// <summary>
        /// Gets the service collection populated by the production extension method.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Accepts container-configuration requests while intentionally doing no extra work because these tests never build a full host.
        /// </summary>
        /// <typeparam name="TContainerBuilder">The container builder type selected by the caller.</typeparam>
        /// <param name="factory">The factory requested by the caller.</param>
        /// <param name="configure">The optional callback that would configure the container builder.</param>
        public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure) where TContainerBuilder : notnull
        {
            // The production code under test never builds the container, so storing the callback would add no value here.
        }
    }
}
