# Specification: Query UI (Skeleton) — `QueryServiceHost`

Version: v0.01  
Status: Draft  
Work Package: `docs/028-query-ui-and/`  
Based on: `docs/search_ui_showcase_spec.md`  

## 1. Overview

### 1.1 Purpose
Specify a comprehensive, implementable skeleton for the developer-oriented Query UI within the `QueryServiceHost` Blazor project.

This spec creates the initial “UI backbone” with:
- the four-panel layout
- essential state and interactions (query, facets, selection)
- URL synchronization
- placeholders/extension points for later integration (map, rich result details, real backend)

### 1.2 Scope
In scope:
- Single primary page with:
  - search input
  - active filter chips
  - facets with counts and multi-select
  - results list
  - details inspector panel
  - status bar for count and duration
- UI state model and orchestration:
  - query text and submit behavior
  - filters add/remove
  - query execution lifecycle (loading/error/success)
  - selected result
- Query-string routing:
  - encode/decode query and filters
  - bookmark/share support
- Service abstractions:
  - `IQueryUiSearchClient` used by UI
  - initial stub implementation to enable development without the real query backend
- Initial Playwright E2E tests for the skeleton flow.

Out of scope:
- Real map rendering (OpenStreetMap/Leaflet) beyond placeholder boundaries.
- Authentication/authorization beyond host defaults.
- Result export features.

### 1.3 Stakeholders
- Developers exploring search capabilities.
- Maintainers of `QueryServiceHost`.

### 1.4 Definitions
- **Hit**: A single search result.
- **Facet group**: A category (e.g., Region, Type).
- **Facet value**: A selectable option within a group (e.g., North Sea).

## 2. System context

### 2.1 Current state
- Concept/spec exists in `docs/search_ui_showcase_spec.md` with a Kibana-Discover-inspired layout.
- Implementation details (components, services, tests) are not yet provided.

### 2.2 Proposed state
- `QueryServiceHost` includes a working single-page UI shell with end-to-end wiring and placeholder data.

### 2.3 Assumptions
- `QueryServiceHost` is a Blazor host project.
- A component library may already be used (Radzen or similar). This skeleton should prefer existing dependencies; do not introduce a new UI library unless necessary.

### 2.4 Constraints
- Maintain “minimal navigation”: one primary page.
- Follow repository coding standards (Allman braces, block-scoped namespaces, one public type per file).
- UI logic remains in host layer (no domain/infrastructure dependencies introduced by the UI).

## 3. Component / service design (high level)

### 3.1 Components
Required UI units (names are implementable suggestions; exact file/component names may vary to match existing conventions):

1. `QueryPage`
   - Route: `/query` (or host-appropriate equivalent)
   - Layout shell that composes the other components

2. `SearchBar`
   - Query input
   - Active filter chips
   - Displays query metadata (count/duration) near input

3. `FacetPanel`
   - Collapsible facet groups
   - Multi-select facet values

4. `ResultsPanel`
   - Scrollable list of hits
   - Selected state

5. `DetailsPanel`
   - Summary of selected hit
   - Placeholder for map
   - Optional raw JSON view toggle (can start as placeholder)

6. `StatusBar`
   - Compact strip with count + duration + status

### 3.2 Data flows

#### 3.2.1 Query execution
- Input source: `SearchBar`.
- Trigger:
  - (MVP) Explicit submit (Enter key or “Search” button).
  - (Optional later) debounce-as-you-type.
- Orchestration: `QueryPage` or a dedicated `QueryUiState` service triggers `IQueryUiSearchClient.SearchAsync()`.

#### 3.2.2 Facet selection
- `FacetPanel` raises events when a value is toggled.
- UI state updates selected facet set.
- Selection triggers `SearchAsync()`.

#### 3.2.3 Result selection
- `ResultsPanel` raises “selected hit changed”.
- UI state updates selected hit.
- `DetailsPanel` renders selected hit.

#### 3.2.4 URL synchronization
- State -> URL:
  - After submitting a query or changing facets, update query-string.
- URL -> State:
  - On initial load, parse query-string and execute query.

### 3.3 Key decisions
- Query-string is canonical for query + facets (shareable).
- Stub `IQueryUiSearchClient` is used to avoid blocking UI work.

## 4. Functional requirements

### FR-1 Single page layout
The UI MUST provide a single primary page with:
- Top: search bar area
- Body: three columns (facets | results | details)
- Bottom: status bar

Layout diagram (from `docs/search_ui_showcase_spec.md`):

    ┌─────────────────────────────────────────────────────────────┐
    │ Search Bar + Active Filters + Query Metadata                │
    └─────────────────────────────────────────────────────────────┘
    ┌───────────────┬───────────────────────────────┬──────────────┐
    │               │                               │              │
    │               │                               │              │
    │   Facets      │        Results List           │   Details    │
    │   Panel       │                               │   Panel      │
    │               │                               │              │
    │               │                               │              │
    └───────────────┴───────────────────────────────┴──────────────┘
    ┌─────────────────────────────────────────────────────────────┐
    │ Status Bar                                                  │
    └─────────────────────────────────────────────────────────────┘

Acceptance criteria:
- Navigating to the route displays the 4 region layout.
- Content reflows reasonably for narrow widths (at minimum: columns stack).

### FR-2 Search input
The UI MUST allow entry of free-text search input.

Acceptance criteria:
- Query text is editable.
- Submitting executes search via `IQueryUiSearchClient`.

### FR-3 Active filters as chips
Active facet selections MUST render as removable chips near the search bar.

Acceptance criteria:
- Each selected facet value renders a chip labeled `<FacetGroup>: <Value>`.
- Clicking the remove action removes that filter and re-executes search.

### FR-4 Facets show counts and support multi-select
Facets MUST render counts and allow selecting multiple values.

Acceptance criteria:
- Each facet value shows a count.
- Selecting one value doesn’t clear other selections.

### FR-5 Selected facet values reflect in chips
Selected facet values MUST be visible in the chip list.

Acceptance criteria:
- Selecting a facet adds a chip.
- Clearing from chip updates facet selection state.

### FR-6 Facet groups are collapsible
Facet groups MUST be collapsible.

Acceptance criteria:
- Each group has an affordance to expand/collapse.
- Collapsing a group hides its values.

### FR-7 Results list renders a minimal row
Results MUST render as a list of hits with minimal fields.

Minimum fields per row (placeholders allowed if the backend data isn’t available yet):
- Title/name
- Type
- Region
- Coordinates (if available)
- Matched fields (placeholder allowed)

Acceptance criteria:
- List shows N hits.
- Each row contains at least title.

### FR-8 Selecting a result populates details panel
Clicking a result MUST set it as selected and populate `DetailsPanel`.

Acceptance criteria:
- Selected row is visually distinct.
- Details panel updates without navigation.

### FR-9 Status bar shows count and duration
Status bar MUST show:
- Total hits (or returned hits)
- Query duration

Acceptance criteria:
- After a search completes, the status bar shows both values.
- While loading, shows a loading indicator/message.

### FR-10 URL query-string synchronization
The UI MUST keep query and selected filters in the URL.

Proposed encoding (implementable, stable, and simple):
- `q=<text>`
- `f=<url-encoded-json>` where JSON is a dictionary mapping facet group -> array of selected values.

Example:
- `?q=wreck&f=%7B%22Region%22%3A%5B%22North%20Sea%22%5D%2C%22Type%22%3A%5B%22Wreck%22%2C%22Pipeline%22%5D%7D`

Acceptance criteria:
- Updating query or facets updates the URL without full page reload.
- Loading a URL restores state and executes a search.

### FR-11 Error states
The UI MUST handle failures from the search client.

Acceptance criteria:
- If `SearchAsync()` throws or returns an error, show an error message in the results panel and keep prior state intact.

## 5. Non-functional requirements

### NFR-1 Performance
- Skeleton UI operations (render, selection) should be responsive.
- The stub search client should simulate latency optionally (configurable) to enable UI loading state testing.

### NFR-2 Accessibility
- Basic keyboard operation:
  - Submit on Enter in search input.
  - Result selection supports keyboard focus (minimum: tab focusable items).

### NFR-3 Maintainability
- Components should be small and composable.
- State management should be centralized (service or page-level) and testable.

## 6. Data model

### 6.1 UI models (suggested)
- `QueryUiState`
  - `string QueryText`
  - `IReadOnlyDictionary<string, IReadOnlySet<string>> SelectedFacets`
  - `QueryResponse? LastResponse`
  - `bool IsLoading`
  - `string? Error`
  - `Hit? SelectedHit`

- `QueryResponse`
  - `IReadOnlyList<Hit> Hits`
  - `IReadOnlyList<FacetGroup> Facets`
  - `long Total`
  - `TimeSpan Duration`

- `FacetGroup`
  - `string Name`
  - `IReadOnlyList<FacetValue> Values`
  - `bool IsCollapsed`

- `FacetValue`
  - `string Value`
  - `long Count`
  - `bool IsSelected`

- `Hit`
  - `string Title`
  - `string? Type`
  - `string? Region`
  - `(double lat, double lon)? Coordinates` (or string placeholder)
  - `IReadOnlyList<string> MatchedFields`
  - `JsonElement? Raw` (optional)

## 7. Interfaces & integration

### 7.1 Search client abstraction
Define an abstraction used by the UI components:
- `IQueryUiSearchClient`
  - `Task<QueryResponse> SearchAsync(QueryRequest request, CancellationToken cancellationToken)`

`QueryRequest` includes:
- `QueryText`
- selected facets

### 7.2 Stub implementation
Provide `StubQueryUiSearchClient` returning deterministic sample data:
- fixed facet groups with counts
- fixed hits list
- supports filtering by selected facets (in-memory)
- supports query text matching (simple `Contains`, case-insensitive)

### 7.3 Dependency injection
Register the search client in `QueryServiceHost` DI:
- default: stub
- future: replace with real client via config/feature flag

## 8. Observability (logging/metrics/tracing)
- Log search begin/end at `Information` level with duration and hit count (UI-side logging, if host has `ILogger`).
- Log errors at `Error` level with exception.

## 9. Security & compliance
- The skeleton should not log full raw documents if they might contain sensitive data (keep raw JSON view behind an explicit toggle and avoid logging it).
- Query-string contents may be sensitive; do not capture it in logs by default.

## 10. Testing strategy

### 10.1 Playwright E2E (preferred)
Add Playwright tests that verify:
1. Page loads and shows layout regions.
2. Submitting a query updates results.
3. Selecting a facet updates chips and results.
4. Clicking a result updates details panel.
5. URL updates when query/facets change.
6. Loading a deep link restores state.

### 10.2 Unit testing (optional)
If present in the repo conventions:
- Unit test parsing/formatting of query-string (`q` and `f`).

## 11. Rollout / migration
- Add the UI behind a route that doesn’t disrupt existing endpoints.
- No data migrations.

## 12. Open questions
1. What is the canonical route for the Query UI within `QueryServiceHost` (`/`, `/query`, `/search`)?
2. Should the initial UI use Radzen controls (if already in use) or plain Blazor components?
3. Do we want “execute on type” (debounce) in the skeleton, or only explicit submit?
4. How should coordinates/geometry be represented in the initial skeleton (string vs structured)?
