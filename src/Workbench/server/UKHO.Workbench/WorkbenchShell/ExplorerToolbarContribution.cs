namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Describes an action rendered in the explorer-toolbar surface at the top of the explorer pane.
    /// </summary>
    public class ExplorerToolbarContribution
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorerToolbarContribution"/> class.
        /// </summary>
        /// <param name="id">The stable contribution identifier used for diagnostics and rendering keys.</param>
        /// <param name="displayName">The label shown for the explorer-toolbar action.</param>
        /// <param name="commandId">The command invoked when the explorer-toolbar action is selected.</param>
        /// <param name="icon">The optional icon key shown by the explorer-toolbar button.</param>
        /// <param name="ownerExplorerId">The optional explorer identifier that owns the contribution when it is explorer-specific.</param>
        /// <param name="order">The relative display order used when composing explorer-toolbar actions.</param>
        public ExplorerToolbarContribution(
            string id,
            string displayName,
            string commandId,
            string? icon = null,
            string? ownerExplorerId = null,
            int order = 0)
        {
            // Explorer-toolbar buttons stay command-driven so the shell can keep left-pane actions on the same shared routing path as every other surface.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

            Id = id;
            DisplayName = displayName;
            CommandId = commandId;
            Icon = icon;
            OwnerExplorerId = ownerExplorerId;
            Order = order;
        }

        /// <summary>
        /// Gets the stable contribution identifier used for diagnostics and rendering keys.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the label shown for the explorer-toolbar action.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the command invoked when the explorer-toolbar action is selected.
        /// </summary>
        public string CommandId { get; }

        /// <summary>
        /// Gets the optional icon key shown by the explorer-toolbar button.
        /// </summary>
        public string? Icon { get; }

        /// <summary>
        /// Gets the optional explorer identifier that owns the contribution when it is explorer-specific.
        /// </summary>
        public string? OwnerExplorerId { get; }

        /// <summary>
        /// Gets the relative display order used when composing explorer-toolbar actions.
        /// </summary>
        public int Order { get; }
    }
}
