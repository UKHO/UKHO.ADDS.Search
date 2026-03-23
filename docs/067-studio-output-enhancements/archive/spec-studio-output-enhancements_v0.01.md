# Work Package: `067-studio-output-enhancements` — Studio Output enhancements

**Target output path:** `docs/067-studio-output-enhancements/spec-studio-output-enhancements_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft capturing the recommended direction for evolving `Studio Output` as an enhanced custom output pane with Theia output-channel-style behavior, explicitly not as a terminal, and including pastel severity coloring.

## 1. Overview

### 1.1 Purpose

This work package defines the next-stage UX and technical direction for `Studio Output` in the Theia-based Studio shell.

The recommendation is to retain `Studio Output` as a Studio-owned custom output widget rather than remodel it as a real terminal or embed `xterm.js` at this stage. Instead, the output surface should evolve toward an IDE-style output-pane model, borrowing the best parts of Theia/VS Code-style output behavior while preserving Studio-specific structure and semantics.

The target experience is:

- more shell-like in density and readability
- more output-pane-like in behavior and affordances
- clearly not an interactive terminal
- better aligned with Studio's structured application diagnostics and workflow feedback

This work package also captures a requirement for colorized lines using subdued pastel shades that remain comfortable on the expected dark Studio background.

### 1.2 Scope

This specification covers:

- the recommended architectural direction for `Studio Output`
- retaining the current custom widget as the implementation base
- adopting output-channel-style behavior and affordances
- explicitly avoiding real terminal semantics for this work package
- compact, console-like line rendering for Studio output entries
- pastel severity coloring for at least `INFO` and `ERROR`
- future-friendly support for richer output-pane behavior such as copy, clear, reveal latest, and optional filtering

This specification does not cover:

- implementing a real Theia terminal session
- embedding `xterm.js` in this work package
- introducing command input or shell execution through `Studio Output`
- redesigning unrelated Studio trees or editor surfaces
- changing the backend data sources that emit Studio output

### 1.3 Stakeholders

- Studio/tooling developers
- engineering leads shaping the long-term Studio shell UX
- developers using Studio output during local diagnostics and workflow review
- future contributors extending Studio logging, diagnostics, and workflow feedback

### 1.4 Definitions

- `Studio Output`: the Studio-owned lower-panel output surface for diagnostics and workflow feedback
- `output-channel-style`: a non-terminal IDE output-pane model that appends text or log entries, supports clear/select/copy/reveal behaviors, and presents sequential diagnostic output without pretending to be an interactive shell
- `terminal semantics`: behavior implying command input, prompts, shell execution, or stdin/stdout interaction
- `pastel severity color`: a muted, theme-compatible foreground color used to distinguish log level or line emphasis without harsh contrast or neon saturation
- `channel`: a logical grouping of output entries by source or category, such as `providers`, `rules`, or `ingestion`

## 2. System context

### 2.1 Current state

`Studio Output` already exists as a custom Studio widget in the lower panel.

Recent work has already moved the output away from a card-based presentation toward a denser log-style layout and has moved `Clear output` into a toolbar action. However, the current implementation is still a lightweight custom log surface rather than a more fully considered output-pane experience.

The current Studio output data is structured application output, not real process output. Entries have fields such as:

- timestamp
- level
- source
- message

This makes the output semantically closer to an IDE output pane than to a terminal emulator.

### 2.2 Proposed state

`Studio Output` shall remain a custom Studio-owned output widget, but it shall evolve toward a Theia output-channel-style model.

That means the output should feel more like a serious IDE output pane by supporting:

- compact monospace rendering
- append-only log presentation
- one merged output stream in the first implementation, with future-friendly support for optional filtering before any explicit channel switching
- severity-aware color treatment
- clear/select/copy-friendly interaction
- optional future channel/source organization
- built-in auto-scroll behavior so the latest line remains visible as new output arrives

The work package explicitly recommends against using a real Theia terminal or `xterm.js` for this requirement set because the goal is only to make the surface feel more shell-like, not to represent or host an actual shell.

### 2.3 Assumptions

- Studio output will continue to represent application diagnostics and workflow feedback rather than true process stdout/stderr streams
- users want stronger console-like readability without the cognitive cost of terminal semantics
- structured output fields such as `level` and `source` remain valuable and should not be flattened into opaque terminal text too early
- the Studio shell is primarily used on dark-oriented themes in local development
- muted pastel coloring is preferable to bright terminal-style colors because it remains more comfortable for persistent viewing
- Theia-style output-pane behavior can be adopted incrementally without rewriting the Studio shell architecture

### 2.4 Constraints

- `Studio Output` must remain clearly non-interactive as a shell surface
- the design must not imply command entry or prompt behavior
- the implementation should remain lighter-weight than embedding a full terminal emulator
- severity coloring must remain readable and theme-compatible on dark backgrounds
- the work should preserve existing output data semantics and command wiring where possible
- the work package should remain frontend-led with no required backend contract changes

## 3. Component / service design (high level)

### 3.1 Components

1. `Studio Output` widget
   - retained as the Studio-owned output panel
   - responsible for compact log rendering and output-pane behavior

2. Output presentation formatter
   - formats timestamps, line layout, and severity emphasis
   - keeps output readable and consistent

3. Output toolbar actions
   - supports `Clear output`
   - may later support `Copy all`, `Reveal latest`, or similar output-pane actions

4. Output service
   - remains the append/clear source of truth for output entries
   - may later grow output-channel-style append helpers or logical source grouping

5. Optional channel/source model
   - future-friendly grouping by output source such as `providers`, `rules`, `ingestion`, or `provider-selection`

### 3.2 Data flows

#### Output append flow

1. a Studio feature emits an output event
2. the output service appends a structured entry
3. the output widget receives the change event
4. the widget renders the entry in compact output-pane form
5. severity and metadata styling are applied consistently

#### Output interaction flow

1. the user opens the `Studio Output` panel
2. the panel presents a sequential log stream in dense monospace layout
3. the user may select and copy content directly from the output surface
4. the user may invoke `Clear output` from the toolbar
5. the widget updates immediately via the existing output-service event flow

#### Future channel flow

1. Studio writes entries tagged by source or logical channel
2. the output view may optionally expose channel selection or filtering
3. the user narrows visible output without changing the underlying stored entries

### 3.3 Key decisions

- **Keep the custom `Studio Output` widget**
  - rationale: the surface is Studio-specific, already integrated, and semantically closer to structured application output than to a general-purpose terminal

- **Adopt Theia output-channel-style behavior rather than terminal behavior**
  - rationale: this gives the right affordances for diagnostics and workflow feedback without implying shell interaction

- **Do not use the real Theia terminal for this work package**
  - rationale: terminal semantics should remain reserved for genuine terminal scenarios

- **Do not adopt `xterm.js` at this stage**
  - rationale: `xterm.js` would provide shell-like feel quickly, but it is heavier than needed and risks blurring the distinction between output pane and terminal

- **Preserve structured log fields**
  - rationale: `timestamp`, `level`, `source`, and `message` remain useful for future filtering, formatting, and diagnostics

- **Use compact monospace output lines**
  - rationale: this gives the shell-like readability users want without terminal emulation

- **Use muted pastel severity colors**
  - rationale: pastel colors preserve scanability and remain comfortable on dark backgrounds during prolonged use

- **Prefer incremental enhancement over wholesale replacement**
  - rationale: Studio already has a working output path, so the safest and most maintainable route is to evolve it rather than replace it

## 4. Functional requirements

### FR-001 Retain `Studio Output` as a custom output widget

The Studio shell shall retain `Studio Output` as a Studio-owned custom output panel rather than replacing it with a real terminal surface in this work package.

### FR-002 Non-terminal semantics

`Studio Output` shall remain a non-interactive output pane and shall not imply command input, prompt handling, or shell execution.

### FR-003 Output-channel-style behavior

`Studio Output` shall adopt output-channel-style behavior appropriate to IDE diagnostics panes, including append-only output presentation and toolbar-driven output actions.

### FR-004 Compact monospace rendering

`Studio Output` shall render entries using a compact monospace-oriented layout suitable for scan-friendly console-like reading.

### FR-005 Structured output line format

Each visible output entry shall continue to present at minimum:

- time
- severity
- source
- message

### FR-006 Copy-friendly output surface

The output presentation shall support natural text selection and copying directly from the rendered output surface.

### FR-006a Single merged stream in the first implementation

The first implementation of `Studio Output` shall present a single merged output stream rather than explicit source or channel switching.

### FR-006b Filtering before channel switching

If output narrowing is introduced, the preferred first step shall be optional filtering over the merged stream rather than separate channel panes or explicit channel switching.

### FR-007 Toolbar-driven clearing

`Clear output` shall remain available through a toolbar action rather than a body-level button.

### FR-008 Immediate clear behavior

Invoking `Clear output` shall remove visible entries immediately through the existing output-service update mechanism.

### FR-009 Pastel severity coloring

`Studio Output` shall use severity-aware line or token coloring using muted pastel shades designed for the expected dark Studio background.

### FR-010 Supported severity colors

The first implementation shall color at least:

- `INFO`
- `ERROR`

and should remain extensible for future severities such as `WARN` or `DEBUG`.

### FR-011 Recommended pastel palette

The recommended default severity colors are:

- `INFO`: pastel blue `#A9C7FF`
- `ERROR`: pastel red `#FFB3BA`

Future optional severities may use:

- `WARN`: pastel amber `#F9D89C`
- `DEBUG`: pastel lavender `#CBB7FF`
- `SUCCESS` or equivalent: pastel green `#B8E6B8`

### FR-011a Token-led severity emphasis

Severity coloring shall apply initially to the severity token rather than tinting the full output line, unless later usability review demonstrates a clear benefit from broader line emphasis.

### FR-012 Theme-respecting implementation

Severity colors shall be implemented in a theme-respecting way, allowing future substitution through local CSS variables or theme-aware configuration if needed.

### FR-013 Future channel readiness

The output model should remain ready for future logical channel or source grouping without requiring a terminal migration.

### FR-014 Future output-pane actions

The design shall leave room for future output-pane affordances such as:

- `Copy all`
- `Reveal latest`
- optional source/channel filtering

### FR-014a Auto-scroll to latest output

The first implementation shall automatically keep the latest output entry visible as new output arrives.

### FR-014b Reveal-latest behavior by default

The default output-pane behavior shall act as a reveal-latest surface, so users can see the newest output line without manual scrolling during normal operation.

### FR-015 No terminal emulation dependency

This work package shall not require embedding `xterm.js` or any equivalent terminal emulator library.

## 5. Non-functional requirements

- The output surface shall feel materially closer to an IDE output pane than to a custom card list.
- The output surface shall remain lightweight compared with a full terminal-emulation approach.
- The implementation shall remain maintainable and easy to extend.
- Severity colors shall remain readable without dominating the layout.
- The design shall remain comfortable for prolonged use on dark backgrounds.
- Output rendering shall remain performant for the expected Studio log volume.
- The surface shall remain accessible for keyboard focus, selection, and reading.

## 6. Data model

No backend data-model change is required.

The current output entry structure remains appropriate:

- `id`
- `timestamp`
- `level`
- `source`
- `message`

Potential future extension points may include:

- `channel`
- `category`
- `isTransient`
- `canCopy`
- `tags`

These are optional future evolutions and not required for the first implementation.

## 7. Interfaces & integration

This work package shall integrate with:

- the existing `SearchStudioOutputWidget`
- the existing `SearchStudioOutputService`
- the existing `SearchStudioClearOutputCommand`
- the existing Theia tab-bar toolbar contribution mechanism
- the current lower-panel hosting used by `Studio Output`

The recommended implementation approach is:

1. keep the current widget and service
2. improve widget presentation and output-pane behavior
3. preserve current command wiring
4. add output-channel-style affordances incrementally

## 8. Observability (logging/metrics/tracing)

No new platform observability requirement is introduced.

The output surface remains a consumer of existing Studio diagnostics rather than a new logging subsystem.

If useful for implementation diagnostics, lightweight local checks may confirm:

- entry append behavior
- clear behavior
- channel/source grouping readiness
- output rendering with different severity values

## 9. Security & compliance

This work package introduces no new security boundary.

The output panel shall continue to avoid implying shell execution or command input.

If copy-related actions are added later, they shall operate only on output already visible to the user.

## 10. Testing strategy

Testing should focus on behavior and UX fit.

Recommended coverage:

1. output rendering
   - verify compact monospace-oriented rendering
   - verify visible line format remains stable
   - verify severity styling is applied correctly for `INFO` and `ERROR`
   - verify severity styling applies to the severity token rather than the full line in the first implementation

2. toolbar behavior
   - verify `Clear output` remains toolbar-based
   - verify tooltip text is present and correct
   - verify output clears immediately when invoked

3. output-pane semantics
   - verify the panel remains non-terminal in behavior
   - verify text remains selectable and copy-friendly
   - verify the design does not require terminal-specific interaction
   - verify new output keeps the latest visible line in view by default

4. future-readiness
   - verify source metadata remains available for possible future grouping or filtering
   - verify the first implementation remains a merged stream rather than channel-switched panes

## 11. Rollout / migration

No data migration is required.

This work should be delivered as an in-place enhancement of the current `Studio Output` implementation.

Recommended implementation order:

1. keep the current output service and widget ownership model
2. refine output-pane rendering and severity treatment
3. preserve toolbar-based actions
4. add future-friendly output-channel-style affordances incrementally

## 12. Open questions

There are currently no unresolved open questions for this work package.

Resolved decisions:

1. The first implementation shall use a single merged output stream, with optional filtering preferred before any explicit channel switching.
2. Severity coloring shall initially apply to the severity token rather than the full line.
3. `Copy all` may remain deferred until the output-pane baseline is accepted.
4. Auto-scroll shall be included so the latest output line remains visible by default.
