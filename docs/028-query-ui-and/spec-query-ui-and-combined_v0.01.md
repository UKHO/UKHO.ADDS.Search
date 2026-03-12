# Specification: Query UI (Skeleton) — `QueryServiceHost`

Version: v0.01  
Status: Draft  
Work Package: `docs/028-query-ui-and/`  
Based on: `docs/search_ui_showcase_spec.md`  

## 1. Overview

### 1.1 Purpose
Define the minimum implementable “skeleton” developer-focused Query UI for the `QueryServiceHost` Blazor application, based on the existing UI concept described in `docs/search_ui_showcase_spec.md`.

This specification provides both:
- high-level system/component context, and
- detailed, implementable functional/technical requirements for the UI skeleton.

### 1.2 Scope
In scope:
- A single-page Blazor UI shell with the four-panel layout:
  - Search Bar (top)
  - Facets Panel (left)
  - Results Panel (center)
  - Details Panel (right)
  - Status Bar (bottom)
- Client-side state model for query text, selected facets, selected result, and query metadata.
- Stubbed data adapters/services to enable UI wiring without requiring a full backend implementation.
- Initial Playwright E2E tests for the skeleton flow.

Out of scope for this work package:
- Production-grade search relevance tuning.
- Full domain-specific result rendering (beyond a representative skeleton).
- Security hardening beyond existing host defaults.
- Complete map integration and geometry rendering; only the placeholders and component boundaries are established.
- Authentication/authorization beyond host defaults.
- Result export features.

### 1.3 Stakeholders
- Developers using the Search platform (primary users)
- Search platform team (owners/maintainers)
- Maintainers of `QueryServiceHost`
- UX/technical writers (optional)

### 1.4 Definitions
- **Facet**: A filter category with selectable values and counts.
- **Facet group**: A category (e.g., Region, Type).
- **Facet value**: A selectable option within a group (e.g., North Sea).
- **Chip**: A compact UI element representing an active filter that can be removed.
- **Hit**: A single search result.
- **Query UI**: Developer-facing UI for exploring the search index.

## 2. System context

### 2.1 Current state
- `QueryServiceHost` exists as the host project for query-related capabilities.
- A showcase UI concept is captured in `docs/search_ui_showcase_spec.md` with a Kibana-Discover-inspired layout.
- UI implementation details for the showcase are not yet formalized into a concrete, implementable skeleton spec for this repo.

### 2.2 Proposed state
- `QueryServiceHost` contains a single primary page implementing the layout and state wiring needed for:
  - entering a query
  - showing results, facets, and details placeholders
  - managing selected filters
  - reflecting state to/from the URL

### 2.3 Assumptions
- The UI is Blazor-based.
- The skeleton UI will use Radzen components (no new UI component library will be introduced for this work package).
- Search execution and facet aggregation APIs are either available already or will be integrated later; this skeleton must not block future integration.

### 2.4 Constraints
- Keep navigation minimal: one primary page.
- Maintain “minimal navigation”: one primary page.
- Follow repository coding standards (Allman braces, block-scoped namespaces, one public type per file).
- Follow Onion Architecture (UI logic stays in the host/UI layer).
- UI logic remains in host layer (no domain/infrastructure dependencies introduced by the UI).
- Prefer Playwright E2E tests for UI verification.

## 3. Component / service design (high level)

### 3.1 Components
Required UI units (names are implementable suggestions; exact file/component names may vary to match existing conventions):

1. `QueryPage`
   - Route: `/` (Query UI is the default landing page)
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

Supporting UI/runtime components:
- UI state container (scoped) for query/filter/result selection state
- Query API adapter abstraction (client-side service)

### 3.2 Data flows

#### 3.2.1 Query execution
- Input source: `SearchBar`.
- Trigger:
  - Explicit submit (Enter key or “Search” button).
- Orchestration: `QueryPage` or a dedicated `QueryUiState` service triggers `IQueryUiSearchClient.SearchAsync()`.

#### 3.2.2 Facet selection
- `FacetPanel` raises events when a value is toggled.
- UI state updates selected facet set.
- Selection triggers `SearchAsync()`.

#### 3.2.3 Result selection
- `ResultsPanel` raises “selected hit changed”.
- UI state updates selected hit.
- `DetailsPanel` renders selected hit.

### 3.3 Key decisions
- Use a UI state container to avoid complex cascading parameter wiring.
- Facets in the skeleton are static placeholders (not derived from the search backend); initial groups are `Region` and `Type`.
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

Minimum fields per row:
- Title/name

Additional fields MAY be shown as placeholders (not derived from a real search schema in this work package), such as:
- Type
- Region
- Coordinates
- Matched fields

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

### FR-11 Error states
The UI MUST handle failures from the search client.

Acceptance criteria:
- If `SearchAsync()` throws or returns an error, show an error message in the results panel and keep prior state intact.

### FR-12 Empty state
Before a query is entered, the UI MUST present guidance.

Empty state content (from `docs/search_ui_showcase_spec.md`):

    Start typing to search the dataset

    Examples:
    wreck north sea
    pipeline norway
    cable baltic

Acceptance criteria:
- When query text is empty, show the guidance and examples.
- When a query is submitted, hide the empty state and show results.

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
  - Location/geometry (placeholder only; representation to be specified later)
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
No automated tests are required for v0.01.

### 10.2 Unit testing (optional)
No unit tests are required for v0.01 beyond any existing repository baseline.

## 11. Rollout / migration
- Add the UI behind a route that doesn’t disrupt existing endpoints.
- No data migrations.

## 12. Open questions
All open questions for v0.01 have been answered.
