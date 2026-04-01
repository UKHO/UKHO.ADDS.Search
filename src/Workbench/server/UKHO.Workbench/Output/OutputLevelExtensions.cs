namespace UKHO.Workbench.Output
{
    /// <summary>
    /// Provides shared severity-ordering helpers for Workbench output levels.
    /// </summary>
    public static class OutputLevelExtensions
    {
        /// <summary>
        /// Converts one output level into a stable severity rank used for comparisons.
        /// </summary>
        /// <param name="level">The output level whose severity rank should be returned.</param>
        /// <returns>An integer rank where larger values represent more severe output.</returns>
        public static int ToSeverityRank(this OutputLevel level)
        {
            // The ranking is explicit so output filtering and hidden-indicator rules do not depend on enum numeric values remaining unchanged forever.
            return level switch
            {
                OutputLevel.Debug => 0,
                OutputLevel.Info => 1,
                OutputLevel.Warning => 2,
                OutputLevel.Error => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, "The output level is not supported.")
            };
        }

        /// <summary>
        /// Determines whether one output level is visible at or above the supplied minimum visible threshold.
        /// </summary>
        /// <param name="level">The output level that is being evaluated for visibility.</param>
        /// <param name="minimumVisibleLevel">The minimum output level that the current view should show.</param>
        /// <returns><see langword="true"/> when the supplied level meets or exceeds the visible threshold; otherwise, <see langword="false"/>.</returns>
        public static bool IsVisibleAtOrAbove(this OutputLevel level, OutputLevel minimumVisibleLevel)
        {
            // Visibility checks centralize the shared severity comparison logic so the host and service stay aligned on which entries count as visible.
            return level.ToSeverityRank() >= minimumVisibleLevel.ToSeverityRank();
        }
    }
}
