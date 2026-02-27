# FileShareEmulator (FSS Emulator) — Overview Specification (Draft)

**Work package:** `docs/001-fss-emulator/`

## 1. Purpose and scope
The `FileShareEmulator` is a simplified emulator of the production **File Share Service API (FSS API)**.

The emulator exists to support local development, integration testing, and/or non-production environments where the full production dependencies (notably blob storage and security) are not required.

This work package defines:
- The high-level goals and non-goals of the emulator.
- Constraints/limitations compared to production.
- The overall component shape and how it relates to the existing solution.
- References to detailed component specifications.

## 2. System context
### 2.1 Existing solution context
The production FSS API code referenced for parity and database behaviour is located in:
- `file-share-service/UKHO.FileShareService.API/UKHO.FileShareService.API/`
- `file-share-service/UKHO.FileShareService.API/UKHO.FileShareService.Common/`

The emulator project is located in:
- `src/UKHO.ADDS.Search.FileShareEmulator/`

### 2.2 External dependencies
- Database: the emulator **must** update the database in the same way as production FSS API.
- Storage:
  - Production uses blob storage.
  - Emulator uses local filesystem storage under `/data/content/`.

### 2.3 Users
- Developers running the system locally.
- Automated test pipelines (where applicable).

## 3. High-level requirements (summary only)
### 3.1 API compatibility
1. The emulator must expose **exactly the same public REST API** as production FSS API.
2. All request/response models, routes, query params, headers, and status code behaviours must match production.

### 3.2 Authentication/authorization
- No security is implemented.
- Every request is treated as maximum privilege.

### 3.3 Filesystem storage model
- Base storage root: `/data/content/{IDprefix}`
- `{IDprefix}` is the **most significant byte** of the `BatchId`.
- A committed batch is stored as: `/data/content/{IDprefix}/{BatchId}.zip`

### 3.4 Batch lifecycle storage behaviour
- On **create batch**: create a new folder under `/data/content/{IDprefix}` for the batch working directory.
- On **commit batch**:
  - zip the batch working directory into `{BatchId}.zip` in `/data/content/{IDprefix}`
  - delete the batch working directory

### 3.5 Database updates
- All database updates must happen exactly as they do in the production FSS API.

## 4. Component specifications
- `docs/001-fss-emulator/file-share-emulator.spec.md` — FileShareEmulator detailed functional + technical specification.
