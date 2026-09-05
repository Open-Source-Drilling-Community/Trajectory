# Trajectory Service

`Service` is the ASP.NET Core microservice for Trajectory. Its code namespace root is `OSDC.Drilling.Trajectory.Service`.

It exposes the Trajectory API and depends on the `Model` project for the domain model and computation logic.

## Responsibilities

- expose SurveyRun and Trajectory CRUD, search, identity/feature assignment, and chunk endpoints
- expose interpolation, realization, aggregation, station-ellipse, and minimum-distance calculation cases
- maintain and query the derived global anti-collision octree index
- provide versioned dependency-closed backup and atomic restore
- provide read-only single-record external-reference validation and bounded audits
- persist resource, catalog, calculation, anti-collision, and usage-history state
- run long calculations asynchronously so requests can poll state and progress instead of blocking

## Container

The service is packaged as the Docker image:

`docker.io/digiwells/osdcdrillingtrajectoryservice:stable`

It is published under the `digiwells` organization:

https://hub.docker.com/?namespace=digiwells

## Endpoints

OpenAPI / Swagger:

https://dev.digiwells.no/Trajectory/api/swagger

https://app.digiwells.no/Trajectory/api/swagger

https://awe.web.intra.norceresearch.no/Trajectory/api/swagger

Trajectory API:

https://dev.digiwells.no/Trajectory/api/Trajectory

https://app.digiwells.no/Trajectory/api/Trajectory

https://awe.web.intra.norceresearch.no/Trajectory/api/Trajectory

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

The service keeps its historical API path (`/Trajectory/api` case-insensitively), database filenames, and `trajectory-claim` storage identity. Its renamed Helm chart is `charts/osdcdrillingtrajectoryservice` and defaults to a `Recreate` deployment strategy with one replica. For a new OSDC Helm release that must reuse production data, set `persistence.existingClaim=trajectory-claim` explicitly. Never run overlapping service pods against these SQLite files.

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
- Published controller tools: 131
- Utility tools: `ping`
- Excluded surface: `TrajectoryUsageStatisticsController`

The descriptions explain the service workflows as well as individual calls. In particular, survey-measurement chunks are uploaded with zero-based indexes and then committed; calculation cases are created and polled through `CalculationState`/`CalculationProgress`; large station, realization, minimum-distance, and aggregation results are retrieved through chunk-count and chunk tools. Octree indexes are maintained automatically by trajectory writes and startup reconciliation. `GET Octrees/{id}/Status` exposes `Missing`, `NotIndexable`, `Stale`, or `Current` plus schema/calculation provenance and compact counts; the list operation can filter indexed UUIDs by `TrajectoryType` and `IsDefinitive`. `GET Octrees/{id}/Search` remains as the synchronous compatibility operation. For long scans, `POST Octrees/SearchJobs` queues candidate discovery, `GET .../{jobId}/Status` reports actual bucket-loading and exact-intersection progress, `GET .../{jobId}/Result` transfers candidates only after completion, and `DELETE .../{jobId}` removes the transient job. Jobs expire one hour after reaching a terminal state and may be safely resubmitted after a service restart. Both search modes combine planned/actual selection with an optional definitive-only restriction, exclude the reference trajectory, and refuse a non-current reference index. POST/PUT index actions are documented as operational repairs and return the resulting status, while DELETE explicitly removes only rebuildable derived data. Unless a field explicitly says otherwise, lengths, depths, coordinates, and distances are metres, angles are radians, and curvature is radians per metre. Octree and adaptive-refinement maximum depths are dimensionless subdivision/recursion levels, not physical depths.

MCP discovery for the two primary resources uses bounded `trajectory_search_trajectory` and `survey_run_search_survey_run` tools, returning deterministic lightweight pages with a total count, default limit 100, and maximum limit 500. Their unbounded full-list REST actions remain available for existing clients but are deliberately not registered as MCP tools.

Trajectory and SurveyRun each expose a read-only `GET {id}/ExternalReferences` validation and `POST ExternalReferenceAudit` diagnostic. Audits accept `All` or an explicit unique UUID selection, order records deterministically by UUID, and return at most 100 results per page. The validator checks the configured Field, Cluster, Well and WellBore services and, for SurveyRuns, SurveyInstrument. A confirmed 404 is `Invalid`; missing configuration, transport failures, non-success dependency responses, and malformed or mismatched responses are `Unavailable` and are never treated as proof of invalid data. Optional unlinked references are permitted, while required empty WellBore or SurveyInstrument UUIDs are invalid. These diagnostics never participate in writes or alter stored records.

PUT and DELETE operations for trajectories, survey runs, saved batch imports, interpolated trajectories, realization and aggregation cases, ellipse calculations, and both minimum-distance calculation families require `expectedModifiedUtc`. Copy this opaque value from the latest `LastModificationDate`; stale mutations return HTTP 409 with `error: stale_write`. The same rule applies to identity and feature-category definitions.

Successful MCP calls return the HTTP-compatible status and any controller payload as structured JSON plus a text fallback. Validation, not-found, conflict, and unexpected failures are returned as genuine MCP errors with stable sanitized envelopes. Server exceptions are logged but their messages and stack details are not exposed to callers.

Global anti-collision create and update operations enqueue the calculation and return immediately with `CalculationState=Queued`. Poll `GET GlobalAntiCollisions/{id}/Status` for the lightweight state, progress fraction, and stage message; retrieve the full record only after `Completed`. The background worker reports preparation stages and completed comparison-trajectory counts, persists the terminal result, and resumes queued/running records after a normal service restart. Requests require a non-empty string ID and a reference trajectory or well path; PUT requires matching route/body IDs, rejects replacement while that job is queued or running, and never performs an implicit upsert. Missing resources, duplicates, queue failures, calculation failures, and persistence failures are surfaced with stable HTTP outcomes.

`POST Trajectory/BatchExport` creates an all-data or selected backup. Selection is dependency-closed: trajectories pull in their survey runs, and survey runs pull in parent runs. `POST Trajectory/BatchRestore` validates the versioned document and catalog dependencies before writing survey runs and then trajectories. Its MCP schema requires `FailIfExists` or `ReplaceExisting`, `MapExisting` or `MapOrCreateMissing`, and an explicit `AllowNormalizedNameMapping` decision. Exact catalog and option UUID matching is the safe default; normalized-name mapping occurs only when that flag is `true`. Record writes are committed in one SQLite transaction and preserve stored measurements and station chunks without triggering calculations.

Persisted calculation cases and results have no age-based retention policy. The service never deletes them automatically after 90 days (or any other age); removal requires an explicit delete request.

Optional registration with an external MCP hub is configured in `appsettings.json` and is disabled by default.

## Local execution and dependency configuration

The service uses the `/trajectory/api` path base. For integration tests, launch it on `http://localhost:8080`; the generated client calls `http://localhost:8080/Trajectory/api/` and routing is case-insensitive in the deployed ingress.

External-reference diagnostics read `FieldHostURL`, `ClusterHostURL`, `WellHostURL`, `WellBoreHostURL`, and `SurveyInstrumentHostURL`. Development values point to the public development host; the Helm chart supplies in-cluster OSDC service URLs in production. These calls are diagnostic only: dependency unavailability is returned as `Unavailable` and never blocks or mutates a Trajectory or SurveyRun write.

The databases and usage history are relative to the service working directory and are mounted under the durable `/home` volume in containers. Use an isolated test working directory when running destructive integration tests; never clear a developer or deployed database to make a test repeatable.

## Shared identities and features

`TrajectoryIdentity` and `TrajectoryFeatureCategory` are common catalogs for both Survey Run and Trajectory resources. Catalog CRUD uses optimistic concurrency through `expectedModifiedUtc`. Referenced definitions and options cannot be deleted, and resource writes reject missing catalog references, duplicate assignment UUIDs, unsupported validity dates, invalid periods, and overlapping assignments in exclusive categories.

Catalogs are stored in `Trajectory.db`, like the sibling DigiWells microservices. This gives resource and catalog restore genuine all-or-nothing SQLite transaction semantics. On the first version-2 startup, definitions from the former `TrajectoryCatalog.db` are copied without removing or modifying that file.
