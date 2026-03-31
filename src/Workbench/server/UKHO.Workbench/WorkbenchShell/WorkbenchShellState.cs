using UKHO.Workbench.Tools;

namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Stores the lightweight bootstrap shell state for explorers and hosted tools.
    /// </summary>
    public class WorkbenchShellState
    {
        private static readonly IReadOnlyList<WorkbenchShellRegion> BootstrapVisibleRegions =
        [
            WorkbenchShellRegion.MenuBar,
            WorkbenchShellRegion.ActivityRail,
            WorkbenchShellRegion.Explorer,
            WorkbenchShellRegion.ToolSurface,
            WorkbenchShellRegion.ActiveToolToolbar,
            WorkbenchShellRegion.StatusBar
        ];

        private readonly Dictionary<string, WorkbenchTab> _tabsById = new(StringComparer.Ordinal);
        private readonly List<WorkbenchTab> _openTabs = [];
        private readonly List<string> _mostRecentlyActiveTabIds = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchShellState"/> class.
        /// </summary>
        public WorkbenchShellState()
        {
            // The bootstrap shell starts with no open tabs so explorer interaction can drive the first tab activation explicitly.
            TabStrip = new WorkbenchTabStripState();
            VisibleRegions = BootstrapVisibleRegions;
            IsExplorerFocused = true;
        }

        /// <summary>
        /// Gets the overflow-aware tab-strip presentation state tracked by the shell.
        /// </summary>
        public WorkbenchTabStripState TabStrip { get; }

        /// <summary>
        /// Gets the fixed bootstrap shell regions that are currently visible.
        /// </summary>
        public IReadOnlyList<WorkbenchShellRegion> VisibleRegions { get; }

        /// <summary>
        /// Gets the active explorer identifier selected by the shell.
        /// </summary>
        public string? ActiveExplorerId { get; private set; }

        /// <summary>
        /// Gets the explorer item currently selected in the explorer pane, or <see langword="null"/> when no explorer item is selected.
        /// </summary>
        public string? SelectedExplorerItemId { get; private set; }

        /// <summary>
        /// Gets a value indicating whether explorer interaction currently owns focus in the shell.
        /// </summary>
        public bool IsExplorerFocused { get; private set; }

        /// <summary>
        /// Gets the currently active tab hosted by the shell, or <see langword="null"/> when the center surface is empty.
        /// </summary>
        public WorkbenchTab? ActiveTab { get; private set; }

        /// <summary>
        /// Gets the currently active tool instance hosted by the shell.
        /// </summary>
        public ToolInstance? ActiveTool => ActiveTab?.ToolInstance;

        /// <summary>
        /// Gets the ordered tabs currently open in the shell.
        /// </summary>
        public IReadOnlyList<WorkbenchTab> OpenTabs => _openTabs;

        /// <summary>
        /// Gets the contiguous subset of open tabs currently visible in the main tab strip.
        /// </summary>
        public IReadOnlyList<WorkbenchTab> VisibleTabs => TabStrip.GetVisibleTabs(_openTabs);

        /// <summary>
        /// Gets the tab identifiers in least-recently-active to most-recently-active order.
        /// </summary>
        public IReadOnlyList<string> MostRecentlyActiveTabIds => _mostRecentlyActiveTabIds;

        /// <summary>
        /// Gets the tool instances currently tracked by the open tabs.
        /// </summary>
        public IReadOnlyCollection<ToolInstance> ToolInstances => _openTabs.Select(openTab => openTab.ToolInstance).ToArray();

        /// <summary>
        /// Updates the active explorer tracked by the shell.
        /// </summary>
        /// <param name="explorerId">The identifier of the explorer that should become active.</param>
        public void SetActiveExplorer(string explorerId)
        {
            // The first tabbed slice still tracks a single active explorer because the left rail selects one explorer pane at a time.
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerId);

            ActiveExplorerId = explorerId;

            // Explorer focus is restored automatically only when the tool surface is empty.
            if (ActiveTab is null)
            {
                IsExplorerFocused = true;
            }
        }

        /// <summary>
        /// Updates the explorer item currently selected by the shell without opening a tab.
        /// </summary>
        /// <param name="explorerItemId">The stable explorer item identifier that should become selected.</param>
        public void SelectExplorerItem(string explorerItemId)
        {
            // Explorer single-click selection is distinct from double-click activation so users can inspect items without changing the center surface.
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerItemId);

            SelectedExplorerItemId = explorerItemId;
            IsExplorerFocused = true;
        }

        /// <summary>
        /// Determines whether a shell region is currently visible.
        /// </summary>
        /// <param name="region">The region to check.</param>
        /// <returns><see langword="true"/> when the region is part of the fixed bootstrap shell; otherwise, <see langword="false"/>.</returns>
        public bool IsRegionVisible(WorkbenchShellRegion region)
        {
            // The bootstrap shell keeps a fixed chrome layout so region visibility is a simple membership check.
            return VisibleRegions.Contains(region);
        }

        /// <summary>
        /// Opens a new tab or focuses an existing tab according to the logical tab identity represented by the activation request.
        /// </summary>
        /// <param name="definition">The static tool definition being activated.</param>
        /// <param name="activationTarget">The shell target that should host the tool.</param>
        /// <param name="toolContextFactory">The factory that creates the bounded tool context for new runtime instances.</param>
        /// <returns>The active runtime tool instance after the activation request completes.</returns>
        public ToolInstance ActivateTool(
            ToolDefinition definition,
            ActivationTarget activationTarget,
            Func<string, ToolContext> toolContextFactory)
        {
            // Activation requires both the static tool registration and the resolved shell target because the shell owns tab creation and reuse rules.
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(activationTarget);
            ArgumentNullException.ThrowIfNull(toolContextFactory);

            ValidateToolSurfaceActivationTarget(activationTarget);

            var tabIdentity = activationTarget.CreateTabIdentity();

            // Reopening the same logical target should focus the already open tab so the hosted component state remains intact.
            if (TryGetOpenTab(tabIdentity, out var existingTab) && existingTab is not null)
            {
                SetActiveTab(existingTab);
                return existingTab.ToolInstance;
            }

            // The first tabbed slice opens a new runtime instance only when the logical target is not already represented by an open tab.
            var instanceId = Guid.NewGuid().ToString("N");
            var activatedAtUtc = DateTimeOffset.UtcNow;
            var newInstance = new ToolInstance(
                instanceId,
                definition,
                activationTarget.LogicalTabKey,
                activationTarget.ParameterIdentity,
                activationTarget.InitialTitle ?? definition.DisplayName,
                activationTarget.InitialIcon ?? definition.Icon,
                null,
                activationTarget.Region,
                activatedAtUtc,
                toolContextFactory(instanceId));
            var newTab = new WorkbenchTab(tabIdentity, newInstance, activatedAtUtc);

            _openTabs.Add(newTab);
            _tabsById[instanceId] = newTab;
            SetActiveTab(newTab);
            return newInstance;
        }

        /// <summary>
        /// Focuses an already open tab.
        /// </summary>
        /// <param name="tabId">The stable tab identifier to focus.</param>
        /// <returns>The tab that is active after the request completes.</returns>
        public WorkbenchTab ActivateTab(string tabId)
        {
            // Explicit tab activation supports tab-strip switching without reopening or recreating the hosted component instance.
            ArgumentException.ThrowIfNullOrWhiteSpace(tabId);

            if (!_tabsById.TryGetValue(tabId, out var openTab))
            {
                throw new InvalidOperationException($"The Workbench tab '{tabId}' is not open.");
            }

            SetActiveTab(openTab);
            return openTab;
        }

        /// <summary>
        /// Closes an open tab and chooses the next active tab according to the current most-recently-active ordering.
        /// </summary>
        /// <param name="tabId">The stable tab identifier to close.</param>
        /// <returns>The new active tab when one remains open; otherwise, <see langword="null"/>.</returns>
        public WorkbenchTab? CloseTab(string tabId)
        {
            // Closing a tab removes only that runtime instance so other open tabs can keep their in-memory component state intact.
            ArgumentException.ThrowIfNullOrWhiteSpace(tabId);

            if (!_tabsById.TryGetValue(tabId, out var openTab))
            {
                throw new InvalidOperationException($"The Workbench tab '{tabId}' is not open.");
            }

            var wasActiveTab = string.Equals(ActiveTab?.Id, tabId, StringComparison.Ordinal);
            _tabsById.Remove(tabId);
            _openTabs.Remove(openTab);
            _mostRecentlyActiveTabIds.Remove(tabId);
            openTab.ToolInstance.Dispose();
            TabStrip.TrimToBounds(_openTabs.Count);

            if (!wasActiveTab)
            {
                return ActiveTab;
            }

            if (_openTabs.Count == 0)
            {
                // When the final tab closes the center surface becomes empty and the explorer regains focus.
                ActiveTab = null;
                IsExplorerFocused = true;
                return null;
            }

            var nextActiveTab = ResolveMostRecentlyActiveOpenTab();
            SetActiveTab(nextActiveTab);
            return nextActiveTab;
        }

        /// <summary>
        /// Attempts to resolve an open tab by its stable tab identifier.
        /// </summary>
        /// <param name="tabId">The stable tab identifier to resolve.</param>
        /// <param name="openTab">The resolved open tab when the lookup succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the tab exists; otherwise, <see langword="false"/>.</returns>
        public bool TryGetOpenTab(string tabId, out WorkbenchTab? openTab)
        {
            // Tab lookups by identifier support explicit switching and close requests from the tab strip.
            ArgumentException.ThrowIfNullOrWhiteSpace(tabId);

            var exists = _tabsById.TryGetValue(tabId, out var resolvedTab);
            openTab = resolvedTab;
            return exists;
        }

        /// <summary>
        /// Attempts to resolve an open tab by its logical identity.
        /// </summary>
        /// <param name="tabIdentity">The logical tab identity to resolve.</param>
        /// <param name="openTab">The resolved open tab when the lookup succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the tab exists; otherwise, <see langword="false"/>.</returns>
        public bool TryGetOpenTab(WorkbenchTabIdentity tabIdentity, out WorkbenchTab? openTab)
        {
            // Logical-identity lookups support open-or-focus behavior without reordering the visible tab strip.
            ArgumentNullException.ThrowIfNull(tabIdentity);

            openTab = _openTabs.FirstOrDefault(candidate => candidate.Identity.Equals(tabIdentity));
            return openTab is not null;
        }

        /// <summary>
        /// Attempts to resolve a tracked tool instance by its runtime instance identifier.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance identifier to resolve.</param>
        /// <param name="toolInstance">The resolved tool instance when the lookup succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the tool instance exists; otherwise, <see langword="false"/>.</returns>
        public bool TryGetToolInstance(string toolInstanceId, out ToolInstance? toolInstance)
        {
            // Tool-context updates still route through runtime instance identifiers so shell-managed tab reuse remains transparent to hosted tools.
            ArgumentException.ThrowIfNullOrWhiteSpace(toolInstanceId);

            var exists = _tabsById.TryGetValue(toolInstanceId, out var resolvedTab);
            toolInstance = resolvedTab?.ToolInstance;
            return exists;
        }

        /// <summary>
        /// Applies the active-tab transition and records most-recently-active metadata.
        /// </summary>
        /// <param name="openTab">The tab that should become active.</param>
        private void SetActiveTab(WorkbenchTab openTab)
        {
            // Tab activation updates only focus metadata so visible tab ordering stays under shell control while close behavior remains deterministic.
            ArgumentNullException.ThrowIfNull(openTab);

            ActiveTab = openTab;
            IsExplorerFocused = false;

            var activatedAtUtc = DateTimeOffset.UtcNow;
            openTab.MarkActivated(activatedAtUtc);
            _mostRecentlyActiveTabIds.Remove(openTab.Id);
            _mostRecentlyActiveTabIds.Add(openTab.Id);
            TabStrip.EnsureTabVisible(openTab.Id, _openTabs);
        }

        /// <summary>
        /// Resolves the most recently active tab that is still open.
        /// </summary>
        /// <returns>The most recently active tab that remains open.</returns>
        private WorkbenchTab ResolveMostRecentlyActiveOpenTab()
        {
            // Close behavior walks the most-recently-active list so the next active tab matches the user's recent focus history instead of visible order.
            for (var index = _mostRecentlyActiveTabIds.Count - 1; index >= 0; index--)
            {
                if (_tabsById.TryGetValue(_mostRecentlyActiveTabIds[index], out var openTab))
                {
                    return openTab;
                }
            }

            return _openTabs[^1];
        }

        /// <summary>
        /// Validates that the supplied activation target can be hosted by the current shell slice.
        /// </summary>
        /// <param name="activationTarget">The activation target to validate.</param>
        private void ValidateToolSurfaceActivationTarget(ActivationTarget activationTarget)
        {
            // The first tabbed slice still limits hosted content to the central tool surface even though multiple tabs may now remain open.
            if (activationTarget.Region != WorkbenchShellRegion.ToolSurface || !IsRegionVisible(activationTarget.Region))
            {
                throw new InvalidOperationException($"The bootstrap Workbench shell can only host tools in the {WorkbenchShellRegion.ToolSurface} region.");
            }
        }
    }
}
