namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Tracks the visible tab-strip window separately from the full logical open-tab order.
    /// </summary>
    public class WorkbenchTabStripState
    {
        /// <summary>
        /// Defines the first-implementation maximum number of tabs shown directly in the strip before overflow is required.
        /// </summary>
        public const int DefaultMaxVisibleTabCount = 4;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchTabStripState"/> class.
        /// </summary>
        /// <param name="maxVisibleTabCount">The bounded number of tabs that may remain directly visible in the strip at one time.</param>
        public WorkbenchTabStripState(int maxVisibleTabCount = DefaultMaxVisibleTabCount)
        {
            // The first overflow slice keeps presentation simple by using a fixed visible-window size instead of measuring available width.
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxVisibleTabCount);

            MaxVisibleTabCount = maxVisibleTabCount;
        }

        /// <summary>
        /// Gets the bounded maximum number of tabs that may remain directly visible in the strip.
        /// </summary>
        public int MaxVisibleTabCount { get; }

        /// <summary>
        /// Gets the zero-based index of the first tab currently visible in the tab strip window.
        /// </summary>
        public int VisibleStartIndex { get; private set; }

        /// <summary>
        /// Determines whether the supplied open-tab count exceeds the visible strip capacity.
        /// </summary>
        /// <param name="openTabCount">The total number of open tabs currently tracked by the shell.</param>
        /// <returns><see langword="true"/> when one or more tabs must be accessed through overflow; otherwise, <see langword="false"/>.</returns>
        public bool IsOverflowing(int openTabCount)
        {
            // Overflow is a simple capacity check because the first implementation uses a fixed visible-window size.
            return openTabCount > MaxVisibleTabCount;
        }

        /// <summary>
        /// Returns the contiguous slice of tabs that should be shown directly in the visible strip.
        /// </summary>
        /// <param name="openTabs">The full logical open-tab order owned by the shell.</param>
        /// <returns>The tabs that should remain visible in the strip for the current window position.</returns>
        public IReadOnlyList<WorkbenchTab> GetVisibleTabs(IReadOnlyList<WorkbenchTab> openTabs)
        {
            // The visible window is derived lazily from the full open order so overflow can preserve logical ordering without duplicating tab storage.
            ArgumentNullException.ThrowIfNull(openTabs);

            NormalizeVisibleStartIndex(openTabs.Count);
            return openTabs.Skip(VisibleStartIndex).Take(MaxVisibleTabCount).ToArray();
        }

        /// <summary>
        /// Adjusts the visible window just enough to ensure the specified tab can be seen directly in the strip.
        /// </summary>
        /// <param name="tabId">The stable tab identifier that must be brought into the visible strip.</param>
        /// <param name="openTabs">The full logical open-tab order owned by the shell.</param>
        public void EnsureTabVisible(string tabId, IReadOnlyList<WorkbenchTab> openTabs)
        {
            // Hidden-tab activation should not reorder the logical tab list, so the window shifts only by the minimum amount required to reveal the target tab.
            ArgumentException.ThrowIfNullOrWhiteSpace(tabId);
            ArgumentNullException.ThrowIfNull(openTabs);

            var tabIndex = openTabs
                .Select((openTab, index) => new { openTab.Id, Index = index })
                .Where(candidate => string.Equals(candidate.Id, tabId, StringComparison.Ordinal))
                .Select(candidate => candidate.Index)
                .DefaultIfEmpty(-1)
                .Single();

            if (tabIndex < 0)
            {
                throw new InvalidOperationException($"The Workbench tab '{tabId}' is not open.");
            }

            if (tabIndex < VisibleStartIndex)
            {
                VisibleStartIndex = tabIndex;
            }
            else if (tabIndex >= VisibleStartIndex + MaxVisibleTabCount)
            {
                VisibleStartIndex = tabIndex - MaxVisibleTabCount + 1;
            }

            NormalizeVisibleStartIndex(openTabs.Count);
        }

        /// <summary>
        /// Normalizes the visible window after tabs are opened or closed.
        /// </summary>
        /// <param name="openTabCount">The total number of open tabs currently tracked by the shell.</param>
        public void TrimToBounds(int openTabCount)
        {
            // Tab close operations can shorten the logical tab list, so the visible window must be clamped back into the new valid range.
            NormalizeVisibleStartIndex(openTabCount);
        }

        /// <summary>
        /// Clamps the visible-window start index into the valid range for the current tab count.
        /// </summary>
        /// <param name="openTabCount">The total number of open tabs currently tracked by the shell.</param>
        private void NormalizeVisibleStartIndex(int openTabCount)
        {
            // A zero or under-capacity tab count always resets the window to the beginning so the strip remains intuitive and deterministic.
            if (openTabCount <= MaxVisibleTabCount)
            {
                VisibleStartIndex = 0;
                return;
            }

            VisibleStartIndex = Math.Clamp(VisibleStartIndex, 0, openTabCount - MaxVisibleTabCount);
        }
    }
}
