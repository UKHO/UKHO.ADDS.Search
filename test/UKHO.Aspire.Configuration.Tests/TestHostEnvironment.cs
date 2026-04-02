using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace UKHO.Aspire.Configuration.Tests
{
    /// <summary>
    /// Supplies deterministic host-environment values for configuration-extension unit tests.
    /// </summary>
    internal sealed class TestHostEnvironment : IHostEnvironment
    {
        /// <summary>
        /// Initialises the environment with stable defaults that are sufficient for configuration registration tests.
        /// </summary>
        public TestHostEnvironment()
        {
            // Use simple deterministic defaults because the configuration extension does not depend on real host paths.
            ApplicationName = "UKHO.Aspire.Configuration.Tests";
            EnvironmentName = Environments.Development;
            ContentRootPath = AppContext.BaseDirectory;
            ContentRootFileProvider = new NullFileProvider();
        }

        /// <summary>
        /// Gets or sets the application name exposed to the host pipeline.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the logical environment name exposed to the host pipeline.
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// Gets or sets the content-root path exposed to the host pipeline.
        /// </summary>
        public string ContentRootPath { get; set; }

        /// <summary>
        /// Gets or sets the content-root file provider exposed to the host pipeline.
        /// </summary>
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
