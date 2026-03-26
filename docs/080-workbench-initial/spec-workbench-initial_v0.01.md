# Work Package: `080-workbench-initial` — Initial Workbench shell scaffolding

**Target output path:** `docs/080-workbench-initial/spec-workbench-initial_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created for the Workbench foundation work package.
- `v0.01` — Captures the requirement to introduce an extensible, modular Workbench application shell inspired by the platform capabilities previously valued in Theia.
- `v0.01` — Confirms that this first work package is limited to scaffolding, Aspire wiring, hosted Blazor WebAssembly setup, placeholder tests, and a minimal `Hello UKHO Workbench` client screen.
- `v0.01` — Confirms that Workbench projects must remain entirely independent of UKHO Search-specific behavior so the shell can be lifted into other repositories later.
- `v0.01` — Records the intended server and client Onion architecture project layout under `src/workbench/server` and `src/workbench/client`, with shared `UKHO.Workbench.Common` under `src/workbench`.
- `v0.01` — Confirms that placeholder manifest contract types should be deferred until a later work package because module definitions have not yet been defined.
- `v0.01` — Confirms that test coverage structure for this work package should use one mirrored test project per production project.
- `v0.01` — Confirms that the mirrored test-project rule applies to every production project, including `WorkbenchHost`, `WorkbenchClient`, and `UKHO.Workbench.Common`.
- `v0.01` — Confirms that `WorkbenchHost` should provide static hosting for `WorkbenchClient` plus default Aspire and health wiring only, with no extra placeholder API surface in this work package.
- `v0.01` — Confirms that `UKHO.Workbench.Common` should exist as a project shell only in this work package, with no concrete shared types yet.
- `v0.01` — Confirms that the initial Blazor client should use the simplest possible shape: a minimal layout plus a single home page that displays `Hello UKHO Workbench`.
- `v0.01` — Confirms that all new Workbench production and test projects should be added to the main repository solution immediately in this work package.
- `v0.01` — Confirms that runnable verification means the solution builds, tests pass, and Aspire can be started manually so the default Workbench link shows the initial page; this startup verification is manual rather than automated.
- `v0.01` — Confirms that placeholder tests should remain compile-only with trivial assertions in this work package, even though Blazor UI test projects still include a suitable `bUnit` package reference.
- `v0.01` — Confirms that the `AppHost` requirement should be expressed simply: the default Aspire link for `WorkbenchHost` must open the hosted `WorkbenchClient` at `/`.
- `v0.01` — Confirms that tests are non-material in this work package beyond basic scaffold presence, so the specification should not over-constrain placeholder test content or naming detail.
- `v0.01` — Confirms that non-host projects should include minimal composition-root wiring placeholders now, including lightweight DI extension methods in services and infrastructure projects.
- `v0.01` — Confirms that the specification should not standardize naming for placeholder DI entry points in this work package and should leave that detail to implementation.
- `v0.01` — Confirms that the initial host and client experience should be anonymous only for this work package, with no active authentication flow or auth placeholder hooks required.
- `v0.01` — Confirms that the hosting model should be specified only at the behavior level: Aspire needs to know about `WorkbenchHost`, and `WorkbenchHost` must serve the client from `/`; the specification should otherwise keep hosting details as simple and unconstrained as possible.
- `v0.01` — Confirms that `AppHost` is the only required run and startup path for this work package; direct standalone execution of `WorkbenchHost` does not need to be specified or verified yet.
- `v0.01` — Confirms that package and version management should simply follow whatever conventions already exist in the repository, without adding a new centralization requirement in this work package.
- `v0.01` — Confirms that the initial server host should not define any explicit API endpoints beyond the minimum host plumbing already implied by the requirement.
- `v0.01` — Confirms that placeholder tests should still pass where present, but they must remain trivial `Assert.True()`-style placeholders and nothing more.
- `v0.01` — Confirms that no additional README or developer notes outside this specification are required in this work package.
- `v0.01` — Confirms that this work package should not require any explicit CI or workflow changes beyond whatever existing automation naturally picks up from adding the new projects to the main solution.
- `v0.01` — Confirms that the specification should explicitly require Onion-aligned project reference direction between the new Workbench projects, not just folder naming and placement.
- `v0.01` — Confirms that the hosted client route should be only `/` in this work package so clicking the Aspire dashboard link always opens the initial Workbench screen directly.
- `v0.01` — Confirms that the client requirement should stay outcome-based and minimal: Blazor must render `Hello UKHO Workbench` at `/`, with no further route, page, or template-cleanup requirements specified in this work package.
- `v0.01` — Confirms that unused default Blazor template pages, navigation, and components should be removed so the initial client remains intentionally minimal.
- `v0.01` — Corrects the infrastructure project naming and reference direction to use the `UKHO.Workbench.*` pattern consistently, with `WorkbenchHost -> UKHO.Workbench.Infrastructure -> UKHO.Workbench.Services -> UKHO.Workbench -> UKHO.Workbench.Common` and the mirrored client-side equivalent.
- `v0.01` — Confirms that `UKHO.Workbench.Common` should be referenced directly by the domain projects only, with higher layers reaching it through chained Onion project references.
- `v0.01` — Confirms that Aspire should register only `WorkbenchHost` in this work package, with no separate `WorkbenchClient` Aspire resource.

## 1. Overview

### 1.1 Purpose

This specification defines the first incremental work package for a new extensible application shell named `Workbench`.

The purpose of this work package is to establish the initial project structure, architectural boundaries, Aspire registration, hosted client/server wiring, and test placeholders needed to make the shell buildable and runnable inside this repository without yet implementing module discovery, module loading, or real feature composition.

### 1.2 Scope

This specification currently includes:

- creating the initial Workbench server-side projects under `src/workbench/server`
- creating the initial Workbench client-side projects under `src/workbench/client`
- creating `UKHO.Workbench.Common` under `src/workbench`
- applying Onion architecture boundaries independently on the server and client sides
- creating mirrored test projects under `test/workbench/server` and `test/workbench/client` following repository conventions
- registering `WorkbenchHost` with the existing Aspire `AppHost`
- configuring `WorkbenchClient` so the default Aspire dashboard link for the host opens the hosted Blazor WebAssembly client experience
- providing only a minimal `Hello UKHO Workbench` UI in the client
- removing unused default Blazor template pages, navigation, and components from the initial client
- inserting placeholder tests, including a suitable `bUnit` package reference for Blazor UI tests
- ensuring the solution builds and runs successfully

This specification currently excludes:

- module manifest implementation details beyond any minimum scaffolding needed for compilation or future extension
- module discovery, loading, activation, or runtime composition
- Search-specific UI, services, workflows, or domain coupling
- feature-complete workbench capabilities such as docking, commands, menus, layout persistence, explorers, or context-awareness beyond documenting the intended direction
- additional README files or broader developer documentation updates outside this work package specification
- explicit CI or workflow changes beyond any incidental effect of adding the new projects to the main solution

### 1.3 Stakeholders

- developers building the future Workbench platform
- developers integrating Workbench-hosted modules
- UKHO Search contributors who will deliver the first module on top of the Workbench shell
- maintainers of repository architecture, test structure, and Aspire orchestration

### 1.4 Definitions

- `Workbench`: the new reusable application shell intended to provide platform concepts previously valued in Theia, such as commands, views, layout composition, and context-aware extension points
- `module`: a future composable feature unit that will have a manifest, server-side implementation, client-side implementation, and shared contracts
- `hosted Blazor WebAssembly`: a deployment model where the Blazor WebAssembly client is served by an ASP.NET Core host application
- `Onion architecture`: the repository architecture pattern where dependencies point inward from hosts to infrastructure to services to domain

## 2. System context

### 2.1 Current state

The repository currently contains UKHO Search solutions and infrastructure, plus an established architectural preference for Onion architecture and Aspire-based orchestration.

A replacement is needed for the richer shell characteristics previously experienced through Theia. The desired Workbench direction is to provide a modular shell in which:

- views and widgets are first-class concepts
- navigation follows predictable built-in patterns
- commands can be reused across menus, toolbars, keybindings, command palettes, and buttons
- menus and toolbars are contribution-based
- layout and docking are platform concepts
- frontend and backend concerns are cleanly separated
- extension and module composition is natural
- lifecycle hooks are predictable
- trees, selections, and explorers fit the platform model
- context-aware enablement and visibility are built in

However, none of those runtime behaviors are required to be fully implemented in this first work package.

### 2.2 Proposed state

In the proposed state after this work package:

- a new `Workbench` solution area exists under `src/workbench`
- the server side contains:
  - `src/workbench/server/UKHO.Workbench`
  - `src/workbench/server/UKHO.Workbench.Services`
  - `src/workbench/server/UKHO.Workbench.Infrastructure`
  - `src/workbench/server/WorkbenchHost`
- the client side contains:
  - `src/workbench/client/UKHO.Workbench.Client`
  - `src/workbench/client/UKHO.Workbench.Client.Services`
  - `src/workbench/client/UKHO.Workbench.Client.Infrastructure`
  - `src/workbench/client/WorkbenchClient`
- shared definitions exist in `src/workbench/UKHO.Workbench.Common`
- one mirrored test project per production project exists under `test/workbench/server` and `test/workbench/client`, including tests for `WorkbenchHost`, `WorkbenchClient`, and `UKHO.Workbench.Common`
- all new Workbench production and mirrored test projects are added to the main repository solution
- `WorkbenchHost` is registered with the existing Aspire `AppHost`
- Aspire registers only `WorkbenchHost`, with no separate `WorkbenchClient` resource in this phase
- the Aspire dashboard default link for `WorkbenchHost` opens the hosted `WorkbenchClient` root at `/`
- the hosted client surface required by this work package is only that `/` lands directly on the initial Blazor-rendered screen
- `WorkbenchHost` exposes only the minimum hosted Blazor and default Aspire or health surface needed for runtime wiring
- no explicit Workbench-specific API endpoints are introduced in this phase
- the initial hosted Workbench experience is anonymous only, with no active authentication flow in this phase
- Aspire only needs to know about `WorkbenchHost`, while `WorkbenchHost` serves the client experience from `/`
- `AppHost` is the only required developer run path in this phase
- `UKHO.Workbench.Common` exists as an empty shared project shell for future common definitions
- non-host services and infrastructure projects include minimal DI registration or composition-root placeholder wiring for future host integration
- the visible UI uses the simplest possible client shape, consisting of a minimal layout and a single page showing `Hello UKHO Workbench`
- unused default Blazor template pages, navigation, and components are removed from the initial client
- placeholder tests are intentionally lightweight and limited to trivial `Assert.True()`-style coverage only
- manual Aspire startup verification confirms that the default Workbench link opens the initial page successfully
- Workbench projects are intentionally ignorant of UKHO Search-specific concerns

### 2.3 Assumptions

- the new Workbench projects will be added to the existing repository solution and orchestration model rather than maintained as a completely separate repo or independent orchestration surface
- the hosting approach should remain as simple as possible and should not be over-constrained beyond requiring `WorkbenchHost` to serve the client from `/`
- Aspire orchestration in this phase only needs to know about `WorkbenchHost` because the client is hosted through it
- direct standalone launch requirements for `WorkbenchHost` are deferred because the initial developer workflow is centered on `AppHost`
- placeholder tests are acceptable for this first work package as long as the solution remains buildable and runnable
- mirrored testing means one test project per production project rather than aggregate server/client-only test containers, and this includes hosts and the shared common project
- repository solution inclusion is part of the initial scaffolding rather than a follow-on package concern
- the host endpoint surface for this first work package is intentionally minimal and limited to hosted client delivery plus default platform wiring
- no placeholder API area is needed in `WorkbenchHost` for this work package
- authentication is deferred entirely from this first work package rather than introduced as active behavior or placeholder hook wiring
- the shared common project is being created ahead of need and may initially contain no concrete shared types
- minimal composition-root placeholders in services and infrastructure projects are acceptable even before those projects contain meaningful runtime behavior
- naming of placeholder DI entry points does not need to be standardized by this work package as long as the composition wiring is present and usable
- package references and version declarations should follow the repository's existing conventions rather than introducing new package-management rules in this work package
- Onion intent in this work package should be reflected in actual project references, not only in naming or folder structure
- `UKHO.Workbench.Common` should sit at the innermost edge of the chain and not be referenced directly by higher layers when chained references already provide access
- the initial Blazor client requirement is limited to rendering `Hello UKHO Workbench`, without further specification of template cleanup beyond what is needed to achieve that result
- runtime verification through Aspire is expected to include a manual startup check rather than an automated end-to-end launch sequence
- the first test baseline is non-material and exists only to satisfy scaffold expectations through trivial passing assertions rather than meaningful behavioral coverage
- module functionality and manifest contract definitions will be added in later work packages rather than front-loading abstractions that are not yet needed
- UKHO Search will be the first module built on Workbench, but no Workbench project may take a dependency on Search-specific code or concepts

### 2.4 Constraints

- every Workbench project must remain generic and reusable outside UKHO Search
- the server side must follow Onion architecture under `src/workbench/server`
- the client side must follow Onion architecture under `src/workbench/client`
- `UKHO.Workbench.Common` must be shared by both client and server domain projects
- this work package must create all requested projects and mirrored test projects
- `WorkbenchHost` must be registered with Aspire `AppHost`
- `WorkbenchClient` must be served from `WorkbenchHost`
- the initial hosted client route must be available directly at `/`
- the initial UI must remain minimal and limited to `Hello UKHO Workbench`
- no module definition loading or runtime module composition is required in this work package
- Blazor UI test projects must include a suitable `bUnit` package reference
- placeholder tests must be present
- the solution must build and run successfully

### 2.5 Open questions

No open questions are currently recorded.

## 3. Component / service design (high level)

### 3.1 Components

1. `UKHO.Workbench.Common`
   - shared home for common definitions needed by both server and client domain layers
   - expected future home for module manifest definitions

2. `Server Workbench domain`
   - `UKHO.Workbench`
   - contains server-side Workbench domain models and abstractions
   - must remain independent of UKHO Search

3. `Server Workbench services`
   - `UKHO.Workbench.Services`
   - contains server-side application or domain services supporting the host

4. `Server Workbench infrastructure`
   - `UKHO.Workbench.Infrastructure`
   - contains infrastructure implementations for server-side concerns

5. `WorkbenchHost`
   - minimal API host for the server-side Workbench runtime
   - serves the Blazor WebAssembly client
   - participates in Aspire orchestration

6. `Client Workbench domain`
   - `UKHO.Workbench.Client`
   - contains client-side domain models and abstractions

7. `Client Workbench services`
   - `UKHO.Workbench.Client.Services`
   - contains client-side application or domain services

8. `Client Workbench infrastructure`
   - `UKHO.Workbench.Client.Infrastructure`
   - contains client-side infrastructure and integration implementations

9. `WorkbenchClient`
   - hosted Blazor WebAssembly client application
   - initially renders only a `Hello UKHO Workbench` experience

10. `Workbench test projects`
    - mirrored tests under `test/workbench/server` and `test/workbench/client`
    - includes placeholder tests and appropriate Blazor testing package references

### 3.2 Data flows

#### Initial runtime flow

1. Aspire starts `WorkbenchHost`
2. the default host link opens the hosted Workbench web experience
3. `WorkbenchHost` serves `WorkbenchClient`
4. `WorkbenchClient` renders the initial `Hello UKHO Workbench` screen

#### Initial development flow

1. developers open the repository solution
2. Workbench projects build as part of the repository solution
3. placeholder tests are present in mirrored test projects
4. the solution can be launched and verified through the existing Aspire developer workflow

### 3.3 Key decisions

- Workbench is being introduced as a reusable shell, not a Search-specific shell
- Search will be the first module, but Workbench itself must remain ignorant of Search
- both server and client sides will use Onion architecture
- the initial reference direction is explicitly `Host -> Infrastructure -> Services -> Domain -> Common` on both server and client sides
- the shell will be built incrementally through multiple work packages
- the first work package is intentionally limited to scaffolding and hosted wiring rather than implementing the full module model or shell capabilities

## 4. Functional requirements

1. The system shall create all initial Workbench projects under the required `src/workbench` structure.
2. The system shall create mirrored Workbench test projects under `test/workbench/client` and `test/workbench/server` according to repository conventions, with one test project per production project, including `WorkbenchHost`, `WorkbenchClient`, and `UKHO.Workbench.Common`.
3. The system shall add all new Workbench production and mirrored test projects to the main repository solution in this work package.
4. The server-side Workbench structure shall include `UKHO.Workbench`, `UKHO.Workbench.Services`, `UKHO.Workbench.Infrastructure`, and `WorkbenchHost`.
5. The client-side Workbench structure shall include `UKHO.Workbench.Client`, `UKHO.Workbench.Client.Services`, `UKHO.Workbench.Client.Infrastructure`, and `WorkbenchClient`.
6. The solution shall include `UKHO.Workbench.Common` under `src/workbench`, and the client and server domain projects shall reference it directly.
7. `WorkbenchHost` shall be registered with the repository's Aspire `AppHost`.
8. Aspire shall not register a separate `WorkbenchClient` resource in this work package.
9. The default Aspire dashboard link for `WorkbenchHost` shall open the hosted `WorkbenchClient` experience at `/`.
10. `WorkbenchHost` shall expose only the minimum hosted Blazor and default Aspire or health wiring required for runtime operation in this work package.
11. The specification shall not over-constrain the client hosting implementation beyond requiring `WorkbenchHost` to serve the client experience from `/`.
12. `AppHost` shall be the only required run and startup path for this work package.
13. `WorkbenchHost` shall not define any explicit Workbench-specific API endpoints in this work package.
14. The initial hosted Workbench experience shall be anonymous only, with no active authentication flow required in this work package.
15. Non-host services and infrastructure projects shall include minimal DI registration or composition-root placeholder wiring suitable for future host integration.
16. The initial `WorkbenchClient` UI shall use a minimal layout with a single home page that displays `Hello UKHO Workbench` and no broader shell functionality.
17. Unused default Blazor template pages, navigation, and components shall be removed from the initial client.
18. The first work package shall not implement runtime module definition loading or module composition.
19. Blazor UI test projects shall include a suitable `bUnit` package reference.
20. The initial test projects shall include only trivial `Assert.True()`-style placeholder tests sufficient to satisfy scaffold expectations for this work package.
21. The resulting solution shall build successfully, the trivial placeholder tests for the new projects shall pass, and Aspire startup shall be manually verifiable such that the default Workbench link shows the initial page successfully.

## 5. Non-functional requirements

1. Workbench projects must remain reusable outside this repository's Search-specific context.
2. Dependency direction and actual project references must follow the repository Onion architecture rules independently on client and server sides.
3. The initial implementation should keep the shell intentionally small so later work packages can evolve it incrementally.
4. Naming, folder placement, and test mirroring should align with the conventions already used in this repository.
5. The developer startup flow through Aspire should be straightforward and predictable.
6. `UKHO.Workbench.Common` should be consumed through chained project references above the domain layer rather than by adding unnecessary direct references in higher layers.

## 6. Data model

The initial work package is not expected to introduce a production data model.

`UKHO.Workbench.Common` is included only as an empty project shell in this phase.

No concrete shared types, primitives, or manifest contract definitions are required until a later work package once module definitions have been agreed.

## 7. Interfaces & integration

1. `AppHost` integration
   - `WorkbenchHost` must be registered so it can be launched from Aspire
   - no separate `WorkbenchClient` Aspire resource is required in this work package
   - the default Aspire link must open the hosted client experience at `/`

2. `WorkbenchHost` and `WorkbenchClient`
   - `WorkbenchHost` must serve the WebAssembly client
   - no broader API surface is required beyond the default platform wiring needed for hosting or health
   - no explicit Workbench-specific API endpoints are required in this phase
   - the initial client surface must be reachable directly at `/`
   - `WorkbenchClient` only needs to render `Hello UKHO Workbench` via Blazor for this work package
   - hosting implementation details should remain loosely specified beyond the requirement that the client is served from `/`

3. `Common project sharing`
   - `UKHO.Workbench.Common` must be referenced directly by both client and server domain projects
   - higher layers should consume it through the chained Onion reference path rather than direct references where unnecessary

## 8. Observability (logging/metrics/tracing)

The initial work package does not require a rich observability model.

Any minimum logging added for startup or host diagnostics should align with repository logging standards and remain generic to Workbench.

## 9. Security & compliance

1. No Search-specific data or privileged business behavior should be introduced into Workbench foundations.
2. The first work package should avoid unnecessary privileged operations and keep the runtime surface minimal.
3. The initial hosted Workbench experience should remain anonymous only, with authentication and authorization deferred to later work packages.
4. Any shared contracts introduced in this phase must avoid embedding repository-specific secrets, endpoints, or business rules.

## 10. Testing strategy

1. Create one mirrored placeholder test project for each new Workbench production project, following repository conventions, including the host projects and `UKHO.Workbench.Common`.
2. Include a suitable `bUnit` package reference in Blazor UI test projects.
3. Add only trivial `Assert.True()`-style placeholder tests sufficient to keep the scaffold complete for this work package.
4. Verify the repository solution builds successfully.
5. Verify the Workbench hosted experience is runnable through a manual Aspire startup check.

## 11. Rollout / migration

1. Introduce Workbench as a new parallel solution area rather than replacing existing Search UI behavior in this work package.
2. Add the new Workbench production and test projects to the main repository solution immediately so they participate in normal build and developer workflows.
3. Defer migration of Search functionality into modules to later work packages.
4. Use this work package as the baseline for future incremental shell capability work.
5. Treat manual Aspire startup verification through `AppHost` as sufficient rollout validation for the first runnable baseline.

## 12. Open questions

No open questions are currently recorded.
