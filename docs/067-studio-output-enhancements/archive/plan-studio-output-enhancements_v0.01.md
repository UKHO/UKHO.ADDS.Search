# Implementation Plan

**Target output path:** `docs/067-studio-output-enhancements/plan-studio-output-enhancements_v0.01.md`

**Based on:** `docs/067-studio-output-enhancements/spec-studio-output-enhancements_v0.01.md`

**Version:** `v0.01` (`Draft`)

---

## Slice 1 — Output-pane baseline with compact merged-stream rendering and pastel severity styling

- [ ] Work Item 1: Evolve `Studio Output` from a lightweight custom log panel into a clearer output-pane baseline while preserving the existing service and command model
  - **Purpose**: Deliver the smallest meaningful end-to-end enhancement by making `Studio Output` feel like a serious IDE output pane without changing its ownership model or introducing terminal semantics.
  - **Acceptance Criteria**:
    - `Studio Output` remains a Studio-owned custom widget in the lower panel.
    - Output entries render as a single merged stream using compact monospace-oriented lines.
    - Each line presents time, severity, source, and message clearly.
    - Severity token styling uses the agreed pastel palette for at least `INFO` and `ERROR`.
    - The panel remains clearly non-terminal and does not imply prompt or command-entry behavior.
  - **Definition of Done**:
    - Widget rendering updated end to end for compact output-pane presentation.
    - Existing `SearchStudioOutputService` and `SearchStudioClearOutputCommand` reused unchanged where possible.
    - Severity-formatting helpers added where needed and covered by focused tests.
    - No backend/API changes introduced.
    - Documentation updated if implementation evidence changes the accepted baseline.
    - Can execute end to end via: Studio shell `Studio Output` panel showing real Studio events.
  - [ ] Task 1.1: Refine the output line presentation
    - [ ] Step 1: Review the current `SearchStudioOutputWidget` rendering to identify remaining custom-log chrome that conflicts with an IDE output-pane feel.
    - [ ] Step 2: Render the output as a single merged stream of compact monospace-oriented lines rather than card-like or panel-like rows.
    - [ ] Step 3: Keep the visible line format stable as `time`, `severity`, `source`, and `message`.
    - [ ] Step 4: Ensure the rendering remains non-terminal in appearance and behavior.
  - [ ] Task 1.2: Introduce severity formatting and pastel palette support
    - [ ] Step 1: Add or refine output-format helpers for timestamp and severity presentation.
    - [ ] Step 2: Apply pastel blue `#A9C7FF` to `INFO` and pastel red `#FFB3BA` to `ERROR`.
    - [ ] Step 3: Apply the color to the severity token only, not the full line.
    - [ ] Step 4: Implement the palette in a theme-respecting way so future CSS-variable substitution remains possible.
  - [ ] Task 1.3: Preserve merged-stream semantics and structured metadata
    - [ ] Step 1: Keep the existing output-entry model and merged-stream behavior as the first implementation.
    - [ ] Step 2: Preserve source metadata in the rendered output and in the underlying entries for later filtering readiness.
    - [ ] Step 3: Avoid introducing explicit channel switching or separate panes in this slice.
  - [ ] Task 1.4: Add targeted verification for the output baseline
    - [ ] Step 1: Add tests for timestamp and severity formatting helpers.
    - [ ] Step 2: Add verification for the agreed `INFO` and `ERROR` pastel token colors.
    - [ ] Step 3: Add verification that the merged stream retains `source` metadata and stable line format.
    - [ ] Step 4: Document a manual smoke path for opening the panel and reviewing real emitted Studio events.
  - **Files**:
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-widget.tsx`: compact output-pane rendering and severity token styling.
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-format.ts`: formatting helpers for timestamps and severity presentation.
    - `src/Studio/Server/search-studio/src/browser/common/search-studio-output-service.ts`: only if small output-entry helper refinements are needed.
    - `src/Studio/Server/search-studio/test/*`: output-format and widget-behavior coverage.
  - **Work Item Dependencies**: Existing `066-studio-minor-ux` implementation only.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio build`
    - `node --test .\src\Studio\Server\search-studio\test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Open `Studio Output` and verify the merged stream, compact line layout, and pastel severity token colors.
  - **User Instructions**: Trigger a few Studio actions so the panel can be reviewed with realistic output entries.

---

## Slice 2 — Reveal-latest behavior and auto-scroll for a real output-pane feel

- [ ] Work Item 2: Ensure `Studio Output` behaves like a reveal-latest output pane and keeps the newest line visible by default
  - **Purpose**: Deliver the next runnable slice by making the panel behave like a practical output pane during active workflows, so users always see the latest emitted line without manual scrolling in normal operation.
  - **Acceptance Criteria**:
    - New output keeps the latest visible line in view by default.
    - The output panel behaves as a reveal-latest surface during normal operation.
    - Clear behavior still resets the panel cleanly.
    - The implementation does not introduce terminal semantics or shell affordances.
  - **Definition of Done**:
    - Auto-scroll/reveal-latest behavior implemented end to end in the current widget.
    - Existing output service event flow reused.
    - Focused tests or stable widget-level verification added.
    - Manual verification path updated.
    - Can execute end to end via: Studio shell `Studio Output` during active navigation/actions.
  - [ ] Task 2.1: Add reveal-latest behavior to the widget
    - [ ] Step 1: Choose the simplest widget-level mechanism for keeping the latest rendered output visible.
    - [ ] Step 2: Implement default auto-scroll when new entries arrive.
    - [ ] Step 3: Confirm the implementation works with the current output ordering strategy.
  - [ ] Task 2.2: Preserve clear and refresh behavior
    - [ ] Step 1: Confirm `Clear output` leaves the panel in a clean empty-state posture.
    - [ ] Step 2: Ensure new output after a clear still reveals the latest line correctly.
    - [ ] Step 3: Ensure no regressions are introduced in the lower-panel hosting behavior.
  - [ ] Task 2.3: Add targeted verification for reveal-latest behavior
    - [ ] Step 1: Add tests or focused verification for auto-scroll when entries are appended.
    - [ ] Step 2: Add verification for output behavior immediately after clear.
    - [ ] Step 3: Add manual smoke notes for continuous output review.
  - **Files**:
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-widget.tsx`: reveal-latest and auto-scroll behavior.
    - `src/Studio/Server/search-studio/src/browser/common/search-studio-output-service.ts`: unchanged unless event sequencing needs a small refinement.
    - `src/Studio/Server/search-studio/test/*`: auto-scroll and clear-followed-by-append coverage.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio build`
    - `node --test .\src\Studio\Server\search-studio\test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Produce multiple output lines and confirm the latest line stays visible in `Studio Output`.
  - **User Instructions**: Leave the output panel open while triggering multiple actions across `Providers`, `Rules`, and `Ingestion`.

---

## Slice 3 — Output-pane interaction polish with selection/copy readiness and future filtering readiness

- [ ] Work Item 3: Strengthen the output-pane interaction model while keeping explicit channel switching and `Copy all` deferred
  - **Purpose**: Deliver a third runnable slice that improves day-to-day usability and future extensibility without expanding scope into channel panes, terminal emulation, or advanced commands.
  - **Acceptance Criteria**:
    - Output remains naturally selectable and copy-friendly.
    - The merged stream preserves `source` metadata for future filtering.
    - No explicit source/channel switching is introduced in the first implementation.
    - `Clear output` remains a toolbar action and `Copy all` remains deferred.
  - **Definition of Done**:
    - Selection/copy-friendliness reviewed and improved where needed.
    - Source metadata remains available and stable for future filtering.
    - No premature channel-switch UI is introduced.
    - Tests and manual verification cover the accepted first-release scope.
    - Can execute end to end via: normal Studio usage with output inspection and copy/select behavior.
  - [ ] Task 3.1: Confirm selection and copy-friendly rendering
    - [ ] Step 1: Review the widget markup/styling for anything that impedes natural selection.
    - [ ] Step 2: Ensure compact output rows remain selectable and readable.
    - [ ] Step 3: Avoid introducing terminal-style affordances such as prompts or fake command lines.
  - [ ] Task 3.2: Preserve future filtering readiness without adding channel switching
    - [ ] Step 1: Keep `source` metadata explicit in the rendered and underlying model.
    - [ ] Step 2: Confirm no UI is introduced for explicit channel switching in this work package.
    - [ ] Step 3: If helpful, add a small internal helper or model note that keeps filtering an easy later extension.
  - [ ] Task 3.3: Add targeted verification for first-release interaction scope
    - [ ] Step 1: Add verification that `source` metadata remains intact and visible.
    - [ ] Step 2: Add verification that the first implementation remains a merged stream.
    - [ ] Step 3: Add manual smoke notes for selecting and copying visible output text.
  - **Files**:
    - `src/Studio/Server/search-studio/src/browser/panel/search-studio-output-widget.tsx`: copy/select-friendly markup refinements.
    - `src/Studio/Server/search-studio/src/browser/common/search-studio-output-service.ts`: preserve stable entry semantics for future filtering.
    - `src/Studio/Server/search-studio/test/*`: merged-stream and source-metadata verification coverage.
  - **Work Item Dependencies**: Work Items 1 and 2.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio build`
    - `node --test .\src\Studio\Server\search-studio\test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Open `Studio Output`, select and copy visible text, and confirm the panel remains a single merged stream with visible sources.
  - **User Instructions**: None beyond the normal Studio startup prerequisites.

---

## Slice 4 — Final consistency pass, documentation, and review-ready baseline

- [ ] Work Item 4: Harmonize the completed `Studio Output` enhancements into one review-ready baseline and document the accepted direction
  - **Purpose**: Close the work package with a consistency review, final verification pass, and documentation updates so future Studio shell work starts from a clear, accepted output-pane baseline.
  - **Acceptance Criteria**:
    - `Studio Output` clearly reads as an IDE-style output pane rather than a custom card list or terminal.
    - Toolbar, rendering, and reveal-latest behavior feel coherent with the wider Studio shell.
    - Documentation captures the accepted direction and smoke path.
    - No scope creep into terminal emulation, explicit channel switching, or `Copy all` occurs.
  - **Definition of Done**:
    - Full verification path completed.
    - Work package plan/spec remain aligned with implementation evidence.
    - Wiki guidance updated if the accepted baseline changed materially.
    - Can execute end to end via: complete Studio walkthrough with output-focused review.
  - [ ] Task 4.1: Review cross-surface consistency
    - [ ] Step 1: Confirm `Studio Output` still aligns visually with the Studio shell toolbars and panel model.
    - [ ] Step 2: Confirm the panel remains non-terminal in semantics despite its shell-like density.
    - [ ] Step 3: Confirm severity palette, merged stream, and reveal-latest behavior feel coherent together.
  - [ ] Task 4.2: Complete verification and documentation
    - [ ] Step 1: Run the full frontend build and test path.
    - [ ] Step 2: Complete a final manual smoke walkthrough focused on output behavior.
    - [ ] Step 3: Update `wiki/Tools-UKHO-Search-Studio.md` if the accepted output-pane baseline needs to be documented for contributors.
    - [ ] Step 4: Record any deferred polish or follow-up ideas separately rather than expanding the scope of this work package.
  - **Files**:
    - `docs/067-studio-output-enhancements/spec-studio-output-enhancements_v0.01.md`: update only if implementation evidence requires clarification.
    - `docs/067-studio-output-enhancements/plan-studio-output-enhancements_v0.01.md`: keep aligned with completed work status if the plan is later progressed.
    - `wiki/Tools-UKHO-Search-Studio.md`: update Studio shell guidance if implementation materially changes contributor expectations.
    - `src/Studio/Server/search-studio/test/*`: any final smoke-oriented verification notes or additions.
  - **Work Item Dependencies**: Work Items 1, 2, and 3.
  - **Run / Verification Instructions**:
    - `yarn --cwd .\src\Studio\Server\search-studio build`
    - `node --test .\src\Studio\Server\search-studio\test`
    - `yarn --cwd .\src\Studio\Server build:browser`
    - `dotnet run --project .\src\Hosts\AppHost\AppHost.csproj`
    - Review `Studio Output` during a complete Studio walkthrough.
  - **User Instructions**: Review the finished output-pane baseline before requesting any additional enhancements such as filtering or `Copy all`.

---

## Overall approach and key considerations

- This plan keeps the work package vertical and lightweight: first establish the output-pane visual/semantic baseline, then add reveal-latest behavior, then strengthen interaction/future-readiness, and finally close with documentation and consistency review.
- The plan intentionally preserves the current custom widget and service model rather than introducing terminal infrastructure or `xterm.js`.
- The merged-stream model remains the first release baseline; future filtering is easier to add later if source metadata is kept explicit and stable now.
- Severity coloring should remain subdued and token-led so the panel stays readable during prolonged dark-theme use.
- Auto-scroll is treated as a first-class part of the output-pane experience rather than as a later enhancement.
- `Copy all` and explicit channel switching remain intentionally deferred to avoid scope expansion before the baseline is accepted.

---

# Architecture

## Overall Technical Approach

- The work remains a frontend-led native Theia extension enhancement within `src/Studio/Server/search-studio`.
- The existing `SearchStudioOutputWidget`, `SearchStudioOutputService`, toolbar contribution pattern, and clear command remain the foundation.
- The implementation should evolve the current panel toward an IDE-style output-pane model by refining rendering, severity formatting, and reveal-latest behavior while preserving the current data and command flow.
- No backend change is required.

```mermaid
flowchart LR
    A[Studio feature emits output] --> B[SearchStudioOutputService]
    B --> C[Structured output entries]
    C --> D[SearchStudioOutputWidget]
    D --> E[Compact merged-stream output pane]
    F[Clear output toolbar action] --> G[SearchStudioClearOutputCommand]
    G --> B
```

## Frontend

- The relevant frontend code lives in `src/Studio/Server/search-studio/src/browser/panel` and `src/Studio/Server/search-studio/src/browser/common`.
- `SearchStudioOutputWidget` remains the UI entry point for the lower-panel output surface.
- `SearchStudioOutputService` remains the append/clear source of truth for output entries.
- `search-studio-output-format.ts` should hold lightweight formatting helpers such as timestamp and severity presentation.
- The toolbar integration should continue to use Theia `TabBarToolbarContribution` rather than custom body buttons.
- The widget should remain intentionally non-terminal while adopting output-pane affordances such as compact line rendering, severity token coloring, and reveal-latest behavior.

## Backend

- No backend work is required for this package.
- Existing Studio features continue to emit output through the current frontend service path.
- No new API endpoints, persistence, or server-side output-channel infrastructure are required.
