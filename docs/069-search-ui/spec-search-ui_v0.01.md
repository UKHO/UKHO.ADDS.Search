# Work Package: `069-search-ui` — Studio search mock UI

**Target output path:** `docs/069-search-ui/spec-search-ui_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft covering a new `Search` activity-bar item, left-side search and facet mock UI, central mock results document, and right-side mock details panel based on the supplied reference screenshot.
- `v0.01` — Added a pending clarification on whether the right-hand mock details panel should show placeholder content immediately after search or only after a mock result is selected.
- `v0.01` — Clarified that the right-hand mock `Details` panel should remain in an empty/select-a-result state after search and should show fake values only after a mock result is selected.
- `v0.01` — Clarified that the right-hand mock `Details` panel should prefer the Theia `Outline` side, but may fall back to the nearest equivalent right-hand panel if unresolved support gaps or open implementation questions make direct `Outline` hosting awkward for the first delivery.
- `v0.01` — Clarified that pressing `Enter` in the search input should trigger the same mock search action as clicking the `Search` button when text is present.

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for adding an initial `Search` area to the Theia-based Studio shell.

The requested outcome is a static mock UI that mirrors the overall structure shown in the supplied screenshot:

- a new `Search` item in the `Activity Bar`
- a search box and search button in the `Side Bar`
- facet checkboxes below the search controls in the `Side Bar`
- a central results surface in the editor/document area
- a right-hand details surface using the Theia `Outline` side if feasible

This work package is intentionally limited to shell structure and mock interaction. It does not require live search execution, backend integration, or real data binding.

### 1.2 Scope

This specification covers:

- adding a new `Search` item to the Studio `Activity Bar`
- placing the `Search` item at the end of the activity-bar item list
- creating a `Search` side-bar view with a text input and search action
- enabling the search action only when the search box contains text
- showing static facet groups and checkboxes beneath the search controls in the side bar
- opening a central mock search-results document when the user clicks the search action
- presenting placeholder result cards that visually align with the supplied screenshot
- showing a right-hand mock details panel in the Theia `Outline` side if feasible within the current shell
- using placeholder values throughout the first version

This specification does not cover:

- real search APIs or index queries
- real facet calculation or filtering logic
- result selection behavior beyond minimal mock presentation unless needed to support the visual shell
- persistence of search text or filters across sessions
- production-ready visual design refinement beyond screenshot-aligned mock fidelity
- replacement of existing Studio work areas

### 1.3 Stakeholders

- Studio/tooling developers
- engineering leads shaping the Studio shell UX
- stakeholders reviewing the future search experience
- users who need an early visual proof of the search workflow inside Studio

### 1.4 Definitions

- `Activity Bar`: the far-left strip of top-level navigation icons in the Theia workbench
- `Side Bar`: the left-hand panel hosting the active view container
- `editor area`: the central document/work surface used for tabbed content
- `search view`: the left-hand Studio search control surface containing the query box, search button, and facets
- `results document`: the central tabbed mock document displaying search results
- `details panel`: the right-hand mock surface showing metadata for a selected or example result
- `facet`: a grouped filter option such as `Region` or `Type`

## 2. System context

### 2.1 Current state

The Studio shell already uses a Theia-style workbench with an `Activity Bar`, `Side Bar`, central editor area, and auxiliary panel regions.

Existing work packages have established multiple activity-bar-driven Studio work areas. However, there is not yet a dedicated `Search` area that presents a search-oriented shell layout.

The supplied screenshot demonstrates the desired first-step structure for this work:

- a single search box spanning the top of the left-side search controls
- a `SEARCH` button adjacent to the search box
- grouped facet sections in the left column
- a central list of result cards
- a right-side details area

### 2.2 Proposed state

Studio shall provide a new `Search` activity-bar work area as a static mock experience.

When the user activates `Search`, Studio shall show a search-oriented side-bar view. The side-bar view shall contain:

- a text input with placeholder text aligned to the screenshot intent
- a search button that is disabled when the input is empty and enabled when the input contains text
- mock facet sections beneath the search controls

When the enabled search button is clicked, Studio shall open a central results document showing mock results aligned to the supplied screenshot.

The mock details content shown on the right side of the screenshot should be presented in the Theia right-hand `Outline` side where practical. After the initial search, the details area should remain in an empty or instructional state until the user selects a mock result. Once a result is selected, the details area shall show fake placeholder values for that selected result. If unresolved support questions, missing supporting infrastructure, or disproportionate complexity make direct `Outline` hosting awkward for the first delivery, the implementation may use the nearest equivalent right-hand side panel while preserving the intended visual layout and future direction.

### 2.3 Assumptions

- this work package is intentionally UI-only and mock-data-only
- the search results may remain static even when the entered text changes
- the goal is to establish layout, navigation, and basic control state rather than real search behavior
- the Studio shell can support another activity-bar item without reworking the overall information architecture
- the side-bar facet list may remain non-functional in the first version
- placeholder details content is acceptable in the right-hand panel
- the supplied screenshot is the primary visual reference for initial layout intent

### 2.4 Constraints

- the `Search` item must appear at the end of the existing activity-bar item list
- no live backend integration is required for this work package
- the first implementation should remain lightweight and low-risk
- search button enablement must depend only on whether text is present in the search box
- facet controls are presentational in this version and should not imply completed filtering behavior
- the right-side details surface should prefer the Theia `Outline` area if feasible
- the implementation should follow normal Theia workbench interaction patterns rather than inventing a custom shell model

## 3. Component / service design (high level)

### 3.1 Components

1. `Search` activity-bar contribution
   - introduces a new top-level `Search` entry in the Studio `Activity Bar`
   - opens the search-specific view container
   - must be ordered last in the activity-bar list

2. `Search` side-bar view
   - contains the search text box and search button
   - contains mock facet groups beneath the search controls
   - acts as the command-and-filter surface for the mock experience

3. Search results document
   - opens in the central editor area as a normal tab/document surface
   - renders placeholder result cards based on the screenshot structure
   - represents the centre pane of the reference UI

4. Right-hand details panel
   - presents placeholder detail values for a representative result
   - should live in the right-hand `Outline` side if practical
   - may use the closest equivalent right-hand side panel if unresolved support gaps or current shell limitations make that the safer first-delivery option

5. Mock search state model
   - tracks current query text
   - determines whether the search button is enabled
   - determines when the mock results document should open

### 3.2 Data flows

#### Search activation flow

1. user selects the `Search` item in the `Activity Bar`
2. Studio opens the `Search` view in the `Side Bar`
3. the search input, search button, and mock facet groups are displayed
4. the search button is disabled until text is entered

#### Query enablement flow

1. user types into the search input
2. Studio updates the local mock search state
3. if the input contains any text, the search button becomes enabled
4. if the input becomes empty again, the search button becomes disabled

#### Mock search execution flow

1. user clicks the enabled search button
2. Studio opens a `Search results` document in the editor area
3. the document displays the static mock results list
4. Studio also shows the mock details panel in the right-hand area
5. the details panel initially shows an empty or `Select a result to view details` state

#### Mock result selection flow

1. user clicks a mock result card in the central results document
2. Studio marks that result as selected in local UI state
3. the right-hand details panel updates to show fake values for the selected result
4. the details content remains mock-only and does not require backend retrieval

#### Facet presentation flow

1. user opens the `Search` view
2. Studio renders facet group headings beneath the search controls
3. each facet group renders mock checkbox items matching the screenshot intent
4. facet interaction may be non-functional in this work package unless minimal toggling is needed for UI realism

### 3.3 Key decisions

- **Add `Search` as a new top-level Studio work area**
  - rationale: search is a distinct user task and benefits from dedicated shell affordances

- **Place `Search` last in the `Activity Bar`**
  - rationale: the user explicitly requested end-of-list placement

- **Keep the first version entirely mock-based**
  - rationale: the current goal is layout validation, not backend delivery

- **Use the `Side Bar` for search entry and facets**
  - rationale: this matches the reference screenshot and fits normal workbench patterns

- **Open results in a normal central document**
  - rationale: the centre portion of the screenshot maps naturally to the editor/document area

- **Prefer the right-hand `Outline` area for details, with a pragmatic fallback**
  - rationale: the user explicitly requested use of the right-hand Theia outline panel if possible, but the first delivery should not be blocked by unresolved shell support questions

## 4. Functional requirements

### FR-001 Add `Search` activity-bar item

Studio shall provide a new `Search` item in the `Activity Bar`.

### FR-002 `Search` ordering

The new `Search` activity-bar item shall appear at the end of the activity-bar item list.

### FR-003 Open search side-bar view

Selecting the `Search` activity-bar item shall open a search-specific view in the `Side Bar`.

### FR-004 Search input control

The search side-bar view shall include a text input for entering search text.

### FR-005 Search placeholder text

The search input shall display placeholder text aligned with the screenshot intent, such as guidance to start typing to search the dataset.

### FR-006 Search button in side bar

The search side-bar view shall include a search button adjacent to or visually paired with the search input.

### FR-007 Disabled search button when input empty

The search button shall be disabled when the search input contains no text.

### FR-008 Enabled search button when input contains text

The search button shall become enabled when the search input contains text.

### FR-009 Mock search execution

Clicking the enabled search button shall trigger a mock search flow rather than a real backend search.

### FR-009a `Enter` key triggers search

Pressing `Enter` while focus is in the search input shall trigger the same mock search flow as clicking the `Search` button, provided the input contains text.

### FR-010 Open central results document

When the mock search flow is triggered, Studio shall open a central results document in the editor area.

### FR-011 Static results content

The central results document shall display static mock result content in the visual style of the centre pane shown in the supplied screenshot.

### FR-012 Result card presentation

The mock results document shall include multiple result cards showing placeholder title and summary metadata values.

### FR-013 Side-bar facets below search controls

The search side-bar view shall render facet controls beneath the search input and search button.

### FR-014 Facet group structure

The side-bar facets shall be grouped under headings matching the screenshot intent, including at minimum `Region` and `Type`.

### FR-015 Facet checkbox presentation

Each facet group shall display mock checkbox items with placeholder counts or labels based on the screenshot intent.

### FR-016 Facets may be non-functional

In the first version, facet checkboxes may remain visually interactive or purely presentational, but they shall not require real filtering behavior.

### FR-017 Right-hand details panel

The search experience shall provide a right-hand details panel containing mock values.

### FR-018 Prefer Theia `Outline` side for details

The right-hand details panel should be hosted in the Theia right-hand `Outline` side if feasible within the current Studio shell.

### FR-019 Accept equivalent right-hand panel if required

If the exact `Outline` slot cannot reasonably host the details mock UI in the current shell, or if doing so depends on unresolved support questions or additional enabling work outside this mock-UI scope, Studio may use the closest equivalent right-hand side panel while preserving the same visual intent and future direction.

### FR-020 Show details alongside results after search

When the mock search results document is opened, the right-hand details panel shall also be made visible.

### FR-020a Empty details state after search

After a mock search is triggered and before a result is selected, the right-hand details panel shall display an empty or instructional state such as `Select a result to view details`.

### FR-020b Selected result updates details

Selecting a mock result card shall update the right-hand details panel to show fake placeholder values for that selected result.

### FR-021 Static first version

The first version of the `Search` work area shall remain a static mock UI and shall not require real dataset search, real facets, or real result-details retrieval.

### FR-022 Keyboard and pointer usability

The search input and search button shall be usable with normal keyboard and pointer interaction.

## 5. Non-functional requirements

- The mock search UI should align visually with the supplied screenshot while still fitting normal Studio/Theia styling.
- The search work area should load with no noticeable delay beyond normal shell rendering.
- The search button enabled/disabled state should update immediately as text is entered or cleared.
- The layout should remain coherent on typical Studio desktop viewport sizes.
- The mock search experience should not introduce any required backend contract changes.
- The implementation should remain easy to replace or extend later with real search and facet behavior.
- Text, controls, and panel content should remain readable on the expected dark Studio theme.

## 6. Data model

No backend data model or API contract change is required for this work package.

A lightweight local UI state model is sufficient for the first version, including:

- current search text
- computed `canSearch` state
- whether the results document has been opened
- placeholder facet definitions
- placeholder result items
- placeholder details fields

## 7. Interfaces & integration

This work package shall integrate with:

- the existing Studio `Activity Bar` contribution model
- the Theia `Side Bar` view-container model
- the Studio editor/document area for the mock results document
- the right-hand Theia side-panel area, preferably the `Outline` side, for the details mock panel
- any existing right-hand side-panel fallback used when direct `Outline` hosting would otherwise block the first delivery
- the existing Studio theme and shell styling approach

No new backend or external-service integration is required.

## 8. UX guidance

### 8.1 Layout intent

The overall layout should closely reflect the supplied screenshot:

- query entry at the top
- facets on the left
- results in the centre
- details on the right

Within the existing Studio shell, this should translate to:

- search controls and facets in the `Side Bar`
- results in a central document
- details in the right-hand side panel

### 8.2 Mock content guidance

The first version should use believable placeholder values so the UI reads as a realistic search experience.

Suitable placeholder examples include:

- `Wreck - North Sea - Example 001`
- `Pipeline - Norway - Example 003`
- `Cable - Baltic - Example 004`

Facet labels and counts may also mirror the screenshot style.

### 8.3 Details guidance

The right-hand details panel should show a short set of placeholder fields for a selected or representative result, such as:

- title
- type
- region
- source
- summary

After search and before result selection, the panel should show an empty or instructional state. After a result is selected, the panel should show fake values for that selected result.

### 8.4 Interaction guidance

The interaction should feel simple and predictable:

- the user opens `Search`
- the user types text
- the search action becomes available
- the user triggers search by clicking `Search` or pressing `Enter`
- the mock results and mock details appear

The first version should avoid implying richer behavior than is actually implemented.

## 9. Acceptance criteria

- A new `Search` item appears in the Studio `Activity Bar`.
- The `Search` item appears at the bottom/end of the activity-bar list.
- Selecting `Search` opens a search-specific side-bar view.
- The side-bar view contains a search text input and search button.
- The search button is disabled when the input is empty.
- The search button becomes enabled when text is present.
- Clicking the enabled search button opens a central mock results document.
- Pressing `Enter` in the search input with text present opens the same central mock results document.
- The central document visually represents the centre pane of the supplied screenshot.
- Facet groups and checkbox items are shown beneath the search controls in the side bar.
- A right-hand mock details panel is shown with placeholder values.
- The right-hand details panel uses the Theia `Outline` side if feasible, or the nearest equivalent right-hand panel if not.
- No real backend search functionality is required for the first delivery.

## 10. Testing and validation

The implementation should be validated through:

- visual verification against the supplied screenshot intent
- verification that the `Search` item appears last in the `Activity Bar`
- verification that selecting `Search` opens the expected side-bar view
- verification that the search button is disabled before text entry
- verification that entering text enables the search button
- verification that clearing the text disables the search button again
- verification that clicking the enabled button opens the mock results document
- verification that pressing `Enter` in the populated search input opens the same mock results document
- verification that facet groups are visible below the search controls
- verification that the right-hand details panel appears in an empty or instructional state after search
- verification that clicking a mock result updates the right-hand details panel with fake placeholder values
- verification that the layout remains usable on common desktop viewport sizes

## 11. Open questions / pending decisions

No open clarification questions remain for the current draft.

Implementation may still need to confirm whether direct right-hand `Outline` hosting is straightforward enough for the first delivery, but the specification now allows a pragmatic fallback if it is not.
