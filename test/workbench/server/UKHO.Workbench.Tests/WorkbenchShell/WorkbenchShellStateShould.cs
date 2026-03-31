using Shouldly;
using UKHO.Workbench.Layout;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;
using Xunit;

namespace UKHO.Workbench.Tests.WorkbenchShell
{
    /// <summary>
    /// Verifies the Workbench shell state model introduced for the first tabbed Workbench slice.
    /// </summary>
    public class WorkbenchShellStateShould
    {
        /// <summary>
        /// Confirms the bootstrap shell exposes the fixed chrome regions required by the first desktop-like layout.
        /// </summary>
        [Fact]
        public void StartWithAllBootstrapShellRegionsVisible()
        {
            // The bootstrap slice keeps the shell layout fixed so the host can render every required region without optional docking rules.
            var state = new WorkbenchShellState();

            // The state should expose the complete region list expected by the shell layout.
            state.VisibleRegions.ShouldBe(
            [
                WorkbenchShellRegion.MenuBar,
                WorkbenchShellRegion.ActivityRail,
                WorkbenchShellRegion.Explorer,
                WorkbenchShellRegion.ToolSurface,
                WorkbenchShellRegion.ActiveToolToolbar,
                WorkbenchShellRegion.StatusBar
            ]);
        }

        /// <summary>
        /// Confirms explorer-item selection is tracked independently from tab opening.
        /// </summary>
        [Fact]
        public void TrackExplorerSelectionWithoutOpeningATab()
        {
            // Explorer single-click interaction should update selection state only so the center surface remains empty until a double-click activation occurs.
            var state = new WorkbenchShellState();

            state.SelectExplorerItem("explorer.item.bootstrap.overview");

            state.SelectedExplorerItemId.ShouldBe("explorer.item.bootstrap.overview");
            state.OpenTabs.Count.ShouldBe(0);
            state.ActiveTab.ShouldBeNull();
            state.IsExplorerFocused.ShouldBeTrue();
        }

        /// <summary>
        /// Confirms opening different logical targets creates ordered tabs and tracks the most recently active tab.
        /// </summary>
        [Fact]
        public void OpenTabsInOrderAndTrackMostRecentlyActiveHistory()
        {
            // The first tabbed slice should preserve visible open order while separately tracking recent activation history for close behavior.
            var state = new WorkbenchShellState();
            var overviewDefinition = CreateToolDefinition("tool.bootstrap.overview", "Workbench overview", "dashboard");
            var searchDefinition = CreateToolDefinition("tool.bootstrap.search", "Search", "search");

            var overviewTool = state.ActivateTool(overviewDefinition, ActivationTarget.CreateToolSurfaceTarget(overviewDefinition.Id), CreateToolContext);
            var searchTool = state.ActivateTool(searchDefinition, ActivationTarget.CreateToolSurfaceTarget(searchDefinition.Id), CreateToolContext);
            var refocusedOverviewTool = state.ActivateTool(overviewDefinition, ActivationTarget.CreateToolSurfaceTarget(overviewDefinition.Id), CreateToolContext);

            state.OpenTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                overviewDefinition.Id,
                searchDefinition.Id
            ]);
            state.ActiveTool.ShouldBe(refocusedOverviewTool);
            state.MostRecentlyActiveTabIds.ShouldBe([
                searchTool.InstanceId,
                overviewTool.InstanceId
            ]);
        }

        /// <summary>
        /// Confirms the shell keeps a separate visible tab window and adjusts it minimally when an overflow selection activates a hidden tab.
        /// </summary>
        [Fact]
        public void KeepASeparateVisibleTabWindowAndMoveItMinimallyWhenAHiddenTabBecomesActive()
        {
            // Overflow support should preserve the overall open order while only shifting the visible window enough to reveal the newly active hidden tab.
            var state = new WorkbenchShellState();
            var openedTools = Enumerable.Range(1, 6)
                .Select(index => state.ActivateTool(
                    CreateToolDefinition($"tool.bootstrap.{index}", $"Tool {index}", $"looks_{index}"),
                    ActivationTarget.CreateToolSurfaceTarget($"tool.bootstrap.{index}"),
                    CreateToolContext))
                .ToArray();

            state.OpenTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                "tool.bootstrap.1",
                "tool.bootstrap.2",
                "tool.bootstrap.3",
                "tool.bootstrap.4",
                "tool.bootstrap.5",
                "tool.bootstrap.6"
            ]);
            state.VisibleTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                "tool.bootstrap.3",
                "tool.bootstrap.4",
                "tool.bootstrap.5",
                "tool.bootstrap.6"
            ]);

            var activatedTab = state.ActivateTab(openedTools[1].InstanceId);

            activatedTab.Id.ShouldBe(openedTools[1].InstanceId);
            state.ActiveTab.ShouldNotBeNull();
            state.ActiveTab.Id.ShouldBe(openedTools[1].InstanceId);
            state.VisibleTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                "tool.bootstrap.2",
                "tool.bootstrap.3",
                "tool.bootstrap.4",
                "tool.bootstrap.5"
            ]);
        }

        /// <summary>
        /// Confirms reopening the same logical target focuses the existing tab instead of duplicating it.
        /// </summary>
        [Fact]
        public void ReuseTheExistingTabWhenTheSameLogicalTargetIsActivatedAgain()
        {
            // The first tabbed slice intentionally reuses a logical target so hosted component state is preserved rather than recreated.
            var state = new WorkbenchShellState();
            var definition = CreateToolDefinition("tool.bootstrap.overview", "Workbench overview", "dashboard");
            var activationTarget = ActivationTarget.CreateToolSurfaceTarget(definition.Id);

            var firstActivation = state.ActivateTool(definition, activationTarget, CreateToolContext);
            var secondActivation = state.ActivateTool(definition, activationTarget, CreateToolContext);

            firstActivation.ShouldBeSameAs(secondActivation);
            state.OpenTabs.Count.ShouldBe(1);
            state.ActiveTab.ShouldNotBeNull();
            state.ActiveTab.ToolInstance.ShouldBe(firstActivation);
        }

        /// <summary>
        /// Confirms parameter identity participates in logical tab reuse so matching requests reuse a tab and different identities open separate tabs.
        /// </summary>
        [Fact]
        public void DistinguishMatchingAndDifferentParameterIdentitiesWhenActivatingTheSameTool()
        {
            // Parameter identity lets the shell distinguish one logical target of the same tool type from another without enabling explicit duplicate-tab overrides.
            var state = new WorkbenchShellState();
            var definition = CreateToolDefinition("tool.bootstrap.search", "Search", "search");

            var firstActivation = state.ActivateTool(
                definition,
                ActivationTarget.CreateToolSurfaceTarget(definition.Id, parameterIdentity: "query=id:1"),
                CreateToolContext);
            var matchingActivation = state.ActivateTool(
                definition,
                ActivationTarget.CreateToolSurfaceTarget(definition.Id, parameterIdentity: "query=id:1"),
                CreateToolContext);
            var differentActivation = state.ActivateTool(
                definition,
                ActivationTarget.CreateToolSurfaceTarget(definition.Id, parameterIdentity: "query=id:2"),
                CreateToolContext);

            matchingActivation.ShouldBeSameAs(firstActivation);
            differentActivation.ShouldNotBeSameAs(firstActivation);
            state.OpenTabs.Count.ShouldBe(2);
            state.OpenTabs.Select(openTab => openTab.ParameterIdentity).ShouldBe([
                "query=id:1",
                "query=id:2"
            ]);
        }

        /// <summary>
        /// Confirms newly opened tabs start with activation metadata supplied by the caller rather than the static tool definition metadata.
        /// </summary>
        [Fact]
        public void InitializeNewTabMetadataFromTheActivationTarget()
        {
            // Explorer-provided metadata should seed the initial tab chrome so the hosted view can replace it later when richer runtime state becomes available.
            var state = new WorkbenchShellState();
            var definition = CreateToolDefinition("tool.bootstrap.search", "Runtime Search", "manage_search");

            var activation = state.ActivateTool(
                definition,
                ActivationTarget.CreateToolSurfaceTarget(
                    definition.Id,
                    parameterIdentity: "query=id:1",
                    initialTitle: "Explorer Search",
                    initialIcon: "travel_explore"),
                CreateToolContext);

            activation.Title.ShouldBe("Explorer Search");
            activation.Icon.ShouldBe("travel_explore");
            activation.ParameterIdentity.ShouldBe("query=id:1");
            state.ActiveTab.ShouldNotBeNull();
            state.ActiveTab.Title.ShouldBe("Explorer Search");
            state.ActiveTab.Icon.ShouldBe("travel_explore");
        }

        /// <summary>
        /// Confirms switching tabs updates active-tab state without changing the ordered open-tab collection.
        /// </summary>
        [Fact]
        public void SwitchTheActiveTabWithoutReorderingTheOpenTabs()
        {
            // Tab-strip switching should update focus and activity history while the shell keeps visible tab order under shell control.
            var state = new WorkbenchShellState();
            var overviewDefinition = CreateToolDefinition("tool.bootstrap.overview", "Workbench overview", "dashboard");
            var searchDefinition = CreateToolDefinition("tool.bootstrap.search", "Search", "search");

            var overviewTool = state.ActivateTool(overviewDefinition, ActivationTarget.CreateToolSurfaceTarget(overviewDefinition.Id), CreateToolContext);
            var searchTool = state.ActivateTool(searchDefinition, ActivationTarget.CreateToolSurfaceTarget(searchDefinition.Id), CreateToolContext);

            var activatedTab = state.ActivateTab(overviewTool.InstanceId);

            activatedTab.ToolInstance.ShouldBe(overviewTool);
            state.OpenTabs.Select(openTab => openTab.Id).ShouldBe([
                overviewTool.InstanceId,
                searchTool.InstanceId
            ]);
            state.MostRecentlyActiveTabIds.ShouldBe([
                searchTool.InstanceId,
                overviewTool.InstanceId
            ]);
        }

        /// <summary>
        /// Confirms closing the active tab promotes the most recently active remaining tab.
        /// </summary>
        [Fact]
        public void CloseTheActiveTabAndPromoteTheMostRecentlyActiveRemainingTab()
        {
            // The shell should restore the most recently active remaining tab so close behavior follows recent user focus rather than raw visible order.
            var state = new WorkbenchShellState();
            var overviewDefinition = CreateToolDefinition("tool.bootstrap.overview", "Workbench overview", "dashboard");
            var searchDefinition = CreateToolDefinition("tool.bootstrap.search", "Search", "search");
            var adminDefinition = CreateToolDefinition("tool.bootstrap.admin", "Admin", "build");

            var overviewTool = state.ActivateTool(overviewDefinition, ActivationTarget.CreateToolSurfaceTarget(overviewDefinition.Id), CreateToolContext);
            _ = state.ActivateTool(searchDefinition, ActivationTarget.CreateToolSurfaceTarget(searchDefinition.Id), CreateToolContext);
            var adminTool = state.ActivateTool(adminDefinition, ActivationTarget.CreateToolSurfaceTarget(adminDefinition.Id), CreateToolContext);
            _ = state.ActivateTab(overviewTool.InstanceId);

            var nextActiveTab = state.CloseTab(overviewTool.InstanceId);

            nextActiveTab.ShouldNotBeNull();
            nextActiveTab.ToolInstance.ShouldBe(adminTool);
            state.ActiveTool.ShouldBe(adminTool);
            state.OpenTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                searchDefinition.Id,
                adminDefinition.Id
            ]);
        }

        /// <summary>
        /// Confirms closing a non-active tab leaves the current active tab unchanged.
        /// </summary>
        [Fact]
        public void CloseANonActiveTabWithoutChangingTheCurrentActiveTab()
        {
            // Closing an inactive tab should not disrupt the current working tab because only the closed runtime instance is being removed.
            var state = new WorkbenchShellState();
            var overviewDefinition = CreateToolDefinition("tool.bootstrap.overview", "Workbench overview", "dashboard");
            var searchDefinition = CreateToolDefinition("tool.bootstrap.search", "Search", "search");

            var overviewTool = state.ActivateTool(overviewDefinition, ActivationTarget.CreateToolSurfaceTarget(overviewDefinition.Id), CreateToolContext);
            var searchTool = state.ActivateTool(searchDefinition, ActivationTarget.CreateToolSurfaceTarget(searchDefinition.Id), CreateToolContext);

            var nextActiveTab = state.CloseTab(overviewTool.InstanceId);

            nextActiveTab.ShouldNotBeNull();
            nextActiveTab.ToolInstance.ShouldBe(searchTool);
            state.ActiveTool.ShouldBe(searchTool);
            state.OpenTabs.Count.ShouldBe(1);
        }

        /// <summary>
        /// Confirms closing the final tab leaves the tool surface empty and restores explorer focus.
        /// </summary>
        [Fact]
        public void CloseTheFinalTabAndReturnFocusToTheExplorer()
        {
            // When the last remaining tab closes the center surface should become empty again and the explorer should regain focus.
            var state = new WorkbenchShellState();
            var definition = CreateToolDefinition("tool.bootstrap.overview", "Workbench overview", "dashboard");

            var activeTool = state.ActivateTool(definition, ActivationTarget.CreateToolSurfaceTarget(definition.Id), CreateToolContext);
            state.SelectExplorerItem("explorer.item.bootstrap.overview");

            var nextActiveTab = state.CloseTab(activeTool.InstanceId);

            nextActiveTab.ShouldBeNull();
            state.ActiveTab.ShouldBeNull();
            state.OpenTabs.Count.ShouldBe(0);
            state.IsExplorerFocused.ShouldBeTrue();
            state.SelectedExplorerItemId.ShouldBe("explorer.item.bootstrap.overview");
            activeTool.IsDisposed.ShouldBeTrue();
        }

        /// <summary>
        /// Confirms the shell state rejects requests to host tool content in unsupported regions.
        /// </summary>
        [Fact]
        public void ThrowWhenAToolIsActivatedIntoANonToolRegion()
        {
            // The first tabbed slice still supports hosting tools only in the central working region.
            var state = new WorkbenchShellState();
            var definition = CreateToolDefinition("tool.bootstrap.overview", "Workbench overview", "dashboard");
            var activationTarget = new ActivationTarget(definition.Id, WorkbenchShellRegion.MenuBar);

            var exception = Should.Throw<InvalidOperationException>(() => state.ActivateTool(definition, activationTarget, CreateToolContext));

            exception.Message.ShouldContain("ToolSurface");
        }

        /// <summary>
        /// Creates a bounded tool definition for shell-state tests.
        /// </summary>
        /// <param name="toolId">The tool identifier to register.</param>
        /// <param name="displayName">The display name to surface in shell chrome.</param>
        /// <param name="icon">The icon used by the shell for the tool.</param>
        /// <returns>A tool definition suitable for shell-state tests.</returns>
        private static ToolDefinition CreateToolDefinition(string toolId, string displayName, string icon)
        {
            // The shell-state tests use simple tool definitions because they verify state transitions rather than rendered component behavior.
            return new ToolDefinition(toolId, displayName, typeof(Grid), "explorer.bootstrap", icon, $"Shows the {displayName} tool.");
        }

        /// <summary>
        /// Creates a bounded tool context for shell-state tests.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance identifier that should be bound to the new context.</param>
        /// <returns>A bounded tool context backed by a no-op test bridge.</returns>
        private static ToolContext CreateToolContext(string toolInstanceId)
        {
            // The shell-state tests exercise only state transitions, so a no-op bridge is sufficient for runtime tool-context creation.
            return new ToolContext(toolInstanceId, new TestToolContextBridge());
        }

        /// <summary>
        /// Provides a no-op Workbench tool-context bridge for shell-state tests.
        /// </summary>
        private sealed class TestToolContextBridge : IToolContextBridge
        {
            /// <summary>
            /// Ignores tool-opening requests because shell-state tests do not exercise nested activation flows.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance issuing the request.</param>
            /// <param name="activationTarget">The shell activation target that would be opened or focused.</param>
            /// <param name="cancellationToken">The cancellation token that would flow with the request.</param>
            /// <returns>A completed task because the test bridge performs no work.</returns>
            public Task OpenToolAsync(string toolInstanceId, ActivationTarget activationTarget, CancellationToken cancellationToken = default)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
                return Task.CompletedTask;
            }

            /// <summary>
            /// Ignores command-invocation requests because shell-state tests do not exercise command routing.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance issuing the request.</param>
            /// <param name="commandId">The command identifier that would be invoked.</param>
            /// <param name="cancellationToken">The cancellation token that would flow with the request.</param>
            /// <returns>A completed task because the test bridge performs no work.</returns>
            public Task InvokeCommandAsync(string toolInstanceId, string commandId, CancellationToken cancellationToken = default)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
                return Task.CompletedTask;
            }

            /// <summary>
            /// Ignores title updates because shell-state tests do not assert runtime shell metadata changes.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="title">The new title that would be shown by the shell.</param>
            public void UpdateTitle(string toolInstanceId, string title)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores icon updates because shell-state tests do not assert runtime shell metadata changes.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="icon">The new icon that would be shown by the shell.</param>
            public void UpdateIcon(string toolInstanceId, string icon)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores badge updates because shell-state tests do not assert runtime shell metadata changes.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="badge">The new badge text that would be shown by the shell.</param>
            public void UpdateBadge(string toolInstanceId, string? badge)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores runtime menu updates because shell-state tests do not exercise contribution composition.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="menuContributions">The runtime menu contributions that would be visible while the tool is active.</param>
            public void UpdateRuntimeMenuContributions(string toolInstanceId, IReadOnlyList<MenuContribution> menuContributions)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores runtime toolbar updates because shell-state tests do not exercise contribution composition.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="toolbarContributions">The runtime toolbar contributions that would be visible while the tool is active.</param>
            public void UpdateRuntimeToolbarContributions(string toolInstanceId, IReadOnlyList<ToolbarContribution> toolbarContributions)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores runtime status-bar updates because shell-state tests do not exercise contribution composition.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="statusBarContributions">The runtime status-bar contributions that would be visible while the tool is active.</param>
            public void UpdateRuntimeStatusBarContributions(string toolInstanceId, IReadOnlyList<StatusBarContribution> statusBarContributions)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores selection updates because shell-state tests do not assert fixed context values.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="selectionType">The logical selection type that would be published by the tool.</param>
            /// <param name="selectionCount">The number of selected items that would be published by the tool.</param>
            public void UpdateSelection(string toolInstanceId, string? selectionType, int selectionCount)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Returns an empty fixed-context snapshot because shell-state tests do not require runtime context values.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance requesting the current context snapshot.</param>
            /// <returns>An empty context-value dictionary.</returns>
            public IReadOnlyDictionary<string, string> GetContextValues(string toolInstanceId)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            /// <summary>
            /// Ignores notification requests because shell-state tests do not exercise user-facing notifications.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance issuing the notification.</param>
            /// <param name="severity">The shell notification severity value that would be raised.</param>
            /// <param name="summary">The short summary that would be shown to the user.</param>
            /// <param name="detail">The longer detail that would be shown to the user.</param>
            /// <param name="cancellationToken">The cancellation token that would flow with the notification request.</param>
            /// <returns>A completed task because the test bridge performs no work.</returns>
            public Task NotifyAsync(string toolInstanceId, string severity, string summary, string detail, CancellationToken cancellationToken = default)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
                return Task.CompletedTask;
            }
        }
    }
}
