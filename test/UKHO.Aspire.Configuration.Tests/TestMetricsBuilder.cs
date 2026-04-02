using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace UKHO.Aspire.Configuration.Tests
{
    /// <summary>
    /// Provides the minimal metrics-builder surface required by <see cref="Microsoft.Extensions.Hosting.IHostApplicationBuilder"/> for these tests.
    /// </summary>
    internal sealed class TestMetricsBuilder : IMetricsBuilder
    {
        /// <summary>
        /// Initialises the fake metrics builder around the shared service collection.
        /// </summary>
        /// <param name="services">The service collection that should receive any metrics registrations.</param>
        public TestMetricsBuilder(IServiceCollection services)
        {
            // Reuse the shared service collection so any metrics registrations are observable in the same container.
            Services = services;
        }

        /// <summary>
        /// Gets the service collection used for metrics registrations.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
