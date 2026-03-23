# Studio Ingestion API and UI Specification (070-studio-ingestion)

## 1. Summary

This work lifts selected ingestion support capabilities out of `FileShareEmulator` and makes them available through `StudioApiHost` and the Studio Theia UI.

This work must bring to life the three existing ingestion placeholder pages in the Studio Theia UI:

- ingest by id
- ingest all unindexed
- ingest by context

The resulting design must preserve a strict boundary:

- Studio and `StudioApiHost` expose only provider-neutral ingestion concepts.
- Provider-specific storage, queue, and database/domain terminology remains encapsulated inside the relevant Studio provider implementation.
- No file-share terminology must leak into Studio UI labels, Studio API contracts, or host-level orchestration contracts.

For the current file-share provider, the provider will internally translate Studio concepts such as `context` into file-share-specific meanings such as business unit.

## 2. Goals

- Add a provider-neutral ingestion API surface to `StudioApiHost`.
- Support Studio UI workflows for:
  - fetch payload by id
  - enqueue fetched payload
  - run provider-wide ingestion for all currently unindexed items, with the meaning of "unindexed" owned by the provider
  - list contexts for a provider
  - run context-based ingestion
  - reset indexing status for all items or a specific context
- Support coarse asynchronous progress updates for long-running operations.
- Keep all file-share-specific concepts, identifiers, queue names, and SQL knowledge inside `UKHO.Search.Studio.Providers.FileShare`.
- Keep the implementation simple and development-focused.

## 3. Non-Goals

- No persistence of operation state across `StudioApiHost` restarts.
- No historical operation listing endpoint.
- No fine-grained diagnostics streamed to Studio.
- No provider-specific capability flags on `/providers`.
- No leaking of provider-specific DTOs, tables, or domain names into Studio API contracts.

## 4. Scope

### In Scope

- `StudioApiHost` ingestion minimal API endpoints.
- The host-side operation tracking and SSE progress streaming.
- Provider-neutral DTOs.
- Theia ingestion page UX and interaction model, specifically the three existing placeholder ingestion pages for by id, all unindexed, and by context.
- File-share provider translation of Studio API concepts to current file-share database and queue behavior.

### Out of Scope

- Changes to the ingestion queue message schema.
- Runtime ingestion pipeline changes beyond consuming the same payloads already produced today.
- Production-grade durable operation stores.
- Multi-consumer API scenarios beyond Studio.

## 5. Terminology

### Studio-Neutral Terms

- **Provider**: a registered Studio provider such as `file-share`.
- **Id**: a provider-defined unique identifier for one fetchable payload source item.
- **Payload**: the JSON body that can be enqueued for ingestion.
- **Context**: a provider-defined string identifier representing a grouping or targeting concept.
- **Operation**: a long-running mutating ingestion activity tracked by `StudioApiHost`.

### File-Share Internal Mapping

For the file-share Studio provider only:

- `context` maps internally to business unit.
- provider-wide `all` ingestion maps internally to the existing file-share "index all pending/unindexed" behavior.
- context discovery returns business unit identifiers and business unit display names.
- reset-by-context maps internally to reset-by-business-unit.
- context indexing maps internally to the existing "index all in this business unit" behavior.

These mappings must remain internal to the provider implementation.

## 6. Architectural Constraints

### 6.1 Encapsulation Boundary

Studio contracts must remain provider-neutral. The following must not appear in public Studio API contracts or Studio UI labels unless they are internal provider implementation details:

- `Batch`
- `BusinessUnit`
- `IndexStatus`
- queue names
- table names
- SQL schema details
- blob container naming rules

### 6.2 Composition Root and DI Boundary

`StudioApiHost` is the composition root.

`StudioApiHost` must register shared infrastructure clients for dependency injection, including:

- SQL server client / SQL connection support
- blob client
- queue client

For this work, `StudioApiHost` already has the required DI setup for SQL server, Azure Blob, and Azure Queue in the same overall style as `IngestionServiceHost`, and that existing host-level wiring must be used by the file-share Studio provider rather than introducing separate bespoke client registration.

The provider implementation remains responsible for:

- provider-specific queue names
- provider-specific blob naming/container rules
- provider-specific database queries and schema knowledge
- translation between Studio-neutral DTOs and provider/domain concepts

### 6.3 API Placement

A new ingestion minimal API must be added to `StudioApiHost`.

- ingestion endpoints must live in their own API class under an `Api` namespace
- existing minimal APIs should follow the same class-per-endpoint-group approach as this work evolves

## 7. Provider Assumptions

All Studio providers are assumed to support the same ingestion capabilities:

- fetch by id
- submit payload
- run provider-wide ingestion for all currently unindexed items, with provider-owned interpretation of "unindexed"
- list contexts
- run context operation
- reset indexing status for all
- reset indexing status for a context

Because this is assumed for all providers, `/providers` does not need to expose ingestion capability flags.

## 8. API Surface

## 8.1 Provider Discovery

### `GET /providers`

Returns provider metadata only.

#### Response

```json
[
  {
    "name": "file-share",
    "displayName": "File Share",
    "description": "Ingests content sourced from File Share."
  }
]
```

No ingestion capability flags are required in this response.

## 8.2 Context Discovery

### `GET /ingestion/{provider}/contexts`

Returns provider contexts sorted by `displayName` ascending.

#### Response Shape

```json
{
  "provider": "file-share",
  "contexts": [
    {
      "value": "12",
      "displayName": "Admiralty",
      "isDefault": false
    }
  ]
}
```

#### Rules

- `value` is the stable opaque identifier used by the API.
- `displayName` is the user-facing label shown in Studio.
- both `value` and `displayName` are strings in the public API.
- providers may map `value` back to typed internal identifiers.
- for file-share:
  - `value = businessUnitId.ToString()`
  - `displayName = businessUnitName`
- contexts must be sorted by `displayName` ascending.

## 8.3 Fetch Payload by Id

### `GET /ingestion/{provider}/{id}`

Fetches the payload for a provider-defined id.

#### Response Shape

```json
{
  "id": "d67ec6e6-735a-4dbf-8b46-0f4b95a7d9a8",
  "payload": {
    "...": "provider-defined ingestion payload"
  }
}
```

#### Rules

- `provider + id` is sufficient to identify the fetch target.
- `id` uniqueness is provider-defined.
- the fetched body must be directly reusable as the request body for `POST /ingestion/{provider}/payload` without client-side transformation.
- the response must remain provider-neutral even though the payload body is opaque JSON.

## 8.4 Submit Payload

### `POST /ingestion/{provider}/payload`

Enqueues a payload for ingestion.

#### Request Shape

```json
{
  "id": "d67ec6e6-735a-4dbf-8b46-0f4b95a7d9a8",
  "payload": {
    "...": "provider-defined ingestion payload"
  }
}
```

#### Success Response Shape

```json
{
  "accepted": true,
  "message": "Payload submitted successfully."
}
```

#### Rules

- the host/provider must attempt the queue write before returning success.
- if the queue write fails, Studio must be notified immediately via an error response.
- this endpoint is synchronous from the API consumer perspective.
- this endpoint is blocked by the global active-operation lock.

## 8.5 Start Provider-Wide Ingestion

### `PUT /ingestion/{provider}/all`

Starts a long-running provider-wide ingestion operation for all items the provider currently considers unindexed.

For the file-share provider, this is the provider-neutral route for the current file-share "index all pending/unindexed" behavior.

#### Accepted Response Shape

```json
{
  "operationId": "4a53becc-c436-45d2-a6b3-4030b19ca5b7",
  "provider": "file-share",
  "operationType": "index-all",
  "context": null,
  "status": "queued"
}
```

#### Rules

- returns `202 Accepted` when the operation is accepted.
- the operation is tracked in memory by `StudioApiHost`.
- the provider owns the meaning of "all" and of "unindexed" for this route.
- for file-share, this means all currently pending/unindexed batches according to the file-share provider implementation.
- this endpoint is blocked by the global active-operation lock.

## 8.6 Start Context Ingestion

### `PUT /ingestion/{provider}/context/{context}`

Starts a long-running provider context ingestion operation.

For the file-share provider, this is the provider-neutral route for the current "index all in this business unit" behavior.

#### Accepted Response Shape

```json
{
  "operationId": "4a53becc-c436-45d2-a6b3-4030b19ca5b7",
  "provider": "file-share",
  "operationType": "context-index",
  "context": "12",
  "status": "queued"
}
```

#### Rules

- returns `202 Accepted` when the operation is accepted.
- the operation is tracked in memory by `StudioApiHost`.
- `context` is a provider-neutral string identifier.
- for file-share, the provider interprets `context` as business unit id.
- this endpoint is blocked by the global active-operation lock.

## 8.7 Reset Indexing Status for All

### `POST /ingestion/{provider}/operations/reset-indexing-status`

Starts a long-running provider-wide reset operation.

#### Accepted Response Shape

Same as other accepted-operation responses, for example:

```json
{
  "operationId": "0c7dcb40-f23a-4d89-ad9b-005e7989c687",
  "provider": "file-share",
  "operationType": "reset-indexing-status",
  "context": null,
  "status": "queued"
}
```

#### Rules

- this is provider-wide reset.
- this endpoint is blocked by the global active-operation lock.

## 8.8 Reset Indexing Status for a Context

### `POST /ingestion/{provider}/context/{context}/operations/reset-indexing-status`

Starts a long-running reset operation scoped to a context.

#### Accepted Response Shape

```json
{
  "operationId": "1ec568ba-1e63-4e2b-b3bb-7c06bbfb7d23",
  "provider": "file-share",
  "operationType": "reset-indexing-status",
  "context": "12",
  "status": "queued"
}
```

#### Rules

- this is context-scoped reset.
- for file-share, the provider interprets `context` as business unit id.
- this endpoint is blocked by the global active-operation lock.

## 8.9 Active Operation Discovery

### `GET /operations/active`

Returns the current active long-running operation.

#### Response Shape

```json
{
  "operationId": "4a53becc-c436-45d2-a6b3-4030b19ca5b7",
  "provider": "file-share",
  "operationType": "context-index",
  "context": "12",
  "status": "running",
  "message": "Processing operation.",
  "completed": 250,
  "total": 1000,
  "startedUtc": "2026-01-01T10:00:00Z",
  "completedUtc": null,
  "failureCode": null
}
```

#### Rules

- returns `404 Not Found` if there is no active operation.
- exists so Studio can rediscover the single active operation after reload/reconnect.
- does not need to include `eventsUrl`; Studio can construct it from `operationId`.

## 8.10 Operation Status Lookup

### `GET /operations/{operationId}`

Returns the latest known state of a tracked operation.

#### Response Shape

```json
{
  "operationId": "4a53becc-c436-45d2-a6b3-4030b19ca5b7",
  "provider": "file-share",
  "operationType": "context-index",
  "context": "12",
  "status": "running",
  "message": "Processing operation.",
  "completed": 250,
  "total": 1000,
  "startedUtc": "2026-01-01T10:00:00Z",
  "completedUtc": null,
  "failureCode": null
}
```

## 8.11 Operation Events

### `GET /operations/{operationId}/events`

Server-Sent Events endpoint for coarse live progress updates.

#### Event Payload Shape

```json
{
  "eventType": "progress",
  "operationId": "4a53becc-c436-45d2-a6b3-4030b19ca5b7",
  "status": "running",
  "message": "Processed 250 of 1000.",
  "completed": 250,
  "total": 1000,
  "timestampUtc": "2026-01-01T10:05:00Z",
  "failureCode": null
}
```

#### Rules

- streams only new live events.
- does not replay prior event history.
- clients that recover after reload/reconnect should use:
  - `GET /operations/active`, or
  - `GET /operations/{operationId}`
  before subscribing for new events.
- when the operation reaches `succeeded` or `failed`, the host must emit the final event and then close the SSE stream.

## 9. DTO Rules

### 9.1 Neutrality

All non-payload DTO fields must be Studio-neutral.

### 9.2 Wrapped Payloads

Use wrapped DTOs rather than raw JSON responses for fetch and submit so that the API can evolve without breaking the client.

### 9.3 Provider Not Repeated in Request Body

`provider` is carried by the route and does not need to be duplicated inside request DTOs.

## 10. Error Handling and Status Codes

### 10.1 Request Validation Errors

Return `400 Bad Request` for:

- unknown `provider`
- unknown `context`
- malformed request body
- invalid payload shape
- other client contract errors

Rationale:

- Studio is the only consumer of `StudioApiHost`
- provider and context values come from controlled Studio dropdowns/API discovery
- an unknown provider or context indicates a client contract issue, not a missing resource

### 10.2 Missing Fetch Target

Return `404 Not Found` for:

- unknown `id` for a known provider on `GET /ingestion/{provider}/{id}`

Studio should present a suitable user-facing not-found message.

### 10.3 Active Operation Conflict

Return `409 Conflict` when a mutating ingestion call is blocked because another operation is already active.

#### Conflict Response Shape

```json
{
  "message": "Another ingestion operation is already active.",
  "activeOperationId": "4a53becc-c436-45d2-a6b3-4030b19ca5b7",
  "activeProvider": "file-share",
  "activeOperationType": "context-index"
}
```

### 10.4 Infrastructure and Provider Failures

Return `5xx` for provider/infrastructure failures such as:

- queue write failures
- storage failures
- database failures
- unexpected provider exceptions

## 11. Operation Lifecycle

### 11.1 Status Values

The following coarse status values are required:

- `queued`
- `running`
- `succeeded`
- `failed`

### 11.2 Progress Detail Level

Progress streaming must remain coarse only. Examples:

- lifecycle changes
- periodic `completed` / `total` updates
- final success/failure

Fine-grained provider diagnostics are not required in the Studio API. Developers can use Aspire logs for detailed troubleshooting.

### 11.3 Failure Codes

Failed operation state/events must include:

- `message`
- `failureCode`

The code must be machine-readable and stable enough to assist diagnosis.

Illustrative values include:

- `unknown-provider`
- `unknown-context`
- `item-not-found`
- `operation-conflict`
- `payload-invalid`
- `queue-write-failed`
- `database-error`
- `provider-error`
- `unexpected-error`

### 11.4 Operation Store Lifetime

The operation store is in-memory only.

Rules:

- active operations remain in memory for their entire runtime, however long they take
- completed operations remain available in memory until `StudioApiHost` restarts
- no time-based eviction is required
- no persistence across restart is required
- no operation listing endpoint is required

## 12. Global Operation Locking

A single global lock applies across all providers.

### 12.1 Lock Rule

While any long-running operation is `queued` or `running`, all mutating ingestion endpoints must be rejected.

### 12.2 Mutating Endpoints Blocked by the Lock

- `POST /ingestion/{provider}/payload`
- `PUT /ingestion/{provider}/context/{context}`
- `POST /ingestion/{provider}/operations/reset-indexing-status`
- `POST /ingestion/{provider}/context/{context}/operations/reset-indexing-status`

### 12.3 Read Endpoints Allowed During the Lock

- `GET /ingestion/{provider}/{id}`
- `GET /ingestion/{provider}/contexts`
- `GET /operations/active`
- `GET /operations/{operationId}`
- `GET /operations/{operationId}/events`

## 13. Studio Theia UI Requirements

## 13.1 General Enablement Rule

Each relevant ingestion screen must contain a provider dropdown.

Until a provider is selected:

- no ingestion control on the screen should be enabled

Because all providers are assumed to have the same ingestion capabilities, provider selection controls availability globally without per-provider capability flags.

## 13.2 Fetch and Enqueue by Id UI

The ingest-by-id screen must provide:

- a provider dropdown
- an id text box
- a `Fetch` button
- an `Index` button
- a read-only Monaco editor on the right of the page

Behavior:

- `Fetch` calls `GET /ingestion/{provider}/{id}`
- the returned payload is shown in the read-only Monaco editor
- `Index` submits that same fetched body to `POST /ingestion/{provider}/payload`
- the fetch response body must therefore be directly reusable for indexing without transformation

## 13.3 All-Unindexed UI

The ingest-all-unindexed screen must correspond to the existing Studio Theia placeholder page for provider-wide ingestion and must be brought to life by this work.

The ingest-all-unindexed screen must:

- provide a provider dropdown
- provide an action to start provider-wide ingestion for all items the provider considers unindexed
- call `PUT /ingestion/{provider}/all`
- show coarse progress and final result using the shared operation model

For file-share this means the page lifts the current "index all pending/unindexed" behavior into Studio without exposing file-share terminology in the UI.

## 13.4 Context-Based UI

The context-based ingestion screen must correspond to the existing Studio Theia placeholder page for by-context ingestion and must be brought to life by this work.

The context-based ingestion UI must:

- call `GET /ingestion/{provider}/contexts`
- show the provider contexts using `displayName`
- use the selected `value` when calling context-based API routes

For file-share this means Studio displays business unit names but uses business unit ids internally as opaque string context values.

## 13.5 Reset Controls

The UI must support both:

- reset indexing status for all
- reset indexing status by context

These actions must call the corresponding neutral API routes and must not expose file-share terminology in Studio.

## 13.6 Active Operation UX

When an operation is active, Studio should be able to:

- discover it via `GET /operations/active`
- show coarse progress
- subscribe to live events using `GET /operations/{operationId}/events`
- recover after reload/reconnect by rediscovering the active operation

When the final event is received and the stream closes, Studio should use `GET /operations/{operationId}` for any further readback of final state.

## 14. File-Share Provider Responsibilities

The file-share Studio provider must encapsulate all file-share-specific behavior required to support these neutral APIs, including:

- fetching the payload for a file-share id from the database
- creating the same ingestion payload JSON currently produced by `FileShareEmulator`
- queue submission for the provider’s queue
- interpreting provider-wide `PUT /ingestion/{provider}/all` as the current file-share index-all-pending/unindexed behavior
- listing business units and mapping them to neutral contexts
- interpreting context-based indexing as business-unit indexing
- interpreting context-scoped reset as business-unit-scoped reset
- interpreting provider-wide reset as reset-all

The host must not know:

- the file-share queue name
- the poison queue naming convention
- the SQL table/query details
- the meaning of context for file-share

## 15. Observability

- use coarse operation state and progress data for Studio notifications
- use host/provider logging for deeper diagnostics
- developers can inspect Aspire logs from `StudioApiHost` for detailed troubleshooting

## 16. Acceptance Criteria

### API Contract

- `StudioApiHost` exposes the neutral ingestion API routes defined in this spec.
- fetch-by-id returns a wrapped payload body reusable for submit.
- provider-wide `PUT /ingestion/{provider}/all` starts the neutral index-all operation for all currently unindexed items as defined by the provider.
- context discovery returns string `value` and `displayName` pairs sorted ascending by `displayName`.
- unknown provider/context returns `400`.
- unknown id on fetch returns `404`.
- mutating calls are blocked with `409` when another operation is active.

### Operation Model

- provider-wide index-all, context indexing, and reset actions run asynchronously and return `operationId`.
- context indexing and reset actions run asynchronously and return `operationId`.
- only one active operation can exist globally.
- operation state is stored in memory only.
- completed operations remain available until host restart.
- `GET /operations/active` returns the current active operation or `404` when none exists.
- SSE streams only new events and closes after the terminal event.

### Encapsulation

- public API DTOs remain provider-neutral.
- file-share-specific terminology does not appear in Studio UI or public Studio API contracts.
- provider-specific queue/database naming remains inside the provider implementation.

### UI

- the three existing Studio Theia ingestion placeholder pages for by id, all unindexed, and by context are brought to life by this work.
- provider must be selected before controls are enabled.
- fetch by id displays the fetched payload in a read-only Monaco editor.
- index reuses the fetched payload body unchanged.
- the all-unindexed page starts provider-wide ingestion through `PUT /ingestion/{provider}/all`.
- reset-all and reset-by-context are both supported.
- Studio can recover active operation state after reload/reconnect.

## 17. Open Questions

None at this stage. The API and UI contract described here reflects the clarified decisions captured during specification discussion.
