# Work Package: `065-studio-tree-widget` — Native Theia tree widget adoption for Studio navigation

**Target output path:** `docs/065-studio-tree-widget/spec-studio-tree-widget_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft capturing the requirement to replace the current hand-rendered card/button navigation with a conventional native Theia tree presentation across `Providers`, `Rules`, and `Ingestion`.
- `v0.01` — Clarified that `New Rule` should remain visible but move from a top-of-view body button to a native Theia toolbar-style affordance with tooltip support, aligned with the built-in Explorer-style pattern.
- `v0.01` — Clarified that explicit refresh affordances may remain in `Rules` and `Ingestion`, but only as native toolbar-style actions using the same low-noise design language as other Studio view actions rather than as body-level buttons.
- `v0.01` — Clarified that provider descriptions should not remain visible in the side-bar tree and should instead be presented in overview/editor surfaces so the navigation stays compact.
- `v0.01` — Clarified that custom CSS should be avoided unless a clear functional issue remains; minor visual tidy-up can be revisited after the UI is finished and native Theia rendering has been assessed end to end.

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for replacing the current Studio side-bar navigation presentation with a conventional native Theia tree experience.

The current Studio shell already exposes the correct high-level information architecture: `Providers`, `Rules`, and `Ingestion` are separate work areas, provider roots are actionable, and child nodes open editor surfaces. However, the current side-bar rendering is implemented as custom `ReactWidget` content using bordered containers and `theia-button` controls, which makes the navigation read visually as nested boxes and action buttons instead of a normal workbench tree.

This work package aims to make the Studio navigation feel like a native Theia workbench feature by:

- adopting Theia's `TreeWidget` framework for side-bar navigation
- using a native Theia toolbar-style affordance for work-area actions such as `New Rule`, rather than placing those actions as prominent buttons inside the view body
- ensuring any retained explicit refresh actions also use the same native toolbar-style treatment rather than body-level buttons
- using native Theia tree interaction and selection behaviour first
- removing visually distracting button-heavy and boxed presentation
- applying one consistent navigation style across `Providers`, `Rules`, and `Ingestion`
- reducing redundant chrome such as duplicate in-body titles
- removing the visible `Refresh Providers` affordance because provider discovery is not a lightweight runtime action in the intended operating model

The intended outcome is not a new information architecture. The intended outcome is a better presentation and interaction model for the existing Studio navigation structure.

### 1.2 Scope

This specification covers:

- replacing the current hand-rendered side-bar content in `Providers`, `Rules`, and `Ingestion` with a native Theia tree-based presentation
- defining a shared tree-navigation approach so all three work areas feel visually and behaviourally consistent
- moving work-area commands such as `New Rule` toward a native toolbar-style presentation with tooltips rather than body-level CTA buttons
- retaining explicit refresh actions only where they still add value and expressing them through the same native toolbar-style pattern rather than body-level buttons
- removing provider descriptions from the steady-state side-bar tree presentation so the navigation remains compact
- preserving the current provider-centric information architecture and editor-opening model
- preferring native Theia styling and behaviour before introducing custom CSS overrides
- removing redundant in-body titles where the enclosing view container already provides the visible title
- removing the visible `Refresh Providers` action from the `Providers` work area
- defining acceptance criteria for conventional tree appearance, subdued selection treatment, and reduced visual noise

This specification does not cover:

- redesigning the overall `Activity Bar` / `Side Bar` / `editor area` structure introduced in the Studio skeleton
- implementing new backend endpoints or changing the `/providers` or `/rules` payloads
- delivering real queue, dead-letter, rule-authoring, or ingestion execution behaviour
- broad branding refresh beyond tree-related look and feel adjustments
- introducing custom styling unless native Theia tree presentation proves insufficient

### 1.3 Stakeholders

- Studio/tooling developers
- engineering leads defining the long-term Studio workbench UX
- current users of `FileShareEmulator` and `RulesWorkbench`
- maintainers of `StudioApiHost`
- reviewers assessing whether the Theia shell feels like a credible long-term replacement for existing tools

### 1.4 Definitions

- `TreeWidget`: Theia's native framework for rendering and interacting with hierarchical navigation content
- `native-first styling`: use default Theia tree behaviour and appearance before adding bespoke styling
- `view container`: the titled Theia side-bar host for a work-area view such as `Providers`
- `provider root`: the top-level provider node in a Studio work area, which remains actionable and opens an overview/editor surface
- `conventional tree`: a navigation control visually expressed as rows, indentation, expansion affordances, icons, selection, and optional badges, rather than as nested cards or prominent buttons

## 2. System context

### 2.1 Current state

The Studio shell currently renders the three main work areas using custom `ReactWidget` implementations that manually lay out text, buttons, and bordered containers.

Observed current behaviour includes:

- duplicate in-body headings such as `Providers` even though the surrounding view container already exposes that title
- provider rows rendered as prominent buttons rather than native tree items
- child nodes such as `Queue` and `Dead letters` rendered as more buttons nested inside boxed containers
- selected provider groups styled with strong background colouring derived from Theia selection tokens, which reads as visually distracting in the current card-based layout
- a visible `Refresh Providers` button even though provider availability is not expected to be changed through routine in-shell interaction

The current code already has a tree-shaped navigation model for providers, rules, and ingestion. The main gap is not data structure; the main gap is presentation and interaction style.

The current Studio skeleton also already depends on Theia concepts such as view containers, commands, context menus, and editor-area document opening, so moving to `TreeWidget` is a refinement of the existing shell rather than a fundamental platform shift.

For the `Rules` work area specifically, stakeholder direction for this work package is that `New Rule` should remain readily discoverable but should not continue as a large body-level button. Instead, it should move toward the kind of toolbar-style affordance commonly seen in native Theia side-bar views, with iconography and tooltip support.

Relevant first-party evidence from Theia documentation confirms that `TreeWidget` is the framework intended for hierarchical content in Theia, with support for:

- rendering of tree UIs
- selection and expansion
- filtering and event handling
- lazy child resolution
- context menus
- optional custom styling when necessary

Reference: [Theia Tree Widget documentation](https://theia-ide.org/docs/tree_widget/).

### 2.2 Proposed state

The Studio shell will continue to expose the same three work areas:

1. `Providers`
2. `Rules`
3. `Ingestion`

Each work area will present its hierarchical content using a native Theia tree implementation rather than a custom button-and-card layout.

The resulting navigation should feel much closer to a standard IDE/workbench tree:

- root and child nodes render as tree rows
- hierarchy is communicated by indentation and expansion behaviour, not boxed nesting
- icons and optional badges support scanability without dominating the layout
- provider descriptions do not consume persistent vertical space in the tree
- selection uses normal Theia tree selection semantics rather than reading like large call-to-action buttons
- double titles inside the view body are removed when the view container title already communicates the work-area identity

The work package shall preserve the established Studio interaction model where:

- provider roots remain actionable
- provider roots open overview/dashboard-style editor surfaces
- child nodes open their corresponding editor surfaces
- context menus and commands remain available where they add value

The work package shall also align action placement more closely with native Theia workbench conventions by preferring a view-level toolbar pattern for lightweight work-area actions, especially in `Rules`.

For refresh behaviour, stakeholder direction for this work package is that the Studio shell should not continue to use body-level refresh buttons. If explicit refresh actions remain visible, they should be limited to `Rules` and `Ingestion` and should use the same toolbar-style pattern as other side-bar actions.

The preferred implementation approach is native-first:

1. adopt `TreeWidget` and related Theia tree services
2. evaluate native visual results in the Studio shell
3. avoid custom CSS unless a clear functional issue remains; revisit minor tidy-up only after the full UI is finished and can be reviewed as a whole

### 2.3 Assumptions

- the current side-bar dissatisfaction is primarily caused by presentation and control choice rather than by the underlying information architecture
- the current provider, rules, and ingestion node models can be adapted into Theia tree node models without backend API changes
- a native Theia tree should provide better visual familiarity and lower distraction than the current button-heavy rendering
- consistency across all three work areas is more important than optimizing one work area in isolation
- redundant visible titles inside the view body do not add value when the enclosing view container already shows the work-area title
- provider refresh is not a meaningful routine end-user action in this shell because adding or changing a provider requires broader system restart or reconfiguration

### 2.4 Constraints

- the work package must preserve the existing top-level Studio work areas and their current meaning
- provider root nodes must remain actionable and continue to open overview/dashboard-style editors rather than becoming purely structural folders
- the design must prefer native Theia behaviour and styling before any custom CSS is introduced
- custom styling, if required, must be minimal, theme-respecting, and justified by a concrete usability gap
- the delivered result must be consistent across `Providers`, `Rules`, and `Ingestion`
- the work package is focused on look and feel and must not expand into unrelated functional redesign
- no changes are required to `StudioApiHost` contracts for this work package

## 3. Component / service design (high level)

### 3.1 Components

1. Shared Studio tree navigation foundation
   - a reusable Theia tree implementation approach for Studio side-bar navigation
   - common node rendering principles, selection behaviour, and interaction model

2. `Providers` tree view
   - provider roots with child nodes such as `Queue` and `Dead letters`
   - provider root remains an actionable overview/dashboard entry point
   - no visible `Refresh Providers` affordance in the primary view body
   - provider descriptions are not shown as persistent body text beneath provider nodes

3. `Rules` tree view
   - provider-scoped rule navigation using the same tree presentation principles
   - provider root remains actionable
   - rule checker and rule list/grouping remain tree content, not boxed sub-panels
   - explicit refresh may remain if needed, but only as a native toolbar-style action
   - provider descriptions are not shown as persistent body text beneath provider nodes

4. `Ingestion` tree view
   - provider-scoped ingestion navigation using the same tree presentation principles
   - provider root remains actionable
   - `By id`, `All unindexed`, and `By context` appear as normal tree nodes
   - explicit refresh may remain if needed, but only as a native toolbar-style action
   - provider descriptions are not shown as persistent body text beneath provider nodes

5. Optional minimal styling layer
   - only if native Theia rendering leaves a clear usability or polish gap
   - limited to small presentation adjustments rather than bespoke component restyling

### 3.2 Data flows

#### Tree population flow

1. Studio loads provider metadata from `/providers`
2. Studio loads rules metadata from `/rules` where relevant
3. Studio maps the current business data into Theia-compatible tree nodes
4. The relevant work-area tree model is populated
5. The side bar renders the tree using native Theia tree presentation

#### Node interaction flow

1. user selects or opens a tree node
2. Theia tree selection state updates using native tree semantics
3. the corresponding Studio command or document-open action is invoked
4. the matching overview or placeholder editor opens in the `editor area`
5. side-bar selection remains synchronized with the current provider context

#### Styling decision flow

1. implement the native Theia tree widget approach
2. review the rendered shell in the current Studio theme(s)
3. implement a native Theia toolbar-style command affordance for view-level actions such as `New Rule`, including tooltip behaviour
4. identify any remaining concrete issues such as spacing, icon alignment, or badge fit
5. add only minimal local styling where native rendering is insufficient

### 3.3 Key decisions

- **Use native Theia `TreeWidget` for Studio side-bar hierarchies**
  - rationale: Theia explicitly positions `TreeWidget` as the standard framework for hierarchical content, including selection, expansion, filtering, event handling, context menus, and styling hooks when needed

- **Apply one consistent tree style across `Providers`, `Rules`, and `Ingestion`**
  - rationale: the shell should feel like one coherent Studio application, not three visually unrelated side-bar experiences

- **Prefer native Theia appearance before custom styling**
  - rationale: native rendering is likely to feel more conventional, lower-risk, theme-compatible, and maintainable than bespoke visual treatment

- **Use a native Theia toolbar-style action surface for `New Rule`**
  - rationale: the action should remain discoverable, but moving it out of the body content reduces clutter and better matches established Theia side-bar patterns such as the built-in Explorer-style view actions with tooltips

- **Retain explicit refresh only where it adds value, and place it in the same toolbar pattern**
  - rationale: if refresh remains visible for `Rules` and `Ingestion`, it should use the same low-noise native toolbar design rather than reintroducing body-level clutter or inconsistent interaction chrome

- **Remove provider descriptions from the steady-state tree**
  - rationale: descriptive copy belongs in overview/editor surfaces; keeping it out of the tree preserves compactness and scanability

- **Remove nested card/button presentation**
  - rationale: bordered cards and `theia-button` controls make navigation look like nested action panels rather than a normal workbench tree

- **Remove duplicate in-body view titles**
  - rationale: the view container already communicates `Providers`, `Rules`, or `Ingestion`, so repeating that label inside the body adds clutter

- **Remove visible `Refresh Providers` from the primary Providers UI**
  - rationale: provider changes are not expected to be an everyday runtime action; the affordance overstates mutability and distracts from navigation

- **Keep provider roots actionable**
  - rationale: this preserves an important prior Studio decision and ensures roots open useful overview/dashboard editors rather than acting only as folders

- **Use minimal custom CSS only if needed after native evaluation**
  - rationale: styling should be evidence-driven and should not pre-emptively replace standard Theia behaviour; minor tidy-up decisions can be deferred until the finished UI can be reviewed holistically

## 4. Functional requirements

### FR-1 Shared tree framework

Studio shall implement side-bar hierarchical navigation for `Providers`, `Rules`, and `Ingestion` using Theia's `TreeWidget` framework or the equivalent native Theia tree infrastructure built on that framework.

### FR-2 Consistent work-area presentation

The three Studio work areas shall present their hierarchical navigation using one consistent visual and interaction model.

### FR-3 Conventional tree appearance

The side-bar hierarchy shall read visually as a conventional workbench tree, using rows, indentation, icons, selection, and expansion affordances rather than nested bordered boxes and prominent action buttons.

### FR-4 Native-first styling

The implementation shall first use native Theia tree behaviour and appearance. Custom CSS shall only be introduced after native rendering has been reviewed and only where a specific usability or polish issue remains.

### FR-4a CSS threshold

Custom CSS shall not be introduced merely for early cosmetic preference while the UI remains incomplete. If no clear functional issue exists, the implementation shall prefer stock Theia presentation and revisit small tidy-up adjustments only after the end-to-end UI is finished.

### FR-5 Providers view title de-duplication

The `Providers` work area shall not render a duplicate in-body `Providers` title when the enclosing Theia view container already displays that title.

### FR-6 Rules and ingestion title de-duplication

The same title de-duplication rule shall apply to `Rules` and `Ingestion` where the enclosing Theia view container already provides the visible title.

### FR-7 Remove visible provider refresh affordance

The `Providers` work area shall not expose a visible primary `Refresh Providers` affordance in the side-bar body.

### FR-7a Refresh visibility scope

Explicit visible refresh affordances may remain in `Rules` and `Ingestion`, but they shall not be reintroduced as body-level buttons.

### FR-7b Refresh action placement

Any visible refresh affordance retained in `Rules` or `Ingestion` shall use the same native Theia toolbar-style design language as other side-bar actions, including tooltip support.

### FR-7c Provider description placement

Provider descriptions shall not appear as persistent descriptive text within the side-bar tree for `Providers`, `Rules`, or `Ingestion`.

### FR-7d Provider description relocation

Where provider descriptions remain useful, they shall be presented in overview or editor surfaces rather than in the steady-state tree.

### FR-8 Preserve actionable provider roots

Provider root nodes shall remain actionable and shall continue to open provider-specific overview or dashboard editor surfaces.

### FR-9 Preserve child-node navigation

Child nodes such as `Queue`, `Dead letters`, `Rule checker`, `Rules`, `By id`, `All unindexed`, and `By context` shall remain navigable tree items that open the corresponding editor or placeholder surface.

### FR-9a Rules action placement

`New Rule` shall remain visible in the `Rules` work area, but it shall be exposed through a native Theia toolbar-style affordance rather than as a prominent top-of-view body button.

### FR-9b Toolbar discoverability

Toolbar-style actions introduced for Studio side-bar views shall provide standard discoverability cues, including tooltip text.

### FR-10 Default tree interaction model

Node opening and expansion behaviour shall follow normal Theia tree conventions unless Studio has a documented reason to diverge.

### FR-11 Theme-respecting selection

Selection and hover presentation shall be visually subdued, theme-respecting, and consistent with the rest of the Theia workbench. The implementation shall avoid making normal navigation rows read as call-to-action buttons.

### FR-12 Context menu continuity

Existing useful context-menu actions shall remain available where they still make sense in the tree-based presentation.

### FR-13 Non-essential body chrome reduction

The side-bar body shall avoid explanatory or decorative content that competes visually with the tree unless that content is essential to status, error handling, or user recovery.

### FR-14 Error and empty-state clarity

Loading, error, and empty states shall remain visible and understandable, but they shall not force the main steady-state navigation experience back into a button-and-panel layout.

### FR-15 Delivery completeness

The work package shall be considered complete only when `Providers`, `Rules`, and `Ingestion` all use the aligned tree presentation, even if implementation work is internally prototyped in one area first.

## 5. Non-functional requirements

- The resulting navigation shall feel credible as a professional Studio/workbench shell rather than a temporary mock screen.
- The implementation shall minimize custom presentation code and avoid unnecessary divergence from stock Theia patterns.
- The tree presentation shall respect active Theia theme variables and remain usable in the repository's expected dark-oriented development usage.
- The design shall support keyboard navigation and focus behaviour expected from standard Theia tree controls.
- The implementation shall remain maintainable and should reduce bespoke inline styling compared with the current approach.
- Styling changes shall not compromise readability, contrast, or discoverability of node labels and icons.
- Toolbar-style view actions shall remain low-noise, discoverable, and consistent with native Theia workbench behaviour.
- If multiple work areas expose toolbar actions, those actions shall use one consistent placement, emphasis, and tooltip pattern across Studio.
- Visual polish decisions should be deferred until the finished UI can be reviewed end to end, unless an earlier issue materially harms usability or clarity.

## 6. Data model

No backend data-contract change is required.

The existing Studio navigation data can continue to be sourced from:

- `GET /providers`
- `GET /rules`

The implementation shall adapt the current business models into native Theia tree node structures as required.

Expected logical node categories remain:

- provider root nodes
- provider child nodes for operational areas
- provider child/group nodes for rules
- provider child nodes for ingestion modes

If needed, Studio-specific node interfaces may extend standard Theia node contracts to retain references to provider metadata, rule metadata, node kind, badge state, and command-routing context.

## 7. Interfaces & integration

The work package shall integrate with:

- Theia `TreeWidget`
- Theia tree model and tree node infrastructure
- native Theia view-level toolbar or dynamic-toolbar mechanisms appropriate for side-bar actions
- existing Studio command contributions
- existing Studio document-opening services
- existing Studio provider and rules catalog services
- existing view containers and activity-bar structure

Required decision order for this feature area is:

1. extension model
   - this remains a native Theia extension concern rather than a `VS Code` extension concern, because the work is about Studio shell composition, custom side-bar views, and integration with existing Theia-native widgets and services
2. architecture placement
   - this capability is frontend-led because the tree and toolbar presentation live in the Theia workbench UI; backend changes are not required by the current requirement
3. closest built-in Theia feature to reuse as a pattern
   - the closest desired pattern is the native Theia side-bar view toolbar approach seen in Explorer-like views, combined with `TreeWidget` for the hierarchical content beneath it

Implementation guidance shall assume the approach documented in Theia's tree widget guidance, including:

- `TreeWidget` as the native hierarchical UI foundation
- tree node interfaces for root, container, and leaf nodes
- tree model initialization and synchronization with business data
- `LabelProvider` or equivalent logic for node labels and icons
- optional styling hooks only where necessary

Reference: [Theia Tree Widget documentation](https://theia-ide.org/docs/tree_widget/).

## 8. Observability (logging/metrics/tracing)

This work package does not require new backend observability.

Frontend logging may continue to record meaningful navigation and loading events, but the look-and-feel change should avoid adding noisy cosmetic logging. If the tree implementation changes load or selection event paths, existing diagnostic messages should remain understandable and scoped to real user or data events.

## 9. Security & compliance

This work package is presentational and does not introduce new security boundaries, authentication flows, or data classifications.

The implementation must not weaken existing handling of provider and rule metadata. No additional sensitive information should be surfaced solely as part of the tree restyling effort.

## 10. Testing strategy

The work package should be validated through a combination of focused technical checks and stakeholder UX review.

Required validation areas:

1. tree rendering
   - `Providers`, `Rules`, and `Ingestion` all render as conventional tree structures

2. navigation continuity
   - provider roots still open overview/dashboard editors
   - child nodes still open the expected placeholder/editor surfaces

3. consistency
   - all three work areas use aligned presentation, spacing, selection behaviour, and visual hierarchy

4. de-cluttering
   - duplicate in-body work-area titles are removed
   - the visible `Refresh Providers` affordance is removed
   - any retained refresh actions in `Rules` and `Ingestion` appear as toolbar actions rather than body buttons
   - provider descriptions are removed from the steady-state tree presentation

5. theme and usability review
   - selection is not visually distracting
   - icons and badges remain legible
   - keyboard navigation works as expected for a native Theia tree
   - custom CSS has not been introduced unless justified by a clear functional or usability issue

6. regression checks
   - existing provider/rules/ingestion node mapping logic remains correct
   - context menus continue to work where retained
   - empty and error states remain clear

Suggested manual smoke path:

1. start the local Studio stack
2. open `http://localhost:3000`
3. inspect `Providers`, `Rules`, and `Ingestion` in turn
4. confirm each side-bar view reads as a normal tree rather than as boxed panels
5. confirm the `Rules` view exposes `New Rule` through a toolbar-style affordance with tooltip support rather than a body-level button
6. open one provider root in each work area
7. open representative child nodes in each work area
8. confirm the workbench still opens the expected placeholder editors
9. confirm no duplicate work-area title appears inside the view body
10. confirm `Refresh Providers` is not shown in the primary `Providers` body UI
11. confirm any visible refresh action in `Rules` or `Ingestion` uses the same toolbar-style pattern as other Studio view actions
12. confirm provider descriptions are not shown as persistent text in the side-bar tree and are instead available in overview/editor surfaces where relevant

## 11. Rollout / migration

This work package may be implemented incrementally in development, but the stakeholder-reviewable outcome shall be treated as one UX change set covering all three work areas.

Migration expectations:

- preserve existing provider/rule/ingestion data loading services
- preserve existing document-opening behaviour
- replace custom side-bar body rendering with tree-based rendering
- keep any custom styling isolated and minimal if later proven necessary

Repository documentation for Studio should be updated after implementation to describe the move to native tree-based navigation if that becomes part of the accepted baseline.

## 12. Open questions

1. Resolved: `Rules` should keep `New Rule` visible, but as a native Theia toolbar-style action with tooltip support rather than as a body-level button.
2. Resolved: explicit refresh affordances may remain visible in `Rules` and `Ingestion`, but they should use the same native toolbar-style design as other Studio view actions rather than body-level buttons.
3. Resolved: provider descriptions should not remain visible in the side bar; descriptive text should move to overview/editor surfaces so the tree stays maximally compact.
4. Resolved: custom CSS should be avoided unless a clear functional issue remains; otherwise, minor visual tidy-up should be revisited later once the UI is finished.
