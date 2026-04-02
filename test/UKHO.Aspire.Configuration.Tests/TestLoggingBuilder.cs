using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UKHO.Aspire.Configuration.Tests
{
    /// <summary>
    /// Provides the minimal logging-builder surface required by <see cref="IHostApplicationBuilder"/> for these tests.
    /// </summary>
    internal sealed class TestLoggingBuilder : ILoggingBuilder
    {
        /// <summary>
        /// Initialises the fake logging builder around the shared service collection.
        /// </summary>
        /// <param name="services">The service collection that should receive any logging registrations.</param>
        public TestLoggingBuilder(IServiceCollection services)
        {
            // Reuse the shared service collection so the fake builder behaves like the real host builder surface.
            Services = services;
        }

        /// <summary>
        /// Gets the service collection used for logging registrations.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
