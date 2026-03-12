# Implementation Plan

Work Package: `docs/028-query-ui-and/`  
Plan Version: v0.01  
Based on:
- `docs/028-query-ui-and/spec-query-ui-and-combined_v0.01.md`

## Overall approach
Deliver a runnable `QueryServiceHost` UI skeleton in vertical slices. Each Work Item results in a buildable, runnable Blazor UI that is demoable via the host’s root route (`/`).

Notes / constraints from spec:
- Root route `/` hosts the Query UI page.
- Radzen is used for UI components.
- Facets are static placeholders (`Region`, `Type`) and are not derived from backend search.
- No URL query-string encoding.
- No automated tests required for v0.01.
- Empty state guidance must match `docs/search_ui_showcase_spec.md`.

## Structure / Setup
- [x] Work Item 1: Establish `QueryServiceHost` Query UI shell at `/` - Completed
  - **Purpose**: Provide a runnable UI entrypoint with the 4-panel layout and empty state.
  - **Acceptance Criteria**:
    - Navigating to `/` renders the page.
    - The page shows the 4-panel layout (Search Bar top, Facets left, Results center, Details right, Status bottom).
    - Empty state content appears when query is empty (exact text and examples from spec).
  - **Definition of Done**:
    - Root route `/` points to `QueryPage`.
    - Layout matches ASCII diagram intent.
    - Builds successfully.
    - Documentation references updated if needed.
    - Can execute end-to-end via: run host and browse `/`.
  - [x] Task 1.1: Locate existing `QueryServiceHost` routing and layout - Completed
    - [x] Step 1: Identify host project path under `src/Hosts/*` and confirm it’s Blazor. (Found at `src/Hosts/QueryServiceHost`)
    - [x] Step 2: Identify current root component (`App.razor` / router) and existing pages. (Root route `/` was `Components/Pages/Home.razor`)
    - [x] Step 3: Determine whether Radzen is already referenced and how it’s configured (theme/css/services). (Radzen services already registered in `Program.cs` and `RadzenComponents` present in `MainLayout`)
  - [x] Task 1.2: Add `QueryPage` at `/` and basic layout regions - Completed
    - [x] Step 1: Create `QueryPage` (page component) and set route to `/`. (Implemented by replacing `Home.razor` content)
    - [x] Step 2: Implement basic layout grid using Radzen layout components (or existing layout patterns in host). (Radzen row/columns + cards)
    - [x] Step 3: Add placeholders for: Search Bar area, Facets panel, Results panel, Details panel, Status bar. (Added placeholders in page)
    - [x] Step 4: Add empty state content per FR-12 when query string is empty. (Exact text + examples)
  - **Files** (expected):
    - `src/Hosts/<QueryServiceHost>/**/Pages/QueryPage.razor`: new page at `/` composing layout.
    - `src/Hosts/<QueryServiceHost>/**/Shared/MainLayout.razor` (or equivalent): adjust if required.
  - **Summary (Work Item 1)**:
    - Implemented the Query UI shell at `/` using Radzen components with the required 4-panel layout and status bar placeholder.
    - Added empty state guidance matching FR-12 (exact text + examples).
    - Removed the default left navigation sidebar to keep navigation minimal.
    - Files touched: `src/Hosts/QueryServiceHost/Components/Pages/Home.razor`, `src/Hosts/QueryServiceHost/Components/Layout/MainLayout.razor`.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - `dotnet run` for the `QueryServiceHost` project.
    - Browse to `http(s)://localhost:<port>/`.

## Vertical Slice 1 — Query state + stub search client
- [x] Work Item 2: Introduce UI state container and stub `IQueryUiSearchClient` - Completed
  - **Purpose**: Enable submit-driven query execution flow with deterministic placeholder results and query metadata.
  - **Acceptance Criteria**:
    - Entering query text and submitting triggers a “search” call.
    - Results panel populates with placeholder hits.
    - Status bar shows total and duration from the stub response.
    - Error message is shown on stubbed failure path.
  - **Definition of Done**:
    - `IQueryUiSearchClient` and stub implementation exist in host/UI layer.
    - DI registration is present in host startup.
    - Query execution is explicit submit only.
    - Builds successfully.
    - Can demo end-to-end: submit query -> see results and status.
  - [x] Task 2.1: Add UI models / contracts in host project - Completed
    - [x] Step 1: Add `QueryRequest`, `QueryResponse`, `Hit`, `FacetGroup`, `FacetValue` models per spec (with geometry as placeholder only).
    - [x] Step 2: Add `IQueryUiSearchClient` interface.
  - [x] Task 2.2: Implement `StubQueryUiSearchClient` - Completed
    - [x] Step 1: Return deterministic sample hits (Title required; other fields optional placeholders).
    - [x] Step 2: Return `Region` and `Type` facet groups with static counts.
    - [x] Step 3: Populate `Total` and `Duration`.
    - [x] Step 4: Provide a simple failure mode (configurable) to demonstrate FR-11 (options: `QueryUi:StubSearch:AlwaysFail`, `SimulatedLatencyMs`).
  - [x] Task 2.3: Add `QueryUiState` (scoped) to orchestrate UI - Completed
    - [x] Step 1: Store query text, loading state, last response, error, selected facets, selected hit.
    - [x] Step 2: Execute `SearchAsync` on submit; update panels based on state.
  - [x] Task 2.4: Wire state + stub into `QueryPage` - Completed
    - [x] Step 1: SearchBar submit triggers state execute. (Search button + Enter key)
    - [x] Step 2: Results and status reflect state. (DataList hits + status bar total/duration)
    - [x] Step 3: Empty state hides after first submit. (controlled by `HasSubmitted`)
  - **Files** (expected):
    - `src/Hosts/<QueryServiceHost>/**/Services/IQueryUiSearchClient.cs`: new.
    - `src/Hosts/<QueryServiceHost>/**/Services/StubQueryUiSearchClient.cs`: new.
    - `src/Hosts/<QueryServiceHost>/**/State/QueryUiState.cs`: new.
    - `src/Hosts/<QueryServiceHost>/**/Models/*.cs`: new models (one public type per file).
    - `src/Hosts/<QueryServiceHost>/**/Program.cs` (or equivalent): DI registration.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - Run host; submit query; confirm results + status.

  - **Summary (Work Item 2)**:
    - Added UI contracts/models: `QueryRequest`, `QueryResponse`, `Hit`, `FacetGroup`, `FacetValue`.
    - Added `IQueryUiSearchClient` and `StubQueryUiSearchClient` with deterministic results, static facets, duration, and configurable failure/latency options.
    - Added scoped `QueryUiState` to orchestrate submit-driven execution, loading/error handling, and last response.
    - Wired `Home.razor` to state: explicit submit (button/Enter), results list population, and status bar total/duration.
    - Files touched/added: `src/Hosts/QueryServiceHost/Models/*`, `src/Hosts/QueryServiceHost/Services/*`, `src/Hosts/QueryServiceHost/State/QueryUiState.cs`, `src/Hosts/QueryServiceHost/Program.cs`, `src/Hosts/QueryServiceHost/Components/Pages/Home.razor`.

## Vertical Slice 2 — Facets as static placeholders + chips
- [x] Work Item 3: Implement Facets panel and filter chips behavior - Completed
  - **Purpose**: Provide the refinement interaction loop with static facet placeholders.
  - **Acceptance Criteria**:
    - Facets panel shows `Region` and `Type` groups.
    - Each group is collapsible.
    - Facet values show counts.
    - Multi-select supported.
    - Selecting facet values adds chips in the Search Bar.
    - Removing a chip clears the corresponding facet selection.
  - **Definition of Done**:
    - Facets UI wiring implemented.
    - Selection state managed centrally.
    - Query execution triggered on facet change (even if stubbed behavior is minimal).
    - Builds successfully.
  - [x] Task 3.1: Implement `FacetPanel` component - Completed
    - [x] Step 1: Create component to render facet groups/values (Radzen).
    - [x] Step 2: Add collapse/expand.
    - [x] Step 3: Raise selection change events. (toggles update shared `QueryUiState`)
  - [x] Task 3.2: Implement chip list rendering - Completed
    - [x] Step 1: Render chips for selected facets: `<FacetGroup>: <Value>`.
    - [x] Step 2: Remove action updates state.
  - [x] Task 3.3: Trigger re-query on facet change - Completed
    - [x] Step 1: On facet toggle, call search execution (auto-trigger after first submit).
    - [x] Step 2: Ensure UI remains responsive and shows loading state. (re-uses existing state loading UI)
  - **Files** (expected):
    - `src/Hosts/<QueryServiceHost>/**/Components/FacetPanel.razor`: new.
    - `src/Hosts/<QueryServiceHost>/**/Components/SearchBar.razor`: new or updated.
    - `src/Hosts/<QueryServiceHost>/**/State/QueryUiState.cs`: update.
  - **Work Item Dependencies**: Work Item 2.
  - **Run / Verification Instructions**:
    - Run host; submit query; select facets; verify chips and results/state update.

  - **Summary (Work Item 3)**:
    - Added `FacetPanel` to render facet groups (`Region`, `Type`) with collapse/expand and value counts.
    - Added `SearchBar` chip rendering for selected facets with remove actions.
    - Extended `QueryUiState` with multi-select facet state, chip projection, collapse state, and auto re-query on facet changes.
    - Updated `Home.razor` to use `SearchBar` and `FacetPanel` components.
    - Files touched/added: `src/Hosts/QueryServiceHost/Components/FacetPanel.razor`, `src/Hosts/QueryServiceHost/Components/SearchBar.razor`, `src/Hosts/QueryServiceHost/State/QueryUiState.cs`, `src/Hosts/QueryServiceHost/Models/FilterChip.cs`, `src/Hosts/QueryServiceHost/Components/Pages/Home.razor`.

## Vertical Slice 3 — Result selection + details panel placeholders
- [x] Work Item 4: Implement Results list selection and Details panel placeholders - Completed
  - **Purpose**: Enable result inspection loop: select hit -> see details.
  - **Acceptance Criteria**:
    - Results list shows placeholder rows with Title required.
    - Clicking a row selects it and highlights it.
    - Details panel shows selected hit fields as placeholders.
    - Map area is present as a placeholder region only.
  - **Definition of Done**:
    - Result selection state wired.
    - Details panel updates on selection.
    - Builds successfully.
  - [x] Task 4.1: Implement `ResultsPanel` component - Completed
    - [x] Step 1: Render list of hits.
    - [x] Step 2: Support click selection + visual highlight.
  - [x] Task 4.2: Implement `DetailsPanel` component - Completed
    - [x] Step 1: Render placeholder fields for title/type/region.
    - [x] Step 2: Add map placeholder.
    - [x] Step 3: Add attributes/raw JSON placeholder toggle (optional placeholder only).
  - **Files** (expected):
    - `src/Hosts/<QueryServiceHost>/**/Components/ResultsPanel.razor`: new.
    - `src/Hosts/<QueryServiceHost>/**/Components/DetailsPanel.razor`: new.
    - `src/Hosts/<QueryServiceHost>/**/State/QueryUiState.cs`: update.
  - **Work Item Dependencies**: Work Item 2.
  - **Run / Verification Instructions**:
    - Run host; submit query; select result; verify details panel updates.

  - **Summary (Work Item 4)**:
    - Added `ResultsPanel` rendering the hits list with click selection and a clear selected-row highlight.
    - Added `DetailsPanel` showing selected hit placeholder fields and a map placeholder region.
    - Extended `QueryUiState` with `SelectedHit` and `SelectHit(...)` plus selection clearing when results change.
    - Updated `Home.razor` to compose the new panels.
    - Files touched/added: `src/Hosts/QueryServiceHost/Components/ResultsPanel.razor`, `src/Hosts/QueryServiceHost/Components/DetailsPanel.razor`, `src/Hosts/QueryServiceHost/State/QueryUiState.cs`, `src/Hosts/QueryServiceHost/Components/Pages/Home.razor`.

## Finalization
- [x] Work Item 5: UI polish + documentation alignment - Completed
  - **Purpose**: Ensure the skeleton meets the spec, is consistent, and is easy to demo.
  - **Acceptance Criteria**:
    - Layout is stable.
    - Loading and error states are visible and non-disruptive.
    - Empty state content matches spec.
    - Facets are present as static placeholders.
  - **Definition of Done**:
    - All work items build.
    - Spec remains accurate (update if implementation forces a change).
    - Demo steps documented.
  - [x] Task 5.1: Add minimal styling for readability - Completed
    - [x] Step 1: Ensure panels are scrollable where appropriate.
    - [x] Step 2: Ensure selected result highlight is clear.
  - [x] Task 5.2: Verify against FR list - Completed
    - [x] Step 1: Walk through each functional requirement and verify behavior.
  - **Files**:
    - `docs/028-query-ui-and/spec-query-ui-and-combined_v0.01.md`: update only if needed.
  - **Work Item Dependencies**: Work Items 1–4.
  - **Run / Verification Instructions**:
    - Run host and manually verify all FR acceptance criteria.

  - **Summary (Work Item 5)**:
    - Added component-scoped css for the query page to stabilize layout sizing and ensure panels fill available height.
    - Improved selected result highlight styling for readability (left accent + background using Radzen theme variables).
    - Confirmed loading/error/empty states are visible and non-disruptive with the existing `QueryUiState` UI.
    - Build verified.
    - Files touched/added: `src/Hosts/QueryServiceHost/Components/Pages/Home.razor`, `src/Hosts/QueryServiceHost/Components/Pages/Home.razor.css`, `src/Hosts/QueryServiceHost/Components/ResultsPanel.razor`.

## Summary
This plan delivers the `QueryServiceHost` Query UI in small runnable steps: first the page + layout + empty state, then a stubbed query flow, then facets/chips as static placeholders, then result selection + details placeholders, and finally polish and verification. The approach intentionally avoids URL encoding and map integration while establishing the component boundaries needed for later iterations.
