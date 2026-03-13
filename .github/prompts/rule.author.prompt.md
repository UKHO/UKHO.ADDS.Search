# Rule authoring reference prompt (`ingestion-rules.json`)

Target output path: `docs/rule-authoring.md`

Copy/paste the prompt below into ChatGPT/Copilot Chat when you want help writing new rules for `src/Hosts/IngestionServiceHost/ingestion-rules.json`.

---

## Reference prompt

You are helping me author ingestion rules for `src/Hosts/IngestionServiceHost/ingestion-rules.json`.

Assume any new rule we create will be inserted into the `file-share` provider array under:

```json
{
  "schemaVersion": "1.0",
  "rules": {
    "file-share": [
      /* INSERT NEW RULE(S) HERE */
    ]
  }
}
```

### Context / constraints

- The rules file is JSON with shape:
  - top-level `schemaVersion`
  - top-level `rules` object keyed by provider (e.g. `file-share`)
  - provider value is an array of rule objects

- A rule object uses:
  - `id` (required, unique, kebab-case)
  - `description` (concise)
  - `if` predicate (preferred over `match`)
  - `then` actions

- **Do not use** `then.documentType` or `then.facets` (these are removed).

- Treat unsupported/unknown fields the same as any other incorrect field (no special handling).

- Only use supported actions in `then`:
  - `keywords.add`, `searchText.add`, `content.add`
  - Additional top-level `*.add` fields for canonical fields such as:
    - `authority.add`, `region.add`, `fornat.add`, `category.add`, `series.add`, `instance.add`
    - `majorVersion.add`, `minorVersion.add` (numbers)

- For runtime data that is missing, rules should simply not match / produce no outputs.

- For `majorVersion.add` and `minorVersion.add`:
  - values must be JSON numbers
  - if the source value is not guaranteed numeric, ask me how to parse/convert it

### What you must produce

- Work iteratively:
  1) Ask any required clarification question(s)
  2) Propose the current draft rule as a JSON object
  3) Ask: **“Is there more, or is the rule complete?”**
  4) Repeat until I explicitly confirm the rule is complete.

- Only when I confirm the rule is complete:
  - Update `src/Hosts/IngestionServiceHost/ingestion-rules.json` by inserting the rule object into the `rules.file-share` array.
  - Do not modify other providers.

- Ensure any JSON you produce is valid and properly escaped (e.g. `properties[\"week\"]`).

### How to interpret my instruction

When I describe a condition like:

- “when `properties[\"week\"]` exists”

Use an `if` block like:

```json
{ "all": [ { "path": "properties[\"week\"]", "exists": true } ] }
```

When I say:

- “put the value of `properties[\"X\"]` into `minorVersion` (and multiple others)”

Use:

- For string fields supporting templates:

```json
"fieldName": { "add": ["$path:properties[\"X\"]"] }
```

- For numeric fields (`majorVersion`, `minorVersion`):
  - ask me if `properties["X"]` is already a number, or how to derive a number
  - if it is numeric, emit a numeric literal in the `add` array

### Before writing the rule

Ask me ONLY the minimum required questions to avoid generating an invalid rule (usually numeric handling).

Now wait for my next instruction and then produce the rule JSON.
