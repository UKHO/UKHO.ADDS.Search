# Work Package: `066-studio-minor-ux` — Minor Studio UX uplifts

**Target output path:** `docs/066-studio-minor-ux/spec-studio-minor-ux_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft covering automatic expansion of the first top-level node in each Studio tree and denser, log-like Studio output with a toolbar-based `Clear output` action.

## 1. Overview

### 1.1 Purpose

This work package defines a small set of targeted Studio UX improvements intended to make the current Theia-based shell feel more polished and efficient during routine use.

The requested uplift has two goals:

1. improve first-glance navigation by automatically expanding the first top-level node in each of the three Studio trees, but only that first node
2. make Studio output read more like conventional log output by increasing density and moving `Clear output` into a native toolbar-style action with tooltip support

The work is intentionally narrow. It does not redefine the Studio information architecture or add new business capability. It improves the usability of existing navigation and output surfaces.

### 1.2 Scope

This specification covers:

- automatic default expansion behaviour for the first top-level node in the `Providers`, `Rules`, and `Ingestion` trees
- ensuring no other top-level nodes are auto-expanded by default
- clarifying how default expansion should interact with ordering, refresh, and user-driven expand/collapse actions
- making the Studio output presentation denser and visually closer to conventional log output
- replacing the current body-level `Clear output` affordance, if present, with a native toolbar-style button and tooltip aligned with the tree-view toolbar language
- defining acceptance criteria for compact output layout and consistent toolbar action placement

This specification does not cover:

- changing the underlying provider, rules, or ingestion data models
- redesigning the overall Studio shell layout
- introducing new output categories, filtering, search, or export features
- adding new operational commands beyond the relocation and restyling of `Clear output`
- changing the meaning of existing Studio tree nodes

### 1.3 Stakeholders

- Studio/tooling developers
- engineering leads shaping the long-term Studio workbench UX
- operational users navigating provider, rules, and ingestion trees
- users relying on Studio output during diagnostics and manual workflows

### 1.4 Definitions

- `top-level node`: a root node directly visible within a Studio tree view
- `first top-level node`: the first root node in the current rendered ordering of a tree
- `default expansion`: expansion applied automatically by the UI before the user explicitly changes tree state
- `Studio output`: the Studio workbench surface used to present operational or diagnostic output lines
- `toolbar-style action`: a compact icon-first command placed in the view toolbar area and surfaced with a tooltip, consistent with the tree-view affordances already used elsewhere in Studio

## 2. System context

### 2.1 Current state

The Studio shell already provides three tree-based navigation areas:

1. `Providers`
2. `Rules`
3. `Ingestion`

Those trees are now structurally correct, but a fully collapsed initial state increases the number of clicks required to begin useful work. Users typically want immediate visibility into at least the first provider's child nodes when entering a view.

The Studio shell also exposes an output surface, but the current presentation is comparatively spacious. It does not yet resemble the compact, scan-friendly log output style users expect from IDE or operational tooling consoles. The current `Clear output` affordance also does not align visually with the native toolbar treatment now used in tree views.

### 2.2 Proposed state

Each Studio tree shall open in a more helpful default state by automatically expanding exactly one top-level node: the first visible root node in that tree.

This creates a better initial affordance by:

- showing users immediately that the tree is hierarchical
- exposing likely next actions without requiring an initial expand click
- keeping the view tidy by avoiding broad multi-root expansion

The Studio output surface shall move toward a denser, log-like visual treatment by reducing unnecessary spacing and using a presentation better suited to sequential output lines.

`Clear output` shall be presented as a native toolbar-style action with tooltip support so it matches the visual language already established for tree-view toolbars.

### 2.3 Assumptions

- provider ordering is already deterministic enough that the first visible top-level node is a sensible default expansion target
- users benefit from seeing one expanded example more than from seeing a completely collapsed tree
- auto-expanding all roots would add noise and reduce scanability
- output is primarily consumed as a chronological stream and therefore benefits from compact, log-like presentation
- Studio should prefer a consistent toolbar language across tree views and output views rather than mixing body buttons and toolbar buttons
- this work package should remain lightweight and should not expand into a broader output-console redesign

### 2.4 Constraints

- only the first top-level node may be auto-expanded by default
- no additional top-level nodes may be auto-expanded unless the user explicitly expands them
- default expansion must not fight against subsequent user interaction
- the output density change must remain theme-respecting and readable
- the `Clear output` action must remain discoverable even though it becomes visually smaller and moves into a toolbar location
- the work package must remain focused on minor UX uplift rather than structural redesign

## 3. Component / service design (high level)

### 3.1 Components

1. Studio tree default-expansion behaviour
   - shared logic governing initial expansion state for `Providers`, `Rules`, and `Ingestion`
   - deterministic selection of the first top-level node based on rendered ordering

2. Tree state coordination
   - handling of initial load, refresh, and user-driven expand/collapse changes without unexpected re-expansion

3. Studio output presentation
   - styling and layout adjustments that make output appear denser and closer to a conventional log stream

4. Studio output toolbar action
   - a toolbar-style `Clear output` command with tooltip support and visual consistency with existing tree-view toolbar actions

### 3.2 Data flows

#### Tree default-expansion flow

1. Studio loads or rebuilds a tree view model
2. root nodes are ordered using the existing tree ordering rules
3. if at least one top-level node exists and no user-defined expansion state has yet been established for that view instance, Studio expands the first top-level node
4. Studio leaves all remaining top-level nodes collapsed
5. child-node navigation proceeds using normal tree interaction rules

#### Tree user-interaction flow

1. user manually expands or collapses nodes
2. Studio records that the view now has user-established expansion state
3. subsequent routine refreshes shall preserve or respect current state rather than reapplying the initial default blindly

#### Output presentation flow

1. Studio receives output entries from the existing output source
2. output entries are rendered in chronological order
3. each entry is displayed using a compact row treatment suited to high-volume scanning
4. the user may invoke `Clear output` from the output toolbar
5. Studio clears the current output view content without leaving the toolbar context

### 3.3 Key decisions

- **Expand only the first top-level node by default**
  - rationale: this improves discoverability without creating the visual clutter of multi-root auto-expansion

- **Use current rendered order to determine which node is first**
  - rationale: the UI should not introduce a second hidden ordering rule just for expansion

- **Apply auto-expansion as an initial default, not as a persistent override**
  - rationale: once the user has interacted with a tree, Studio should respect that state rather than repeatedly forcing the first node open

- **Keep other top-level nodes collapsed**
  - rationale: the requirement is explicitly scoped to one example expansion and should preserve compactness

- **Make Studio output denser and more log-like**
  - rationale: output is best consumed as a compact stream, not as spacious content blocks

- **Move `Clear output` into a toolbar-style action**
  - rationale: the command should align with the native workbench affordances already established in Studio tree views and reduce visual inconsistency

- **Provide tooltip support for `Clear output`**
  - rationale: the toolbar button becomes more compact, so tooltip text is required for discoverability and accessibility

## 4. Functional requirements

### FR-001 Default expansion in `Providers`

The `Providers` tree shall automatically expand the first visible top-level node when the tree is first populated.

### FR-002 Default expansion in `Rules`

The `Rules` tree shall automatically expand the first visible top-level node when the tree is first populated.

### FR-003 Default expansion in `Ingestion`

The `Ingestion` tree shall automatically expand the first visible top-level node when the tree is first populated.

### FR-004 Single-root default expansion only

For each of the three trees, Studio shall auto-expand only the first visible top-level node and shall leave all other top-level nodes collapsed by default.

### FR-005 Deterministic first-node selection

The auto-expanded node shall be determined by the tree's current visible ordering, so the first rendered top-level node is the one expanded.

### FR-006 No-op for empty trees

If a tree has no top-level nodes, Studio shall not attempt any default expansion behaviour.

### FR-007 Respect user-driven expansion state

After the user manually expands or collapses nodes in a given tree view instance, Studio shall not reapply the initial default-expansion rule in a way that overrides that user choice during routine refresh or rerender.

### FR-008 Initial-state hint, not broad persistence reset

Default expansion shall behave as an initial hint for first render of a tree view state, not as a repeated reset that reopens the first root every time data changes.

### FR-009 Dense output presentation

Studio output shall use a denser presentation with reduced vertical spacing so the surface reads more like conventional log output than a spacious content panel.

### FR-010 Log-like visual treatment

Studio output shall present entries as a compact sequential stream suitable for rapid scanning of timestamps, levels, categories, and messages where such metadata is available.

### FR-011 Output readability

The denser output presentation shall remain readable and shall preserve clear separation between adjacent output entries.

### FR-012 Toolbar-based `Clear output`

Studio shall expose `Clear output` as a toolbar-style action within the output view rather than as a body-level button.

### FR-013 `Clear output` tooltip

The toolbar-based `Clear output` action shall provide a tooltip with the text `Clear output` or equivalent clear wording.

### FR-014 Toolbar visual consistency

The `Clear output` toolbar action shall use the same low-noise toolbar language as the toolbars already used in the Studio tree views.

### FR-015 Clear action behaviour

Invoking the toolbar-based `Clear output` action shall remove the currently displayed output entries from the output surface.

### FR-016 Output command discoverability

The move from a body button to a toolbar button shall not reduce command discoverability below an acceptable workbench standard; tooltip and standard toolbar placement shall provide the primary discoverability mechanism.

## 5. Non-functional requirements

- Tree default expansion shall feel immediate and shall not introduce noticeable lag during view population.
- The default-expansion behaviour shall remain predictable across sessions and themes.
- Output density changes shall respect active Theia theme variables and remain readable in the repository's expected dark-oriented usage.
- The output surface should feel closer to an IDE or operational log console than to a general content page.
- Toolbar styling should remain consistent with existing Studio view actions and should avoid bespoke visual treatment unless required.
- Compact output rendering shall support high-volume streams without excessive layout churn.
- Accessibility shall be preserved for toolbar focus, tooltip exposure, and readable output contrast.

## 6. Data model

No backend contract or data-model change is required for this work package.

The tree-expansion uplift operates on existing tree node collections and their current ordering.

The output-density uplift operates on existing output entries and their current render pipeline.

If implementation detail requires local UI state, the only additional state expected is lightweight view state such as:

- whether initial expansion has already been applied for a given tree instance
- whether user interaction has established explicit expansion state

## 7. Interfaces & integration

This work package shall integrate with:

- the existing Studio tree-view implementation for `Providers`, `Rules`, and `Ingestion`
- the existing tree node ordering rules
- the existing Studio output widget or view
- the existing Studio command framework used for toolbar actions
- the existing tooltip and icon mechanisms used by native toolbar affordances

Implementation should prefer shared view-toolbar patterns rather than introducing a one-off output-button style.

## 8. Observability (logging/metrics/tracing)

No new platform observability requirement is introduced.

If local diagnostics are useful during implementation, they should remain low-noise and developer-oriented, for example:

- whether default expansion was applied on first render
- whether expansion was skipped because no nodes existed
- whether user-established expansion state suppressed reapplication of defaults

These diagnostics are optional and should not become visible end-user output.

## 9. Security & compliance

This work package introduces no new security boundary and no new sensitive data flow.

The output-density change shall not increase exposure of sensitive data; it only changes presentation density and command placement.

## 10. Testing strategy

Testing should focus on behaviour and UX acceptance rather than backend contract change.

Recommended coverage:

1. tree default-expansion behaviour
   - verify each of the three trees expands the first visible top-level node on first population
   - verify no additional top-level nodes are auto-expanded
   - verify empty trees do not throw or show broken state
   - verify manual collapse of the first node is respected and not immediately undone by routine rerender

2. output presentation
   - verify output rows render in a visibly denser format than the prior implementation
   - verify the output remains readable with multiple sequential lines
   - verify the `Clear output` action appears in the toolbar rather than the view body
   - verify tooltip text is present and correct
   - verify invoking `Clear output` clears the visible output lines

3. UI-level validation
   - prefer Playwright-style end-to-end verification for the visible UX changes where practical, consistent with repository guidance for Blazor and UI verification
   - supplement with focused frontend tests where they provide stable coverage of tree-state logic or toolbar command wiring

## 11. Rollout / migration

No data migration is required.

The work can be released as an in-place UI refinement.

Expected rollout steps:

1. update tree default-expansion logic
2. update Studio output styling/layout for denser rendering
3. relocate `Clear output` into the output toolbar with tooltip support
4. validate consistency across all three trees and the output view

## 12. Open questions

1. Should the first-node default expansion be reapplied only once per view creation, or should it also be reapplied after a full explicit refresh that rebuilds the tree from scratch and discards prior UI state?
   - current recommendation: treat it as a first-render default for the current view state and do not reapply once the user has established explicit expansion behaviour

2. Should the denser output presentation also adopt a monospace treatment if that is not already in place?
   - current recommendation: yes, if needed to better resemble log output and improve alignment, provided theme readability remains strong

3. Should the output toolbar use only an icon for `Clear output`, or icon plus label if the hosting surface does not provide sufficient discoverability?
   - current recommendation: prefer icon-first with tooltip, matching the tree-view toolbar pattern unless usability review shows the output surface needs stronger affordance
