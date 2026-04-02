using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;
using Xunit;

namespace UKHO.Workbench.Services.Tests
{
    /// <summary>
    /// Verifies explorer-toolbar composition for the mixed left-pane action surface.
    /// </summary>
    public class ExplorerToolbarCompositionTests
    {
        /// <summary>
        /// Confirms the explorer toolbar mixes shell-global actions with contributions owned by the currently active explorer.
        /// </summary>
        [Fact]
        public void ComposeShellGlobalAndActiveExplorerContributionsIntoOneExplorerToolbarSurface()
        {
            // The explorer toolbar should always include shell-global left-pane actions, then add only the actions that belong to the selected explorer.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterExplorer(new ExplorerContribution("explorer.search", "Search", "search", 100));
            shellManager.RegisterExplorer(new ExplorerContribution("explorer.admin", "Administration", "build", 200));
            shellManager.RegisterExplorerToolbar(new ExplorerToolbarContribution("toolbar.explorer.global.home", "Home", "command.home", icon: "dashboard", order: 100));
            shellManager.RegisterExplorerToolbar(new ExplorerToolbarContribution("toolbar.explorer.search.refresh", "Refresh search", "command.search.refresh", icon: "refresh", ownerExplorerId: "explorer.search", order: 200));
            shellManager.RegisterExplorerToolbar(new ExplorerToolbarContribution("toolbar.explorer.admin.audit", "Open audit", "command.admin.audit", icon: "fact_check", ownerExplorerId: "explorer.admin", order: 200));

            shellManager.SetActiveExplorer("explorer.search");

            shellManager.ExplorerToolbarContributions.Select(contribution => contribution.DisplayName).ShouldBe([
                "Home",
                "Refresh search"
            ]);

            shellManager.SetActiveExplorer("explorer.admin");

            shellManager.ExplorerToolbarContributions.Select(contribution => contribution.DisplayName).ShouldBe([
                "Home",
                "Open audit"
            ]);
        }

        /// <summary>
        /// Confirms explorer-toolbar composition stays separate from the active-tool toolbar composition path.
        /// </summary>
        [Fact]
        public void KeepExplorerToolbarContributionsSeparateFromActiveToolToolbarContributions()
        {
            // The new explorer toolbar must not leak into the active-tool toolbar, and active-tool toolbar contributions must remain scoped to the active tab only.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterExplorer(new ExplorerContribution("explorer.search", "Search", "search", 100));
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search tool", typeof(TestToolComponent), "explorer.search", "search"));
            shellManager.RegisterExplorerToolbar(new ExplorerToolbarContribution("toolbar.explorer.global.home", "Home", "command.home", icon: "dashboard", order: 100));
            shellManager.RegisterToolbar(new ToolbarContribution("toolbar.tool.search.run", "Run query", "command.search.run", icon: "play_arrow", order: 100));
            shellManager.SetActiveExplorer("explorer.search");
            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.search"));

            shellManager.ExplorerToolbarContributions.Select(contribution => contribution.DisplayName).ShouldBe(["Home"]);
            shellManager.ToolbarContributions.Select(contribution => contribution.DisplayName).ShouldBe(["Run query"]);
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
