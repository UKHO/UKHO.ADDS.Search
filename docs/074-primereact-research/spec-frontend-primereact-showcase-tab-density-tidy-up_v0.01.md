# Work Package: `074-primereact-research` — PrimeReact showcase tab density tidy-up

**Target output path:** `docs/074-primereact-research/spec-frontend-primereact-showcase-tab-density-tidy-up_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft created for a further visual tidy-up of the consolidated PrimeReact `Showcase` tab.
- `v0.01` — Recorded the requirement to correct the grid vertical scrollbar behavior when the workbench width becomes narrow.
- `v0.01` — Recorded the requirement to reduce font size, font weight, spacing, and title scale so the page aligns more closely with Theia desktop density.
- `v0.01` — Recorded the requirement to use the existing filter textbox typography as the preferred compact baseline for nearby showcase controls.
- `v0.01` — Recorded the requirement to remove the summary/status panel row entirely from the `Showcase` tab.

## 1. Overview

### 1.1 Purpose

This specification defines a focused refinement pass for the consolidated PrimeReact `Showcase` tab inside the Theia-based Studio shell.

The purpose is to make the retained `Showcase` tab feel materially closer to native Theia workbench density by tightening typography, reducing visual weight, removing unnecessary summary chrome, and correcting an observed scroll behavior defect in the grid region.

This work is a follow-on tidy-up rather than a redesign. It builds on the existing compact showcase direction and narrows the styling further toward:

- smaller typography
- lighter font weight
- tighter row and node density
- flatter presentation
- less summary chrome
- more predictable internal scrolling

### 1.2 Scope

This specification covers only the `Showcase` tab within the consolidated PrimeReact demo page.

This specification includes:

- correcting the grid vertical scrollbar behavior at narrower workbench widths
- reducing grid typography size and visual weight
- reducing hierarchy tree typography size and spacing
- using the existing showcase filter textbox typography as a visual density reference for nearby controls
- removing the summary/status strip currently showing items such as `Grid selection`, `Hierarchy selection`, `Scenario`, and `Last action`
- reducing the size and emphasis of remaining section titles and headings
- aligning the overall presentation more closely with Theia density, rhythm, and weight

This specification does not include:

- changes to non-`Showcase` tabs unless shared styling tokens or shared helper classes must be adjusted safely
- functional changes to mock data, scenario behavior, or detail-edit workflows
- broader Theia shell changes outside the PrimeReact showcase surface
- backend or service changes

### 1.3 Stakeholders

- Studio shell developers
- reviewers assessing PrimeReact fit inside Theia
- UX and product stakeholders reviewing workbench-style density and readability
- maintainers of the temporary PrimeReact research package

### 1.4 Definitions

- `showcase tab`: the first tab in the consolidated PrimeReact demo page that combines hierarchy, grid, and detail regions in one surface
- `summary/status strip`: the upper section currently showing compact status panels such as `Grid selection`, `Hierarchy selection`, `Scenario`, and `Last action`
- `density baseline`: the preferred compact visual scale for typography, spacing, row height, and padding that should resemble Theia more closely
- `filter textbox reference`: the existing `Grid filter` and `Filter showcase hierarchy` textbox typography, which is considered closer to the desired font size and weight than other current text in the page
- `single-owner scrolling`: the rule that the relevant inner pane or control should own scrolling for its own overflowing content without depending on incidental resize side effects

## 2. System context

### 2.1 Current state

The consolidated PrimeReact showcase already uses a flatter and tighter presentation than earlier research pages, but the current `Showcase` tab still appears too large and too heavy for the intended Theia workbench look and feel.

The current issues observed on the `Showcase` tab include:

- the grid does not reliably display a vertical scrollbar when the window becomes too narrow, even though the lower detail panel continues to behave correctly
- grid text appears too large and too visually heavy
- typography across the page generally uses more weight than desired, with too much bold emphasis
- the hierarchy tree with checkboxes uses excessive vertical spacing and oversized text
- the hierarchy tree does not visually align with the compact typography already seen in the `Filter showcase hierarchy` textbox
- the summary/status strip consumes valuable vertical space while adding low-value chrome
- remaining titles are still larger and heavier than desired for a Theia-style workbench surface

This leaves the `Showcase` tab denser than earlier iterations but still not sufficiently close to the tighter Theia desktop aesthetic.

### 2.2 Proposed state

The `Showcase` tab shall be refined into a smaller, lighter, and denser workbench-like surface.

In the proposed state:

- the grid shall reliably show its own vertical scrollbar whenever its content exceeds the available height, including at narrower widths
- the grid typography shall be reduced and made visually lighter
- the hierarchy tree shall use materially tighter node spacing and smaller text
- nearby showcase typography shall take its sizing and weight cues from the existing compact filter textboxes rather than from current heavier labels and headings
- the summary/status strip shall be removed entirely
- remaining titles shall be smaller, less heavy, and more consistent with the Theia workbench visual language
- the overall page shall feel flatter, denser, and calmer, with reduced emphasis and less decorative hierarchy

### 2.3 Assumptions

- The page remains a temporary research surface for evaluating PrimeReact inside Theia.
- The existing `Grid filter` and `Filter showcase hierarchy` textbox typography is close enough to the desired compact baseline to be used as a reference point for nearby controls.
- The detail panel scrollbar behavior is already acceptable and should not be loosened while correcting the grid behavior.
- The requested refinement is primarily visual and layout-oriented rather than functional.
- The design target remains a compact desktop workbench surface rather than a spacious web-page presentation.

### 2.4 Constraints

- The refinement must remain local to the `Showcase` tab, except where safe shared styling reuse is required.
- Typography and spacing changes must not damage readability or basic usability.
- The grid scrollbar correction must be deterministic and must not depend on the user widening the window before the scrollbar appears.
- The summary/status strip shall be removed in full rather than partially restyled.
- The implementation must continue to favor styled PrimeReact and a Theia-aligned workbench appearance.

## 3. Component / service design (high level)

### 3.1 Components

1. `Showcase tab shell`
   - the outer content area of the retained `Showcase` tab
   - should become visually lighter and less vertically wasteful after the summary/status strip is removed

2. `Showcase title and section headings`
   - the remaining visible headings within the tab
   - should move to a smaller, less dominant scale with lighter weight

3. `Grid filter region`
   - the filter label and textbox above the grid
   - remains functionally unchanged
   - acts as a reference point for preferred compact typography and weight

4. `Hierarchy filter region`
   - the hierarchy filter textbox above the tree
   - remains functionally unchanged
   - acts as a reference point for the desired compact watermark/textbox typography

5. `Hierarchy tree pane`
   - the tree control with checkboxes and expandable nodes on the left side
   - should adopt smaller typography and materially tighter row spacing

6. `Grid pane`
   - the data table region on the right side
   - should adopt more compact text and reliable internal vertical scrolling

7. `Detail pane`
   - the lower record detail/editor surface
   - remains present
   - should continue to own its scrolling correctly without regression

### 3.2 Data flows

#### Layout and resize flow

1. the user opens the consolidated PrimeReact page on the `Showcase` tab
2. the tab renders the hierarchy, grid, and detail regions with reduced visual scale and lighter emphasis
3. the user resizes the workbench narrower or wider
4. the grid pane recalculates within the available layout space
5. if the grid content exceeds the available vertical area, the grid region displays and owns its vertical scrollbar consistently

#### Visual hierarchy flow

1. the tab renders headings, labels, filters, hierarchy nodes, and grid content
2. compact textbox typography provides the reference for nearby content density
3. titles, row text, and hierarchy node text remain readable but lighter and smaller than before
4. the absence of the summary/status strip increases space available for working content rather than decorative context

### 3.3 Key decisions

- The requested refinement is limited to the retained `Showcase` tab rather than all tabs equally.
- The existing filter textbox typography is the preferred local reference for desired compact sizing and weight.
- The grid scrollbar issue is treated as a concrete layout defect, not as an optional polish item.
- The hierarchy tree must become substantially denser than it is now.
- The summary/status strip is removed rather than redesigned.
- Remaining titles should be quieter and smaller so the page aligns more closely with Theia workbench styling.

## 4. Functional requirements

### 4.1 Scope and page structure

1. The implementation shall apply this refinement only to the consolidated PrimeReact `Showcase` tab unless a safe shared styling mechanism is required.
2. The implementation shall preserve the existing broad working structure of hierarchy, grid, and detail regions.
3. The implementation shall remove the summary/status strip currently showing `Grid selection`, `Hierarchy selection`, `Scenario`, `Last action`, and any equivalent title or panel wrappers belonging to that section.
4. After removal of the summary/status strip, the available vertical space shall be reallocated to the remaining working content rather than left as decorative whitespace.

### 4.2 Grid scrolling behavior

5. The grid region shall display and own a vertical scrollbar whenever its row content exceeds the visible height available to the grid pane.
6. The grid vertical scrollbar shall appear consistently when the workbench becomes narrower and causes the effective grid viewport height to be insufficient.
7. The appearance of the grid vertical scrollbar shall not depend on the user first resizing the window wider.
8. The scrollbar correction shall be achieved through deterministic layout and overflow behavior rather than through incidental repaint side effects.
9. The detail pane below the grid shall retain its currently correct scrollbar behavior and shall not regress as part of the grid fix.

### 4.3 Grid typography and density

10. Grid body text shall be reduced from the current size to a more compact desktop-oriented baseline.
11. Grid header text shall also be reduced and shall not remain visually oversized relative to the compact body text.
12. Grid typography shall use a lighter visual weight than the current presentation.
13. Strong bold emphasis in the grid shall be reduced or removed unless it remains necessary for specific high-value cues.
14. Grid row density, padding, and line-height shall be reviewed together so the grid feels tighter overall rather than merely smaller in font size.
15. The `Grid filter` label and textbox shall remain functionally and visually acceptable and shall not be enlarged or made heavier.
16. The existing `Grid filter` textbox typography shall be treated as a local reference for desired compact size and weight when refining nearby grid typography.

### 4.4 Typography weight and emphasis

17. Typography across the `Showcase` tab shall move toward lighter weight and reduced emphasis.
18. The implementation shall ease off bold usage across labels, headings, tree nodes, and grid text unless bold emphasis is clearly justified.
19. The default visual reading of the page shall be compact and calm rather than strong, heavy, or over-emphasized.
20. Where semantic emphasis is still needed, the implementation should prefer restrained weight differences over broad use of bold text.

### 4.5 Hierarchy tree density

21. The hierarchy tree control with checkboxes shall use materially tighter vertical spacing than the current presentation.
22. Hierarchy node text shall be reduced so it no longer reads larger than the desired compact baseline.
23. The target visual scale for hierarchy node text shall align closely with the watermark text size seen in the `Filter showcase hierarchy` textbox above it.
24. Checkbox, expander, icon, and row padding around hierarchy nodes shall be tightened so the control feels dense and workbench-like.
25. The hierarchy pane shall remain readable and interactive after density reduction.
26. The `Filter showcase hierarchy` textbox itself shall remain broadly unchanged because it already reflects the preferred density direction.

### 4.6 Titles and headings

27. Remaining titles and headings on the `Showcase` tab shall be reduced in size.
28. Remaining titles and headings shall use lighter weight than the current presentation.
29. Section titles shall support orientation without dominating the page.
30. The resulting title hierarchy shall feel closer to Theia workbench text scale than to a marketing or dashboard layout.

### 4.7 Theia alignment goal

31. The overall styling outcome shall align more closely with Theia in font size, font weight, density, and spacing.
32. The refinement shall favor compact workbench utility over decorative presentation.
33. The implementation shall preserve usability while materially increasing visible information density.

## 5. Non-functional requirements

1. The refined `Showcase` tab should feel noticeably closer to native Theia density at first glance.
2. The page should present smaller and lighter typography without becoming difficult to read.
3. The hierarchy and grid should feel denser and more efficient without appearing broken or cramped.
4. Removal of the summary/status strip should improve focus on working content rather than reducing clarity.
5. The scrollbar behavior should remain stable across repeated workbench resize operations.
6. The result should continue to look like one coherent styled PrimeReact surface rather than a patchwork of one-off overrides.

## 6. Data model

No domain or persistence data-model changes are required for this refinement.

The work concerns only presentation density, layout behavior, and local interaction affordances for existing mock showcase content.

## 7. Interfaces & integration

### 7.1 UI integration

- The refinement applies to the existing consolidated PrimeReact page and its retained `Showcase` tab.
- No new route, command, menu item, or backend integration is required.
- Existing local mock content and page interaction patterns remain in place.

### 7.2 Styling integration

- The implementation may adjust local showcase styling, component wrappers, and layout rules.
- If shared styling hooks are reused, they must not unintentionally loosen density on other retained tabs.
- PrimeReact component behavior should continue to align with styled-mode expectations.

## 8. Observability (logging/metrics/tracing)

No new logging, metrics, or tracing requirements are introduced for this styling-focused refinement.

If implementation diagnostics are temporarily required during development, they should not be treated as a required runtime feature of the final outcome.

## 9. Security & compliance

No new security, privacy, identity, or compliance requirements are introduced by this refinement.

The change is limited to local presentation behavior within an existing temporary research surface.

## 10. Testing strategy

1. Verify visually that the summary/status strip is no longer present on the `Showcase` tab.
2. Verify that narrowing the workbench causes the grid to show and own a vertical scrollbar when the available grid viewport becomes insufficient.
3. Verify that widening the workbench afterward does not represent a prerequisite for scrollbar appearance.
4. Verify that the detail pane scrollbar behavior remains correct after the grid layout changes.
5. Verify that grid text is visibly smaller and lighter than before.
6. Verify that hierarchy node text and spacing are materially tighter than before.
7. Verify that the hierarchy node text scale is visually close to the watermark typography in the `Filter showcase hierarchy` textbox.
8. Verify that remaining titles are smaller and less visually dominant.
9. Verify that the `Grid filter` and `Filter showcase hierarchy` textboxes remain acceptable and are not unintentionally degraded.
10. Where automated UI tests exist for the showcase layout, update them to reflect the removed summary/status strip and the refined density expectations.

## 11. Rollout / migration

This change may be delivered as an in-place refinement of the existing consolidated PrimeReact showcase surface.

No migration of user data, configuration, or routes is required.

Any implementation notes should preserve the understanding that this is a refinement of the current styled showcase baseline rather than a new page or alternate mode.

## 12. Open questions

No additional clarification questions are required for this draft.

The requested outcomes are sufficiently specific to proceed with a planning or implementation phase focused on layout, typography, and density refinement for the `Showcase` tab.
