using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace UKHO.Aspire.Configuration.Emulator.Tests.TestSupport
{
    /// <summary>
    /// Supplies deterministic authentication scheme configuration data to unit tests.
    /// </summary>
    internal sealed class TestAuthenticationConfigurationProvider : IAuthenticationConfigurationProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestAuthenticationConfigurationProvider"/> class.
        /// </summary>
        /// <param name="authenticationConfiguration">The configuration tree exposed to authentication option binders.</param>
        public TestAuthenticationConfigurationProvider(IConfiguration authenticationConfiguration)
        {
            // Preserve the supplied configuration tree so tests can simulate named scheme configuration sections.
            AuthenticationConfiguration = authenticationConfiguration;
        }

        /// <summary>
        /// Gets the authentication configuration tree exposed to the option binder under test.
        /// </summary>
        public IConfiguration AuthenticationConfiguration { get; }
    }
}
