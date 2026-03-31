namespace UKHO.Workbench.Output
{
    /// <summary>
    /// Provides shell-wide append, clear, and change-notification behavior for the current Workbench output stream.
    /// </summary>
    public interface IWorkbenchOutputService
    {
        /// <summary>
        /// Occurs when the visible output-entry collection has changed.
        /// </summary>
        event EventHandler? EntriesChanged;

        /// <summary>
        /// Occurs when the output-panel session state has changed.
        /// </summary>
        event EventHandler? PanelStateChanged;

        /// <summary>
        /// Gets the current output entries in chronological order.
        /// </summary>
        IReadOnlyList<OutputEntry> Entries { get; }

        /// <summary>
        /// Gets the current shell-owned output-panel session state.
        /// </summary>
        OutputPanelState PanelState { get; }

        /// <summary>
        /// Creates and appends a new output entry to the shell-wide stream.
        /// </summary>
        /// <param name="level">The severity or intent level assigned to the new entry.</param>
        /// <param name="source">The subsystem, tool, or shell area that is emitting the entry.</param>
        /// <param name="summary">The compact summary text that should be rendered in the stream.</param>
        /// <param name="details">Optional longer diagnostic detail associated with the summary.</param>
        /// <param name="eventCode">Optional stable event code that callers can use to correlate repeated messages.</param>
        /// <returns>The immutable entry instance that was appended to the stream.</returns>
        OutputEntry Write(
            OutputLevel level,
            string source,
            string summary,
            string? details = null,
            string? eventCode = null);

        /// <summary>
        /// Appends an already-created immutable output entry to the shell-wide stream.
        /// </summary>
        /// <param name="entry">The immutable entry that should be appended to the stream.</param>
        void Write(OutputEntry entry);

        /// <summary>
        /// Removes all entries from the current shell-wide stream.
        /// </summary>
        void Clear();

        /// <summary>
        /// Updates whether the shell output panel is currently visible.
        /// </summary>
        /// <param name="isVisible"><see langword="true"/> to mark the panel visible; otherwise, <see langword="false"/>.</param>
        void SetPanelVisibility(bool isVisible);

        /// <summary>
        /// Updates the current-session authored pane heights used by the shell layout.
        /// </summary>
        /// <param name="centerPaneHeight">The grid-track token that should be used for the centre working area.</param>
        /// <param name="outputPaneHeight">The grid-track token that should be used for the output panel.</param>
        void SetPaneHeights(string centerPaneHeight, string outputPaneHeight);

        /// <summary>
        /// Updates whether the panel should automatically keep the viewport pinned to the newest output entry.
        /// </summary>
        /// <param name="isEnabled"><see langword="true"/> to enable automatic scrolling; otherwise, <see langword="false"/>.</param>
        void SetAutoScrollEnabled(bool isEnabled);

        /// <summary>
        /// Updates whether the panel should wrap long output lines.
        /// </summary>
        /// <param name="isEnabled"><see langword="true"/> to enable wrapping; otherwise, <see langword="false"/>.</param>
        void SetWordWrapEnabled(bool isEnabled);

        /// <summary>
        /// Replaces the current set of expanded output-entry identifiers.
        /// </summary>
        /// <param name="expandedEntryIds">The identifiers of entries whose details are currently expanded.</param>
        void SetExpandedEntryIds(IReadOnlyList<string> expandedEntryIds);
    }
}
