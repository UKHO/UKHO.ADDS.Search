# Theia Knowledgebase

This page captures practical Eclipse Theia lessons learned while implementing work packages `064-studio-skeleton` and `065-studio-tree-widget` in this repository.

It is intentionally an internal working knowledge base, not a replacement for official Theia documentation.

Use it when:

- setting up another Theia solution in this repository or elsewhere
- integrating a Theia shell into Aspire
- wiring Theia into Visual Studio `F5` workflows
- debugging a Theia `TreeWidget` that compiles but does not render
- deciding which Theia patterns were proven to work here

## Purpose

The goal of this page is to preserve the practical details that are easy to rediscover the hard way:

- what worked reliably in this repository
- what failed or behaved unexpectedly
- what commands and file wiring were actually required
- what to verify when a Theia UI looks correct in code but does not appear at runtime

## Official references still come first

This page complements, but does not replace, official sources:

- [Theia extensions overview](https://theia-ide.org/docs/extensions/)
- [Authoring Theia extensions](https://theia-ide.org/docs/authoring_extensions/)
- [Theia widgets](https://theia-ide.org/docs/widgets/)
- [Theia tree widget](https://theia-ide.org/docs/tree_widget/)
- [Theia architecture](https://theia-ide.org/docs/architecture/)
- [Theia API docs](https://eclipse-theia.github.io/theia/docs/next/index.html)

## What was implemented in `064` and `065`

Work package `064` established the first usable Studio shell baseline:

- a Theia browser application under `src/Studio/Server`
- a native Theia extension package `search-studio`
- Aspire integration for local hosting
- runtime bridge from Aspire to Theia backend to browser configuration
- live provider/rule loading from `StudioApiHost`
- initial `Providers`, `Rules`, and `Ingestion` work areas

Work package `065` hardened the shell into a more native Theia experience:

- `Providers`, `Rules`, and `Ingestion` all moved to native `TreeWidget`
- visible body-level buttons were removed in favor of native tree navigation and toolbar actions
- `Rules` and `Ingestion` adopted Theia tab-bar toolbar actions
- shared tree behavior was centralized to reduce drift between work areas

## Setup pattern that worked with Aspire

### Hosting model

The Theia shell is started from Aspire in `src/Hosts/AppHost/AppHost.cs`.

The key working pattern was:

- host the shell as a JavaScript app resource
- pass the resolved `StudioApiHost` endpoint into the Theia process as environment
- expose the shell over fixed local HTTP for predictable developer access

Relevant code:

- `src/Hosts/AppHost/AppHost.cs`
- `src/Hosts/AppHost/appsettings.json`

### Fixed port configuration

The shell port is read from Aspire configuration:

- `Studio:Server:Port`

Current location:

- `src/Hosts/AppHost/appsettings.json`

Current value used here:

- `3000`

That made the shell URL stable for local usage:

- `http://localhost:3000`

### Runtime environment bridge

The working pattern was:

1. Aspire resolves the `StudioApiHost` endpoint
2. `AppHost` passes it into the JavaScript app as `STUDIO_API_HOST_API_BASE_URL`
3. the Theia backend exposes the value via `/search-studio/api/configuration`
4. browser code reads configuration from that same-origin endpoint

This avoided hard-coding hostnames and reduced browser-side environment assumptions.

## What made Visual Studio `F5` work reliably

### Problem

A normal .NET build does not automatically guarantee that the Theia browser assets are present and current.

That is especially painful on:

- a fresh clone
- a machine where JavaScript dependencies are missing
- a normal Visual Studio `F5` flow where developers expect the shell to be ready once the host project runs

### Working solution

The working solution in this repository was to make `StudioApiHost.csproj` build the Theia shell before build.

Relevant file:

- `src/Studio/StudioApiHost/StudioApiHost.csproj`

The target that made this work:

- `BuildTheiaStudioShell`

It runs:

- `src/Studio/Server/build.ps1`

before normal build, while skipping design-time builds.

### Why this worked well

It gave the repository a practical `F5` experience:

- Visual Studio build prepares the shell
- the shell is not forgotten on a clean clone
- subsequent builds are incremental rather than always rebuilding the full Theia browser bundle

### Incremental behavior

The Theia build script and `.csproj` target both watch concrete inputs such as:

- `build.ps1`
- `package.json`
- `yarn.lock`
- `lerna.json`
- `browser-app/package.json`
- `search-studio/package.json`
- `search-studio/tsconfig.json`
- `search-studio/src/**`

and emit a stamp file under:

- `src/Studio/StudioApiHost/obj/<Configuration>/<TFM>/theia-shell-build.stamp`

This meant:

- clean clone: full restore/build happens
- unchanged workspace: build is skipped
- changed Theia sources: browser build reruns

## Clean clone vs existing workspace initialization

### Clean clone behavior to expect

On a clean clone, expect at least these steps to matter:

1. correct Node version must be available
2. `yarn install --ignore-engines` may need to run
3. native Node dependencies may compile
4. `yarn build:browser` must produce the browser bundle
5. Aspire may also show a companion installer resource on first run

This is normal and can take noticeable time.

### Existing workspace behavior to expect

On an already-restored workspace:

- dependency restore is usually skipped
- the incremental stamp often prevents unnecessary browser rebuilds
- startup is much faster

### Practical takeaway

When diagnosing a problem, always ask first:

- is this a clean clone problem?
- is this a stale browser bundle problem?
- is this a dependency restore problem?
- is this a runtime wiring problem?

## Node, Yarn, and native build constraints that mattered here

The working baseline in this repository was:

- Node `18.20.4`
- Yarn classic `1.x`
- restore with `yarn install --ignore-engines`

The working script also had to defend against Visual Studio toolchain contamination.

Relevant file:

- `src/Studio/Server/build.ps1`

Important behavior inside that script:

- clears selected Visual Studio C++ environment variables before JavaScript/native builds
- forces `npm_config_msvs_version` and `GYP_MSVS_VERSION` to `2022`
- validates the active Node version explicitly

Practical takeaway:

- if Theia restore/build fails from a VS developer shell, retry from a clean PowerShell session
- if Node version is wrong, fix that before debugging anything else

## Tree widget knowledge base

## Symptom: tree view opens but no rows are visible

### What we observed

A Theia view could open correctly, data could load correctly, and there could still be no visible tree rows.

This can happen without browser console errors.

### What caused it here

A combination of subtle issues mattered:

- the browser bundle had not been rebuilt after code changes
- model/widget wiring needed to follow the native `createTreeContainer(...)` pattern precisely
- relying only on implicit lifecycle initialization was not always enough

### What fixed it here

The working pattern was:

1. bind widget and model explicitly
2. create the widget through `createTreeContainer(...)`
3. ensure the tree model is initialized idempotently
4. also initialize the model from the widget lifecycle
5. rebuild the browser bundle

Relevant examples:

- `src/Studio/Server/search-studio/src/browser/search-studio-frontend-module.ts`
- `src/Studio/Server/search-studio/src/browser/providers/search-studio-provider-tree-model.ts`
- `src/Studio/Server/search-studio/src/browser/search-studio-widget.tsx`
- corresponding `rules` and `ingestion` tree model/widget files

## Symptom: extension TypeScript builds, but the running UI still looks old

### Cause

`yarn --cwd .\src\Studio\Server\search-studio build` only compiles the extension package.

It does **not** prove the served browser bundle has been rebuilt.

### Fix

Run:

```powershell
yarn --cwd .\src\Studio\Server build:browser
```

Then restart the shell.

### Practical rule

If a Theia UI change is not visible, do not trust package compilation alone. Rebuild the browser bundle.

## Symptom: icons do not appear in tree rows

### Cause

The current Theia tree behavior used here did not render item icons by default even when icon classes were available.

### Fix

Override `renderIcon(...)` in the shared tree widget and explicitly render the icon node.

Relevant file:

- `src/Studio/Server/search-studio/src/browser/common/search-studio-tree-widget.ts`

## Symptom: tree data exists but still feels inconsistent across views

### Cause

Per-view custom handling for context menus and selection easily drifts.

### Fix

Move shared tree behavior into a shared base widget.

The working shared behavior here includes:

- icon rendering
- label rendering
- context-menu selection/rendering flow
- common native-tree assumptions

Relevant file:

- `src/Studio/Server/search-studio/src/browser/common/search-studio-tree-widget.ts`

## Tree node patterns that worked

### Use a hidden root

A hidden root node with `visible: false` worked well for all three trees.

This allowed:

- one consistent tree model shape
- status nodes for loading/empty/error states
- clean provider-root rendering at the first visible level

### Use stable ids

Stable node ids were essential.

Examples:

- `provider:<name>`
- `rules:<provider>`
- `ingestion:<provider>`

Child nodes also used stable id schemes.

Practical takeaway:

- stable ids make selection sync and node lookup reliable
- changing id conventions casually is high-risk

### Map status states into nodes

Loading, empty, and error states worked best as native tree nodes rather than special body UI.

That kept the whole experience tree-native and avoided falling back to custom layout blocks.

## Toolbar knowledge base

## What worked for native side-bar actions

For `Rules` and `Ingestion`, the working native pattern was:

- use Theia tab-bar toolbar contributions
- scope toolbar items by widget id
- keep visible actions in the toolbar, not the body

Relevant files:

- `src/Studio/Server/search-studio/src/browser/rules/search-studio-rules-toolbar-contribution.ts`
- `src/Studio/Server/search-studio/src/browser/ingestion/search-studio-ingestion-toolbar-contribution.ts`

## Symptom: toolbar command receives the wrong argument

### What we observed

A toolbar-backed command can receive the current widget as an argument.

If the command implementation assumes the first argument is a provider name string, it can misbehave.

### Fix

Defensively normalize the command argument.

The working pattern here was:

- treat the first argument as a provider name only if it is actually a string
- otherwise fall back to current provider resolution

Relevant file:

- `src/Studio/Server/search-studio/src/browser/search-studio-command-contribution.ts`

## Native-first UI guidance that held up well

The most successful rule from `065` was:

- use native Theia tree and toolbar infrastructure first
- avoid custom CSS unless there is a clear functional gap

This worked well because it:

- reduced implementation risk
- stayed theme-compatible
- made the shell feel more like a normal IDE/workbench
- reduced time spent on speculative styling

In these work packages, the final tree baseline remained CSS-free.

## Terms and wording that helped consistency

Using consistent terminology mattered.

The final naming pattern that read best was:

- `Providers navigation`
- `Rules navigation`
- `Ingestion navigation`

See also:

- `docs/theia_terminology_glossary.md`

## Repo-specific guidance worth reusing elsewhere

### Provider roots can still be actionable

A provider root does not need to be just a structural folder.

In this repository, the working pattern was:

- provider root opens an overview/dashboard document
- child nodes open more specific surfaces

That pattern worked well in all three work areas.

### Placeholder editors are acceptable in early workbench phases

Early Theia work can still deliver value when:

- navigation is real
- workbench structure is real
- data-backed trees are real
- editor surfaces are placeholders

This is a useful pattern when replacing simpler tools progressively.

## Recommended verification sequence for future Theia work

### Build verification

```powershell
yarn --cwd .\src\Studio\Server\search-studio build
node --test .\src\Studio\Server\search-studio\test
yarn --cwd .\src\Studio\Server build:browser
dotnet build .\src\Hosts\AppHost\AppHost.csproj
```

### Runtime verification

1. start `AppHost` in `runmode=services`
2. open `http://localhost:3000`
3. verify the expected Activity Bar items appear
4. open the relevant view
5. verify the tree renders
6. verify toolbar actions render if expected
7. verify root-node and child-node opening behavior
8. verify `Studio Output` receives expected logs

## Symptom -> likely cause -> fix quick table

| Symptom | Likely cause | Fix |
|---|---|---|
| View opens but tree is blank | model/widget init not fully wired, or stale browser bundle | ensure explicit model init and rebuild `build:browser` |
| Data loads in output but no rows appear | tree model has not produced a visible root/row path in runtime bundle | inspect root mapping, stable ids, and rebuild browser bundle |
| Icons missing in tree | default icon rendering not enough | override `renderIcon(...)` |
| Toolbar action opens wrong target | command assumed string arg but received widget | normalize first argument defensively |
| UI still shows old layout after code changes | only extension package was built | run `yarn --cwd .\src\Studio\Server build:browser` and restart |
| Native build fails unexpectedly in VS shell | inherited VS toolchain environment interferes | use clean PowerShell and let `build.ps1` clear toolchain env |

## Suggested reuse checklist for other solutions

If another solution adopts Theia, start with this checklist:

1. decide fixed local port strategy early
2. decide how Aspire or the host passes runtime API configuration into Theia
3. add a pre-build/incremental shell build path for Visual Studio `F5`
4. lock Node and Yarn versions early
5. keep a shared tree base widget for common behavior
6. use native `TreeWidget` and toolbar contributions before custom UI
7. document symptom-to-fix knowledge as it is discovered

## Key files in this repository

### Hosting and build integration

- `src/Hosts/AppHost/AppHost.cs`
- `src/Hosts/AppHost/appsettings.json`
- `src/Studio/StudioApiHost/StudioApiHost.csproj`
- `src/Studio/Server/build.ps1`

### Theia backend/runtime bridge

- `src/Studio/Server/search-studio/src/node/search-studio-backend-application-contribution.ts`
- `src/Studio/Server/search-studio/src/browser/search-studio-api-configuration-service.ts`

### Shared tree foundation

- `src/Studio/Server/search-studio/src/browser/common/search-studio-tree-types.ts`
- `src/Studio/Server/search-studio/src/browser/common/search-studio-tree-widget.ts`
- `src/Studio/Server/search-studio/src/browser/search-studio-frontend-module.ts`

### Concrete trees

- `src/Studio/Server/search-studio/src/browser/providers/*`
- `src/Studio/Server/search-studio/src/browser/rules/*`
- `src/Studio/Server/search-studio/src/browser/ingestion/*`

## Future additions to this page

As new work packages land, extend this page with:

- additional Theia-specific gotchas
- backend/frontend split decisions that proved important
- testing patterns that worked well for Theia extensions
- packaging/distribution lessons if the shell moves beyond local-only developer usage
