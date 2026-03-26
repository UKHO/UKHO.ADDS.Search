using Xunit;

namespace UKHO.Workbench.Infrastructure.Tests
{
    /// <summary>
    /// Provides the initial compile-only placeholder coverage for the server-side Workbench infrastructure project.
    /// </summary>
    public class PlaceholderTests
    {
        /// <summary>
        /// Confirms the mirrored placeholder test project is discovered and can execute successfully.
        /// </summary>
        [Fact]
        public void Placeholder_WhenExecuted_ShouldPass()
        {
            // Keep the first work-package baseline intentionally trivial until the infrastructure layer contains real integrations.
            Assert.True(true);
        }
    }
}
