# FileShareEmulator (FSS Emulator) — Component Specification (Draft)

**Target output path:** `docs/001-fss-emulator/file-share-emulator.spec.md`

## 1. Purpose
Provide a simplified emulator of the production **File Share Service API (FSS API)** that:
- Exposes **exactly the same public REST API** as the production FSS API.
- Removes security requirements for development/testing convenience.
- Replaces blob storage with a deterministic filesystem-based storage layout.
- Preserves production-aligned **database update behaviour**.

## 2. Goals
- API parity with production FSS API.
- Predictable, local filesystem-backed batch storage.
- Minimal behavioural differences, restricted to documented limitations.

## 3. Non-goals
- Implementing authentication, authorization, or any security model.
- Emulating blob storage semantics.
- Providing production-grade scalability/performance.

## 4. Constraints and mandatory behaviours
### 4.1 REST API parity (hard requirement)
- The emulator must provide **exactly the same public REST API** as production FSS API.
- “Same API” includes:
  - routes/paths
  - HTTP methods
  - request/response contracts (JSON shapes, required/optional fields)
  - response status codes and error shapes
  - headers required/returned (where applicable)
  - media types

**Source of truth:** the production FSS API controllers.

**Hard constraint:** the emulator **must not** reference/link to production code in any way (no project references, shared assemblies, shared controller packages). Controllers and endpoint registrations must be replicated within the emulator project.

**Project boundary constraint:** all emulator implementation code must live within the existing emulator project.

**No cross-project references:** the emulator project must not reference any other projects (no project references at all), due to Docker constraints.

Additional endpoints:
- The emulator must provide all endpoints that the real service provides.
- The emulator may provide extra, emulator-only endpoints in addition to the production surface, but these are **not required** right now and are **out of scope** for this work package.

### 4.2 No security
- All endpoints are callable without credentials.
- All operations are permitted.

Authorization responses:
- **Decision:** the emulator must not enforce authorization and should not return 401/403 due to missing/invalid credentials. All calls assume maximum privilege.

### 4.3 Storage model (filesystem)
- Base path: `/data/content/{IDprefix}`
- `{IDprefix}` is the **most significant byte** of the `BatchId`.
- Committed batch materialization:
  - zip file at `/data/content/{IDprefix}/{BatchId}.zip`

**Decision:** the filesystem base path is fixed at `/data/content`.

### 4.4 Batch directory lifecycle
#### 4.4.1 Create batch
When a batch is created:
- A working directory for the batch is created under `/data/content/{IDprefix}`.
- The working directory name is exactly `{BatchId}`.

In-progress file writes (prior to commit):
- Files are written directly into `/data/content/{IDprefix}/{BatchId}/...`.

#### 4.4.2 Commit batch
When a batch is committed:
- The batch working directory is zipped to `/data/content/{IDprefix}/{BatchId}.zip`.
- The original batch working directory is deleted after successful zip creation.
- The zip file contains **only the contents** of the batch working directory (i.e., files/folders are at the zip root; the `{BatchId}` folder name is not included as a top-level entry).

If `/data/content/{IDprefix}/{BatchId}.zip` already exists at commit time:
- The request must fail (conflict) and must not modify the existing zip.

If zip creation succeeds but deletion of the working directory fails:
- The request returns success and the emulator must log the cleanup failure.

### 4.5 Database updates (hard requirement)
- The emulator must perform database updates **identically** to production FSS API.
- The emulator should reuse production DAL/business logic where possible (e.g., via `UKHO.FileShareService.Common`).

## 5. Functional requirements (to be confirmed)
### 5.1 Batch operations
- Create batch
- Upload/add files to an uncommitted batch
- Commit batch
- Retrieve/download batch content
- List/query batches (if present in production API)
- Delete batch (if present in production API)

> Open point: the exact endpoint set must be derived from the production API.

### 5.2 Error handling and validation
- Validation rules (e.g., batch state transitions) must match production.
- Error response contracts must match production.

### 5.3 Limits
**Decision:** the emulator does not enforce production limits (file size, batch size, file count), other than what the filesystem and host environment inherently impose.

### 5.4 Concurrency
**Decision:** the emulator supports concurrent uploads/writes to the same batch.

Implication:
- The implementation must define a locking strategy to avoid corruption (e.g., per-batch and/or per-file locking, and consistent handling of concurrent writes to the same path).

Same-path concurrent writes:
- **Decision:** first write wins; subsequent concurrent writes to the same relative path within a batch fail with conflict.

## 6. Technical design (proposed)
### 6.1 Approach: replicate production controllers without linking
- Replicate the production controller set into the emulator project.
- Keep controller class names, routes, and action signatures aligned to production.
- Replicate request/response DTOs as needed (subject to the same “no linking to production code” constraint).
- Maintain parity via automated checks (e.g., OpenAPI snapshot comparison between prod and emulator builds) rather than shared code.

### 6.2 Storage abstraction
If production uses an abstraction for storage (e.g. blob client wrapper), implement an emulator version that:
- Streams to/from the filesystem paths described above.
- Ensures commit semantics (zip + delete folder).

### 6.3 Determining `{IDprefix}`
- `BatchId` is a `Guid` emitted as a string (e.g., `d85b1407-351d-4694-9392-03acc5870eb1`).
- `{IDprefix}` = the most significant byte of the `BatchId`.

**Canonical derivation:** use the same method as `FileShareImageBuilder.ContentImporter.GetMostSignificantByteHex(Guid)`:
- Obtain the 16-byte representation using `Guid.TryWriteBytes(Span<byte>)` (or `Guid.ToByteArray()` fallback).
- `{IDprefix}` is `bytes[0]` formatted as two-character uppercase hex (`X2`).

> Open point: confirm `BatchId` type and canonical string format used in paths.

### 6.4 Zip implementation
- Use `System.IO.Compression.ZipFile` / `ZipArchive`.
- Ensure atomicity best-effort:
  - write zip to temp file then move into place.
  - delete working directory only after move succeeds.

## 7. Operational considerations
- Storage root `/data/content` must exist and be writable by the emulator process.
- Logging should be sufficient to diagnose:
  - batch folder creation
  - commit zip creation
  - delete failures

Health endpoint:
- The emulator already has a health endpoint and this functionality must be preserved.

## 8. Database connectivity and persistence approach
**Decision:** use the Aspire SQL client that is already set up in this solution for database connectivity.

Persistence implementation preferences:
- Prefer creating simple POCO entity classes.
- Prefer using Entity Framework (EF Core) for database access and updates.

Constraint:
- Despite using EF Core in the emulator, the *effects* of all database updates must match production FSS API behaviour.

Clarification on “match production”:
- Updates must occur in the same order as production.
- The same values must be updated to the same resulting values as production.

## 9. Open questions
1. What is the canonical `BatchId` type and format (GUID? string? long?) used by production, and how should it map to `{BatchId}.zip`?
2. What is the exact list of production endpoints (routes/methods) that must be mirrored?
3. On commit, should the zip include the folder itself as the top-level entry, or only its contents?
4. What should happen if a commit is attempted twice (idempotency expectations)?
5. What environment(s) must this run on (Windows dev box, Linux container)? The path `/data/content` implies Linux/container.

## 10. Production DB parity approach
**Decision:** replicate production’s database update logic by inspecting the production implementation (code and/or migrations) and implementing equivalent behaviour in the emulator.

Parity definition:
- Ensure updates occur in the same order as production.
- Ensure the same values are updated to the same resulting values as production.

## 11. API inventory and traceability
**Decision:** maintain a human-readable API inventory in this document.

The emulator implementation must replicate the production controller set (without linking to production code). For traceability, this spec will include an “API inventory” section listing **all** production public endpoints (verb + route) and their request/response contracts.

Endpoint discovery approach:
- **Decision:** the production endpoint set must be identified by thoroughly inspecting the production source (controllers + routing configuration) within `UKHO.FileShareService.API`.

Non-controller endpoints:
- The emulator must also replicate non-controller HTTP endpoints configured in production routing (e.g., health/heartbeat endpoints registered via endpoint routing), as part of providing the same public HTTP surface.

## 12. API inventory (production)

> Note: this is derived by inspecting the production source in `file-share-service/UKHO.FileShareService.API/UKHO.FileShareService.API`.

### 12.1 Non-controller endpoints
- `GET /health`
- `GET /heartbeat`

Health checks in the emulator:
- **Decision:** the emulator health check(s) must remain **exactly as implemented in the existing emulator code** (no changes to health check routes, behaviour, or underlying checks as part of this work).

Existing UI/routes:
- **Decision:** leave the existing Blazor UI endpoints and routing exactly as-is.

### 12.1.1 Swagger endpoints
Production exposes Swagger endpoints (e.g., `/swagger/*`) via `UseSwagger()`/`UseSwaggerUI()`.

**Decision:** the emulator does not expose Swagger endpoints.

### 12.2 Controller endpoints (initial list)

#### 12.2.1 `BatchController`
- `POST /batch` (OperationId: `startBatch`)
- `PUT /batch/{batchId}` (OperationId: `commitBatch`)
- `DELETE /batch/{batchId}` (OperationId: `rollbackBatch`)
- `GET /batch/{batchId}/status` (OperationId: `getBatchStatus`)
- `PUT /batch/{batchId}/expiry` (OperationId: `setExpiryDate`)
- `GET /batch/{batchId}` (OperationId: `getBatchDetails`)
- `GET /batch` (OperationId: `getBatches`)
- `GET /attributes` (OperationId: `getBatchAttributesList`)
- `GET /attributes/search` (OperationId: `getBatchAttributesAndValuesForSearch`)

#### 12.2.2 `FileController`
- `POST /batch/{batchId}/files/{fileName}` (OperationId: `addFileToBatch`)
- `PUT /batch/{batchId}/files/{fileName}` (OperationId: `putBlocksInFile`)
- `GET /batch/{batchId}/files/{filename}` (OperationId: `getFile`)
- `GET /batch/{batchId}/files` (SwaggerOperation: `GetFilesAsZip`)

#### 12.2.3 `FileBlockController`
- `PUT /batch/{batchId}/files/{fileName}/{blockId}` (OperationId: `uploadBlockOfFile`)

#### 12.2.4 `AclController`
- `POST /batch/{batchId}/acl` (OperationId: `appendAcl`)
- `GET /batch/{batchId}/acl` (OperationId: `getAcl`)
- `PUT /batch/{batchId}/acl` (OperationId: `replaceAcl`)
