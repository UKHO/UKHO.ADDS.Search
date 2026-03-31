using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Radzen;
using Shouldly;
using UKHO.Workbench.Services;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using WorkbenchHost.Components.Pages;
using WorkbenchHost.Services;
using Xunit;

namespace WorkbenchHost.Tests
{
    /// <summary>
    /// Verifies the Workbench center-surface page introduced for the first tabbed Workbench slice.
    /// </summary>
    public class IndexRenderingTests
    {
        /// <summary>
        /// Confirms the page renders the explicit empty state when no tab is open.
        /// </summary>
        [Fact]
        public async Task RenderTheEmptyStateWhenNoWorkbenchTabIsOpen()
        {
            // The first tabbed slice should communicate clearly when the center surface is empty so users know to return to the explorer.
            await using var serviceProvider = CreateServiceProvider();
            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());

            var html = await RenderIndexAsync(renderer);

            html.ShouldContain("No Workbench tab is open");
            html.ShouldContain("Return to the explorer");
        }

        /// <summary>
        /// Confirms the page keeps inactive tabs mounted while showing only the active tab surface.
        /// </summary>
        [Fact]
        public async Task RenderAllOpenTabsAndHideInactiveTabContent()
        {
            // The page should keep all open tab components in the render tree so inactive tabs preserve in-memory state while only the active tab remains visible.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            shellManager.RegisterTool(new ToolDefinition("tool.one", "Tool one", typeof(TestToolComponent), "explorer.bootstrap", "looks_one"));
            shellManager.RegisterTool(new ToolDefinition("tool.two", "Tool two", typeof(TestToolComponent), "explorer.bootstrap", "looks_two"));

            var firstTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.one"));
            var secondTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.two"));
            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());

            var html = await RenderIndexAsync(renderer);

            html.ShouldContain($"data-tab-id=\"{firstTool.InstanceId}\"");
            html.ShouldContain($"data-tab-id=\"{secondTool.InstanceId}\"");
            html.ShouldContain($"data-tool-instance=\"{firstTool.InstanceId}\"");
            html.ShouldContain($"data-tool-instance=\"{secondTool.InstanceId}\"");
            html.ShouldContain("workbench-tool-surface-pane workbench-tool-surface-pane--inactive");
            html.ShouldContain("workbench-tool-surface-pane workbench-tool-surface-pane--active");
        }

        /// <summary>
        /// Creates the service provider used by the Workbench page rendering tests.
        /// </summary>
        /// <returns>A fully configured service provider for the Workbench host shell.</returns>
        private static ServiceProvider CreateServiceProvider()
        {
            // The test provider mirrors the host registrations so the center-surface page renders with the same service graph as the runtime host.
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddRadzenComponents();
            services.AddWorkbenchServices();
            services.AddSingleton<WorkbenchStartupNotificationStore>();
            services.AddSingleton<IErrorBoundaryLogger, TestErrorBoundaryLogger>();
            services.AddSingleton<IJSRuntime, TestJsRuntime>();
            services.AddSingleton<NavigationManager, TestNavigationManager>();
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Renders the Workbench index page to static HTML.
        /// </summary>
        /// <param name="renderer">The renderer that should produce the HTML output.</param>
        /// <returns>The rendered HTML for the Workbench index page.</returns>
        private static Task<string> RenderIndexAsync(HtmlRenderer renderer)
        {
            // Rendering the page through the real HtmlRenderer verifies the same component structure that the runtime host serves.
            return renderer.Dispatcher.InvokeAsync(async () =>
            {
                var output = await renderer.RenderComponentAsync<WorkbenchHost.Components.Pages.Index>();
                return output.ToHtmlString();
            });
        }

        /// <summary>
        /// Supplies a minimal tool component for center-surface rendering tests.
        /// </summary>
        private sealed class TestToolComponent : ComponentBase
        {
            /// <summary>
            /// Gets or sets the bounded tool context supplied by the Workbench shell.
            /// </summary>
            [Parameter]
            public ToolContext ToolContext { get; set; } = null!;

            /// <summary>
            /// Builds the simple marker markup used by the rendering tests.
            /// </summary>
            /// <param name="builder">The render-tree builder used to produce component output.</param>
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                // The test component renders only a stable marker so the page tests can assert that active and inactive tabs both remain mounted.
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "test-workbench-tool");
                builder.AddAttribute(2, "data-tool-instance", ToolContext.ToolInstanceId);
                builder.AddContent(3, ToolContext.ToolInstanceId);
                builder.CloseElement();
            }
        }

        /// <summary>
        /// Supplies a minimal JS runtime stub for static component rendering tests.
        /// </summary>
        private sealed class TestJsRuntime : IJSRuntime
        {
            /// <summary>
            /// Returns a default value because the page rendering test does not execute JavaScript.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The JavaScript identifier requested by the component.</param>
            /// <param name="args">The arguments that would be passed to JavaScript.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                // Static HTML rendering never executes JavaScript for these page tests, so returning the default value is sufficient.
                return ValueTask.FromResult(default(TValue)!);
            }

            /// <summary>
            /// Returns a default value because the page rendering test does not execute JavaScript.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The JavaScript identifier requested by the component.</param>
            /// <param name="cancellationToken">The cancellation token that would flow to the JavaScript invocation.</param>
            /// <param name="args">The arguments that would be passed to JavaScript.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                // Static HTML rendering never executes JavaScript for these page tests, so returning the default value is sufficient.
                return ValueTask.FromResult(default(TValue)!);
            }
        }

        /// <summary>
        /// Provides a no-op error-boundary logger for static page rendering tests.
        /// </summary>
        private sealed class TestErrorBoundaryLogger : IErrorBoundaryLogger
        {
            /// <summary>
            /// Accepts error-boundary exceptions without performing any additional logging.
            /// </summary>
            /// <param name="exception">The exception captured by the error boundary.</param>
            /// <returns>A completed value task because the test logger performs no work.</returns>
            public ValueTask LogErrorAsync(Exception exception)
            {
                // Static page rendering tests only need the error-boundary service to exist so component construction can succeed.
                return ValueTask.CompletedTask;
            }
        }

        /// <summary>
        /// Supplies a minimal navigation manager for Radzen services during static rendering tests.
        /// </summary>
        private sealed class TestNavigationManager : NavigationManager
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestNavigationManager"/> class.
            /// </summary>
            public TestNavigationManager()
            {
                // Static rendering only needs a stable base URI so component services can be constructed successfully.
                Initialize("http://localhost/", "http://localhost/");
            }

            /// <summary>
            /// Ignores navigation requests because the page rendering tests do not exercise navigation behavior.
            /// </summary>
            /// <param name="uri">The destination URI.</param>
            /// <param name="options">The navigation options associated with the request.</param>
            protected override void NavigateToCore(string uri, NavigationOptions options)
            {
                // The static renderer never navigates, so the test stub simply tracks the last URI value.
                Uri = ToAbsoluteUri(uri).ToString();
            }
        }
    }
}
