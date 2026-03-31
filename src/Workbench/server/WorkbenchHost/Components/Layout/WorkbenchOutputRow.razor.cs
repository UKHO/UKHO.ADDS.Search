using Microsoft.AspNetCore.Components;
using UKHO.Workbench.Output;

namespace WorkbenchHost.Components.Layout
{
    /// <summary>
    /// Renders one compact structured output row for the Workbench shell output surface.
    /// </summary>
    public partial class WorkbenchOutputRow : ComponentBase
    {
        /// <summary>
        /// Gets or sets the immutable output entry that should be rendered by the row.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public OutputEntry Entry { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the current row is expanded.
        /// </summary>
        [Parameter]
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shell-wide output panel wrap mode is enabled.
        /// </summary>
        [Parameter]
        public bool IsWordWrapEnabled { get; set; }

        /// <summary>
        /// Gets or sets the callback raised when the disclosure button is activated for this row.
        /// </summary>
        [Parameter]
        public EventCallback<OutputEntry> ExpansionToggled { get; set; }

        /// <summary>
        /// Gets or sets the callback raised when the row copy action is requested.
        /// </summary>
        [Parameter]
        public EventCallback<OutputEntry> CopyRequested { get; set; }

        /// <summary>
        /// Gets the disclosure icon that matches the current expansion state.
        /// </summary>
        private string DisclosureIcon => IsExpanded ? "expand_more" : "chevron_right";

        /// <summary>
        /// Gets the compact local-time timestamp rendered in the collapsed row summary.
        /// </summary>
        private string FormattedTimestamp => Entry.TimestampUtc.ToLocalTime().ToString("HH:mm:ss");

        /// <summary>
        /// Gets the stable DOM identifier used to associate the disclosure button with the details region.
        /// </summary>
        private string DetailsRegionId => $"output-entry-details-{Entry.Id}";

        /// <summary>
        /// Gets the tooltip text used for the subtle visual severity marker.
        /// </summary>
        private string LevelMarkerTooltip => $"{Entry.Level} output";

        /// <summary>
        /// Validates the required row parameters before the component renders.
        /// </summary>
        protected override void OnParametersSet()
        {
            // The row cannot render useful shell output without an immutable entry payload.
            ArgumentNullException.ThrowIfNull(Entry);

            base.OnParametersSet();
        }

        /// <summary>
        /// Raises the row-specific expansion callback for the disclosure control.
        /// </summary>
        /// <returns>A task that completes when the parent layout has processed the expansion request.</returns>
        private Task ToggleExpansionAsync()
        {
            // Expansion remains parent-owned so the shared output panel state continues to track all expanded row identifiers centrally.
            return ExpansionToggled.InvokeAsync(Entry);
        }

        /// <summary>
        /// Raises the row-specific copy callback for the currently rendered entry.
        /// </summary>
        /// <returns>A task that completes when the parent layout has processed the copy request.</returns>
        private Task CopyAsync()
        {
            // Copy behavior remains parent-owned so the shell can continue to use its existing clipboard interop helper.
            return CopyRequested.InvokeAsync(Entry);
        }
    }
}
