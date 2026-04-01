# Workbench shell

The `083-workbench-model` bootstrap slice introduces the first runnable Workbench shell under `src/workbench/server/WorkbenchHost`.

## What the bootstrap slice delivers

- a desktop-like shell rendered by Blazor Server
- a desktop-like shell whose menu-bar region still exists in the shell model but is currently marked not visible, so the active-tool toolbar, activity rail, explorer, central tool surface, and status bar remain as the visible chrome
- a host-owned exemplar tool (`Workbench overview`) that opens in the center region
- singleton tool activation, so reopening the same tool focuses the existing instance instead of duplicating it
- shell layout built with `UKHO.Workbench.Layout` grid and splitter primitives

## What the dynamic module-loading slice adds

- a host-owned `modules.json` file under `src/workbench/server/WorkbenchHost` that declares approved probe roots and per-module enablement flags
- reflection-based discovery of assemblies named `UKHO.Workbench.Modules.*` before the host finalizes the DI container
- a bounded `IWorkbenchModule` registration contract in `UKHO.Workbench` so modules can register services and tools without direct shell access
- structured discovery and load logging, plus buffered user-safe startup notifications shown by the interactive shell
- the initial module map from `UKHO.Workbench.Modules.Search`, `UKHO.Workbench.Modules.PKS`, `UKHO.Workbench.Modules.FileShare`, and `UKHO.Workbench.Modules.Admin`, all of which open through the same singleton shell activation path as host-owned tools

## What the command-and-runtime-contribution slice adds

- explorer items are now declarative Workbench contributions backed by command ids and activation targets rather than direct component activation in the layout
- commands are now the shared action abstraction across explorer buttons, menu items, toolbar buttons, and hosted tool interactions
- the shell now composes menu, toolbar, and status-bar surfaces from static contributions plus runtime contributions published by the active tool instance only
- `ToolContext` now provides bounded runtime capabilities for command invocation, tool activation requests, title/icon/badge updates, notifications, selection publication, and runtime shell contribution updates
- the dummy `Search query` tool now demonstrates active-tool runtime menu, toolbar, and status-bar participation, while `Search ingestion`, `Ingestion rule editor`, `PKS operations`, `File Share workspace`, and `Administration` prove the first repository-specific multi-module tool map

## What the first tabbed shell slice adds

- logical tab identity is now a bounded shell concept, so reopening the same logical target focuses the existing tab instead of replacing the current center-surface content
- the shell now tracks ordered open tabs, an active tab, explorer-item selection, and most-recently-active tab history for close behavior
- explorer single-click now selects an item without opening it, while explorer double-click routes through the shared command path and opens or focuses the matching tab immediately
- the center surface now renders a visible tab strip above the content area and keeps inactive tab components mounted so open tabs preserve in-memory state while they remain open
- closing the last remaining tab returns the shell to an explicit empty-state center surface and restores explorer focus

## What the tab lifecycle and metadata slice adds

- activation targets now carry bounded parameter identity, so the same tool can reuse a tab for matching parameters and open separate tabs for different parameter identities
- explorer-owned activation paths now seed initial tab title and icon metadata, and hosted views can replace that metadata immediately through `ToolContext` even while their tabs are inactive
- tab close now marks the runtime tool instance disposed as soon as the shell removes the tab, while Blazor continues to dispose the hosted component through normal render-tree removal
- the tab strip now exposes a basic right-click context menu with `Close` only, and that action routes through the same shell close path used by the visible tab-close button
- shell diagnostics now cover runtime title and icon updates as well as activation and close flows, so metadata changes remain traceable in logs

## What the overflow and tooltip slice adds

- the shell now tracks a bounded visible tab-strip window separately from the full logical open-tab order, so overflow activation can reveal hidden tabs with minimal strip movement instead of reordering the underlying tab collection
- the center tab strip now renders an always-visible overflow dropdown on the right and keeps its entries text-only, with active-tab indication but no close, filter, or search affordances in this first implementation
- both visible-strip tab titles and overflow entry titles now truncate with ellipsis and open a Radzen tooltip on every hover so long runtime titles remain readable without changing the stock Material look and feel
- overflow selection now flows through the shared shell activation path, which keeps diagnostics, active-tab composition, and visible-window adjustments aligned with the rest of the tab lifecycle

## What the shell style refinement slice adds

- the outer shell now renders flush with the browser viewport instead of using decorative outer padding, which makes the Workbench read more like a desktop surface than a padded web page
- the second-row toolbar no longer shows an `Active tab` eyebrow label and now surfaces the host-owned `Home` action in that leading position
- the activity rail now renders as an icon-only strip, while hover and focus use the shared Radzen tooltip service so explorer labels remain discoverable without persistent rail text
- the host-owned `Overview` menu and toolbar action is now labeled `Home` to match the refined shell chrome
- the working area now keeps the activity rail fixed at `64px` with no splitter between that rail and the explorer, leaving only the explorer-to-centre boundary resizeable
- the centre tab host now removes its extra top, bottom, and left shell padding so the tab strip sits flush with the content surface while the always-visible overflow affordance stays anchored to the right edge

## Temporary menu-bar state

- the shell still keeps `MenuBar` as a first-class shell region and continues to support menu contributions, but the bootstrap visible-region set currently marks that region as hidden so `MainLayout` omits the menu row entirely while toolbar and tool-page actions mature

## What the output foundation slice adds

- the shell now owns a shared `IWorkbenchOutputService` and immutable output contracts in `UKHO.Workbench.Output`, giving host and shell code a single append-only session stream
- the output stream is retained in memory with a bounded `250`-entry limit, preserving chronological order while discarding the oldest entries when the limit is exceeded
- `MainLayout` now renders an `Output` toggle on the far-left side of the status bar and keeps the bottom output panel collapsed by default even when startup entries already exist
- opening the panel inserts a full-width bottom pane between the centre working area and the status bar using the existing `UKHO.Workbench.Layout` grid and splitter primitives with an initial `1* : 4*` output-to-centre ratio
- the first shell-owned startup entry is now written during host bootstrap so the output panel shows real Workbench activity rather than synthetic demo text when it is opened

## What the output toolbar and session-state slice adds

- the shared `OutputPanelState` now tracks hidden unseen severity, auto-scroll, wrap mode, expanded-row ids, and the current-session output height so the shell can restore one coherent output-panel view state
- the status-bar `Output` toggle now shows the most severe unseen hidden output level while the panel remains collapsed and clears that indicator immediately when the panel is opened or the stream is cleared
- the output panel now includes a compact toolbar with `Clear`, `Auto-scroll`, `Scroll to end`, and `Wrap`, plus a lightweight per-entry copy action that uses a shell-owned browser helper instead of turning the panel into a JavaScript-owned widget
- manual upward scrolling now disables `Auto-scroll`, while `Scroll to end` re-enables it and requests a deferred browser scroll after the next render
- dragging the existing shell splitter between the centre surface and the output pane now stores the user-adjusted height for the current session, so closing and reopening the panel restores the last in-session size instead of resetting to the default split

## What the structured output rendering slice adds

- the output panel now renders compact structured rows through a focused shell-owned `WorkbenchOutputRow` component so `MainLayout` can stay readable while the output surface gains IDE-like density
- collapsed rows now show a disclosure chevron, subtle visual severity marker, local timestamp, source, and summary, while optional event codes remain hidden until the row is expanded
- multiple rows can remain expanded at once, and expanded details now render inline beneath the summary with preserved line breaks plus a row-scoped copy action
- the global `Wrap` toggle now applies consistently to both collapsed summaries and expanded details, while the unwrapped mode keeps horizontal scrolling available for long diagnostic content

## What the output-first shell migration slice adds

- module discovery, module-load success, and module-load failure events are now buffered during startup and replayed into the shared Workbench output stream as `Debug`, `Warning`, or `Error` entries once the shell output service is available
- startup and runtime user-safe notifications are now mirrored into the output stream under the `Notifications` source while the existing toast behaviour continues to surface the same safe summaries to users
- shell context snapshots and historical status-bar messages are now written into the output stream as `Shell context` and `Status` diagnostics, so the output panel becomes the historical trace for shell state changes
- the status bar is now intentionally reduced to the far-left `Output` toggle and its hidden unseen-severity indicator instead of carrying persistent right-aligned context and readiness text
- historical output remains shell-wide for the whole in-memory session, so messages written before navigation or tool switches remain visible after the user changes tabs or explorers

## What the output-trimming polish slice adds

- the output toolbar now keeps the minimum-level selector in a dedicated leading group and surfaces a shell-owned `Visible: ...` summary so users can immediately see which retained entries are currently shown
- the toolbar layout now keeps the selector, visibility summary, and panel actions visually aligned with the existing Radzen Material shell styling without introducing module-specific CSS workarounds
- panel-local interactions continue to behave predictably with filtered output: clearing the stream empties both retained and visible output, closing the panel dismisses the find strip, and reopening preserves the selected session filter
- overlapping output-terminal synchronization requests are now serialized so first-render and post-render refresh paths do not duplicate retained output lines when the panel initializes
- integrated host tests now cover the polished filter summary, filtered clear and auto-scroll behavior, and visibility-toggle compatibility with the panel-local find workflow

## Project responsibilities

| Project | Responsibility |
|---|---|
| `src/workbench/server/UKHO.Workbench` | Shared shell contracts and models such as shell regions, tool definitions, tool instances, activation targets, and shell state. |
| `src/workbench/server/UKHO.Workbench.Services` | Shell orchestration, including command routing, explorer composition, runtime contribution composition, fixed context projection, tool activation, and the host-facing `WorkbenchShellManager`. |
| `src/workbench/server/UKHO.Workbench.Infrastructure` | `modules.json` reading, probe-root scanning, bounded reflection-based module loading, and composition root extensions. |
| `src/workbench/server/WorkbenchHost` | Blazor host composition, shell UI, startup bootstrap, module discovery orchestration, and host-owned notifications. |
| `src/Workbench/modules/UKHO.Workbench.Modules.Search` | Dynamic Search module assembly contributing the dummy `Search ingestion`, `Search query`, and `Ingestion rule editor` tools. |
| `src/Workbench/modules/UKHO.Workbench.Modules.PKS` | Dynamic PKS module assembly contributing the dummy `PKS operations` tool. |
| `src/Workbench/modules/UKHO.Workbench.Modules.FileShare` | Dynamic File Share module assembly contributing the dummy `File Share workspace` tool. |
| `src/Workbench/modules/UKHO.Workbench.Modules.Admin` | Dynamic Admin module assembly contributing the dummy `Administration` tool. |

## Startup flow for the Workbench shell

1. `WorkbenchHost` registers the Workbench infrastructure and service-layer dependencies.
2. `WorkbenchHost` reads `modules.json`, resolves probe roots, and scans for enabled `UKHO.Workbench.Modules.*` assemblies.
3. Valid modules register services and tool definitions through the bounded `IWorkbenchModule` contract before DI finalization.
4. Host startup registers the `Workbench overview` tool, replays buffered startup output into the shared shell-wide output stream, and then applies module-contributed tools to the tab-aware shell manager.
5. The shell manager selects the bootstrap explorer and activates the first enabled module tool when one is available, otherwise it falls back to the host-owned overview tool.
6. `MainLayout` renders the desktop-like shell chrome, mirrors startup notifications into the output stream, and replays those same safe notifications through the existing toast surface.
7. `Index` renders the open tab collection into the center working surface and shows only the active tab while inactive tabs remain mounted.

## Runtime interaction flow

1. A user selects or double-clicks an explorer item, or invokes a menu item, toolbar button, or hosted tool button.
2. The interaction resolves to a registered `CommandContribution`.
3. Explorer single-click updates shell selection only, while a command-driven activation request opens a new tab or focuses the existing logical tab.
4. The active tool instance can update its title, icon, badge, selection, notifications, and runtime shell contributions through `ToolContext`.
5. The shell recomposes menu, toolbar, and status-bar surfaces so only the active tool contributes runtime items.

## Verification

Use the targeted commands from `docs/086-workbench-output/implementation-plan.md` for the current output slice:

- `dotnet build src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
- `dotnet test test/workbench/server/UKHO.Workbench.Tests/UKHO.Workbench.Tests.csproj`
- `dotnet test test/workbench/server/UKHO.Workbench.Services.Tests/UKHO.Workbench.Services.Tests.csproj`
- `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj`
- `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`

When the host starts, browse to `/` and confirm the shell loads with the enabled module map visible in the explorer, that `Search ingestion`, `Search query`, `Ingestion rule editor`, `PKS operations`, `File Share workspace`, and `Administration` open in the center region, and that reopening them re-focuses the existing singleton tool instance. Disable one or more modules in `modules.json` and restart to confirm the disabled tools disappear from the explorer.

For the current output slice, also open the `Output` panel, confirm the toolbar reads `Visible: Info and above`, switch between `Error`, `Warning and above`, `Info and above`, and `Debug`, use `Find`, `Clear`, `Auto-scroll`, and `Scroll to end`, then close and reopen the panel to confirm the selected session filter is preserved while the panel-local find strip is dismissed.
