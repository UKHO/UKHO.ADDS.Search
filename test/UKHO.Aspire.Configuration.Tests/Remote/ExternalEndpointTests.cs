using Shouldly;
using UKHO.Aspire.Configuration.Remote;
using Xunit;

namespace UKHO.Aspire.Configuration.Tests.Remote
{
    /// <summary>
    /// Verifies the small behaviour surface exposed directly by <see cref="ExternalEndpoint"/>.
    /// </summary>
    public sealed class ExternalEndpointTests
    {
        /// <summary>
        /// Verifies that the default scope is derived from the configured client identifier.
        /// </summary>
        [Fact]
        public void GetDefaultScope_WhenClientIdPresent_ShouldAppendDefaultSuffix()
        {
            // Create a concrete endpoint instance with a representative application client identifier.
            var endpoint = new ExternalEndpoint
            {
                ClientId = "api://catalogue-client",
                Host = EndpointHostSubstitution.None,
                Tag = string.Empty,
                Uri = new Uri("https://catalogue.test")
            };

            // The default scope should follow the standard client-id/.default pattern used by Azure clients.
            endpoint.GetDefaultScope().ShouldBe("api://catalogue-client/.default");
        }
    }
}
