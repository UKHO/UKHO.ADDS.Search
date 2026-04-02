using Microsoft.AspNetCore.Components;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace WorkbenchHost.Components.Pages
{
    /// <summary>
    /// Hosts the currently active Workbench tool inside the shell's central working region.
    /// </summary>
    public partial class Index : IDisposable
    {
        private static readonly IDictionary<string, object> EmptyParameters = new Dictionary<string, object>();

        [Inject]
        private WorkbenchShellManager ShellManager { get; set; } = null!;

        /// <summary>
        /// Gets the ordered tabs currently open in the shell.
        /// </summary>
        private IReadOnlyList<WorkbenchTab> OpenTabs => ShellManager.OpenTabs;

        /// <summary>
        /// Returns the dynamic component parameters that should be supplied to the supplied tool component.
        /// </summary>
        /// <param name="toolInstance">The tool instance whose component parameters should be built.</param>
        /// <returns>The dynamic component parameters for the supplied tool instance.</returns>
        private IDictionary<string, object> GetToolParameters(ToolInstance toolInstance)
        {
            // Only components that explicitly declare a ToolContext parameter receive it so host-owned tools without that contract continue to render safely.
            ArgumentNullException.ThrowIfNull(toolInstance);

            var componentType = toolInstance.Definition.ComponentType;
            var toolContextParameter = componentType.GetProperty("ToolContext");
            if (toolContextParameter is null || toolContextParameter.PropertyType != typeof(ToolContext))
            {
                return EmptyParameters;
            }

            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ToolContext"] = toolInstance.Context
            };
        }

        /// <summary>
        /// Determines whether the supplied tab is the currently active tab.
        /// </summary>
        /// <param name="openTab">The open tab to compare.</param>
        /// <returns><see langword="true"/> when the supplied tab is active; otherwise, <see langword="false"/>.</returns>
        private bool IsTabActive(WorkbenchTab openTab)
        {
            // The active-tab check keeps the rendered panes aligned with the shell-state active tab while still mounting all open tabs.
            ArgumentNullException.ThrowIfNull(openTab);

            return string.Equals(ShellManager.State.ActiveTab?.Id, openTab.Id, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns the CSS class used for a hosted tab pane.
        /// </summary>
        /// <param name="openTab">The open tab whose pane is being rendered.</param>
        /// <returns>The CSS class string for the pane.</returns>
        private string GetTabPaneCss(WorkbenchTab openTab)
        {
            // Inactive panes stay in the render tree so component state is preserved while the active pane remains the only visible surface.
            return IsTabActive(openTab)
                ? "workbench-tool-surface-pane workbench-tool-surface-pane--active"
                : "workbench-tool-surface-pane workbench-tool-surface-pane--inactive";
        }

        /// <summary>
        /// Subscribes to shell state changes so the hosted tool surface refreshes when focus moves.
        /// </summary>
        protected override void OnInitialized()
        {
            // The hosted tool surface reacts to shell-state changes so explorer activation immediately updates the center region.
            ShellManager.StateChanged += HandleShellStateChanged;
            base.OnInitialized();
        }

        /// <summary>
        /// Responds to shell state changes by scheduling a component re-render.
        /// </summary>
        /// <param name="sender">The object that raised the state change event.</param>
        /// <param name="e">The event arguments for the notification.</param>
        private void HandleShellStateChanged(object? sender, EventArgs e)
        {
            // State updates may be triggered outside the current render cycle, so the page marshals the refresh back onto the renderer.
            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Unsubscribes from shell state notifications when the page is disposed.
        /// </summary>
        public void Dispose()
        {
            // The page releases its subscription so stale component instances are not retained after navigation or reconnection.
            ShellManager.StateChanged -= HandleShellStateChanged;
        }
    }
}