namespace UKHO.Workbench.Output
{
    /// <summary>
    /// Represents shell-owned session state for the Workbench output panel without coupling UI state to immutable output entries.
    /// </summary>
    public sealed record OutputPanelState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutputPanelState"/> type.
        /// </summary>
        /// <param name="isVisible">A value indicating whether the output panel is currently visible in the shell layout.</param>
        /// <param name="centerPaneHeight">The authored grid-track height used for the centre working area while the output panel is visible.</param>
        /// <param name="outputPaneHeight">The authored grid-track height used for the output panel while it is visible.</param>
        /// <param name="isAutoScrollEnabled">A value indicating whether new output should automatically scroll the viewport to the newest entry.</param>
        /// <param name="isWordWrapEnabled">A value indicating whether output rows should wrap long content instead of relying on horizontal scrolling.</param>
        /// <param name="hiddenUnseenLevel">The most severe unseen level accumulated while the panel was hidden, if any.</param>
        /// <param name="expandedEntryIds">The identifiers of output rows whose details are currently expanded for the active session view.</param>
        public OutputPanelState(
            bool isVisible,
            string centerPaneHeight,
            string outputPaneHeight,
            bool isAutoScrollEnabled = true,
            bool isWordWrapEnabled = false,
            OutputLevel? hiddenUnseenLevel = null,
            IReadOnlyList<string>? expandedEntryIds = null)
        {
            // The shell keeps layout, toolbar, and row-expansion tokens together so the UI can restore one coherent session state after any shell refresh.
            ArgumentException.ThrowIfNullOrWhiteSpace(centerPaneHeight);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputPaneHeight);

            var expandedEntryIdentifiers = expandedEntryIds?.ToArray() ?? [];
            if (expandedEntryIdentifiers.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Expanded entry identifiers must not contain null, empty, or whitespace values.", nameof(expandedEntryIds));
            }

            IsVisible = isVisible;
            CenterPaneHeight = centerPaneHeight;
            OutputPaneHeight = outputPaneHeight;
            IsAutoScrollEnabled = isAutoScrollEnabled;
            IsWordWrapEnabled = isWordWrapEnabled;
            HiddenUnseenLevel = hiddenUnseenLevel;
            ExpandedEntryIds = expandedEntryIdentifiers;
        }

        /// <summary>
        /// Gets a value indicating whether the output panel is currently visible.
        /// </summary>
        public bool IsVisible { get; init; }

        /// <summary>
        /// Gets the grid-track height token applied to the centre working area while the output panel is visible.
        /// </summary>
        public string CenterPaneHeight { get; init; }

        /// <summary>
        /// Gets the grid-track height token applied to the output panel while it is visible.
        /// </summary>
        public string OutputPaneHeight { get; init; }

        /// <summary>
        /// Gets a value indicating whether new output should automatically keep the viewport pinned to the newest retained entry.
        /// </summary>
        public bool IsAutoScrollEnabled { get; init; }

        /// <summary>
        /// Gets a value indicating whether long output lines should wrap instead of requiring horizontal scrolling.
        /// </summary>
        public bool IsWordWrapEnabled { get; init; }

        /// <summary>
        /// Gets the most severe unseen output level accumulated while the panel remained hidden.
        /// </summary>
        public OutputLevel? HiddenUnseenLevel { get; init; }

        /// <summary>
        /// Gets the identifiers of output rows whose detail regions are expanded for the current session view.
        /// </summary>
        public IReadOnlyList<string> ExpandedEntryIds { get; init; }
    }
}
