using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;
using Xunit;

namespace UKHO.Workbench.Services.Tests
{
    /// <summary>
    /// Verifies the Workbench service-layer orchestration introduced for the first tabbed Workbench slice.
    /// </summary>
    public class PlaceholderTests
    {
        /// <summary>
        /// Confirms executing the same activation command twice focuses the existing logical tab instead of opening a duplicate tab.
        /// </summary>
        [Fact]
        public async Task ExecuteRegisteredActivationCommandAndFocusTheExistingLogicalTab()
        {
            // The shell manager should route repeated activation requests through the shared open-or-focus contract so logical targets reuse tabs.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search", typeof(TestToolComponent), "explorer.bootstrap", "search"));
            shellManager.RegisterCommand(
                new CommandContribution(
                    "command.search.open",
                    "Open Search",
                    CommandScope.Host,
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget("tool.search")));

            await shellManager.ExecuteCommandAsync("command.search.open");
            var firstActivation = shellManager.State.ActiveTool;
            await shellManager.ExecuteCommandAsync("command.search.open");
            var secondActivation = shellManager.State.ActiveTool;

            firstActivation.ShouldNotBeNull();
            secondActivation.ShouldBeSameAs(firstActivation);
            shellManager.OpenTabs.Count.ShouldBe(1);
            shellManager.State.ActiveTab.ShouldNotBeNull();
            shellManager.State.ActiveTab.ToolInstance.ShouldBe(firstActivation);
        }

        /// <summary>
        /// Confirms activation requests for the same tool respect parameter identity across command entry points.
        /// </summary>
        [Fact]
        public async Task OpenSeparateTabsForDifferentParameterIdentitiesAndReuseMatchingOnes()
        {
            // Activation-capable commands should all route through the same parameter-aware open-or-focus contract so matching requests reuse a tab and different requests open another tab.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search", typeof(TestToolComponent), "explorer.bootstrap", "search"));
            shellManager.RegisterCommand(
                new CommandContribution(
                    "command.search.open.one",
                    "Open Search 1",
                    CommandScope.Host,
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget("tool.search", parameterIdentity: "query=id:1")));
            shellManager.RegisterCommand(
                new CommandContribution(
                    "command.search.open.two",
                    "Open Search 2",
                    CommandScope.Host,
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget("tool.search", parameterIdentity: "query=id:2")));

            await shellManager.ExecuteCommandAsync("command.search.open.one");
            var firstActivation = shellManager.State.ActiveTool;
            await shellManager.ExecuteCommandAsync("command.search.open.one");
            var matchingActivation = shellManager.State.ActiveTool;
            await shellManager.ExecuteCommandAsync("command.search.open.two");
            var differentActivation = shellManager.State.ActiveTool;

            matchingActivation.ShouldBeSameAs(firstActivation);
            differentActivation.ShouldNotBeSameAs(firstActivation);
            shellManager.OpenTabs.Count.ShouldBe(2);
            shellManager.OpenTabs.Select(openTab => openTab.ParameterIdentity).ShouldBe([
                "query=id:1",
                "query=id:2"
            ]);
        }

        /// <summary>
        /// Confirms tool-context-driven activation requests honor the same parameter-aware reuse rules as host-driven activation requests.
        /// </summary>
        [Fact]
        public async Task HonorSharedReuseRulesForToolContextOpenRequests()
        {
            // Hosted tools should not be able to bypass the shared reuse rules when they request another tool through the bounded context bridge.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.overview", "Overview", typeof(TestToolComponent), "explorer.bootstrap", "dashboard"));
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search", typeof(TestToolComponent), "explorer.bootstrap", "search"));

            var overviewTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.overview"));

            await overviewTool.Context.OpenToolAsync(ActivationTarget.CreateToolSurfaceTarget("tool.search", parameterIdentity: "query=id:1"));
            var firstSearchTab = shellManager.State.ActiveTab;
            await overviewTool.Context.OpenToolAsync(ActivationTarget.CreateToolSurfaceTarget("tool.search", parameterIdentity: "query=id:1"));
            var reusedSearchTab = shellManager.State.ActiveTab;
            await overviewTool.Context.OpenToolAsync(ActivationTarget.CreateToolSurfaceTarget("tool.search", parameterIdentity: "query=id:2"));
            var secondSearchTab = shellManager.State.ActiveTab;

            reusedSearchTab.ShouldNotBeNull();
            firstSearchTab.ShouldNotBeNull();
            secondSearchTab.ShouldNotBeNull();
            reusedSearchTab.ShouldBe(firstSearchTab);
            secondSearchTab.ShouldNotBe(firstSearchTab);
            shellManager.OpenTabs.Count.ShouldBe(3);
        }

        /// <summary>
        /// Confirms tab close orchestration promotes the most recently active remaining tab when the active tab is closed.
        /// </summary>
        [Fact]
        public void CloseTheActiveTabAndPromoteTheMostRecentlyActiveRemainingTab()
        {
            // The shell manager should preserve the user's recent focus history so closing the active tab returns them to the most relevant remaining tab.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.overview", "Overview", typeof(TestToolComponent), "explorer.bootstrap", "dashboard"));
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search", typeof(TestToolComponent), "explorer.bootstrap", "search"));
            shellManager.RegisterTool(new ToolDefinition("tool.admin", "Admin", typeof(TestToolComponent), "explorer.bootstrap", "build"));

            var overviewTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.overview"));
            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.search"));
            var adminTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.admin"));
            shellManager.ActivateTab(overviewTool.InstanceId);

            shellManager.CloseTab(overviewTool.InstanceId);

            shellManager.State.ActiveTool.ShouldBe(adminTool);
            shellManager.OpenTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                "tool.search",
                "tool.admin"
            ]);
        }

        /// <summary>
        /// Confirms closing the final tab leaves the center surface empty and restores explorer focus.
        /// </summary>
        [Fact]
        public void CloseTheFinalTabAndRestoreExplorerFocus()
        {
            // The empty-shell close path should remove the final tab cleanly and return focus responsibility to the explorer pane.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.overview", "Overview", typeof(TestToolComponent), "explorer.bootstrap", "dashboard"));
            shellManager.SelectExplorerItem("explorer.item.bootstrap.overview");

            var activeTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.overview"));
            shellManager.CloseTab(activeTool.InstanceId);

            shellManager.OpenTabs.Count.ShouldBe(0);
            shellManager.State.ActiveTab.ShouldBeNull();
            shellManager.State.IsExplorerFocused.ShouldBeTrue();
            shellManager.State.SelectedExplorerItemId.ShouldBe("explorer.item.bootstrap.overview");
        }

        /// <summary>
        /// Confirms runtime menu and status-bar contributions participate only while their owning tab is active.
        /// </summary>
        [Fact]
        public void RecomposeRuntimeContributionsWhenTheActiveTabChanges()
        {
            // Static shell contributions remain visible, while tab-owned runtime contributions should disappear automatically when focus moves away.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.overview", "Overview", typeof(TestToolComponent), "explorer.bootstrap", "dashboard"));
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search", typeof(TestToolComponent), "explorer.bootstrap", "search"));
            shellManager.RegisterMenu(new MenuContribution("menu.host.overview", "Overview", "command.host.overview", order: 100));
            shellManager.RegisterStatusBar(new StatusBarContribution("status.host.ready", "Workbench ready", order: 100));

            var searchTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.search"));
            searchTool.Context.SetRuntimeMenuContributions([new MenuContribution("menu.runtime.search", "Run sample query", "command.search.run", ownerToolId: "tool.search", order: 200)]);
            searchTool.Context.SetRuntimeStatusBarContributions([new StatusBarContribution("status.runtime.search", "Sample query executed", ownerToolId: "tool.search", order: 200)]);

            shellManager.MenuContributions.Select(menuContribution => menuContribution.DisplayName).ShouldContain("Run sample query");
            shellManager.StatusBarContributions.Select(statusBarContribution => statusBarContribution.Text).ShouldContain("Sample query executed");

            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.overview"));

            shellManager.MenuContributions.Select(menuContribution => menuContribution.DisplayName).ShouldContain("Overview");
            shellManager.MenuContributions.Select(menuContribution => menuContribution.DisplayName).ShouldNotContain("Run sample query");
            shellManager.StatusBarContributions.Select(statusBarContribution => statusBarContribution.Text).ShouldContain("Workbench ready");
            shellManager.StatusBarContributions.Select(statusBarContribution => statusBarContribution.Text).ShouldNotContain("Sample query executed");
        }

        /// <summary>
        /// Confirms inactive tabs receive title and icon updates immediately through the shared runtime metadata path.
        /// </summary>
        [Fact]
        public void UpdateInactiveTabMetadataImmediately()
        {
            // Hosted views should be able to update their tab metadata while inactive so the shell reflects background state changes without forcing focus changes.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.one", "Runtime tool one", typeof(TestToolComponent), "explorer.bootstrap", "dashboard"));
            shellManager.RegisterTool(new ToolDefinition("tool.two", "Runtime tool two", typeof(TestToolComponent), "explorer.bootstrap", "build"));

            var firstTool = shellManager.ActivateTool(
                ActivationTarget.CreateToolSurfaceTarget(
                    "tool.one",
                    parameterIdentity: "item=1",
                    initialTitle: "Explorer tool one",
                    initialIcon: "explore"));
            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.two"));

            firstTool.Context.SetTitle("Updated inactive title");
            firstTool.Context.SetIcon("travel_explore");

            shellManager.OpenTabs[0].Title.ShouldBe("Updated inactive title");
            shellManager.OpenTabs[0].Icon.ShouldBe("travel_explore");
        }

        /// <summary>
        /// Confirms overflow-driven tab activation keeps the full open order intact while shifting the visible window only enough to reveal the selected tab.
        /// </summary>
        [Fact]
        public void ActivateAnOverflowTabAndShiftTheVisibleWindowMinimally()
        {
            // Overflow activation should reuse the shared tab-focus path while giving the layout a stable, minimally adjusted visible tab segment to render.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);

            foreach (var index in Enumerable.Range(1, 6))
            {
                shellManager.RegisterTool(new ToolDefinition($"tool.{index}", $"Tool {index}", typeof(TestToolComponent), "explorer.bootstrap", $"looks_{index}"));
                shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget($"tool.{index}"));
            }

            shellManager.VisibleTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                "tool.3",
                "tool.4",
                "tool.5",
                "tool.6"
            ]);

            var selectedOverflowTabId = shellManager.OpenTabs[1].Id;
            shellManager.ActivateTabFromOverflow(selectedOverflowTabId);

            shellManager.State.ActiveTab.ShouldNotBeNull();
            shellManager.State.ActiveTab.Id.ShouldBe(selectedOverflowTabId);
            shellManager.OpenTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                "tool.1",
                "tool.2",
                "tool.3",
                "tool.4",
                "tool.5",
                "tool.6"
            ]);
            shellManager.VisibleTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                "tool.2",
                "tool.3",
                "tool.4",
                "tool.5"
            ]);
        }

        /// <summary>
        /// Confirms the fixed Workbench context values reflect the current active tab and published selection summary.
        /// </summary>
        [Fact]
        public void ExposeTheFixedContextKeysForTheActiveTab()
        {
            // The fixed context model should continue to describe the currently active tab even after the shell moved away from the single-active-tool implementation.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search", typeof(TestToolComponent), "explorer.bootstrap", "search"));

            var activeTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.search"));
            activeTool.Context.SetSelection("search.query", 2);

            shellManager.ContextValues[WorkbenchContextKeys.ActiveTool].ShouldBe("tool.search");
            shellManager.ContextValues[WorkbenchContextKeys.ActiveRegion].ShouldBe(WorkbenchShellRegion.ToolSurface.ToString());
            shellManager.ContextValues[WorkbenchContextKeys.SelectionType].ShouldBe("search.query");
            shellManager.ContextValues[WorkbenchContextKeys.SelectionCount].ShouldBe("2");
            shellManager.ContextValues[WorkbenchContextKeys.ToolSurfaceReady].ShouldBe(bool.TrueString);
        }

        /// <summary>
        /// Confirms the shell raises a user-safe notification when a close request targets an unknown tab.
        /// </summary>
        [Fact]
        public void RaiseASafeNotificationWhenClosingAnUnknownTabFails()
        {
            // Close-path failures should surface through the same safe-notification path as activation failures so the host receives consistent UX behavior.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            WorkbenchNotificationEventArgs? notification = null;
            shellManager.NotificationRaised += (_, args) => notification = args;

            var exception = Should.Throw<InvalidOperationException>(() => shellManager.CloseTab("tab.unknown"));

            exception.Message.ShouldContain("tab.unknown");
            notification.ShouldNotBeNull();
            notification.Summary.ShouldBe("Workbench action failed");
            notification.Detail.ShouldBe("The selected Workbench action could not be completed. Check the application logs for more detail.");
        }

        /// <summary>
        /// Confirms closing a tab marks the runtime instance disposed immediately.
        /// </summary>
        [Fact]
        public void DisposeTheClosedToolInstanceImmediately()
        {
            // Removing the tab from shell state should also dispose the runtime instance so per-tab state is released immediately on close.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.overview", "Overview", typeof(TestToolComponent), "explorer.bootstrap", "dashboard"));

            var activeTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.overview"));

            shellManager.CloseTab(activeTool.InstanceId);

            activeTool.IsDisposed.ShouldBeTrue();
        }

        /// <summary>
        /// Provides a minimal renderable component type for shell-service tests.
        /// </summary>
        private sealed class TestToolComponent : IComponent
        {
            /// <summary>
            /// Gets or sets the renderer handle supplied by Blazor.
            /// </summary>
            private RenderHandle RenderHandle { get; set; }

            /// <summary>
            /// Attaches the component to the supplied renderer.
            /// </summary>
            /// <param name="renderHandle">The renderer handle supplied by Blazor.</param>
            public void Attach(RenderHandle renderHandle)
            {
                // The test component stores the renderer handle only so it satisfies the IComponent contract.
                RenderHandle = renderHandle;
            }

            /// <summary>
            /// Accepts incoming parameters without rendering any UI because these tests exercise only service-layer behavior.
            /// </summary>
            /// <param name="parameters">The incoming parameters supplied by the renderer.</param>
            /// <returns>A completed task because the test component performs no rendering work.</returns>
            public Task SetParametersAsync(ParameterView parameters)
            {
                // The service-layer tests do not need component UI, so the stub simply completes immediately.
                return Task.CompletedTask;
            }
        }
    }
}
