using Xunit;

namespace UKHO.Workbench.Common.Tests
{
    /// <summary>
    /// Provides the initial compile-only placeholder coverage for the shared Workbench common project shell.
    /// </summary>
    public class PlaceholderTests
    {
        /// <summary>
        /// Confirms the mirrored placeholder test project is discovered and can execute successfully.
        /// </summary>
        [Fact]
        public void Placeholder_WhenExecuted_ShouldPass()
        {
            // Keep the first work-package baseline intentionally trivial until the shared project contains real behavior.
            Assert.True(true);
        }
    }
}
