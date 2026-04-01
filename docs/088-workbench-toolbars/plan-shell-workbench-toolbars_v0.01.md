# Implementation Plan

- Version: `v0.01`
- Work Package: `088-workbench-toolbars`
- Based on: `docs/088-workbench-toolbars/spec-domain-workbench-toolbars_v0.01.md`
- Target output path: `docs/088-workbench-toolbars/plan-shell-workbench-toolbars_v0.01.md`

## Mandatory repository standards

- All code-writing work in this plan MUST comply with `./.github/instructions/documentation-pass.instructions.md` in full.
- `./.github/instructions/documentation-pass.instructions.md` is a hard Definition of Done gate for every code-writing task in this plan.
- Every touched class, including internal and other non-public classes, must receive developer-level documentation comments.
- Every touched method and constructor, including methods and constructors on internal and other non-public types, must receive developer-level documentation comments.
- Every public method and constructor parameter must be documented with its purpose.
- Every property whose meaning is not obvious from its name must be documented.
- Sufficient inline or block comments must be added so a developer can understand purpose, logical flow, and any non-obvious algorithms.
- All C# changes must also follow `./.github/copilot-instructions.md` and `.github/instructions/coding-standards.instructions.md`, including block-scoped namespaces, Allman braces, one public type per file, and underscore-prefixed private fields.
- For this work package, validation should use targeted builds and targeted tests only; do not run the full test suite.

## Shell toolbar restructuring

- [ ] Work Item 1: Restore a visible menu-bar baseline with minimum shell menus
  - **Purpose**: Establish the always-visible shell menu bar as the first runnable slice, including the temporary right-aligned theme toggle and the minimum host-provided shell menu presence required by the specification.
  - **Acceptance Criteria**:
    - The Workbench menu bar renders visibly above all other shell content.
    - The menu bar remains visible even when there are no active-tool menu contributions.
    - The host provides at least `Help`, `Edit`, and `View` menu entries as minimum shell menus.
    - The theme toggle renders right-aligned in the menu bar.
    - Existing shell navigation remains runnable end to end after the menu bar is restored.
  - **Definition of Done**:
    - Code implemented for visible menu-bar rendering, minimum host menus, and temporary menu-bar theme-toggle placement.
    - Logging and error handling remain intact for command routing.
    - Targeted tests pass for menu-bar visibility and minimum shell menu presence.
    - Developer-level comments and XML documentation are updated in full compliance with `./.github/instructions/documentation-pass.instructions.md`.
    - Documentation updated in the current work package where implementation decisions or verification notes need recording.
    - Can execute end to end via: `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj` or by launching `WorkbenchHost` from Visual Studio and opening the shell.
  - [ ] Task 1: Update shell defaults so the menu bar is part of the normal visible Workbench chrome.
    - [ ] Step 1: Update shell region defaults in `src/workbench/server/UKHO.Workbench/WorkbenchShell/WorkbenchShellState.cs` so `WorkbenchShellRegion.MenuBar` is visible by default.
    - [ ] Step 2: Preserve the existing menu-bar placement rule that spans the full shell width above all other content.
    - [ ] Step 3: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched files, including explicit comments for type purpose, method flow, constructors, parameters, and any non-obvious properties.
  - [ ] Task 2: Register the host-provided minimum shell menus required by the specification.
    - [ ] Step 1: Update `src/workbench/server/WorkbenchHost/Program.cs` and `src/workbench/server/WorkbenchHost/Components/WorkbenchShell/WorkbenchHostShellDefaults.cs` to define and register minimum shell menu contributions for `Help`, `Edit`, and `View`.
    - [ ] Step 2: Use placeholder or skeletal menu commands only as needed to satisfy menu presence, without specifying the detailed future menu contents that are explicitly deferred by the specification.
    - [ ] Step 3: Ensure command registration continues to use the shared shell manager path rather than introducing menu-specific shortcuts.
    - [ ] Step 4: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched files.
  - [ ] Task 3: Render the temporary right-aligned theme toggle inside the menu bar.
    - [ ] Step 1: Update `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor` so the menu bar includes a right-aligned action area containing `RadzenAppearanceToggle`.
    - [ ] Step 2: Keep the layout visually close to the current Radzen Material styling and avoid module-specific CSS workarounds.
    - [ ] Step 3: Update `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css` only as needed to support the restored menu-bar structure and right alignment.
    - [ ] Step 4: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched files.
  - [ ] Task 4: Add targeted regression coverage for the restored menu-bar slice.
    - [ ] Step 1: Update `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs` to assert visible menu-bar rendering, minimum host menu presence, and right-aligned theme-toggle markup.
    - [ ] Step 2: Add or update shell-manager-level tests if command/menu registration behavior needs direct verification outside markup assertions.
    - [ ] Step 3: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched test files.
  - **Files**:
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor`: render the visible menu bar and temporary theme-toggle placement.
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css`: support restored menu-bar layout.
    - `src/workbench/server/WorkbenchHost/Program.cs`: register minimum shell menu contributions.
    - `src/workbench/server/WorkbenchHost/Components/WorkbenchShell/WorkbenchHostShellDefaults.cs`: add stable identifiers for host menu contributions as needed.
    - `src/workbench/server/UKHO.Workbench/WorkbenchShell/WorkbenchShellState.cs`: make the menu bar visible by default.
    - `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`: assert menu-bar behavior.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet build src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj --filter MainLayoutRenderingTests`
    - Run `WorkbenchHost`, open the shell, confirm `Help`, `Edit`, and `View` are visible and the theme toggle appears at the right side of the menu bar.
  - **User Instructions**:
    - Use the shell home page after startup.
    - No special data setup is required for this slice.

- [ ] Work Item 2: Introduce mixed explorer-toolbar contributions and migrate `Home`
  - **Purpose**: Create the dedicated explorer-toolbar contribution surface so left-pane and workspace-global actions no longer depend on the old top toolbar, and migrate `Home` to the new explorer toolbar.
  - **Acceptance Criteria**:
    - The explorer pane renders a toolbar at its top.
    - The explorer toolbar supports a mixed surface composed from shell-global left-pane actions and active-explorer contributions.
    - The host-owned `Home` action renders in the explorer toolbar.
    - Executing `Home` from the explorer toolbar still opens or focuses the overview tool through the shared command path.
    - The shell remains runnable end to end after `Home` is migrated away from the top toolbar.
  - **Definition of Done**:
    - Explorer-toolbar contribution contracts, composition, and rendering are implemented.
    - `Home` is registered through the explorer-toolbar path rather than the active-tool toolbar path.
    - Targeted tests pass for explorer-toolbar composition and `Home` placement.
    - Developer-level comments and XML documentation are updated in full compliance with `./.github/instructions/documentation-pass.instructions.md`.
    - Documentation updated in the current work package where implementation decisions or verification notes need recording.
    - Can execute end to end via: `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj` or by launching `WorkbenchHost` from Visual Studio and using the explorer pane.
  - [ ] Task 1: Introduce the explorer-toolbar contribution contract in the Workbench model.
    - [ ] Step 1: Add a dedicated explorer-toolbar contribution type in `src/workbench/server/UKHO.Workbench` using existing contribution naming and ordering conventions.
    - [ ] Step 2: Keep the contribution contract shell-owned and generic so modules can contribute explorer-scoped actions without knowing layout details.
    - [ ] Step 3: If new C# types are introduced, place one public type per file and follow repository namespace and brace conventions.
    - [ ] Step 4: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched files.
  - [ ] Task 2: Extend shell contribution composition to expose mixed explorer-toolbar content.
    - [ ] Step 1: Update `src/workbench/server/UKHO.Workbench.Services/Contributions/RuntimeContributionManager.cs` so explorer-toolbar contributions can be composed separately from active-tool toolbar contributions.
    - [ ] Step 2: Update `src/workbench/server/UKHO.Workbench.Services/Shell/WorkbenchShellManager.cs` to register and expose explorer-toolbar contributions.
    - [ ] Step 3: Ensure the composition model supports both host-global left-pane actions and active-explorer-specific actions in one explorer toolbar surface.
    - [ ] Step 4: Preserve existing menu, toolbar, and status-bar composition behavior outside the new explorer-toolbar path.
    - [ ] Step 5: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched files.
  - [ ] Task 3: Render the explorer toolbar and migrate `Home`.
    - [ ] Step 1: Update `src/workbench/server/WorkbenchHost/Program.cs` so `Home` is registered as an explorer-toolbar contribution rather than as a top-toolbar contribution.
    - [ ] Step 2: Update `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor` and `MainLayout.razor.cs` to render the explorer toolbar at the top of the explorer pane.
    - [ ] Step 3: Update `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css` so the explorer header and explorer toolbar coexist cleanly without breaking the left-pane layout.
    - [ ] Step 4: Remove host-specific `Home` extraction logic from the active-tool toolbar path once `Home` is available through the explorer toolbar.
    - [ ] Step 5: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched files.
  - [ ] Task 4: Add targeted regression coverage for explorer-toolbar composition.
    - [ ] Step 1: Update `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs` to assert explorer-toolbar rendering and `Home` placement.
    - [ ] Step 2: Add or update service-level tests in `test/workbench/server/UKHO.Workbench.Services.Tests` for mixed explorer-toolbar composition rules.
    - [ ] Step 3: Add or update model-level tests in `test/workbench/server/UKHO.Workbench.Tests` if the new contribution contract requires direct behavioral coverage.
    - [ ] Step 4: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched test files.
  - **Files**:
    - `src/workbench/server/UKHO.Workbench/`: add explorer-toolbar contribution model types.
    - `src/workbench/server/UKHO.Workbench.Services/Contributions/RuntimeContributionManager.cs`: compose explorer-toolbar contributions.
    - `src/workbench/server/UKHO.Workbench.Services/Shell/WorkbenchShellManager.cs`: register and expose explorer-toolbar contributions.
    - `src/workbench/server/WorkbenchHost/Program.cs`: migrate `Home` to explorer-toolbar registration.
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor`: render the explorer toolbar.
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.cs`: support explorer-toolbar state and command execution.
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css`: style explorer-toolbar placement.
    - `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`: assert explorer-toolbar rendering.
    - `test/workbench/server/UKHO.Workbench.Services.Tests/`: add composition tests for explorer-toolbar behavior.
    - `test/workbench/server/UKHO.Workbench.Tests/`: add model tests if needed for new contracts.
  - **Work Item Dependencies**: Depends on Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet build src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj --filter MainLayoutRenderingTests`
    - `dotnet test test/workbench/server/UKHO.Workbench.Services.Tests/UKHO.Workbench.Services.Tests.csproj`
    - Run `WorkbenchHost`, use the explorer pane toolbar, and confirm `Home` opens or focuses the overview tool.
  - **User Instructions**:
    - Start `WorkbenchHost`.
    - Confirm the explorer toolbar is visible in the left pane and use `Home` from that location.

- [ ] Work Item 3: Move active-tool toolbars into the active tab view and delete the old shell row
  - **Purpose**: Complete the toolbar restructuring by moving active-tool toolbar contributions into the centre tab view, removing the shell-wide top toolbar row, and deleting the obsolete `WorkbenchShellRegion.ActiveToolToolbar` region.
  - **Acceptance Criteria**:
    - The shell no longer renders the old full-width top toolbar row.
    - Active-tool toolbar contributions render inside the active centre-pane tab view.
    - Switching tabs updates the in-tab active-tool toolbar content.
    - No empty active-tool toolbar renders when no tab is open.
    - `WorkbenchShellRegion.ActiveToolToolbar` is deleted rather than repurposed.
    - Output-panel toolbar behavior remains unchanged.
  - **Definition of Done**:
    - Layout, region-model, and active-tool toolbar rendering changes are implemented.
    - The top toolbar row and related special-case host logic are removed.
    - Targeted tests pass for in-tab toolbar rendering, tab switching, and top-row removal.
    - Developer-level comments and XML documentation are updated in full compliance with `./.github/instructions/documentation-pass.instructions.md`.
    - Documentation updated in the current work package where implementation decisions or verification notes need recording.
    - Can execute end to end via: `dotnet run --project src/workbench/server/WorkbenchHost/WorkbenchHost.csproj` or by launching `WorkbenchHost` from Visual Studio and switching between tabs.
  - [ ] Task 1: Reshape the shell layout so active-tool actions live inside the tab view.
    - [ ] Step 1: Update `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor` to remove the shell-wide toolbar row from the outer grid.
    - [ ] Step 2: Render the active-tool toolbar inside the tool-surface/tab-view structure, directly above the active body and below the tab strip.
    - [ ] Step 3: Ensure no in-tab toolbar surface renders when no tab is active.
    - [ ] Step 4: Update `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css` so the new in-tab toolbar integrates cleanly with the existing tab strip and tool body.
    - [ ] Step 5: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched files.
  - [ ] Task 2: Remove the obsolete shell-region concept for the old top toolbar.
    - [ ] Step 1: Delete `ActiveToolToolbar` from `src/workbench/server/UKHO.Workbench/WorkbenchShell/WorkbenchShellRegion.cs`.
    - [ ] Step 2: Update `src/workbench/server/UKHO.Workbench/WorkbenchShell/WorkbenchShellState.cs` and any dependent layout calculations so the shell no longer models a dedicated top-toolbar row.
    - [ ] Step 3: Remove any stale region checks or properties in `MainLayout.razor.cs` that only exist to support the old row.
    - [ ] Step 4: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched files.
  - [ ] Task 3: Align host registration and contribution access with the final toolbar arrangement.
    - [ ] Step 1: Remove old host assumptions that treat the top toolbar as a mixed shell surface.
    - [ ] Step 2: Keep active-tool toolbar contributions composed through the existing active-tool path, but rename local members in `MainLayout.razor.cs` if needed so the code clearly distinguishes explorer-toolbar versus active-tool-toolbar responsibilities.
    - [ ] Step 3: Confirm the menu bar, explorer toolbar, and in-tab active-tool toolbar all continue to use the shared `ExecuteCommandAsync` path.
    - [ ] Step 4: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched files.
  - [ ] Task 4: Add targeted regression coverage for the final shell arrangement.
    - [ ] Step 1: Update `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs` to assert absence of the old top-toolbar row and presence of the in-tab active-tool toolbar.
    - [ ] Step 2: Update `test/workbench/server/WorkbenchHost.Tests/WorkbenchShellManagerTests.cs` and any related contribution tests if region or contribution expectations change.
    - [ ] Step 3: Add assertions that output-panel toolbar markup and behavior remain unaffected.
    - [ ] Step 4: Apply the mandatory documentation pass requirements from `./.github/instructions/documentation-pass.instructions.md` to all touched test files.
  - **Files**:
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor`: remove the old top row and render the in-tab toolbar.
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.cs`: remove old top-toolbar helpers and support in-tab toolbar rendering.
    - `src/workbench/server/WorkbenchHost/Components/Layout/MainLayout.razor.css`: style the final tool-surface layout.
    - `src/workbench/server/UKHO.Workbench/WorkbenchShell/WorkbenchShellRegion.cs`: delete `ActiveToolToolbar`.
    - `src/workbench/server/UKHO.Workbench/WorkbenchShell/WorkbenchShellState.cs`: remove obsolete region usage.
    - `test/workbench/server/WorkbenchHost.Tests/MainLayoutRenderingTests.cs`: assert final shell layout.
    - `test/workbench/server/WorkbenchHost.Tests/WorkbenchShellManagerTests.cs`: assert any affected shell-manager behavior.
    - `test/workbench/server/UKHO.Workbench.Services.Tests/`: update contribution tests if surface separation changes expected composition.
  - **Work Item Dependencies**: Depends on Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet build src/workbench/server/WorkbenchHost/WorkbenchHost.csproj`
    - `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj --filter MainLayoutRenderingTests`
    - `dotnet test test/workbench/server/WorkbenchHost.Tests/WorkbenchHost.Tests.csproj --filter WorkbenchShellManagerTests`
    - `dotnet test test/workbench/server/UKHO.Workbench.Services.Tests/UKHO.Workbench.Services.Tests.csproj`
    - Run `WorkbenchHost`, open one or more tabs, verify active-tool actions render inside the tab view, and confirm the old shell-wide top toolbar row is gone.
  - **User Instructions**:
    - Start `WorkbenchHost`.
    - Open the overview tool and any available module tools.
    - Switch between tabs and confirm the active-tool toolbar follows the active tab.

## Overall approach summary

This plan delivers the toolbar redesign in three runnable vertical slices:

1. restore the menu bar as stable shell chrome
2. introduce the mixed explorer-toolbar contribution surface and migrate `Home`
3. move active-tool toolbars into the active tab view and delete the obsolete top-toolbar region

Key implementation considerations:

- keep command routing centralized through the existing shell manager
- separate explorer-toolbar composition from active-tool toolbar composition rather than overloading one surface
- avoid CSS or module-specific workarounds by enforcing layout behavior in the Workbench shell itself
- preserve the output-panel toolbar and bottom-pane behavior unchanged
- treat `./.github/instructions/documentation-pass.instructions.md` as mandatory completion criteria for every code-writing slice in this plan
