# Specification: Query UI (Skeleton) — Overview

Version: v0.01  
Status: Draft  
Work Package: `docs/028-query-ui-and/`  

## 1. Overview

### 1.1 Purpose
Define the minimum implementable “skeleton” developer-focused Query UI for the `QueryServiceHost` Blazor application, based on the existing UI concept described in `docs/search_ui_showcase_spec.md`.

This overview spec is intentionally high-level and does not contain detailed, testable requirements. Detailed functional/technical requirements live in the component spec referenced below.

### 1.2 Scope
In scope:
- A single-page Blazor UI shell with the four-panel layout:
  - Search Bar (top)
  - Facets Panel (left)
  - Results Panel (center)
  - Details Panel (right)
  - Status Bar (bottom)
- Client-side state model for query text, selected facets, selected result, and query metadata.
- URL query-string synchronization for bookmarking/sharing the current query and filters.
- Stubbed data adapters/services to enable UI wiring without requiring a full backend implementation.

Out of scope for this work package:
- Production-grade search relevance tuning.
- Full domain-specific result rendering (beyond a representative skeleton).
- Security hardening beyond existing host defaults.
- Complete map integration and geometry rendering; only the placeholders and component boundaries are established.

### 1.3 Stakeholders
- Developers using the Search platform (primary users)
- Search platform team (owners/maintainers)
- UX/technical writers (optional)

### 1.4 Definitions
- **Facet**: A filter category with selectable values and counts.
- **Chip**: A compact UI element representing an active filter that can be removed.
- **Query UI**: Developer-facing UI for exploring the search index.

## 2. System context

### 2.1 Current state
- `QueryServiceHost` exists as the host project for query-related capabilities.
- A showcase UI concept is captured in `docs/search_ui_showcase_spec.md`.
- UI implementation details for the showcase are not yet formalized into a concrete, implementable skeleton spec for this repo.

### 2.2 Proposed state
- `QueryServiceHost` contains a single primary page implementing the layout and state wiring needed for:
  - entering a query
  - showing results, facets, and details placeholders
  - managing selected filters
  - reflecting state to/from the URL

### 2.3 Assumptions
- The UI is Blazor-based (per repo characteristics).
- The UI will leverage whatever component library is already used by `QueryServiceHost` (e.g., Radzen) if present in the solution.
- Search execution and facet aggregation APIs are either available already or will be integrated later; this skeleton must not block future integration.

### 2.4 Constraints
- Keep navigation minimal: one primary page.
- Follow repository coding standards and Onion Architecture (UI logic stays in the host/UI layer).
- Prefer Playwright E2E tests for UI verification.

## 3. Component / service design (high level)

### 3.1 Components
- `QueryPage` (single primary page)
- `SearchBar` component
- `FacetPanel` component
- `ResultsPanel` component
- `DetailsPanel` component
- `StatusBar` component
- UI state container (scoped) for query/filter/result selection state
- Query API adapter abstraction (client-side service)

### 3.2 Data flows
1. User enters query in `SearchBar`.
2. UI state updates and triggers query execution via adapter.
3. Adapter returns results, facet counts, and query metrics.
4. `FacetPanel` renders aggregations; selecting values updates state and re-queries.
5. `ResultsPanel` renders hits; selecting a hit updates state.
6. `DetailsPanel` shows selected hit and map placeholder.
7. URL query-string encodes current query and filters; UI reads it on load.

### 3.3 Key decisions
- Use URL query-string as the shareable/canonical representation of query and selected facets.
- Use a UI state container to avoid complex cascading parameter wiring.

## Referenced component / service specifications
- `docs/028-query-ui-and/spec-frontend-query-ui-skeleton_v0.01.md`
