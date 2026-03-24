# Specification: `073-new-theia-shell` — Studio Home surface cleanup

**Target output path:** `docs/073-new-theia-shell/spec-frontend-home-surface-cleanup_v0.01.md`

**Related work package:** `docs/073-new-theia-shell/spec-new-theia-shell_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft covering removal of the default Theia `Welcome` surface from the active Studio shell and removal of the lower Theia-explanatory box from the custom Studio `Home` page.
- `v0.01` — Clarified that the default Theia `Welcome` surface is permanently superseded by the Studio `Home` page and is not expected to remain available anywhere in the active shell.
- `v0.01` — Clarified that the lower explanatory box on the Studio `Home` page should be removed entirely rather than replaced with alternate placeholder content.

## 1. Overview

### 1.1 Purpose

This specification defines a focused cleanup of the active Studio shell landing experience in `docs/073-new-theia-shell`.

The current fresh Theia scaffold still exposes default Theia-first onboarding content that is no longer wanted for this product direction. The Studio shell now has its own `Home` page and should present only Studio-owned landing behavior.

This specification therefore covers two related outcomes:

1. removing the default Theia `Welcome` surface from the active shell composition
2. removing the lower explanatory box from the Studio `Home` page where it currently explains default Theia behavior or coexistence

The intended result is a cleaner Studio-first startup experience in which the Studio `Home` page is the only supported landing surface.

### 1.2 Scope

This specification covers:

- removal of the Theia default `Welcome` page from the active Studio shell
- removal of any active package, composition, menu, command, or startup behavior that keeps the default Theia `Welcome` page reachable where practical
- cleanup of the Studio `Home` page content so the lower Theia-oriented explanatory box is removed entirely
- retention of the existing Studio `Home` page as the supported landing experience
- preservation of the existing Studio branding, logo, startup-open behavior, and `View -> Home` reopen behavior

This specification does not cover:

- redesign of the wider Studio workbench layout
- new `Home` page functionality beyond removal of the unwanted lower box
- replacement onboarding flows, tutorials, or future dashboard content
- unrelated Theia package cleanup outside what is needed to remove the default `Welcome` experience

### 1.3 Stakeholders

- Studio shell developers
- maintainers of the active Theia workspace under `src/Studio/Server`
- repository maintainers responsible for the local Studio developer experience
- users of the Studio shell who should now see only Studio-owned landing content

### 1.4 Definitions

- `Studio Home`: the custom Studio-owned landing document opened by the `search-studio` frontend extension
- `Theia Welcome`: the default Eclipse Theia scaffold-provided welcome/getting-started landing surface
- `active shell composition`: the set of packages, commands, menus, startup behaviors, and frontend contributions that make a surface available in the running Studio shell
- `lower explanatory box`: the lower section on the current Studio `Home` page that explains default Theia behavior or coexistence

## 2. System context

### 2.1 Current state

The active Studio shell under `src/Studio/Server` now contains a custom Studio `Home` document that opens automatically and can be reopened from the `View` menu.

However, the active shell still retains parts of the default Theia onboarding experience from the fresh scaffold. The work package currently allows the generated `Welcome` surface to coexist with the Studio `Home` page.

The Studio `Home` page also currently contains a lower box that explains the temporary coexistence of generated Theia welcome surfaces. That message was acceptable during the earlier bootstrap stage, but it is now no longer wanted.

### 2.2 Proposed state

The active Studio shell shall present a Studio-owned landing experience only.

The default Theia `Welcome` surface shall be removed completely from the active shell composition because it has been permanently superseded by the Studio `Home` page and is not expected to be needed again.

The Studio `Home` page shall remain the single supported landing document, retaining its current logo, branding, orientation content, startup-open behavior, and `View -> Home` reopen behavior.

The lower explanatory box on the Studio `Home` page shall be removed entirely. The page shall end after the remaining intended Studio orientation content, with no replacement placeholder box.

### 2.3 Assumptions

- the current Studio `Home` page is now the permanent replacement for the default Theia landing experience
- no user role or future near-term scenario requires the default Theia `Welcome` page to remain accessible
- removal of the lower explanatory box does not require replacement text, new links, or layout filler content
- the active shell can remove the Theia `Welcome` experience without changing the preserved Studio `Home` command/menu behavior

### 2.4 Constraints

- the work must stay within the existing `docs/073-new-theia-shell` work package
- the default Theia `Welcome` experience must be removed from the active shell composition, not merely hidden on initial startup
- the Studio `Home` page must remain the supported landing surface
- the cleanup should avoid widening scope into unrelated Theia workbench customization

## 3. Component / service design (high level)

### 3.1 Components

1. `Studio frontend composition`
   - the active `search-studio` frontend extension and browser application composition under `src/Studio/Server`
   - responsible for which landing surfaces are available in the running Studio shell

2. `Studio Home document`
   - the custom `Home` widget/document contribution
   - remains the single supported landing document after cleanup

3. `Theia scaffold onboarding surface`
   - the default Theia `Welcome` or getting-started surface currently inherited from the scaffold
   - removed from the active Studio shell composition as part of this specification

4. `Studio Home presentation`
   - the rendered content and layout of the Studio `Home` page
   - simplified by removing the lower Theia-oriented explanatory box entirely

### 3.2 Data flows

#### Landing experience flow after cleanup

1. the Studio shell starts
2. the Studio `Home` document opens automatically
3. no default Theia `Welcome` page opens
4. if the user closes `Home`, they can reopen it from `View -> Home`
5. no default Theia `Welcome` surface remains available from the active shell composition where practical

#### Home presentation flow after cleanup

1. the Studio `Home` page renders its primary Studio branding and orientation content
2. the lower explanatory box is not rendered
3. the page ends after the remaining intended Studio content

### 3.3 Key decisions

- **Remove the default Theia `Welcome` experience entirely from the active shell composition**
  - rationale: it is permanently superseded by the Studio `Home` page and should not remain available for future use in the active shell

- **Remove the lower explanatory box entirely rather than replacing it**
  - rationale: the user has stated that the page should no longer explain default Theia behavior, and no replacement placeholder is wanted

- **Keep the cleanup narrowly focused on landing-surface behavior**
  - rationale: this work should simplify the startup experience without broadening into general workbench redesign

## 4. Functional requirements

### FR-001 Remove the default Theia `Welcome` page from startup behavior

The active Studio shell shall no longer open the default Theia `Welcome` page during startup.

### FR-002 Remove the default Theia `Welcome` page from active shell composition

The active Studio shell shall remove the default Theia `Welcome` page entirely from the active shell composition where practical, including package, command, menu, and startup exposure required only for that surface.

### FR-003 Treat Studio `Home` as the permanent landing replacement

The Studio `Home` document shall remain the supported landing surface and shall be treated as the permanent replacement for the default Theia `Welcome` page.

### FR-004 Preserve existing `Home` startup behavior

The cleanup shall preserve the current automatic startup opening of the Studio `Home` page.

### FR-005 Preserve existing `View -> Home` reopen behavior

The cleanup shall preserve the existing `View` menu path and command behavior for reopening the Studio `Home` document.

### FR-006 Remove the lower explanatory box from the Studio `Home` page

The Studio `Home` page shall no longer render the lower box that explains default Theia behavior, generated surface coexistence, or similar scaffold-oriented messaging.

### FR-007 Do not replace the removed lower box with placeholder content

The removed lower explanatory box shall not be replaced with alternate placeholder text, an empty container, or future-content filler.

### FR-008 Preserve existing Studio-owned Home content

The cleanup shall preserve the remaining intended Studio-owned `Home` content, including the UKHO logo and primary orientation content, unless adjustment is required solely to remove the lower explanatory box cleanly.

## 5. Non-functional requirements

### NFR-001 Studio-first user experience

The resulting landing experience shall read as Studio-owned rather than scaffold-owned.

### NFR-002 Minimal-scope change

The cleanup should be implemented with minimal changes limited to removing the unwanted landing surfaces and messaging.

### NFR-003 Maintainability

The implementation should avoid leaving dormant scaffold-specific `Welcome` configuration behind where it would mislead future maintainers about the intended landing experience.

### NFR-004 Documentation alignment

Repository documentation for the active Studio shell should no longer describe the default Theia `Welcome` surface as an accepted concurrent landing experience.

## 6. Data model

No new persistent data model is introduced by this specification.

The relevant UI state remains limited to frontend workbench composition and rendering behavior for the Studio `Home` page.

## 7. Interfaces & integration

### 7.1 Frontend integration

The cleanup applies to the active Theia frontend composition under `src/Studio/Server`, especially:

- browser application composition that currently includes default Theia scaffold onboarding capability
- the `search-studio` frontend extension that owns the Studio `Home` document

### 7.2 Menu and command integration

The Studio-owned `View -> Home` entry remains in scope and must continue working.

The default Theia `Welcome` exposure should be removed from active menus or commands where practical.

### 7.3 Hosting and runtime integration

No change is required to:

- Aspire hosting
- runtime API base URL bridging
- `StudioServiceHost`
- the existing `Home` page runtime asset usage

## 8. Observability (logging/metrics/tracing)

No new observability capability is required.

Existing logging should remain sufficient for diagnosing Studio `Home` open failures if any frontend composition changes affect startup behavior.

## 9. Security & compliance

This specification introduces no new security boundary, secret handling, or compliance requirement.

Removal of the Theia `Welcome` experience and the lower explanatory box is a presentation and composition change only.

## 10. Testing strategy

The implementation should add or update focused frontend verification covering:

- automatic startup opening of the Studio `Home` page after the cleanup
- absence of the default Theia `Welcome` surface from the active shell behavior where practical to verify
- absence of the removed lower explanatory box from the rendered Studio `Home` content
- continued availability of `View -> Home`

Manual smoke verification should confirm:

1. the shell opens to the Studio `Home` page
2. no default Theia `Welcome` page appears
3. the lower explanatory box is absent
4. `View -> Home` still reopens the Studio `Home` page after closure

## 11. Rollout / migration

This change is an in-place refinement of the active shell created by work package `073-new-theia-shell`.

No data migration is required.

The rollout sequence is expected to be:

1. remove the default Theia `Welcome` surface from the active shell composition
2. remove the lower explanatory box from the Studio `Home` page
3. validate startup and `View -> Home` behavior
4. update any work-package or wiki documentation that still describes generated welcome-surface coexistence as acceptable

## 12. Open questions

At the time of this draft, no further open questions remain for the requested cleanup scope.
