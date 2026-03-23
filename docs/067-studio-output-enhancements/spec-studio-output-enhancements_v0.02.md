# Work Package: `067-studio-output-enhancements` — Studio Output enhancements

**Target output path:** `docs/067-studio-output-enhancements/spec-studio-output-enhancements_v0.02.md`

**Version:** `v0.02` (Draft)

**Supersedes:** `docs/067-studio-output-enhancements/spec-studio-output-enhancements_v0.01.md`

> **Current direction note**
>
> This is the current specification for the read-only `xterm.js` direction.
>
> Use the matching implementation plan here:
> - `docs/067-studio-output-enhancements/plan-studio-output-enhancements_v0.03.md`
>
> Older plan history remains in:
> - `docs/067-studio-output-enhancements/plan-studio-output-enhancements_v0.02.md`

## Change Log

- `v0.02` — Revised the draft direction to reopen the output-surface architecture around a read-only `xterm.js` control, replacing the previous custom-widget-first recommendation while preserving the `Copy all` baseline requirement.
- `v0.02` — Promoted `Copy all` from deferred follow-up to baseline scope as a toolbar action for the merged output stream, and aligned the functional requirements, testing strategy, and rollout notes accordingly.
- `v0.01` — Initial draft capturing the recommended direction for evolving `Studio Output` as an enhanced custom output pane with Theia output-channel-style behavior, explicitly not as a terminal, and including pastel severity coloring.

## 1. Overview

### 1.1 Purpose

This work package defines the next-stage UX and technical direction for `Studio Output` in the Theia-based Studio shell.

The current draft direction is to pivot `Studio Output` toward a read-only `xterm.js`-based output surface rather than continuing to refine the existing custom compact-row widget. The intention is to use terminal-grade rendering density, selection behavior, and scrollback ergonomics while still keeping the surface clearly non-interactive and Studio-owned.

The target experience is:

- more shell-like in density and readability
- more output-pane-like in behavior and affordances
- clearly not an interactive terminal
- better aligned with Studio's structured application diagnostics and workflow feedback

This work package also captures a requirement for colorized lines using subdued pastel shades that remain comfortable on the expected dark Studio background, while also establishing `Copy all` as a baseline toolbar action for the merged output stream.

### 1.2 Scope

This specification covers:

- the recommended architectural direction for `Studio Output`
- introducing a read-only `xterm.js`-based rendering surface inside the Studio-owned output panel
- adopting output-pane behavior and affordances on top of a terminal-grade renderer
- explicitly avoiding real terminal input semantics for this work package
- compact, console-like line rendering for Studio output entries
- pastel severity coloring for at least `INFO` and `ERROR`
- natural text selection and copying from the rendered output surface
- a toolbar-based `Copy all` action for the full merged stream
- future-friendly support for richer output-pane behavior such as clear, reveal latest, and optional filtering

This specification does not cover:

- implementing a real Theia terminal session
- introducing command input or shell execution through `Studio Output`
- redesigning unrelated Studio trees or editor surfaces
- changing the backend data sources that emit Studio output
- introducing explicit source/channel switching in the first implementation

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

`Studio Output` shall remain a Studio-owned output surface, but it shall evolve toward a read-only `xterm.js`-based output-pane model.

That means the output should feel more like a serious IDE output pane by supporting:

- compact terminal-grade monospace rendering
- append-only log presentation
- one merged output stream in the first implementation, with future-friendly support for optional filtering before any explicit channel switching
- severity-aware color treatment
- clear/select/copy-friendly interaction
- a toolbar-based `Copy all` action that copies the merged stream text to clipboard
- built-in auto-scroll behavior so the latest line remains visible as new output arrives

The work package explicitly keeps `Studio Output` read-only and Studio-controlled even if `xterm.js` is adopted as the rendering primitive, because the goal is to gain terminal-grade density and selection behavior without exposing command entry or shell execution.

### 2.3 Assumptions

- Studio output will continue to represent application diagnostics and workflow feedback rather than true process stdout/stderr streams
- users want stronger console-like readability without the cognitive cost of terminal semantics
- the current custom widget approach has not met density expectations for the output pane
- structured output fields such as `level` and `source` remain valuable and should not be flattened into opaque terminal text too early
- users need a quick way to copy the full visible output stream during diagnostics and review
- the Studio shell is primarily used on dark-oriented themes in local development
- muted pastel coloring is preferable to bright terminal-style colors because it remains more comfortable for persistent viewing
- a read-only `xterm.js` surface can be integrated without turning `Studio Output` into an interactive terminal session

### 2.4 Constraints

- `Studio Output` must remain clearly non-interactive as a shell surface
- the design must not imply command entry or prompt behavior
- the implementation must disable or avoid terminal input semantics even if a terminal-rendering library is used
- severity coloring must remain readable and theme-compatible on dark backgrounds
- `Copy all` must operate only on output that is already available in the widget's merged stream
- the work should preserve existing output data semantics and command wiring where possible
- the work package should remain frontend-led with no required backend contract changes

## 3. Component / service design (high level)

### 3.1 Components

1. `Studio Output` widget
   - retained as the Studio-owned output panel host
   - responsible for hosting the read-only terminal surface and output-pane behavior

2. Read-only terminal surface
   - uses `xterm.js` as the rendering primitive
   - provides dense line packing, scrollback, and terminal-grade text selection without command entry

3. Output presentation formatter
   - formats timestamps, line layout, and severity emphasis
   - provides stable text serialization for the merged stream when copied in full

4. Output toolbar actions
   - supports `Clear output`
   - supports `Copy all`
   - may later support `Reveal latest` or similar output-pane actions

5. Output service
   - remains the append/clear source of truth for output entries
   - feeds formatted merged-stream lines into the read-only terminal surface

6. Optional channel/source model
   - future-friendly grouping by output source such as `providers`, `rules`, `ingestion`, or `provider-selection`

### 3.2 Data flows

#### Output append flow

1. a Studio feature emits an output event
2. the output service appends a structured entry
3. the output widget receives the change event
4. the formatter converts the entry into stable merged-stream output text with severity styling metadata
5. the read-only terminal surface writes the line into the visible scrollback

#### Output interaction flow

1. the user opens the `Studio Output` panel
2. the panel presents a sequential log stream in dense read-only terminal layout
3. the user may select and copy content directly from the output surface
4. the user may invoke `Copy all` from the toolbar to copy the merged stream text
5. the user may invoke `Clear output` from the toolbar
6. the widget updates immediately via the existing output-service event flow

#### Future channel flow

1. Studio writes entries tagged by source or logical channel
2. the output view may optionally expose filtering before any explicit channel switching
3. the user narrows visible output without changing the underlying stored entries

### 3.3 Key decisions

- **Keep the Studio-owned `Studio Output` panel but introduce read-only `xterm.js` rendering**
  - rationale: the surface remains Studio-specific while gaining the density, scrollback, and selection ergonomics that users expect from shell-like output

- **Adopt output-pane behavior on top of a terminal-grade renderer**
  - rationale: this preserves diagnostics-oriented semantics while using a rendering primitive that naturally produces compact output

- **Do not use the real Theia terminal for this work package**
  - rationale: terminal semantics should remain reserved for genuine terminal scenarios

- **Use `xterm.js` in read-only mode only**
  - rationale: the renderer should provide density and copy/select behavior without exposing prompts, stdin, or shell execution

- **Preserve structured log fields**
  - rationale: `timestamp`, `level`, `source`, and `message` remain useful for future filtering, formatting, and diagnostics

- **Use compact monospace output lines**
  - rationale: this gives the shell-like readability users want, and a terminal-grade renderer is now the preferred way to achieve it

- **Use muted pastel severity colors**
  - rationale: pastel colors preserve scanability and remain comfortable on dark backgrounds during prolonged use

- **Include `Copy all` in the baseline toolbar actions**
  - rationale: copying the full merged stream is a practical diagnostics need and does not require introducing terminal semantics or channel complexity

- **Prefer incremental enhancement over wholesale replacement**
  - rationale: Studio already has a working output path, so the safest and most maintainable route is to evolve it rather than replace it

## 4. Functional requirements

### FR-001 Retain `Studio Output` as a custom output widget

The Studio shell shall retain `Studio Output` as a Studio-owned output panel rather than replacing it with a real interactive terminal surface in this work package.

### FR-002 Non-terminal semantics

`Studio Output` shall remain a non-interactive output pane and shall not imply command input, prompt handling, or shell execution.

### FR-003 Output-channel-style behavior

`Studio Output` shall adopt output-pane behavior appropriate to IDE diagnostics panes, including append-only output presentation and toolbar-driven output actions, even when rendered through a read-only terminal control.

### FR-003a Read-only terminal renderer

`Studio Output` shall use `xterm.js` or an equivalent read-only terminal-grade renderer as the rendering primitive for the output stream.

### FR-004 Compact monospace rendering

`Studio Output` shall render entries using a compact terminal-grade monospace layout suitable for scan-friendly console-like reading.

### FR-005 Structured output line format

Each visible output entry shall continue to present at minimum:

- time
- severity
- source
- message

### FR-006 Copy-friendly output surface

The output presentation shall support natural text selection and copying directly from the rendered output surface.

### FR-006c Read-only interaction model

The rendered output surface shall remain read-only and shall not expose a prompt, cursor-driven command entry, or shell execution behavior.

### FR-006a Single merged stream in the first implementation

The first implementation of `Studio Output` shall present a single merged output stream rather than explicit source or channel switching.

### FR-006b Filtering before channel switching

If output narrowing is introduced, the preferred first step shall be optional filtering over the merged stream rather than separate channel panes or explicit channel switching.

### FR-007 Toolbar-driven clearing

`Clear output` shall remain available through a toolbar action rather than a body-level button.

### FR-007a `Copy all` toolbar action

`Copy all` shall be available through a toolbar action and shall copy the full merged output stream currently presented by `Studio Output`.

### FR-007b Stable copied text format

The text produced by `Copy all` shall preserve the merged-stream line ordering and the visible `time`, `severity`, `source`, and `message` structure in a stable text format.

### FR-008 Immediate clear behavior

Invoking `Clear output` shall remove visible entries immediately through the existing output-service update mechanism.

### FR-008a Immediate copy behavior

Invoking `Copy all` shall copy the current merged output text immediately without requiring per-line selection.

### FR-009 Pastel severity coloring

`Studio Output` shall use severity-aware line or token coloring using muted pastel shades designed for the expected dark Studio background and compatible with the chosen terminal renderer.

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

- `Reveal latest`
- optional source/channel filtering

### FR-014a Auto-scroll to latest output

The first implementation shall automatically keep the latest output entry visible as new output arrives.

### FR-014b Reveal-latest behavior by default

The default output-pane behavior shall act as a reveal-latest surface, so users can see the newest output line without manual scrolling during normal operation.

### FR-015 No terminal emulation dependency

This work package shall not require a real interactive terminal session, but it may embed `xterm.js` in a strictly read-only configuration.

## 5. Non-functional requirements

- The output surface shall feel materially closer to an IDE output pane than to a custom card list, while delivering terminal-grade density.
- The output surface shall remain maintainable despite introducing a terminal-rendering dependency.
- The implementation shall remain maintainable and easy to extend.
- Severity colors shall remain readable without dominating the layout.
- The design shall remain comfortable for prolonged use on dark backgrounds.
- Output rendering shall remain performant for the expected Studio log volume.
- Full-stream copy shall remain predictable and fast for expected Studio output volumes.
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
- a read-only `xterm.js` integration inside the output widget
- a `Copy all` output command and toolbar contribution wired through the current frontend module pattern
- the existing Theia tab-bar toolbar contribution mechanism
- the current lower-panel hosting used by `Studio Output`

The recommended implementation approach is:

1. keep the current output service and widget ownership model
2. introduce a read-only `xterm.js` surface inside the output widget
3. preserve current command wiring
4. add `Copy all` as a toolbar action for the merged stream
5. add future-friendly output-pane affordances incrementally

## 8. Observability (logging/metrics/tracing)

No new platform observability requirement is introduced.

The output surface remains a consumer of existing Studio diagnostics rather than a new logging subsystem.

If useful for implementation diagnostics, lightweight local checks may confirm:

- entry append behavior
- clear behavior
- copy-all behavior
- read-only terminal initialization and disposal behavior
- channel/source grouping readiness
- output rendering with different severity values

## 9. Security & compliance

This work package introduces no new security boundary.

The output panel shall continue to avoid implying shell execution or command input.

`Copy all` shall operate only on output already visible to the user through the merged stream and shall not introduce hidden data expansion or separate privileged retrieval behavior.

## 10. Testing strategy

Testing should focus on behavior and UX fit.

Recommended coverage:

1. output rendering
   - verify compact terminal-grade rendering density
   - verify visible line format remains stable
   - verify severity styling is applied correctly for `INFO` and `ERROR`
   - verify severity styling applies to the severity token rather than the full line in the first implementation
   - verify the output surface remains read-only despite terminal-grade rendering

2. toolbar behavior
   - verify `Clear output` remains toolbar-based
   - verify `Copy all` is exposed as a toolbar action
   - verify tooltip text is present and correct
   - verify output clears immediately when invoked
   - verify `Copy all` copies the expected merged-stream text ordering and line structure

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
2. introduce the read-only terminal surface
3. refine line formatting and severity treatment for the terminal renderer
4. preserve toolbar-based actions and add `Copy all` for the merged stream
5. add future-friendly output-pane affordances incrementally

## 12. Open questions

There are currently no unresolved open questions for this work package.

Resolved decisions:

1. The first implementation shall use a single merged output stream, with optional filtering preferred before any explicit channel switching.
2. Severity coloring shall initially apply to the severity token rather than the full line unless renderer constraints require a closely related prefix treatment.
3. `Copy all` shall be included in the baseline as a toolbar action for the merged stream.
4. Auto-scroll shall be included so the latest output line remains visible by default.
5. The output-surface baseline is now being reconsidered around a read-only `xterm.js` control rather than further refinement of the current custom compact-row widget.
6. The read-only terminal surface shall keep explicit visible `time`, `severity`, and `source` text on each line in the first implementation; density can be revisited later if needed.
