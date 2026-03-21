# Eclipse Theia Instructions

## Purpose
- Use this document when researching or designing Eclipse Theia extensions or Theia-based applications.
- Prefer authoritative and current sources.
- Preserve a clear decision order before implementation starts.

## Required decision order
1. Decide whether the work should be implemented as a **Theia extension** or a **VS Code extension**.
2. Decide whether the feature belongs in the **frontend**, **backend**, or **both**.
3. Identify the closest built-in Theia feature or example to reuse as a pattern.

Do not start from implementation details before answering those questions.

## Source priority
Use sources in this order unless there is a specific reason not to:
1. Official Theia extension documentation
2. Theia API documentation and source
3. Working example repositories
4. Open VSX and VS Code compatibility references

Rationale:
- Current Theia guidance distinguishes between **Theia extensions** and **VS Code extensions**.
- Older Theia plug-in guidance is deprecated.
- The first task is to choose the correct extension mechanism, not to start coding immediately.

## Primary sources

### Official Theia extension authoring
- https://theia-ide.org/docs/authoring_extensions/

Use this for:
- getting started
- understanding extension structure
- learning the recommended development workflow

### The extension model overview
- https://theia-ide.org/docs/extensions/

Use this for:
- choosing the right extension model
- understanding current supported approaches
- avoiding deprecated paths

### Composing applications
- https://theia-ide.org/docs/composing_applications/

Use this for:
- product-level customization
- bundling extensions into your own application
- understanding app composition instead of isolated extensions

### Architecture
- https://theia-ide.org/docs/architecture/

Use this for:
- deciding whether code belongs in frontend or backend
- understanding the role of the Theia backend
- learning how services communicate

### Theia API docs
- https://eclipse-theia.github.io/theia/docs/next/index.html

Use this for:
- finding classes, interfaces, services, and contribution points
- checking signatures and package layout
- moving from conceptual docs into implementation

### Main Theia repository
- https://github.com/eclipse-theia/theia

Use this for:
- finding real examples inside the platform
- understanding how built-in features are implemented
- resolving gaps left by higher-level docs

### Example extensions repository
- https://github.com/eclipsesource/eclipse-theia-examples

Use this for:
- working examples
- extension scaffolding patterns
- learning by adaptation rather than from scratch

### Theia Blueprint and example app
- https://github.com/eclipsesource/theia-example
- https://theia-ide.org/docs/blueprint_documentation/

Use this for:
- product shell customization
- branding and application-level structure
- seeing how a larger Theia-based tool is put together

### Authoring VS Code extensions for Theia
- https://theia-ide.org/docs/authoring_vscode_extensions/

Use this for:
- deciding whether compatibility with VS Code matters
- using the VS Code extension model instead of the native Theia one
- understanding cross-compatibility strategy

### Open VSX and VS Code extension support
- https://theia-ide.org/docs/user_install_vscode_extensions/
- https://open-vsx.org/
- https://theia-ide.org/docs/faq/

Use this for:
- extension distribution
- compatibility questions
- packaging and install expectations

## Supporting documentation
- https://theia-ide.org/docs/

Use this broader documentation entry point for detailed information about:
- commands
- menus
- keybindings
- widgets
- preferences
- frontend/backend contributions
- service wiring

## Recommended working set
Keep these open together when actively building a Theia extension:
- https://theia-ide.org/docs/authoring_extensions/
- https://theia-ide.org/docs/extensions/
- https://theia-ide.org/docs/architecture/
- https://eclipse-theia.github.io/theia/docs/next/index.html
- https://github.com/eclipse-theia/theia
- https://github.com/eclipsesource/eclipse-theia-examples
- https://theia-ide.org/docs/composing_applications/
- https://theia-ide.org/docs/authoring_vscode_extensions/

This working set should cover:
- official guidance
- architectural boundaries
- API discovery
- implementation examples
- source-level investigation

## Use-case starting points

### Building a product feature panel or widget in Theia
Start with:
- https://theia-ide.org/docs/authoring_extensions/
- https://theia-ide.org/docs/architecture/
- https://eclipse-theia.github.io/theia/docs/next/index.html
- https://github.com/eclipsesource/eclipse-theia-examples

Reason:
- extension authoring flow is required
- frontend/backend placement must be understood early
- API references are needed immediately
- concrete examples accelerate implementation

### Building a full Theia-based product
Start with:
- https://theia-ide.org/docs/composing_applications/
- https://theia-ide.org/docs/blueprint_documentation/
- https://github.com/eclipsesource/theia-example
- https://github.com/eclipse-theia/theia

Reason:
- this is an application-composition problem, not just an extension problem
- product structure matters more than isolated extension mechanics
- larger examples are more useful than small samples

### Deciding whether this should be a VS Code extension instead
Start with:
- https://theia-ide.org/docs/extensions/
- https://theia-ide.org/docs/authoring_vscode_extensions/
- https://theia-ide.org/docs/user_install_vscode_extensions/
- https://open-vsx.org/

Reason:
- the extension model must be chosen before implementation
- VS Code compatibility may be the simplest strategic option

## Research workflow

### Phase 1: Decide the extension type
Use:
- https://theia-ide.org/docs/extensions/
- https://theia-ide.org/docs/authoring_extensions/
- https://theia-ide.org/docs/authoring_vscode_extensions/

Answer these questions:
- Is this best as a native Theia extension?
- Is this better as a VS Code extension?
- Is this really an application-composition problem?

This decision affects packaging, APIs, portability, and the amount of Theia-specific code required.

### Phase 2: Decide frontend vs backend placement
Use:
- https://theia-ide.org/docs/architecture/
- https://github.com/eclipse-theia/theia

Answer these questions:
- Should this feature live in the frontend?
- Does it need a backend contribution?
- Is it mostly a widget, panel, or command?
- Does it need server-side services or filesystem/process integration?

Treat this as a primary architecture decision.

### Phase 3: Find the right APIs
Use:
- https://eclipse-theia.github.io/theia/docs/next/index.html
- https://github.com/eclipse-theia/theia

Guidance:
- Use the API docs to discover what exists.
- Use the repository to understand how the APIs are used in practice.
- Prefer the combination of API reference plus source example over either source alone.

### Phase 4: Find a nearby working example
Use:
- https://github.com/eclipsesource/eclipse-theia-examples
- https://github.com/eclipse-theia/theia
- https://github.com/eclipsesource/theia-example
- https://theia-ide.org/docs/blueprint_documentation/

Guidance:
- Reuse the pattern of an existing widget, contribution, or command when possible.
- Prefer adaptation of a nearby example over inventing a structure from scratch.

## Research standards
- Prefer official docs, generated API docs, official source, and maintained example repositories.
- Avoid relying on random blog posts, outdated forum threads, or old tutorials unless authoritative sources do not answer the question.
- Give extra weight to current official guidance because Theia extension mechanisms and recommended approaches have changed over time.

## High-value bookmarks
Use this minimal bookmark set when you need only the highest-value sources:
1. https://theia-ide.org/docs/authoring_extensions/
2. https://theia-ide.org/docs/extensions/
3. https://theia-ide.org/docs/architecture/
4. https://eclipse-theia.github.io/theia/docs/next/index.html
5. https://github.com/eclipse-theia/theia
6. https://github.com/eclipsesource/eclipse-theia-examples
7. https://theia-ide.org/docs/composing_applications/
8. https://theia-ide.org/docs/authoring_vscode_extensions/

## Final checklist
Before proposing or implementing a Theia solution, answer these questions in order:
1. Should this be a Theia extension or a VS Code extension?
2. Does the feature belong in the frontend, backend, or both?
3. Which built-in Theia feature is closest to the desired behavior?

Use the sources in this document to answer those questions with the least wasted effort.
