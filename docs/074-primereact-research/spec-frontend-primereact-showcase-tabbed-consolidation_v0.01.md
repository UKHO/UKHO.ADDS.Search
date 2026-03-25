# Work Package: `074-primereact-research` — PrimeReact showcase tabbed consolidation

**Target output path:** `docs/074-primereact-research/spec-frontend-primereact-showcase-tabbed-consolidation_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft created for consolidating the temporary PrimeReact research pages into a single tabbed showcase page.
- `v0.01` — Recorded the confirmed work package location as `docs/074-primereact-research`.
- `v0.01` — Recorded the requested root `TabView`-style PrimeReact tab control requirement for the showcase page.
- `v0.01` — Recorded the requested migration of `Forms`, `Data View`, `Data Table`, `Tree`, and `Tree Table` page content into dedicated tabs.
- `v0.01` — Recorded the requested removal of all other PrimeReact demo pages apart from the consolidated showcase page.
- `v0.01` — Recorded the non-negotiable requirement that the current showcase page styling remains intact and becomes the inherited visual baseline for the migrated tab content.
- `v0.01` — Confirmed that the `View` menu should expose only the single consolidated showcase entry and that the retired PrimeReact pages should be removed entirely, including their source code.
- `v0.01` — Confirmed that the consolidated showcase page should always open on the `Showcase` tab.
- `v0.01` — Confirmed that tab content should preserve its local UI state while the single consolidated page remains open.
- `v0.01` — Confirmed that the root tab strip may scroll horizontally when width is constrained and tab labels should not be abbreviated.
- `v0.01` — Confirmed that migrated tab content may be lightly simplified where needed so it fits the unified showcase shell more cleanly.
- `v0.01` — Confirmed that migrated tabs should use a minimal tab-local heading pattern rather than retaining larger page-specific introductory chrome.
- `v0.01` — Confirmed that non-active tabs should render on first activation and then remain mounted so state is preserved without forcing all tabs to render on initial load.
- `v0.01` — Confirmed that there should be no toolbar above the root tabs, that the tabs should contain the full page experience, and that no extra page padding should be introduced around the tab shell.
- `v0.01` — Confirmed that tab-internal framing should remain almost completely flat rather than using inset pane treatments.
- `v0.01` — Confirmed that scrolling should follow the existing desktop-style showcase pattern, with the tab acting as a host for the current compact page behavior and scroll ownership remaining with the relevant inner regions rather than the overall tab content area.
- `v0.01` — Confirmed that all retained tabs should use the same compact shared padding and spacing baseline as the current showcase page.
- `v0.01` — Confirmed that the tab order must remain fixed exactly as requested: `Showcase`, `Forms`, `Data View`, `Data Table`, `Tree`, `Tree Table`.
- `v0.01` — Confirmed that the consolidated page should keep one stable showcase title regardless of the selected tab.
- `v0.01` — Confirmed that the root tabs should include PrimeReact icons alongside the approved text labels.
- `v0.01` — Confirmed that tab icons are decorative only and do not need to carry stable semantic meaning.
- `v0.01` — Confirmed that keyboard focus should move into the newly displayed tab content after a tab switch.
- `v0.01` — Confirmed that the primary purpose of the page is look-and-feel, layout, and scrolling assessment, and that unspecified secondary interaction details should use sensible implementation defaults rather than requiring further detailed specification.

## 1. Overview

### 1.1 Purpose

This specification defines a consolidation of the temporary PrimeReact research area inside the Theia-based Studio shell so that the research experience is exposed through a single showcase page with a PrimeReact tab control at the root.

The purpose is to simplify the temporary PrimeReact demo surface, remove redundant standalone demo pages, and preserve the compact showcase styling and workbench-like presentation that has already been established for the existing showcase page.

### 1.2 Scope

This specification includes:

- adding a PrimeReact tab control at the root of the existing showcase page
- keeping the current showcase page content as the `Showcase` tab
- moving the existing `Forms` page content into a `Forms` tab
- moving the existing `Data View` page content into a `Data View` tab
- moving the existing `Data Table` page content into a `Data Table` tab
- moving the existing `Tree` page content into a `Tree` tab
- moving the existing `Tree Table` page content into a `Tree Table` tab
- removing the other standalone PrimeReact research pages once their required content has been consolidated
- preserving the existing showcase styling as the visual and layout baseline for all migrated tab content

This specification does not currently include:

- introduction of any new PrimeReact demo categories beyond those already requested
- backend, persistence, or service changes
- any change in the wider product direction beyond the temporary PrimeReact research surface

### 1.3 Stakeholders

- Studio shell developers
- reviewers evaluating PrimeReact inside Theia
- UX and product stakeholders assessing whether PrimeReact can support compact workbench-style surfaces
- maintainers of the temporary PrimeReact research package

### 1.4 Definitions

- `consolidated showcase page`: the single remaining PrimeReact demo page that hosts all approved demo content through tabs
- `root tab control`: the top-level PrimeReact tab component rendered as the main organizer for the page
- `showcase styling baseline`: the existing compact, flatter, desktop-like styling already applied to the current showcase page and required to remain intact
- `migrated tab content`: content previously hosted on standalone PrimeReact research pages that is moved into the consolidated showcase page

## 2. System context

### 2.1 Current state

The current temporary PrimeReact research area contains multiple separate demo pages, including the existing showcase page and additional pages such as `Forms`, `Data View`, `Data Table`, `Tree`, `Tree Table`, and other pages that are no longer wanted.

The current showcase page has already been refined to use a compact, desktop-oriented style with flatter chrome, tighter spacing, and more controlled scroll behavior. That styling is now the preferred direction for the research area.

### 2.2 Proposed state

The PrimeReact research area shall be consolidated so the user opens a single showcase page from the existing Theia pathway and uses a root-level PrimeReact tab control to switch between the retained demo content areas.

The retained tabs shall be:

1. `Showcase`
2. `Forms`
3. `Data View`
4. `Data Table`
5. `Tree`
6. `Tree Table`

All other standalone PrimeReact pages shall be removed from the research area.

The `View` menu shall expose only the consolidated showcase page after this change. Legacy PrimeReact page entries shall not remain as temporary redirects.

When the consolidated showcase page opens, it shall default to the `Showcase` tab rather than restoring a prior tab selection or opening to a caller-provided tab.

The root tab control shall act as the outermost page container for the consolidated experience. There shall be no separate toolbar or header band above the tabs, and the tab shell shall not introduce unnecessary outer padding around the retained content.

Scrolling behavior shall mirror the current compact showcase page pattern. The selected tab should behave as though the existing showcase page has been placed directly inside a tab, with scroll ownership retained by the relevant inner panes and controls instead of moving to a general tab-panel scroll surface.

The existing showcase page styling shall remain intact and shall become the styling baseline inherited by the migrated tab content so the consolidated page continues to feel like one coherent workbench surface rather than a set of unrelated embedded pages.

### 2.3 Assumptions

- The existing showcase page remains the single surviving PrimeReact research page.
- The migrated tab content may require structural adaptation so it inherits the showcase styling baseline cleanly.
- The current Theia integration pathway for opening the showcase page remains broadly valid.
- Styled PrimeReact remains the only supported presentation mode for this research area.
- The consolidated page remains mounted as one page while users switch between tabs, allowing each tab to preserve its local session state.
- The main evaluation goal for this work is the visual and spatial behavior of the consolidated page, especially look and feel, layout density, and scroll ownership.
- For secondary interaction details that are not explicitly constrained in this specification, implementation may choose sensible defaults that support the look-and-feel research goal.

### 2.4 Constraints

- The current showcase styling is a non-negotiable baseline and must remain intact.
- The migrated tabs must inherit that styling rather than reintroduce older page-specific visual treatments.
- The work remains frontend-only and local to the temporary PrimeReact research surface.
- All non-retained PrimeReact pages shall be removed after consolidation.
- Removal of retired PrimeReact pages includes removal of their source code, not just hiding their menu entries.
- The consolidated tab shell must not add a separate toolbar band above the tabs or introduce avoidable outer padding that makes the page feel less compact.

## 3. Component / service design (high level)

### 3.1 Components

1. `Consolidated showcase page shell`
   - remains the single surviving PrimeReact demo page
   - hosts the root tab control and shared compact styling baseline

2. `PrimeReact root tab control`
   - provides top-level navigation between the retained demo surfaces
   - presents the approved tabs in one coherent page

3. `Showcase tab`
   - preserves the current showcase page content and styling behaviour

4. `Forms tab`
   - hosts the existing forms demo content after migration into the consolidated shell

5. `Data View tab`
   - hosts the existing data view demo content after migration into the consolidated shell

6. `Data Table tab`
   - hosts the existing data table demo content after migration into the consolidated shell

7. `Tree tab`
   - hosts the existing tree demo content after migration into the consolidated shell

8. `Tree Table tab`
   - hosts the existing tree table demo content after migration into the consolidated shell

9. `Retired standalone page registrations`
   - the old per-page registrations, routes, and menu exposure for removed PrimeReact pages
   - removed so the research area exposes only the consolidated showcase page

### 3.2 Data flows

#### Navigation flow

1. the user opens the PrimeReact showcase page from the existing Theia entry point
2. the consolidated showcase page renders with the root tab control
3. the user switches between retained PrimeReact demo areas using tabs instead of separate pages
4. the selected tab displays migrated content within the shared showcase styling baseline

#### Styling flow

1. the consolidated showcase page applies the existing compact showcase styling baseline
2. each migrated tab renders within the same shell and styling contract
3. older per-page presentation treatments do not override or visually fragment the consolidated surface

### 3.3 Key decisions

- the PrimeReact research area will be reduced to a single surviving page
- the root organizer for that page will be a PrimeReact tab control
- the current showcase content remains intact as the `Showcase` tab
- the `Forms`, `Data View`, `Data Table`, `Tree`, and `Tree Table` content moves into sibling tabs
- all other PrimeReact pages are removed
- the compact showcase styling remains intact and is inherited by the migrated tab content

## 4. Functional requirements

1. The consolidated PrimeReact research experience shall expose a single surviving page based on the current showcase page.
2. The consolidated showcase page shall render a PrimeReact tab control at the root of the page.
3. The existing showcase content shall remain available as the `Showcase` tab.
4. The content currently presented on the `Forms` page shall be moved into a `Forms` tab.
5. The content currently presented on the `Data View` page shall be moved into a `Data View` tab.
6. The content currently presented on the `Data Table` page shall be moved into a `Data Table` tab.
7. The content currently presented on the `Tree` page shall be moved into a `Tree` tab.
8. The content currently presented on the `Tree Table` page shall be moved into a `Tree Table` tab.
9. The tab labels shall match the approved names exactly: `Showcase`, `Forms`, `Data View`, `Data Table`, `Tree`, and `Tree Table`.
9a. The tab order shall remain fixed exactly as `Showcase`, `Forms`, `Data View`, `Data Table`, `Tree`, `Tree Table`.
9b. The root tabs shall display the approved text labels together with icons rather than text-only labels.
9c. Tab icon choices are decorative only and do not need to encode or preserve semantic meaning beyond basic visual differentiation.
10. All other PrimeReact pages that are not represented by the retained tabs shall be removed from the research area.
11. The current showcase page styling shall remain intact after the tab control is introduced.
12. The migrated tab content shall inherit the current showcase page styling baseline.
13. The implementation shall not allow migrated tabs to reintroduce older page-specific visual treatments that materially break the unified showcase appearance.
14. The consolidated page shall continue to open through the existing Theia-based PrimeReact entry point.
15. The consolidation shall avoid creating multiple surviving page entry points for the retained demo areas when tabs are now the intended navigation model.
16. The `View` menu shall expose only the single consolidated showcase page after the change.
17. Retired PrimeReact pages shall be removed entirely, including their source files and page registration code.
18. The consolidated showcase page shall always open on the `Showcase` tab.
19. The root tab control shall surround and contain the full page experience rather than sitting beneath a separate page toolbar or header band.
20. The implementation shall not introduce extra outer padding around the consolidated tab shell that materially loosens the current compact showcase density.
21. Scrolling behavior inside the consolidated page shall follow the existing showcase page pattern, with the relevant inner panes and controls owning overflow rather than the overall tab content area becoming a long scrolling surface.
22. The workbench or page title for the consolidated showcase shall remain stable and shall not change as the user switches between tabs.
23. After a tab switch, keyboard focus shall move into the newly displayed tab content area rather than remaining on the tab header.

## 5. Non-functional requirements

1. The consolidated tabbed page shall preserve the compact, desktop-like, workbench-oriented styling already established for the showcase page.
2. The tabbed experience shall feel visually coherent across all retained tabs.
3. The consolidation shall minimise unnecessary duplication of page-level wrappers, styling rules, and demo host structures.
4. The removal of retired pages shall not break the intended remaining PrimeReact research entry point.
5. The implementation shall remain local to the frontend PrimeReact demo area and shall not introduce backend dependencies.
6. While the page remains open, switching between tabs should preserve each tab's local UI state unless a deliberate reset action exists within that tab.
7. If the available width is insufficient for the full tab list, the root tab strip shall support horizontal scrolling rather than abbreviating the approved tab labels.
8. Migrated tab content may be lightly simplified where needed to fit the unified showcase shell cleanly, provided the retained demo intent remains clear and the current showcase styling baseline is preserved.
9. Migrated tabs should use a minimal tab-local heading pattern and should not retain larger page-specific introductory headings or descriptive chrome that conflicts with the compact showcase styling.
10. Non-active tabs should render lazily on first activation and remain mounted afterward so local tab state is preserved without requiring all tab content to render during the initial page load.
11. The consolidated experience should use the root tab control itself as the containing page frame and should not add a separate toolbar/header strip above it.
12. The consolidated page should avoid additional outer padding around the tab shell so the compact showcase styling remains tight and workbench-like.
13. Tab-internal layout treatment should remain almost completely flat and should not add inset pane framing except where an existing PrimeReact control inherently provides its own minimal structure.
14. The consolidated tabbed experience should preserve the existing desktop-style scroll ownership model from the current showcase page rather than introducing broad tab-panel scrolling for normal use.
15. All retained tabs should use the same compact shared padding and spacing baseline as the current showcase page rather than relaxing density on a per-tab basis.
16. The consolidated experience should retain one stable showcase title so tab switching does not create title churn in the workbench.
17. Where this specification does not explicitly constrain secondary interaction behavior, implementation should choose the simplest sensible behavior that best supports evaluation of look and feel, layout, and scrolling.

## 6. Dependencies and impacts

### 6.1 Dependencies

- existing Theia shell integration for the PrimeReact demo widget
- existing showcase page shell and styling baseline
- existing standalone PrimeReact demo page content for `Forms`, `Data View`, `Data Table`, `Tree`, and `Tree Table`

### 6.2 Impacted areas

- PrimeReact demo page components under `src/Studio/Server/search-studio/src/browser/primereact-demo/`
- PrimeReact demo page registration and menu exposure within the Theia integration layer
- showcase-specific CSS that now becomes the shared visual baseline for all retained tabs
- tests that currently assume the presence of separate PrimeReact page entries

## 7. Open questions

No further clarification questions are currently required. Any remaining secondary interaction details may use sensible implementation defaults so long as they do not conflict with the core goals for look and feel, layout, compact density, and scroll ownership.
