using System.Reflection;
using Shouldly;
using UKHO.Search.Configuration;
using Xunit;

namespace UKHO.Search.Tests.Configuration
{
    /// <summary>
    /// Verifies that the shared service-name catalog only exposes identifiers that belong to the retained active workflow.
    /// </summary>
    public sealed class ServiceNamesTests
    {
        /// <summary>
        /// Verifies that the retired Studio and Theia service identifiers are no longer part of the shared service-name catalog.
        /// </summary>
        [Fact]
        public void ServiceNames_does_not_expose_retired_studio_service_identifiers()
        {
            // Inspect the public constant fields so the retained domain-facing service catalog cannot silently reintroduce Studio-only identifiers.
            var publicConstantFields = typeof(ServiceNames).GetFields(BindingFlags.Public | BindingFlags.Static);

            // Ensure the discontinued Studio API identifier has been removed from the shared active-workflow contract.
            publicConstantFields.Any(field => field.Name == "StudioApi").ShouldBeFalse();

            // Ensure the discontinued Studio shell identifier has been removed from the shared active-workflow contract.
            publicConstantFields.Any(field => field.Name == "StudioShell").ShouldBeFalse();
        }
    }
}
