# Work Package: `073-new-theia-shell` — Rebuild Studio shell on a fresh Theia scaffold

**Target output path:** `docs/073-new-theia-shell/spec-new-theia-shell_v0.01.md`

**Version:** `v0.01` (Draft)

## Change Log

- `v0.01` — Initial draft covering replacement of the existing `src/Studio/Server` Theia workspace with a newly scaffolded Theia project, preservation of the current build/Aspire/runtime bridge patterns, and migration constraints for keeping the old implementation as a read-only reference.
- `v0.01` — Clarified that the new Theia shell remains browser-hosted only and does not introduce Electron/packageable application structure in this work item.
- `v0.01` — Clarified that this work item does not port the old Studio UI structure or feature set, except for restoring a simple startup home page; the remaining question is only about repository/package integration shape.
- `v0.01` — Clarified that the new scaffold should keep a similar external repository/package/build shape where practical so existing build and Aspire integration can be adapted with minimal change.
- `v0.01` — Clarified that the `STUDIO_API_HOST_API_BASE_URL` mechanism should keep the same overall backend-to-browser bridge pattern where practical because the existing approach worked well.
- `v0.01` — Clarified that `src/Studio/OldServer` must be explicitly non-active and clearly marked as read-only reference material so it is not accidentally edited or used instead of the new shell.
- `v0.01` — Clarified that the initial home page should contain only the UKHO logo and orientation text, with no links yet, and should copy the acceptable layout pattern from the old project.
- `v0.01` — Clarified that the generated Theia scaffold should remain close to generator output and only receive the minimum custom changes needed for repository build, Aspire, configuration, and home-page integration.
- `v0.01` — Clarified that the generator/tooling baseline does not need to be pinned to an exact generator version in this work item, but the implementation must still follow the known Node/Yarn/nvm lessons documented in the repository wiki.
- `v0.01` — Clarified that `src/Studio/OldServer` does not need an explicit marker file because it is expected to be deleted soon; folder move, non-active status, and specification constraints are sufficient.
- `v0.01` — Clarified that the home page should copy the acceptable old layout pattern, but the orientation wording can be refreshed and chosen sensibly because wording is not important in this work package.
- `v0.01` — Clarified that the home page should be re-openable later through a normal Theia command or menu path, not only opened by default at startup.
- `v0.01` — Clarified that the rebuilt local Studio shell must use HTTPS hosting, and that this protocol change is the intentional exception to otherwise unchanged local hosting and integration behavior.
- `v0.01` — Clarified that the initial home page only needs a close-enough placeholder reuse of the old layout pattern and does not need close visual matching because visual style will change in a later work package.
- `v0.01` — Clarified that the initial shell should expose only the absolute minimum Theia chrome needed to start and show the home page, with no placeholder activity/workbench structure yet.
- `v0.01` — Clarified that this work item should keep an explicit `build.ps1` entrypoint for the Theia shell rather than only requiring an equivalent alternative mechanism.
- `v0.01` — Clarified that incremental build behavior must preserve the current stamp-file-style approach in substance, and that the current Visual Studio `F5` build/integration model should remain exactly the same.
- `v0.01` — Clarified that the Aspire resource pattern should also remain exactly the same in substance, with the Studio shell still hosted as the same kind of JavaScript application resource from `AppHost`.
- `v0.01` — Clarified that the fixed local port configuration pattern should remain exactly the same, still read from `AppHost` configuration in the same way as today.
- `v0.01` — Clarified that existing internal workspace/package names such as `browser-app` and `search-studio` should be preserved where practical, alongside the unchanged external integration shape.
- `v0.01` — Clarified that `src/Studio/OldServer` must be excluded completely from active JavaScript workspace declarations and tooling visibility, remaining only as reference files in a directory until deletion.
- `v0.01` — Clarified that existing root script names such as `build:browser` and `start:browser` should be preserved where practical so `AppHost` wiring and developer habits stay unchanged.
- `v0.01` — Clarified that the Theia build process must remain exactly the same as the old project, including the explicit tracked-input pattern in `StudioServiceHost.csproj`, and must not be simplified or reworked.
- `v0.01` — Clarified that the runtime API base-address environment variable name must remain exactly `STUDIO_API_HOST_API_BASE_URL` with no renaming.
- `v0.01` — Clarified that the `Home` page should open as a normal closable document tab and should be available to show again from the Theia `View` menu like the old shell.
- `v0.01` — Clarified that the `Home` page should open automatically, but other default generated surfaces may also open for now and can be refined in a later work package.
- `v0.01` — Clarified that the application branding/title should keep the Studio branding and should reuse the branding configuration from the old project rather than generated defaults.
- `v0.01` — Clarified that broader old-project browser/icon branding assets are not required in this work item beyond the Studio branding/title and the required copied UKHO logo PNG used by the home page.
- `v0.01` — Clarified that the generated Theia default welcome/getting-started surface may remain for now alongside the new `Home` page and can be refined in a later work package.
- `v0.01` — Clarified that the runtime bridge should explicitly keep the same same-origin backend configuration-endpoint pattern used by the old shell, not just the same high-level bridge intent.
- `v0.01` — Clarified that the old `View` menu command naming for showing `Home` should be preserved where practical, not just the presence of a `Home` entry.
- `v0.01` — Clarified that for remaining low-level shell-behavior choices not otherwise called out in this work package, the default should be to do what the old project did where practical rather than inventing new behavior.
- `v0.01` — Clarified that the `Home` page shall include a simple manual smoke-test button labelled `test` which calls the `StudioServiceHost` `/echo` endpoint and displays the returned text next to the button, with no formatting polish required.

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for replacing the current Studio Theia workspace with a brand new Theia-based client application scaffolded from the current Yeoman/Theia extension generation approach.

The intent is not to incrementally repair the existing `src/Studio/Server` implementation. Instead, the repository shall:

1. move the current Studio shell code from `src/Studio/Server` to `src/Studio/OldServer`
2. keep that moved code as a read-only reference until later deletion is considered safe
3. create a new Theia project at `src/Studio/Server`
4. preserve the proven local development and hosting mechanisms that already work in this repository, especially:
   - Visual Studio build integration through `src/Studio/StudioServiceHost/StudioServiceHost.csproj`
   - Aspire startup orchestration from `src/Hosts/AppHost/AppHost.cs`
   - runtime API base URL handoff via `STUDIO_API_HOST_API_BASE_URL`
5. reintroduce a simple always-available Studio home page using the existing UKHO logo asset

This specification intentionally treats the old implementation as a migration reference only. The new application must stand on its own and must not depend on code located under `src/Studio/OldServer`.

This work item does not require the new shell to preserve the old Studio UI structure, activity-bar layout, pages, or feature surfaces beyond the requested simple home page. The old UI is reference material for selected mechanisms only, not a template for the new application structure.

The `Home` page in this work item also serves as a very simple manual integration smoke-test surface. It shall include a lightweight `test` button that calls the `StudioServiceHost` `/echo` endpoint and shows the returned text.

### 1.2 Scope

This specification covers:

- moving the existing Theia workspace from `src/Studio/Server` to `src/Studio/OldServer`
- defining `src/Studio/OldServer` as a temporary read-only reference only
- generating a new Theia project at `src/Studio/Server` using the current Yeoman-generated Theia extension approach
- preserving the repository's working build pattern for Theia within Visual Studio and normal local command-line usage
- preserving or re-establishing the incremental Theia shell build that is triggered from `src/Studio/StudioServiceHost/StudioServiceHost.csproj`
- preserving or re-establishing the Aspire-hosted local startup path from `src/Hosts/AppHost/AppHost.cs`
- preserving the existing `STUDIO_API_HOST_API_BASE_URL` configuration bridge pattern used by the current Studio shell
- restoring a simple startup home page that displays the copied `docs/ukho-logo-transparent.png` asset on launch in the same general way as the old shell
- restoring a simple startup home page that displays the copied `docs/ukho-logo-transparent.png` asset on launch in the same general way as the old shell
- adding a minimal `Home`-page smoke-test button that calls the `StudioServiceHost` `/echo` endpoint and displays the returned text
- explicitly forbidding runtime or compile-time dependency from the new shell to code in `src/Studio/OldServer`
- documenting the migration and validation expectations needed to avoid repeating the previous Theia build/toolchain issues

This specification does not cover:

- feature-for-feature migration of all existing Studio views, commands, panels, trees, or editor surfaces
- preservation of the old Studio activity items, page structure, or workbench layout beyond the requested home page behavior
- new Studio business functionality beyond what is necessary to establish the new shell baseline
- deletion of `src/Studio/OldServer`
- redesign of Studio backend APIs
- changes to domain, service, or provider model behavior unrelated to hosting the new shell
- broad re-architecture of Aspire outside the shell start/build integration points

### 1.3 Stakeholders

- Studio shell developers
- maintainers of `src/Studio/StudioServiceHost`
- maintainers of `src/Hosts/AppHost`
- repository maintainers responsible for local developer experience
- users of the local Studio development shell

### 1.4 Definitions

- `OldServer`: the relocated legacy Theia workspace at `src/Studio/OldServer`, retained only as a read-only reference during migration
- `new Server`: the new Theia workspace created at `src/Studio/Server`
- `Theia scaffold`: the freshly generated Theia project structure based on the current Yeoman-generated Theia extension approach
- `runtime bridge`: the mechanism by which Aspire resolves the Studio API endpoint and passes it into the Theia process through `STUDIO_API_HOST_API_BASE_URL`
- `home page`: the first Studio page shown on shell startup, displaying branding and basic orientation content

## 2. System context

### 2.1 Current state

The repository already contains a browser-hosted Studio shell under `src/Studio/Server` and a working local integration pattern around it.

Current proven repository behavior includes:

- a Theia workspace rooted at `src/Studio/Server`
- local hosting from Aspire in `src/Hosts/AppHost/AppHost.cs`
- a fixed local shell port read from `Studio:Server:Port`
- a Visual Studio build hook in `src/Studio/StudioServiceHost/StudioServiceHost.csproj`
- a PowerShell-based Theia build script at `src/Studio/Server/build.ps1`
- runtime environment propagation of `STUDIO_API_HOST_API_BASE_URL`
- a home page pattern that opens on Studio startup
- a copied UKHO logo asset already used by the current shell

Internal repository knowledge also records important practical constraints for the Theia build chain, including:

- Node `18.20.4`
- Yarn classic `1.x`
- restore via `yarn install --ignore-engines`
- defensive clearing of Visual Studio C++ toolchain environment variables before Node/native module work
- explicit `GYP_MSVS_VERSION` and `npm_config_msvs_version` handling
- incremental build stamping to prevent unnecessary shell rebuilds

The user has stated that the old Studio shell accumulated numerous build and toolchain issues. The new work item therefore aims to start again from a fresh Theia scaffold while preserving only the proven repository integration mechanisms.

### 2.2 Proposed state

The repository shall contain two distinct Studio shell code locations during the migration period:

1. `src/Studio/OldServer`
   - contains the current shell moved out of the way
   - kept as a read-only implementation reference only
   - must not be referenced by the new shell at runtime or build time
   - must be clearly marked and handled so it is not accidentally edited or treated as the active shell

2. `src/Studio/Server`
   - contains a newly generated Theia application workspace
   - becomes the only active Studio shell workspace used by Visual Studio build integration and Aspire startup

The new Studio shell shall be a fresh Theia application composition based on the Yeoman-generated Theia extension model, aligned with current Theia guidance for generated extensions and product composition.

The active new shell remains browser-hosted only for this work item and shall continue to fit the current Aspire startup model rather than introducing an Electron/packageable desktop application path.

The new shell shall preserve the repository's proven local integration patterns by reusing the existing mechanism categories where practical rather than inventing a new build and startup model.

Where practical, the new scaffold should also preserve a similar external repository-facing package and script shape so that existing `StudioServiceHost`, `AppHost`, and build-script integration can be retained or adapted with minimal change.

At minimum, the new shell baseline shall:

- build from `src/Studio/Server` through the existing .NET-triggered Theia build path
- start from Aspire through the existing JavaScript application resource pattern
- receive the Studio API base address through `STUDIO_API_HOST_API_BASE_URL`
- expose a simple home page on startup containing the UKHO logo and orientation text only
- display a runtime-served copy of `docs/ukho-logo-transparent.png`
- expose only the minimum Theia shell chrome needed to support startup and the home page

### 2.3 Assumptions

- the new Studio shell remains a browser-hosted Theia application for this work item
- the fresh scaffold should use native Theia extension/application composition rather than a VS Code extension as the primary customization mechanism
- the old shell remains available for inspection only and should not participate in normal build or runtime flows after the move
- proven build-script behavior from the old workspace may be copied into new files, but not referenced in place from `OldServer`
- preserving the existing environment-variable bridge is preferable to inventing a different API discovery mechanism
- preserving the existing backend-to-browser bridge pattern for `STUDIO_API_HOST_API_BASE_URL` is preferred where practical because it already worked well
- the first deliverable of the new shell can be intentionally minimal as long as startup, build, hosting, and home-page behavior are reliable
- the initial home page should not include navigational links or actions because there is nothing meaningful to link to yet
- the current repository wiki and Theia knowledgebase are the primary internal source for the previous working build and hosting pattern
- keeping similar repo-facing package naming, script naming, and integration shape is preferred where it reduces unnecessary retargeting
- preserving existing internal workspace/package names such as `browser-app` and `search-studio` is preferred where practical
- preserving existing root script names such as `build:browser` and `start:browser` is preferred where practical
- the generated scaffold should stay close to generator output and only be customized where needed for repository integration and the requested home page
- exact generator version pinning is not required for this work item, but the implementation must explicitly follow the known Node, Yarn, nvm, and toolchain constraints already documented in the repository wiki
- for remaining low-level behavior choices not otherwise specified in this work package, the implementation should default to what the old project did where practical

### 2.4 Constraints

- the new project must be created at `src/Studio/Server`
- the current `src/Studio/Server` content must first be moved to `src/Studio/OldServer`
- `src/Studio/OldServer` must remain read-only reference material and not an implementation dependency
- `src/Studio/OldServer` must be explicitly marked and kept non-active so developers do not accidentally edit it instead of the new shell
- the new project must not reference any code from `src/Studio/OldServer`
- anything required from the old implementation must be copied into new files under the new workspace
- existing Studio build orchestration from `src/Studio/StudioServiceHost/StudioServiceHost.csproj` must continue to work for the new shell
- existing Aspire startup orchestration from `src/Hosts/AppHost/AppHost.cs` must continue to work for the new shell
- the runtime `STUDIO_API_HOST_API_BASE_URL` mechanism must remain in use
- the new home page must use a copied application-served version of `docs/ukho-logo-transparent.png`
- the implementation should avoid reintroducing the earlier Yarn, Node, nvm, and Visual Studio toolchain contamination issues already documented in the repository

## 3. Component / service design (high level)

### 3.1 Components

1. `Legacy shell reference` (`src/Studio/OldServer`)
   - relocated old Theia workspace
   - reference-only source during migration
   - excluded from new-shell dependency paths

2. `New Theia workspace` (`src/Studio/Server`)
   - freshly generated Theia application workspace
   - becomes the active Studio shell source
   - contains the new application package structure and Studio-specific extension code

3. `Studio shell build integration`
   - `src/Studio/StudioServiceHost/StudioServiceHost.csproj`
   - build target and incremental inputs/outputs for preparing the new shell during normal .NET builds

4. `Studio shell build script`
   - a new `src/Studio/Server/build.ps1`
   - preserves the proven repository behavior around Node version validation, Yarn restore, incremental browser build, and Visual Studio environment cleanup
   - remains the explicit shell build entrypoint used by repository integration

5. `Aspire shell host integration`
   - `src/Hosts/AppHost/AppHost.cs`
   - starts the new shell as the active JavaScript application resource
   - passes the Studio API base URL and shell port settings into the new process
   - preserves the current Aspire resource pattern in substance rather than replacing it with a different hosting model

6. `Runtime configuration bridge`
   - preserves the `STUDIO_API_HOST_API_BASE_URL` handoff from Aspire into the shell runtime
   - remains the supported way for the shell to discover the Studio API base address
   - preserves the same-origin backend configuration-endpoint pattern used by the old shell to surface configuration to browser code

7. `Startup home page`
   - a simple initial Studio landing page
   - displays the copied UKHO logo asset and orientation text only
   - follows the acceptable layout pattern from the old project
   - opens on shell startup

8. `Home page smoke test action`
   - a simple button labelled `test`
   - calls the `StudioServiceHost` `/echo` endpoint when clicked
   - displays the returned text next to the button
   - exists only as a manual smoke test and does not require polished styling

9. `Minimal shell chrome`
   - only the minimum Theia workbench/shell structure needed to start the application and show the home page
   - no placeholder activity-bar, workbench-area, or future feature structure required in this work item

10. `Branding configuration`
   - keeps the Studio application/product branding rather than generated defaults
   - reuses the relevant branding configuration from the old project by copying it into the new active workspace as needed
   - does not require broader old-project favicon or browser-icon migration in this work item beyond the required UKHO logo used on the home page

### 3.2 Data flows

#### Legacy-to-new workspace transition flow

1. the current `src/Studio/Server` workspace is moved to `src/Studio/OldServer`
2. the new `src/Studio/Server` is created from a fresh Theia scaffold
3. build and startup integration points are updated to target only the new workspace
4. the old workspace remains available for reference but is no longer active in build/start flows

#### Visual Studio build flow

1. a normal .NET build reaches `src/Studio/StudioServiceHost/StudioServiceHost.csproj`
2. the project evaluates the tracked Theia workspace inputs under the new `src/Studio/Server`
3. if restore or browser build is required, the project runs the new `src/Studio/Server/build.ps1`
4. the build script validates Node/Yarn prerequisites, restores dependencies when required, and builds the browser shell
5. the script writes the incremental stamp file so subsequent unchanged builds can skip the shell rebuild

#### Aspire startup flow

1. `src/Hosts/AppHost/AppHost.cs` starts the Studio API project
2. `AppHost` starts the new shell from `src/Studio/Server`
3. `AppHost` passes the resolved Studio API endpoint into the shell process as `STUDIO_API_HOST_API_BASE_URL`
4. `AppHost` passes the configured shell port into the process
5. the new shell starts and serves the Studio browser application
6. the user opens the shell at the configured local HTTPS endpoint

#### Runtime API configuration flow

1. Aspire resolves the Studio API endpoint
2. Aspire passes the endpoint through `STUDIO_API_HOST_API_BASE_URL`
3. the new shell runtime reads that value using the same mechanism category as the old shell
4. browser-side Studio code uses the resolved base address for API calls

#### Home page startup flow

1. the user opens the new Studio shell
2. the shell initializes its default workbench state
3. the home page opens automatically
4. the page displays a runtime-served copy of the UKHO logo and simple orientation content

#### Home page smoke-test flow

1. the user opens the `Home` page
2. the user clicks the `test` button
3. the shell calls the `StudioServiceHost` `/echo` endpoint
4. the endpoint returns text
5. the shell displays the returned text next to the button

### 3.3 Key decisions

- **Start from a fresh Theia scaffold rather than repairing the old shell in place**
  - rationale: the user explicitly wants a clean restart because the old shell accumulated build and tooling problems

- **Retain the old shell as `src/Studio/OldServer` temporarily**
  - rationale: it provides migration reference material without forcing the new shell to inherit its structure

- **Do not reference `OldServer` from the new implementation**
  - rationale: the old folder is intended for eventual deletion and must not become a hidden dependency

- **Preserve the proven repository build integration model**
  - rationale: `StudioServiceHost.csproj` plus `build.ps1` already solve Visual Studio and clean-clone readiness problems when implemented correctly

- **Preserve the Aspire runtime bridge pattern**
  - rationale: `STUDIO_API_HOST_API_BASE_URL` already solves API endpoint discovery without hard-coded browser configuration

- **Use copied code and assets, not linked code, when reusing legacy implementation details**
  - rationale: the new shell must remain independent from the old workspace

- **Restore a minimal branded home page early**
  - rationale: the user explicitly wants startup parity for the landing experience, and it provides a clear proof that the new shell is wired correctly

- **Use current Theia guidance for generated extensions and application composition**
  - rationale: the work item explicitly points to the official blueprint and generator documentation and should align with current supported patterns

## 4. Functional requirements

### FR-001 Move the current shell to `OldServer`

The implementation shall move the current contents of `src/Studio/Server` to `src/Studio/OldServer` before creating the new shell workspace.

### FR-002 Preserve `OldServer` as read-only reference

The moved workspace at `src/Studio/OldServer` shall be treated as read-only reference material until later deletion is deemed safe.

### FR-002a Make `OldServer` explicitly non-active

The implementation shall make `src/Studio/OldServer` clearly non-active so that routine build, startup, and day-to-day editing workflows target the new `src/Studio/Server` workspace only.

### FR-002b Mark `OldServer` clearly as legacy reference

The implementation shall ensure `src/Studio/OldServer` is understood to be for reading reference only and must not be treated as the active shell for ongoing changes, but this work item does not require a dedicated marker file inside that folder.

### FR-002c Exclude `OldServer` from active JavaScript workspaces and tooling

`src/Studio/OldServer` shall be excluded completely from active JavaScript workspace declarations and related active tooling scope so that it is not treated as part of the active monorepo and remains visible only as reference files in a directory until deletion.

### FR-003 Create a fresh Theia project at `src/Studio/Server`

A brand new Theia project shall be created at `src/Studio/Server` using the Yeoman-generated Theia extension approach referenced by the user.

### FR-003a Stay close to generated scaffold output

The implementation shall use the generated scaffold as the primary starting point and shall make only the minimum custom changes needed for repository build integration, Aspire startup, runtime configuration bridging, and the requested home page.

### FR-003b Follow documented repository tooling lessons

The implementation shall explicitly follow the Node, Yarn, nvm, and related build/toolchain lessons already documented in the repository wiki when generating and integrating the new scaffold.

### FR-004 Use current Theia extension/application composition patterns

The new project shall follow current Theia guidance for generated extensions and application composition rather than reviving deprecated plugin-era structure from the old workspace.

### FR-005 Keep the new shell independent from `OldServer`

The new shell shall not reference any code, package path, workspace path, or runtime asset path located under `src/Studio/OldServer`.

### FR-006 Copy required legacy implementation details into new files

If any logic, configuration, or assets from the old shell are needed, they shall be copied into new files under the new active workspace or other appropriate active locations rather than linked or referenced in place.

This includes relevant branding configuration from the old project where needed to preserve Studio naming and product identity.

For remaining low-level behavioral details not otherwise specified by this work package, the implementation shall prefer the old project's established behavior where practical.

### FR-006a Preserve similar external integration shape where practical

The new scaffold shall keep a similar external repository-facing package, script, and build-entrypoint shape where practical so that existing `StudioServiceHost`, `AppHost`, and related integration can be adapted with minimal change.

### FR-006b Preserve existing internal workspace/package names where practical

The new scaffold shall preserve existing internal workspace/package names such as `browser-app` and `search-studio` where practical, so long as this does not conflict with using the generated scaffold as the starting point.

### FR-006c Preserve existing root script names where practical

The new scaffold shall preserve existing root script names such as `build:browser` and `start:browser` where practical so that `AppHost` integration and normal developer habits remain unchanged.

### FR-007 Preserve .NET-triggered shell build integration

`src/Studio/StudioServiceHost/StudioServiceHost.csproj` shall continue to build the active Theia shell from `src/Studio/Server` during normal non-design-time builds.

### FR-008 Preserve incremental shell build behavior

The new shell build integration shall continue to use concrete tracked inputs and a stamp-file output so that unchanged workspaces do not rebuild the full browser shell unnecessarily.

### FR-008a Preserve current stamp-file-style approach in substance

The incremental shell build shall preserve the current stamp-file-style mechanism in substance rather than replacing it with a materially different incremental-build approach.

### FR-008b Keep explicit tracked-input declarations in `StudioServiceHost.csproj`

`src/Studio/StudioServiceHost/StudioServiceHost.csproj` shall keep the same explicit tracked-input pattern in substance, with concrete Theia files and folders declared in the project file in the same style as the old project rather than delegating change detection only to `build.ps1`.

### FR-009 Preserve prerequisite validation in the shell build script

The new `src/Studio/Server/build.ps1` shall validate the supported Node version and Yarn availability before attempting restore or build.

### FR-009a Keep explicit `build.ps1` entrypoint

The implementation shall keep an explicit `src/Studio/Server/build.ps1` shell build entrypoint rather than replacing it with a differently named or purely indirect mechanism.

### FR-009b Keep Visual Studio `F5` integration exactly the same

The implementation shall keep the current Visual Studio `F5` build and integration model exactly the same in substance, with the new shell fitting into the existing developer workflow rather than introducing a different startup or preparation path.

### FR-009c Keep the old build process exactly the same

The implementation shall keep the old Theia build process exactly the same in substance where it was already working, and shall not simplify, redesign, or replace that build/integration model.

### FR-010 Preserve Node/Yarn/toolchain hardening

The new shell build path shall preserve the previously proven mitigations for:

- required Node version selection/validation
- Yarn classic usage
- `yarn install --ignore-engines`
- clearing contaminated Visual Studio native-build environment variables where required
- enforcing `GYP_MSVS_VERSION` and `npm_config_msvs_version` expectations for native Node dependency compilation

### FR-011 Preserve Aspire shell startup wiring

`src/Hosts/AppHost/AppHost.cs` shall continue to start the active Studio shell from `src/Studio/Server` as the local Studio JavaScript application resource.

### FR-011a Keep the same Aspire resource pattern

The implementation shall keep the current Aspire resource pattern exactly the same in substance, with the Studio shell continuing to be hosted from `AppHost` as the same kind of JavaScript application resource rather than introducing a different resource model.

### FR-012 Preserve configured shell port behavior

The new shell shall continue to use the configured Studio shell port provided through Aspire configuration.

### FR-012c Keep the same fixed local port configuration pattern

The implementation shall keep the current fixed local port configuration pattern exactly the same in substance, with the shell port continuing to be read from `AppHost` configuration in the same way as today.

### FR-012a Browser-hosted only delivery

This work item shall deliver a browser-hosted Theia shell only and shall not introduce Electron or other packageable desktop application structure.

### FR-012b Use HTTPS local hosting

The rebuilt local Studio shell shall use HTTPS hosting for local Aspire integration rather than remaining HTTP-only.

This protocol change is the intentional exception to the otherwise unchanged local hosting and integration model.

### FR-013 Preserve `STUDIO_API_HOST_API_BASE_URL`

The new shell shall continue to receive the Studio API base address through the `STUDIO_API_HOST_API_BASE_URL` environment variable.

### FR-013a Keep the exact environment variable name

The implementation shall keep the exact environment variable name `STUDIO_API_HOST_API_BASE_URL` and shall not rename or alias it to a different configuration variable.

### FR-014 Preserve the old runtime bridge mechanism category

The new shell shall use the same overall mechanism pattern as the old shell for making the environment-provided API base address available to browser-side Studio code.

Where practical, the implementation shall preserve the same backend-to-browser bridge pattern rather than replacing it with a different configuration-delivery approach.

### FR-014a Keep the same same-origin backend configuration-endpoint pattern

The implementation shall explicitly preserve the same same-origin backend configuration-endpoint pattern used by the old shell to expose runtime configuration to browser-side code, rather than using a materially different browser-configuration mechanism.

### FR-015 Provide a startup home page

The new shell shall open a simple home page on Studio startup.

### FR-015f Other default generated surfaces may coexist for now

The `Home` page does not need to be the only automatically opened surface in this work item; other default generated Theia surfaces may also open for now and can be refined later.

### FR-015j Generated welcome/getting-started surface may remain for now

The generated Theia default welcome or getting-started surface does not need to be removed or replaced in this work item and may remain alongside the Studio `Home` page for now.

### FR-015g Keep Studio branding/title

The application/window title and related product branding shall keep the Studio branding rather than using generated default naming.

### FR-015h Reuse old branding configuration

Relevant branding configuration shall be copied from the old project into the new active workspace where needed, rather than being reinvented from scratch.

### FR-015i Broader browser/icon branding not required yet

This work item does not require copying broader old-project favicon, app icon, or browser branding assets beyond the Studio branding/title and the required copied UKHO logo PNG used by the home page.

### FR-015d Home as normal closable tab

The `Home` page shall open as a normal closable document tab rather than as a pinned or non-closable surface.

### FR-015c Home page re-openability

The home page shall also be re-openable later through a normal Theia command or menu path.

### FR-015e Re-open from `View` menu

The `Home` page shall be available to show again from the Theia `View` menu, matching the old shell behavior.

### FR-015k Preserve old `View` menu command naming where practical

The old `View` menu command naming for showing `Home` should be preserved where practical, rather than changing the label unnecessarily.

### FR-015a Home page content scope

The initial home page shall contain only the UKHO logo and short orientation text.

### FR-015b No home-page links or actions yet

The initial home page shall not include links, jump points, or other action affordances because there are no meaningful destinations to link to yet.

### FR-015l Add manual smoke-test button

The `Home` page shall include a simple button labelled `test` for manual smoke testing.

### FR-015m Call `StudioServiceHost` `/echo` from the `test` button

When the `test` button is clicked, the `Home` page shall call the `StudioServiceHost` `/echo` endpoint.

### FR-015n Display returned text next to the button

The text returned from the `/echo` endpoint shall be displayed next to the `test` button on the `Home` page.

### FR-015o No formatting polish required for the smoke test

The `test` button and returned-text display exist only as a manual smoke test in this work item and do not require polished formatting or final UX treatment.

### FR-016 Reuse the UKHO logo asset by copy

The implementation shall copy `docs/ukho-logo-transparent.png` into an appropriate runtime-served asset location for the new shell and display it on the startup home page.

### FR-017 Do not serve the logo directly from `docs/`

The runtime home page shall not depend on the repository `docs/` folder as its asset-serving location.

### FR-018 Preserve old-shell startup intent for home page behavior

The new home page shall appear on startup in the same general way as the old shell so users have an immediate branded landing experience.

### FR-018a Reuse acceptable old layout pattern

The new home page layout shall copy the acceptable presentation pattern from the old project, adapted into new files only, but it does not need to match the old page visually in detail.

### FR-018b Home-page wording may be refreshed

The home page orientation wording does not need to copy the old text exactly and may be refreshed or chosen sensibly for the new shell.

### FR-019 Deliver a minimal working shell baseline

The new shell baseline shall be considered complete for this work item when the shell can be built, started through Aspire, discover its Studio API base address through the preserved mechanism, and display the startup home page.

### FR-019a No placeholder workbench structure required

The initial delivery shall not be required to introduce placeholder activity items, workbench areas, or future feature structure beyond the minimum shell chrome needed to host the home page.

### FR-020 Exclude `OldServer` from active shell ownership

After migration, active build and startup ownership shall belong only to the new `src/Studio/Server` workspace, not to `src/Studio/OldServer`.

## 5. Non-functional requirements

1. The new shell setup must be reliable on a fresh clone.
2. The new shell setup must be reliable from normal Visual Studio `F5` workflows.
3. The build integration must remain incremental to avoid unnecessary front-end rebuilds.
4. The new shell must not depend on hidden local-machine assumptions beyond the documented Node/Yarn/toolchain baseline.
5. The migration must reduce, not increase, coupling to the legacy shell structure.
6. The startup home page must render quickly and remain lightweight.
7. The new shell should align with current official Theia guidance so future maintenance is clearer than with the legacy structure.
8. The deliverable should favor a simple, stable baseline over premature feature depth.

## 6. Data model

No domain data-model change is required by this work item.

The main data/configuration concerns are:

- shell build inputs and stamp-file outputs
- shell runtime configuration for the Studio API base URL
- shell runtime asset placement for the copied UKHO logo

If any generated package metadata or workspace manifests are introduced by the new scaffold, they are implementation artifacts of the new Theia workspace rather than business data-model changes.

## 7. Interfaces & integration

### 7.1 `StudioServiceHost` build integration

The active .NET project `src/Studio/StudioServiceHost/StudioServiceHost.csproj` shall remain the entry point that prepares the Theia shell during build.

The future implementation shall update its tracked Theia inputs to match the new scaffold structure as needed, while preserving:

- non-design-time execution only
- concrete input tracking
- stamp-file output tracking
- execution of the active `src/Studio/Server/build.ps1`

### 7.2 Aspire shell orchestration

`src/Hosts/AppHost/AppHost.cs` shall continue to register the Studio shell as a JavaScript application resource rooted at `src/Studio/Server`.

The future implementation shall preserve:

- startup command support for the active shell package
- build command support for the active shell package
- port binding from configuration
- environment-variable injection for `STUDIO_API_HOST_API_BASE_URL`

### 7.3 Studio API discovery

The new shell shall continue to discover the Studio API base address from the `STUDIO_API_HOST_API_BASE_URL` value supplied by Aspire.

Any Theia backend or browser bridge used to surface that value to browser-side code shall be recreated inside the new shell workspace rather than referenced from `OldServer`.

The `Home` page smoke-test button shall use that same runtime-discovered Studio API base address to call the `StudioServiceHost` `/echo` endpoint.

### 7.4 Legacy reference boundary

`src/Studio/OldServer` shall remain outside the active dependency boundary of the new shell.

This means the future implementation shall not rely on:

- imported TypeScript modules from `OldServer`
- copied path references into `OldServer`
- package-workspace declarations that include `OldServer`
- shared runtime asset paths under `OldServer`

## 8. Observability (logging/metrics/tracing)

This work item does not require new product-level observability capabilities.

However, the shell build and startup flow should remain diagnosable through:

- clear build-script output during restore/build steps
- clear failure messages for invalid Node/Yarn prerequisites
- preserved Aspire resource visibility for the Studio shell process

If the old shell already had minimal startup diagnostics relevant to configuration loading, equivalent behavior may be copied into the new shell only where needed to make startup failures actionable.

## 9. Security & compliance

1. The new shell shall not introduce a dependency on arbitrary local file paths from the legacy workspace.
2. Runtime configuration of the Studio API base address shall continue to be supplied by local orchestration rather than hard-coded into browser assets.
3. The copied logo asset shall be sourced from the repository's existing documentation asset as instructed by the user.
4. The migration shall avoid exposing unintended development endpoints beyond the existing local shell and Studio API behavior.

## 10. Testing strategy

Validation for the future implementation shall include at least:

1. confirming the old workspace has been moved to `src/Studio/OldServer`
2. confirming the new shell exists at `src/Studio/Server`
3. confirming `src/Studio/StudioServiceHost/StudioServiceHost.csproj` still prepares the shell during normal build
4. confirming incremental no-op behavior when no Theia inputs have changed
5. confirming a clean restore/build path with the documented Node/Yarn baseline
6. confirming `src/Hosts/AppHost/AppHost.cs` starts the new shell successfully
7. confirming the new shell receives and uses `STUDIO_API_HOST_API_BASE_URL`
8. confirming the startup home page opens automatically
9. confirming the copied UKHO logo is displayed from the new runtime asset location
10. confirming the `test` button calls the `StudioServiceHost` `/echo` endpoint successfully and displays the returned text next to the button
11. confirming the new shell has no dependency on `src/Studio/OldServer`

Where automated tests are not practical for the Theia startup path, a documented manual validation sequence is acceptable.

## 11. Rollout / migration

Recommended implementation sequence:

1. move the current `src/Studio/Server` workspace to `src/Studio/OldServer`
2. create the fresh Theia scaffold at `src/Studio/Server`
3. recreate the active workspace scripts/manifests/build structure required by local hosting
4. recreate `build.ps1` in the new workspace using the proven repository hardening and incremental-build patterns
5. update `src/Studio/StudioServiceHost/StudioServiceHost.csproj` input tracking if the new scaffold structure differs from the old one
6. update or confirm `src/Hosts/AppHost/AppHost.cs` startup/build commands and environment wiring for the new scaffold
7. recreate the runtime bridge for `STUDIO_API_HOST_API_BASE_URL`
8. copy the UKHO logo asset into the new runtime asset location
9. implement the minimal startup home page
10. validate build, startup, API discovery, and home-page behavior
11. leave `src/Studio/OldServer` in place as reference-only until a later explicit deletion work item

## 12. Open questions

None at present.

## 13. Clarified decisions

- The new Theia shell remains browser-hosted only for this work item.
- This work item does not include Electron or other packageable desktop application structure.
- This work item does not port the old Studio UI structure or feature set, except for the requested simple startup home page.
- The new scaffold should keep a similar external repo-facing package/build shape where practical, but this does not imply preserving the old UI or feature structure.
- The `STUDIO_API_HOST_API_BASE_URL` mechanism should keep the same overall backend-to-browser bridge pattern where practical because the current approach worked well.
- `src/Studio/OldServer` must be explicitly non-active and clearly marked as read-only reference material so it is not edited by mistake instead of the new shell.
- The initial home page should contain only the UKHO logo and orientation text, with no links yet, and should reuse the acceptable layout pattern from the old project by copying it into new files.
- The initial home page should reuse the acceptable old layout pattern, but its wording may be refreshed and chosen sensibly because exact wording is not important in this work item.
- The home page should open by default on startup and also be re-openable later through a normal Theia path.
- The initial home page only needs a close-enough placeholder reuse of the old layout pattern and does not need close visual matching because visual style will change later.
- The initial shell should expose only the absolute minimum Theia chrome needed to start and show the home page, with no placeholder workbench structure yet.
- This work item should keep an explicit `src/Studio/Server/build.ps1` entrypoint for shell build integration.
- Incremental shell build behavior should preserve the current stamp-file-style approach in substance.
- `StudioServiceHost.csproj` should keep the same explicit tracked-input pattern in substance, rather than delegating change detection only to `build.ps1`.
- The current Visual Studio `F5` build and integration model should remain exactly the same in substance.
- The old Theia build process should remain exactly the same in substance where it was already working, and should not be simplified or reworked.
- The current Aspire resource pattern should remain exactly the same in substance, with the shell still hosted as the same kind of JavaScript application resource from `AppHost`.
- The fixed local port configuration pattern should remain exactly the same in substance, with the shell port still read from `AppHost` configuration in the same way as today.
- Existing internal workspace/package names such as `browser-app` and `search-studio` should be preserved where practical.
- Existing root script names such as `build:browser` and `start:browser` should be preserved where practical.
- The runtime API base-address environment variable name must remain exactly `STUDIO_API_HOST_API_BASE_URL` with no renaming.
- The `Home` page should open as a normal closable tab and should be available to show again from the Theia `View` menu like the old shell.
- The `Home` page should open automatically, but other default generated surfaces may also open for now and can be refined later.
- The generated Theia default welcome/getting-started surface may remain for now alongside the Studio `Home` page and can be refined later.
- The application/window title should keep the Studio branding, using branding configuration copied from the old project rather than generated defaults.
- Broader old-project favicon/app-icon/browser-branding asset migration is not required in this work item beyond the Studio branding/title and the required copied UKHO logo PNG for the home page.
- The runtime bridge should explicitly keep the same same-origin backend configuration-endpoint pattern used by the old shell.
- The old `View` menu command naming for showing `Home` should be preserved where practical.
- For remaining low-level shell-behavior choices not otherwise called out in this work package, the default should be to do what the old project did where practical.
- The `Home` page should include a simple manual smoke-test button labelled `test` which calls the `StudioServiceHost` `/echo` endpoint and displays the returned text next to the button, with no formatting polish required.
- The rebuilt local Studio shell must use HTTPS hosting, and this protocol change is the intentional exception to the otherwise unchanged local hosting and integration model.
- The generated Theia scaffold should remain close to generator output and should only be customized where needed for build, Aspire, configuration, and home-page integration.
- Exact generator version pinning is not required for this work item, but the implementation must still follow the documented Node/Yarn/nvm and toolchain lessons from the repository wiki.
- `src/Studio/OldServer` does not require a dedicated marker file in this work item because it is expected to be deleted soon.
- `src/Studio/OldServer` must be excluded completely from active JavaScript workspace declarations and tooling visibility, remaining only as reference files until deletion.
