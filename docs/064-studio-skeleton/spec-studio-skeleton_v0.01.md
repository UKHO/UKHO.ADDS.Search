# Work Package: `064-studio-skeleton` — Theia Studio skeleton

**Target output path:** `docs/064-studio-skeleton/spec-studio-skeleton_v0.01.md`

**Version:** `v0.01` (Draft)

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for delivering a first interactive skeleton of the Theia-based Studio application.

The goal is to establish a high-quality mock UI in Eclipse Theia that lifts, with suitable abstraction, the navigation and editor/workbench concepts currently found in `FileShareEmulator` and `RulesWorkbench`, without yet implementing the real editor functionality. The delivered experience shall provide working `Activity Bar` navigation, `Side Bar` trees, editor opening behavior, and placeholder editor surfaces that can later be replaced with real implementations.

The skeleton shall use the existing Studio backend APIs where already available, specifically `StudioApiHost /providers` and `StudioApiHost /rules`, so that provider and rule trees are populated from live application metadata rather than hard-coded UI fixtures.

All other workbench surfaces in this package are intentionally placeholder-driven. The purpose of the skeleton is to let stakeholders assess the overall look, navigation model, and workbench shape before subsequent work packages begin lifting and translating real functionality from `RulesWorkbench` and `FileShareEmulator`.

### 1.2 Scope

This specification covers:

- introducing a Studio shell UX organized around three `Activity Bar` items: `Providers`, `Rules`, and `Ingestion`
- implementing corresponding `view containers` in the `Side Bar`
- representing providers as the root navigation concept in all three areas
- opening provider dashboards and placeholder editors in the `editor area`
- using existing provider and rule APIs to populate navigation trees
- shaping ingestion navigation around the existing ingestion modes already present in `FileShareEmulator`, uplifted into Studio terminology
- defining placeholder editor/document surfaces for queue inspection, dead-letter inspection, rule checking, rule editing, new-rule creation, and ingestion workbench flows
- defining interaction patterns, context menus, commands, badges, and layout expectations for a polished first skeleton
- using live `/providers` and `/rules` data where available, while keeping other editor and tool surfaces mocked or placeholder-based for now

This specification does not cover:

- implementing real queue inspection, dead-letter browsing, rule editing, rule persistence, or ingestion execution behavior
- replacing existing Blazor tools feature-for-feature in this work package
- final visual design, branding, or detailed component styling beyond a strong initial UX baseline
- authentication or authorization changes
- extending backend APIs except where minimal adaptation is required to support the shell skeleton

### 1.3 Stakeholders

- Studio/tooling developers
- search ingestion and rules developers
- maintainers of `StudioHost` and `StudioApiHost`
- engineering leads defining the long-term Studio experience
- users currently relying on `FileShareEmulator` and `RulesWorkbench`

### 1.4 Definitions

- `Activity Bar`: the far-left strip of top-level navigation icons in the Theia workbench
- `Side Bar`: the left-hand navigation panel hosting the active `view container`
- `editor area`: the primary central workspace where editor-like content surfaces open
- provider: a named ingestion provider, such as `file-share`
- provider dashboard: the operational overview editor opened when a provider root node is selected
- placeholder editor: a navigable, non-production editor surface that proves layout, routing, and interaction wiring without full business behavior

## 2. System context

### 2.1 Current state

The repository already contains a Theia Studio shell baseline and an emerging provider metadata model.

Existing user workflows are currently split across separate Blazor-based tools, primarily `FileShareEmulator` and `RulesWorkbench`. Those tools provide useful operational and authoring capabilities, but their UI model is comparatively basic and does not take advantage of richer workbench concepts such as IDE-style navigation trees, multi-editor layouts, panel-based diagnostics, command surfaces, or contextual work areas.

The repository also already exposes backend APIs relevant to this skeleton work:

- `StudioApiHost /providers`
- `StudioApiHost /rules`

These APIs provide enough information to begin building live navigation trees without committing to full editor implementations in this work package.

Current ingestion workflows already exist in `FileShareEmulator`, including:

- individual ingestion by id
- ingest all remaining unindexed items
- ingest by business unit

For Studio terminology, `business unit` should be presented as `context`, aligning with the broader Studio and rule vocabulary.

### 2.2 Proposed state

The Theia Studio application will evolve into a multi-work-area workbench organized around three top-level modes exposed through the `Activity Bar`:

1. `Providers`
2. `Rules`
3. `Ingestion`

Each `Activity Bar item` opens a corresponding `view container` in the `Side Bar`. In each container, providers remain the top-level organizing concept because:

- rules are provider-specific
- ingestion flows are provider-specific
- users are likely to focus on one provider at a time
- a single rule is not expected to be shared across multiple providers

The `editor area` becomes the primary work surface. Tree nodes act as navigation. Selecting nodes opens dashboard or editor content in the `editor area`.

Provider root nodes are actionable. Selecting a provider root opens a provider-level operational dashboard rather than acting only as a structural node.

For this work package, only the provider and rule navigation data is expected to come from live backend endpoints. Queue inspectors, dead-letter inspectors, rule editors, rule checker surfaces, ingestion overviews, ingestion mode screens, and panel content remain placeholder implementations whose purpose is to validate structure, navigation, and overall UX direction rather than to deliver feature-complete tooling.

### 2.3 Assumptions

- the first delivery may use placeholder editors as long as navigation and workbench behavior are real and coherent
- the existing `StudioApiHost /providers` API is sufficient to populate provider roots for all three work areas
- the existing `StudioApiHost /rules` API is sufficient to populate provider-specific rule nodes in the `Rules` area
- the shell should favor Theia workbench conventions over reproducing existing Blazor layouts one-for-one
- the UX should feel like a serious tool from the first skeleton, using editor tabs, trees, commands, and diagnostics surfaces where appropriate
- provider context should remain visually obvious throughout the experience
- the provider list is effectively constant for the skeleton and will be present in all three work areas
- ingestion should reflect the existing supported ingestion modes rather than inventing unrelated placeholder concepts such as `Recent runs`
- Studio terminology should use `context` where legacy tools currently use `business unit`
- future work packages will refine visuals, behaviors, and feature depth without changing the core navigation model established here

### 2.4 Constraints

- the work package delivers a UI skeleton, not full business implementations
- only `/providers` and `/rules` are expected to supply live data in this work package; other workbench surfaces remain placeholder-driven
- provider roots shall be present in all three `Activity Bar` work areas
- provider root nodes shall open an operational dashboard/editor rather than acting as purely structural folders
- the `Rules` tree shall use a dedicated `Rules` grouping node under each provider to avoid excessive vertical sprawl when providers have large rule counts
- `New rule` shall be exposed as a command and context-menu action rather than a permanent tree node
- the design should build on existing Theia shell foundations already established in earlier work packages
- the design should use Theia terminology and workbench patterns consistently
- the primary outcome of this package is to validate overall look, navigation, and workbench structure before later work packages introduce real lifted functionality

## 3. Component / service design (high level)

### 3.1 Components

1. `Providers` work area
   - `Activity Bar item` and `view container`
   - provider-centric operational navigation
   - provider dashboard and resource inspectors
   - initial child nodes limited to `Queue` and `Dead letters`

2. `Rules` work area
   - `Activity Bar item` and `view container`
   - provider-scoped rule navigation
   - provider root rules overview page
   - rule checker, rule editors, and new-rule flow

3. `Ingestion` work area
   - `Activity Bar item` and `view container`
   - provider-scoped ingestion tooling and manual submission flow
   - placeholder support for ingestion by id, all remaining unindexed, and by context
   - provider root overview page with dashboard metrics and pertinent operational actions
   - explicit child navigation nodes for the supported ingestion modes because those experiences are expected to grow independently

4. Studio tree data adapters
   - adapters that transform `StudioApiHost /providers` and `StudioApiHost /rules` responses into Theia tree models

5. Placeholder editors
   - central editor-area widgets/pages representing each navigable work surface

6. Command and menu contributions
   - toolbar actions, context menus, and command palette integration for core Studio actions

7. Diagnostics and auxiliary panel integration
   - initial integration with the lower `Panel` for logs, problems, validation output, and operational traces

### 3.2 Data flows

#### Workbench navigation flow

1. user selects an `Activity Bar item`
2. corresponding `view container` opens in the `Side Bar`
3. provider tree is populated from Studio APIs or from derived UI state
4. user selects a tree node
5. the corresponding dashboard or editor opens in the `editor area`
6. editor title, breadcrumbs, and contextual actions reflect the selected provider and work surface

#### Rules tree population flow

1. Studio loads provider metadata from `/providers`
2. Studio loads available rules from `/rules`
3. rules are grouped under their owning provider
4. each provider gains a dedicated `Rules` grouping node
5. selecting an individual rule opens a placeholder rule editor document in the `editor area`

#### Rules overview flow

1. user selects a provider root in the `Rules` work area
2. Studio opens a provider-scoped rules overview editor
3. the overview presents a dashboard-style summary of the provider's rules
4. the overview presents useful summary information such as active and invalid rule counts
5. the overview may reserve placeholder space for future concepts such as versioning without implying implemented version support
6. the overview exposes quick actions such as `New rule` and navigation to the rule checker
7. the `Rule checker` child node remains a distinct editor target rather than duplicating the provider root behavior

#### Provider dashboard flow

1. user selects a provider root such as `file-share`
2. Studio opens a provider overview editor
3. the overview shows placeholder operational summary cards, including queue and dead-letter counts
4. the overview also presents any other useful provider information already available through provider metadata
5. quick actions navigate to related queue, dead letters, rule checker, or ingestion surfaces

#### Ingestion overview flow

1. user selects a provider root in the `Ingestion` work area
2. Studio opens a provider-scoped ingestion overview editor
3. the overview shows placeholder dashboard information, including indexed and non-indexed counts
4. the overview exposes quick actions that navigate to supported ingestion modes
5. the overview may also expose pertinent operational actions such as resetting indexing status

#### Ingestion mode navigation flow

1. Studio shows explicit child nodes beneath each provider in the `Ingestion` work area
2. the child nodes represent `By id`, `All unindexed`, and `By context`
3. selecting one of those nodes opens a dedicated placeholder editor for that ingestion mode
4. each mode-specific editor is free to evolve its own layout, such as preview, progress, parameters, and results, without overloading the provider overview page

### 3.3 Key decisions

- **Three top-level work areas:** `Providers`, `Rules`, and `Ingestion`
  - rationale: these represent distinct user modes of work while remaining extensible for future growth

- **Provider as root concept in every work area**
  - rationale: provider is the dominant business axis across operations, rules, and ingestion workflows

- **Provider roots are actionable**
  - rationale: root nodes should open useful dashboards, not act only as folders

- **Tree nodes are for navigation; rich content opens in the `editor area`**
  - rationale: this best matches Theia/IDE workbench expectations and supports a more sophisticated tool experience

- **Dedicated `Rules` grouping node under each provider**
  - rationale: some providers may have `50-60` rules, so a flat list would waste vertical space and reduce scannability

- **`New rule` as an action, not a persistent tree item**
  - rationale: command surfaces are cleaner and more scalable than adding synthetic tree clutter

- **Rules provider roots should open a rules overview page**
  - rationale: a dashboard-style landing page provides space for summary information and rule-related actions, while keeping the `Rule checker` as a distinct focused tool

- **Rules overview should emphasize actionable quality signals**
  - rationale: active and invalid summaries help users assess the ruleset quickly, while quick actions such as `New rule` turn the overview into a useful working landing page

- **Rules tree nodes should support visual status decoration**
  - rationale: lightweight status indicators such as invalid-state decoration improve scanability in large rule sets, while the exact badge or icon treatment can be refined later

- **Use live provider/rule APIs for the tree model**
  - rationale: a navigation skeleton backed by real metadata provides higher confidence and better future continuity than a fully mocked tree

- **Placeholder editors are acceptable in this work package**
  - rationale: the main objective is to establish the workbench model, navigation flows, and user experience skeleton

- **Ingestion should follow existing workflow types from `FileShareEmulator`**
  - rationale: this keeps the Studio skeleton grounded in known user workflows while allowing the UI to be lifted into a more capable Theia workbench shape

- **Use `context` instead of `business unit` in Studio UX**
  - rationale: the Studio information model should use one consistent domain term across rule and ingestion experiences

- **Ingestion provider roots should open an overview page**
  - rationale: this gives users a clear landing surface for provider-scoped ingestion work, creates space for useful metrics and quick actions, and avoids overcrowding individual ingestion-mode screens

- **Useful operational actions may appear in more than one place**
  - rationale: duplicating high-value actions in appropriate contexts can improve usability without weakening the information architecture

- **Ingestion modes should be separate child nodes beneath the provider**
  - rationale: the three ingestion experiences are expected to grow and diverge, so they should have independent navigation targets and dedicated editor surfaces

- **Provider selection should stay aligned across all three work areas**
  - rationale: the provider list is constant across `Providers`, `Rules`, and `Ingestion`, so carrying the current provider between them reinforces the user's current working context

- **Use the lower `Panel` in the first skeleton**
  - rationale: introducing the `Panel` now strengthens the workbench model early and provides a natural home for logs, validation output, and operation feedback even while editors remain placeholders

- **Keep the initial `Providers` tree minimal**
  - rationale: limiting the first skeleton to `Queue` and `Dead letters` keeps the navigation focused while still covering the highest-value operational surfaces

- **Providers overview should use available provider metadata**
  - rationale: reusing existing provider metadata makes the overview more useful immediately without requiring new concepts to be invented for the first skeleton

- **Provider nodes should support useful tree decorations**
  - rationale: lightweight provider-level badges or warning indicators improve scanability and make operational state visible before opening overview editors, while the exact visual treatment can be refined later

- **Editor opening should follow normal Theia behavior**
  - rationale: using the platform's default interaction model makes the skeleton feel native to Theia and avoids introducing custom navigation semantics unnecessarily

## 4. Functional requirements

### FR-001 Provide three Studio work areas

The Studio shell shall expose three `Activity Bar` items named `Providers`, `Rules`, and `Ingestion`.

### FR-002 Open corresponding view containers

Selecting each `Activity Bar item` shall open or focus its corresponding `view container` in the `Side Bar`.

### FR-003 Use providers as root nodes

Each of the three `view containers` shall use named providers as the root nodes of their tree structure.

### FR-004 Populate providers from API

The provider tree roots shall be populated from `StudioApiHost /providers` rather than being hard-coded.

### FR-004a Limit live data usage in the skeleton

The first skeleton shall use live backend data for provider and rule navigation where available, but all other editor and tool surfaces shall remain mocked or placeholder-based unless explicitly stated otherwise.

### FR-005 Providers tree structure

Within the `Providers` work area, each provider shall expose navigable child nodes for operational resources, initially limited to `Queue` and `Dead letters`.

### FR-006 Provider root dashboard

Selecting a provider root in the `Providers` work area shall open an operational dashboard editor in the `editor area`.

### FR-006a Provider overview content

The provider overview shall present queue and dead-letter counts together with any other useful provider information already available from provider metadata.

### FR-006b Provider tree decorations

Provider nodes in Studio trees shall support useful provider-level decorations or badges in the first skeleton, including counts or warning states where that information is available.

### FR-007 Queue inspector placeholder

Selecting a queue node shall open a queue inspector placeholder editor in the `editor area`.

### FR-008 Dead-letter inspector placeholder

Selecting a dead-letter node shall open a dead-letter inspector placeholder editor in the `editor area`.

### FR-009 Rules tree structure

Within the `Rules` work area, each provider shall expose a `Rule checker` node and a dedicated `Rules` grouping node.

### FR-010 Populate rules from API

The child rule nodes beneath each provider's `Rules` grouping node shall be populated from `StudioApiHost /rules`.

### FR-011 Rule checker placeholder

Selecting the `Rule checker` node in the `Rules` work area shall open a provider-scoped rule-checking placeholder editor in the `editor area`.

### FR-011a Rules overview placeholder

Selecting a provider root in the `Rules` work area shall open a provider-scoped rules overview placeholder editor in the `editor area`.

### FR-011b Rules overview summary information

The rules overview shall present placeholder summary information including active and invalid rule counts, and may include placeholder space for future concepts such as versioning.

### FR-011c Rules overview quick actions

The rules overview shall expose quick actions including at minimum `New rule` and navigation to the `Rule checker`.

### FR-012 Individual rule editor placeholder

Selecting an individual rule node shall open a placeholder rule editor document in the `editor area`.

### FR-012a Rule tree status decoration

Individual rule nodes in the `Rules` tree shall support visual status decoration in the first skeleton, including at minimum a useful invalid-state indicator, with the exact badge or icon style to be refined later.

### FR-013 New rule command

The Studio shell shall expose a `New rule` action from appropriate command surfaces, including toolbar and context menu locations relevant to the `Rules` work area.

### FR-014 New rule editor placeholder

Invoking `New rule` shall open a placeholder new-rule editor in the `editor area` for the currently selected provider.

### FR-015 Ingestion provider structure

Within the `Ingestion` work area, each provider shall be represented as a root node that opens a provider-scoped ingestion overview placeholder editor.

### FR-015a Ingestion mode coverage

The placeholder ingestion experience shall cover the existing ingestion workflow types currently used in `FileShareEmulator`, namely ingestion by id, ingestion of all remaining unindexed items, and ingestion by `context`.

### FR-015aa Ingestion mode child nodes

Within the `Ingestion` work area, each provider shall expose explicit child nodes for `By id`, `All unindexed`, and `By context` beneath the provider root overview node.

### FR-015b Ingestion overview metrics

The provider root ingestion overview shall present placeholder dashboard metrics relevant to ingestion state, including indexed versus non-indexed counts.

### FR-015c Ingestion overview actions

The provider root ingestion overview shall expose quick actions for the supported ingestion modes and may expose pertinent operational actions such as resetting indexing status.

### FR-015d Mode-specific ingestion placeholders

Selecting `By id`, `All unindexed`, or `By context` shall open a dedicated placeholder editor for that ingestion mode in the `editor area`.

### FR-016 Editor tabs and navigation

Opened dashboards and placeholder editors shall appear as editor-like documents in the Theia `editor area`, with titles that clearly indicate the provider and work surface.

### FR-016a Default editor interaction model

Editor opening behavior in the first skeleton shall follow the normal default Theia interaction model, including preview-versus-pinned behavior where that is the platform default.

### FR-017 Context menus

Relevant tree nodes shall expose context menus appropriate to their node type.

### FR-018 Toolbar commands

Relevant work areas and editors shall expose toolbar actions for their primary tasks.

### FR-019 Command palette integration

Core Studio commands, including at minimum `New rule`, shall be invokable through the command palette.

### FR-020 Provider context visibility

The active provider context shall be visually clear in navigation and editor surfaces.

### FR-021 Cross-work-area provider alignment

Switching between `Providers`, `Rules`, and `Ingestion` shall preserve and auto-focus the currently selected provider where possible.

### FR-022 Lower panel integration

The first skeleton shall include the lower `Panel` as part of the Studio workbench experience for placeholder diagnostics, output, validation feedback, and operation traces.

## 5. Non-functional requirements

- the skeleton shall present a polished IDE-style workbench experience rather than a temporary-looking demo shell
- layout and interaction choices shall favor clarity, scannability, and extensibility
- the design shall conserve vertical space in navigation trees
- the shell shall remain responsive when providers expose high rule counts
- placeholder surfaces shall use consistent language, iconography, and layout structure
- the implementation should be structured so real editors can replace placeholders without reworking the navigation model

## 6. Data model

### 6.1 Navigation concepts

- Work area
- Provider
- Provider resource node
- Rule checker node
- Rules grouping node
- Rule node
- Ingestion overview node
- Ingestion mode node
- Editor descriptor

### 6.2 Derived UI metadata

The UI layer may derive additional metadata per node, such as:

- node type
- provider name
- target editor identifier
- badge count
- icon
- context menu capability set
- placeholder status text

## 7. Interfaces & integration

### 7.1 Existing APIs

The skeleton shall integrate with:

- `StudioApiHost /providers`
- `StudioApiHost /rules`

### 7.2 UI integration expectations

- provider lists shall be built from `/providers`
- rule lists shall be grouped beneath providers using `/rules`
- if API responses omit information needed for ideal presentation, the UI may use sensible placeholder text or derived labels until later work packages refine the contracts

## 8. Observability (logging/metrics/tracing)

This work package does not require production observability features for the placeholder editors.

However, the implementation should support basic diagnostics useful during shell development, such as:

- loading state visibility for tree population
- placeholder error surfaces for API failures
- output/panel logging for command execution and navigation diagnostics

## 9. Security & compliance

No new security model is introduced by this work package.

The shell shall continue to rely on existing host and API security boundaries. Placeholder editors shall not introduce sensitive mock data beyond what is already available through the relevant APIs.

## 10. Testing strategy

Testing for this work package should focus on navigation and UX skeleton behavior, including:

- `Activity Bar` switching
- `Side Bar` tree population
- correct grouping of rules under providers
- provider root opening behavior
- editor opening for all supported node types
- command visibility and invocation for `New rule`
- graceful placeholder behavior when APIs return empty or failed responses

Where end-to-end UI verification is automated, Playwright-style tests are preferred.

## 11. Rollout / migration

This work package introduces the first navigable Studio skeleton and does not replace the existing Blazor tools in the same step.

Existing tools may continue to exist while the Theia Studio experience matures. Later work packages can progressively replace placeholder editors with fully functional implementations and migrate workflows from `FileShareEmulator` and `RulesWorkbench` in stages.

The immediate purpose of this stage is to produce something concrete that stakeholders can review for overall look, navigation, information architecture, and workbench feel before substantive functionality is lifted and translated into the new Studio surfaces.

## 12. Open questions

None at present. Further clarification can extend this specification in-place.
