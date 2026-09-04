# Trajectory Service

`Service` is the ASP.NET Core microservice for Trajectory. Its code namespace root is `OSDC.Drilling.Trajectory.Service`.

It exposes the Trajectory API and depends on the `Model` project for the domain model and computation logic.

## Responsibilities

- expose CRUD endpoints for trajectory data
- expose trajectory interpolation cases
- expose trajectory realization cases
- persist data in SQLite
- run trajectory realization calculations asynchronously so long-running cases do not block the request that creates or updates the case

## Container

The service is packaged as the Docker image:

`docker.io/digiwells/osdcdrillingtrajectoryservice:stable`

It is published under the `digiwells` organization:

https://hub.docker.com/?namespace=digiwells

## Endpoints

OpenAPI / Swagger:

https://dev.digiwells.no/Trajectory/api/swagger

https://app.digiwells.no/Trajectory/api/swagger

Trajectory API:

https://dev.digiwells.no/Trajectory/api/Trajectory

https://app.digiwells.no/Trajectory/api/Trajectory

Trajectory realization cases are exposed through:

- `TrajectoryRealizationCase`
- `TrajectoryRealizationCase/LightData`
- `TrajectoryRealizationCase/{id}`
- `TrajectoryRealizationCase/{id}/Realizations/ChunkCount`
- `TrajectoryRealizationCase/{id}/Realizations/Chunks/{chunkIndex}`

The light data endpoint is intended for grids and polling calculation status. Realized trajectories are stored separately in chunks, with 25 realizations per chunk by default, so clients can load large result sets progressively.

## Related Projects

- `Model` contains the main model and trajectory calculation logic used by the service.
- `ModelSharedOut` contains generated client-side types and service schemas for consumers.
- `WebPages` contains the reusable Razor UI pages for Trajectory, TrajectoryInterpolation, and TrajectoryRealization.
- `WebApp` is the host application that renders the UI using `WebPages`.

## Persistence and identity cutover

The service keeps its historical API path (`/Trajectory/api` case-insensitively), database filenames, and `trajectory-claim` storage identity. Its renamed Helm chart is `charts/osdcdrillingtrajectoryservice` and defaults to a `Recreate` deployment strategy. For a new OSDC Helm release that must reuse production data, set `persistence.existingClaim=trajectory-claim` explicitly.

Fresh `Trajectory.db` files are created transactionally at schema version 2. Exact version-0/1 databases are upgraded additively in one transaction: the identity and feature tables are created in the main database and rows are copied from a validated sibling `TrajectoryCatalog.db`, when present. The legacy catalog file is deliberately retained as a rollback copy and existing survey/trajectory rows are not rewritten. Unexpected tables, missing or malformed columns, malformed legacy catalogs, and newer schema versions fail startup without automatic deletion or reconstruction.

`GlobalAntiCollision.db` is a derived spatial index stored on the same persistent volume. Schema version 2 separates one-row-per-trajectory state (`TrajectoryType`, `IsDefinitive`, source modification time, and calculation provenance) from coarse octree bucket memberships. Each membership is uniquely keyed by octree depth/code and trajectory UUID and stores that trajectory's compacted detailed codes as a BLOB. Spatial lookup uses the bucket index first, joins the trajectory state for filtering, and then performs exact octree intersection on the detailed codes.

An existing version-1 octree database is copied to a timestamped `GlobalAntiCollision.schema-v1-*.bak` file and integrity-checked before migration. The migration preserves every legacy trajectory state and membership row, verifies row counts and references, and replaces the legacy tables in one transaction. Unexpected or malformed schemas fail closed. On startup, a background reconciliation removes orphaned cache entries and rebuilds missing or outdated entries from the authoritative trajectories. Normal trajectory calculation, update, delete, and batch restore operations also maintain the index automatically; an individual replacement is atomic inside the octree database.

## Source Code Origin

The original service and host web application solution was generated from a NORCE Drilling and Wells Modelling Team .NET template.

Creation date: `02/12/2025`

Version: `4.0.22`

Template source:

https://github.com/NORCE-DrillingAndWells/Templates

Template documentation:

https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki/.NET-Templates

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

## MCP server

The service publishes its non-statistics REST actions as MCP tools. Tool registration discovers controller actions and preserves support for asynchronous operations, chunked trajectory data, filters, and multi-ID requests. Every tool has a human-readable title, an operation-specific description, strict JSON input and success-output schemas, and read-only/destructive/idempotent/open-world safety annotations. Input schemas include nested model properties, non-empty UUID and date-time formats, enum values, defaults, and SI-unit guidance; unknown top-level arguments are rejected before controller invocation.

- Streamable HTTP: `/trajectory/api/mcp`
- WebSocket: `/trajectory/api/mcp/ws`
- Published controller tools: 121
- Utility tools: `ping`
- Excluded surface: `TrajectoryUsageStatisticsController`

The descriptions explain the service workflows as well as individual calls. In particular, survey-measurement chunks are uploaded with zero-based indexes and then committed; calculation cases are created and polled through `CalculationState`/`CalculationProgress`; large station, realization, minimum-distance, and aggregation results are retrieved through chunk-count and chunk tools. Octree indexes are maintained automatically by trajectory writes and startup reconciliation. `GET Octrees/{id}/Status` exposes `Missing`, `NotIndexable`, `Stale`, or `Current` plus schema/calculation provenance and compact counts; the list operation can filter indexed UUIDs by `TrajectoryType` and `IsDefinitive`. POST/PUT are documented as operational repair actions and return the resulting status, while DELETE explicitly removes only rebuildable derived data. Unless a field explicitly says otherwise, lengths, depths, coordinates, and distances are metres, angles are radians, and curvature is radians per metre.

Successful MCP calls return the HTTP-compatible status and any controller payload as structured JSON plus a text fallback. Validation, not-found, conflict, and unexpected failures are returned as genuine MCP errors with stable sanitized envelopes. Server exceptions are logged but their messages and stack details are not exposed to callers.

Global anti-collision create and update tools return the calculated stored representation. They require a non-empty string ID, PUT requires the route and body IDs to match and no longer performs an implicit upsert, missing reads/updates/deletes return not found, duplicate creates return conflict, and calculation or persistence failures are surfaced instead of being logged as false successes. All string-ID SQL operations are parameterized.

`POST Trajectory/BatchExport` creates an all-data or selected backup. Selection is dependency-closed: trajectories pull in their survey runs, and survey runs pull in parent runs. `POST Trajectory/BatchRestore` validates the versioned document and catalog dependencies before writing survey runs and then trajectories. Its MCP schema requires `FailIfExists` or `ReplaceExisting` and `MapExisting` or `MapOrCreateMissing`; unusable `Unspecified` enum members are not advertised. Record writes are committed in one SQLite transaction and preserve stored measurements and station chunks without triggering calculations.

Optional registration with an external MCP hub is configured in `appsettings.json` and is disabled by default.

## Shared identities and features

`TrajectoryIdentity` and `TrajectoryFeatureCategory` are common catalogs for both Survey Run and Trajectory resources. Catalog CRUD uses optimistic concurrency through `expectedModifiedUtc`. Referenced definitions and options cannot be deleted, and resource writes reject missing catalog references, duplicate assignment UUIDs, unsupported validity dates, invalid periods, and overlapping assignments in exclusive categories.

Catalogs are stored in `Trajectory.db`, like the sibling DigiWells microservices. This gives resource and catalog restore genuine all-or-nothing SQLite transaction semantics. On the first version-2 startup, definitions from the former `TrajectoryCatalog.db` are copied without removing or modifying that file.
