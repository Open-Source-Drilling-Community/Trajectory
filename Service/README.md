# Trajectory Service

`Service` is the ASP.NET Core microservice for Trajectory. Its code namespace root is `OSDC.Drilling.Trajectory.Service`.

It exposes the Trajectory API and depends on the `Model` project for the domain model and computation logic.

## Responsibilities

- expose SurveyRun and Trajectory CRUD, search, identity/feature assignment, and chunk endpoints
- expose interpolation, extrapolation, realization, aggregation, station-ellipse, and minimum-distance calculation cases
- expose asynchronous uncertainty-aware target-landing design cases
- maintain and query the derived global anti-collision octree index
- provide versioned dependency-closed backup and atomic restore
- provide read-only single-record external-reference validation and bounded audits
- persist resource, catalog, calculation, anti-collision, and usage-history state
- run long calculations asynchronously so requests can poll state and progress instead of blocking

Aggregation results sample the fitted section chain at the case's interpolation interval and include the standard derived survey values (DLS, BUR, TR, and vertical section) used by the Web UI and exports. When the source starts vertically, where its initial azimuth is physically undefined, the calculator aligns the compact fitted chain's departure direction with the source displacement without adding sections or changing source data. The distance results report the remaining approximation error of that compact representation.

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

Trajectory extrapolation cases are exposed through `TrajectoryExtrapolationCase`, its `LightData` and `{id}/Status` polling views, and `{id}/SurveyStations/ChunkCount` plus zero-based station chunks. Create and update queue the calculation. Fixed-length cases extend straight or continue a fitted final curve; reconnect cases may add a current-curve lead-in before solving a two-section path to an advanced point and tangent on a reference trajectory; geosteering cases add the same optional lead-in and solve two steering sections to a depth and attitude using an overall departure/bearing or steering-length/section-ratio constraint. Steering length is the sum of the two steering sections and excludes the lead-in. Well-path cases enforce exactly three constraints per section across circular-arc, constant-build-and-turn, and constant-curvature-and-toolface variants. Failed well-path calculations return an actionable `CalculationMessage` that identifies constraint-count errors, singular sensitivity systems, zero-sensitivity constraints, or the largest remaining residual as applicable. Before solving, the service rematerializes the source trajectory from authoritative SurveyRuns and recalculates their uncertainty lineage. Sampled stations inherit the last defined source-trajectory survey instrument and include curve-family-consistent DLS, BUR, TR, toolface, cumulative vertical section, and uncertainty continued from the final source-trajectory station. Wolff-de Wardt and ISCWSA cases replay the source stations before extending them so their non-persisted transfer matrix or error-source accumulators, rather than only derived endpoint covariance, remain continuous. Canonical lengths are metres, angles radians, curvatures radians per metre, and confidence factors are dimensionless proportions greater than zero and no greater than 0.999.

Directional-control evaluation cases use `DirectionalControlEvaluationCase`, a dedicated `LightData`/`{id}/Status` polling contract, and `{id}/Samples/ChunkCount` plus zero-based sample chunks. Create and update persist only compact case configuration and queue a durable background calculation; interrupted queued/running work is recovered at startup. Reference and actual trajectories are validated server-side as members of the same WellBore. The reconnect uses a 60 m default correction length and always fixes the azimuth branch to zero (the shortest rotation). Compact case reads contain bundle statistics but never the interval sample array. Terminal samples are replaced atomically in 250-row chunks, so listing, editing, polling, and optimistic-concurrency checks do not read or transmit the large result payload. SQLite schema version 9 adds the case and sample-chunk tables without rewriting existing trajectory data.

Target landing cases are exposed through `TargetLandingCase`, `LightData`, `{id}/Status`, `{id}/EditData`, and `{id}/DisplayData`. `LightData` is selected only from a compact covering index over the dedicated scalar columns; it neither deserializes nor traverses the large calculation aggregate stored earlier in the SQLite row. `EditData` reads a separately persisted compact edit projection with no calculation samples or mesh triangles; `DisplayData` returns contours and at most 250 boundary-path samples with reduced survey stations. The unrestricted endpoint remains available for callers that explicitly need the complete sampled result. Create and update persist a queued case and return immediately. A managed single-reader background worker performs calculations outside HTTP request threads; its durable queued/running records resume at service startup. Progress writes update only scalar state columns instead of repeatedly serializing the growing result, and report source loading, source uncertainty reconstruction, and target-plane sampling separately. The lightweight status endpoint reads calculation state, progress, and message through the same covering index so clients can poll without loading the large sample and mesh result. A calculation remains `Running` through its terminal result write and becomes `Completed` only when the complete result has been persisted; a failed terminal write is reported as `Failed` rather than exposing the previous result as current. The worker first rematerializes the source trajectory with authoritative SurveyRun uncertainty, preserves its terminal station, samples the continued final-source trend through the requested lead, evaluates a conforming target-plane seed mesh, bisects every CA/BT/CTC state transition to the same 0.25 metre tolerance, solves the landing sections, evaluates geological-target confidence-ellipse containment, and classifies specified, uncertainty-safe, and curvature/geometry-reachable zones. Curvature-constrained BT inverse iteration uses that same Cartesian tolerance. When every BT root violates the curvature limit, the solver retains the rejected geometric section and returns a curvature-specific status instead of repeating the target solve. Curvature-rejected driller-target samples skip station interpolation and uncertainty propagation because neither can change those classifications. Plane origins accept equivalent WGS84 geographic or Riemannian North/East coordinates and are canonicalized into both representations. Free landing attitude uses one section; `PerpendicularToTargetPlane` uses two. A CTC root that approaches within 3 degrees of either vertical direction anywhere along its exact section is classified as having no valid geometric solution because toolface and turn rate become ill-conditioned there; this rule does not apply to CA or BT curves, and turn rate is not otherwise a rejection criterion. Maximum Landing Curvature defaults to 3 degrees per 30 metres and applies only to the new landing sections. For free-attitude BT targets, alternative roots are considered when the conventional solution exceeds that limit, preventing a compliant target from being classified as unreachable. Completed cases retain the source endpoint, sampled lead, sampled solutions, and mesh contours and become stale when the source trajectory revision or calculation input fingerprint changes; saving queues recalculation.

Global anti-collision submissions may provide `RequestedPolicyAssignmentID`. When omitted, the worker calculates separation factors without Alert/Alarm policy classification. When supplied, the worker validates that the assignment belongs to the reference trajectory's Field, resolves its exact immutable policy revision, replaces the submitted confidence factor with that policy's confidence, and stores the applied assignment, evaluation time and frozen policy snapshot as server-derived result data. A missing, invalid or wrong-Field selection fails the asynchronous job with a sanitized calculation message.


## Survey reference correction

SurveyRun calculation preserves raw observed inclination/azimuth and converts them to WGS84-geodetic inclination and true-north azimuth before producing canonical survey stations. Gravity-referenced observations are evaluated through Earth Gravity, and magnetic-north observations through Earth Magnetic Field, after an initial station-position calculation. A retained context measurement above the resolved tie-in has no calculated station of its own, so its reference fields are evaluated at the authoritative tie-in WGS84 position; the calculated survey still begins at the tie-in. The service iterates position-dependent corrections to convergence, records the applied differences and model provenance per measurement, and persists station-level failures for diagnosis. A completed station time is preferred for magnetic evaluation; otherwise a complete SurveyRun acquisition interval is required and its midpoint is used. For a Wolff-de Wardt or ISCWSA SurveyRun tied to an earlier run, including a run on a sidetrack WellBore, the service recursively reconstructs the parent-run path through the physical tie-in and propagates the same transfer matrix or error-source accumulator state into the child. A sidetrack may restart its local measured depth at zero; a temporary continuous abscissa is used only during covariance propagation, while the persisted child measured depths remain unchanged. Trajectories preserve these continued station covariances when composing SurveyRun sections and when replacing a sidetrack's first station with the parent-trajectory tie-in.

A SurveyRun may have one optional terminal bit extrapolation. `CalculateFromLastMeasurement` continues the last measured curve using the run's calculation method and a positive frozen measurement-tool-to-bit distance in SI metres; with one measured station it uses a straight extension. `LastStationAlreadyExtrapolated` requires exactly the final submitted row to be marked `Extrapolated` and validates the MD increment against the same distance. The service excludes that row from Earth-reference correction, acquisition-age evidence, and the instrument observation sequence. Uncertainty is calculated through the last measured station and its terminal covariance state is carried to the deterministic bit endpoint without introducing a fictitious measurement.

Configure `EarthGravityHostURL` and `EarthMagneticFieldHostURL`. Development defaults use the DigiWells development endpoints; production and Helm defaults use `osdcearthgravityservice` and `osdcearthmagneticfieldservice`. Dependency failure fails the SurveyRun calculation without discarding the raw observation.

The REST/OpenAPI and MCP contracts carry the same reference-frame semantics and SI units. Run defaults must be concrete (`GeodeticVertical` or `GravityVertical`, and `TrueNorth` or `MagneticNorth`); `InheritRun` is valid only on an individual measurement. Dependency exception details are logged server-side while persisted and returned failure messages remain sanitized.

Ellipse calculations intended for a stored resource use `POST SurveyStationEllipseCalculation/SurveyRun/{surveyRunId}` or `POST SurveyStationEllipseCalculation/Trajectory/{trajectoryId}`. These routes rebuild authoritative SurveyRun uncertainty from the complete parent chain, rematerialize trajectories from those corrected runs, and replace submitted covariance at exact or interpolated display depths. The original station-only route remains available for genuinely standalone complete histories, but rejects partial Wolff-de Wardt or ISCWSA covariance because the missing propagation state cannot be recovered. Trajectory realizations use the same lineage-aware rematerialization and fail rather than silently restarting at a slot or tie-in.

Within each ellipse calculation, every vertical ellipse uses the same first-to-last vertical-section curtain. It does not follow the instantaneous station azimuth, avoiding false changes in projected semi-axis size when azimuth becomes ill-conditioned near vertical inclination.

## Related Projects

- `Model` contains the main model and trajectory calculation logic used by the service.
- `ModelSharedOut` contains generated client-side types and service schemas for consumers.
- `WebPages` contains the reusable Razor UI pages for Trajectory, TrajectoryInterpolation, and TrajectoryRealization.
- `WebApp` is the host application that renders the UI using `WebPages`.

## Persistence and identity cutover

The service keeps its historical API path (`/Trajectory/api` case-insensitively), database filenames, and `trajectory-claim` storage identity. Its renamed Helm chart is `charts/osdcdrillingtrajectoryservice` and defaults to a `Recreate` deployment strategy with one replica. TCP startup, readiness, and liveness probes keep the pod out of the Service until Kestrel is accepting connections, including while SQLite reconciliation and calculation recovery run during startup. For a new OSDC Helm release that must reuse production data, set `persistence.existingClaim=trajectory-claim` explicitly. Never run overlapping service pods against these SQLite files.

Each database uses private SQLite connection caches, a bounded busy timeout, and WAL journaling. Concurrent HTTP and background-calculation transactions therefore wait for the active writer instead of failing immediately with `SQLITE_LOCKED`; this does not make multiple service replicas safe, so the one-replica `Recreate` requirement still applies.

Fresh `Trajectory.db` files are created transactionally at schema version 8. Version-1 through version-7 databases are upgraded additively in one transaction. The upgrade preserves survey/trajectory data, shared catalogs, extrapolation cases, policies, and assignments while adding the target-landing case table where absent. Version 5 target-landing rows gain and backfill dedicated name, description, source-revision, fingerprint, and compact edit-data columns. Version 6 trajectory rows gain and backfill dedicated name and description columns; trajectory light-list queries read only scalar columns and never deserialize complete trajectory JSON. The version-7-to-8 upgrade adds a compact target-landing covering index so light listing and status polling do not traverse the large result value merely to reach scalar columns stored after it. The legacy catalog file is deliberately retained as a rollback copy. Unexpected tables, missing or malformed columns, malformed legacy catalogs, and newer schema versions fail startup without automatic deletion or reconstruction.

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

The service publishes its non-statistics REST actions as MCP tools. Tool registration discovers controller actions and preserves support for asynchronous operations, chunked trajectory data, filters, and multi-ID requests. Every tool has a human-readable title, an operation-specific description, strict JSON input and success-output schemas, and read-only/destructive/idempotent/open-world safety annotations. Input schemas include nested model properties, non-empty UUID and date-time formats, enum values, defaults, nullability, and SI-unit guidance. Unknown top-level arguments are rejected before controller invocation; the octree-scan and separation-factor binders also reject unknown nested fields and invalid cross-field combinations before any job is queued.

- Streamable HTTP: `/trajectory/api/mcp`
- WebSocket: `/trajectory/api/mcp/ws`
- Published controller tools: 153
- Utility tools: `ping`
- Excluded surface: `TrajectoryUsageStatisticsController`

The descriptions explain the service workflows as well as individual calls. In particular, survey-measurement chunks are uploaded with zero-based indexes and then committed; calculation cases are created and polled through `CalculationState`/`CalculationProgress`; large station, realization, minimum-distance, and aggregation results are retrieved through chunk-count and chunk tools. Octree indexes are maintained automatically by trajectory writes and startup reconciliation. Spatial-index algorithm version 3 represents the 99.9%-confidence uncertainty volume with a compact, one-cell-padded conservative swept-AABB cover at detailed depth 22. Filling the swept interiors and end regions closes the former strict-containment and open-end blind spots; AABB corner space can deliberately yield false-positive candidates that the separation-factor narrow phase rejects. Compaction never crosses the cache depth, preserving exact bucket lookup. Existing trajectory data is untouched, while prior derived indexes become stale by provenance hash and are rebuilt automatically. `GET Octrees/{id}/Status` exposes `Missing`, `NotIndexable`, `Stale`, or `Current` plus algorithm/calculation provenance and compact counts; the list operation can filter indexed UUIDs by `TrajectoryType` and `IsDefinitive`. Octree candidate discovery is asynchronous only: `octrees_queue_search` queues it, `octrees_get_search_status` reports actual bucket-loading and exact-intersection progress, `octrees_get_search_result` transfers unique candidate UUIDs only after completion, and `octrees_delete_search` removes the transient job. The MCP request schema rejects an empty planned/actual selection. The fixed 0.999 index confidence is a conservative superset of every downstream confidence accepted by the API, whose maximum is also 0.999. Jobs expire one hour after reaching a terminal state and may be safely resubmitted after a service restart. Searches combine planned/actual selection with an optional definitive-only restriction, exclude the reference trajectory, and refuse a non-current reference index. POST/PUT index actions are documented as operational repairs and return the resulting status, while DELETE explicitly removes only rebuildable derived data. Unless a field explicitly says otherwise, lengths, depths, coordinates, and distances are metres, angles are radians, and curvature is radians per metre. Octree and mesh-refinement maximum depths are dimensionless subdivision/recursion levels, not physical depths.

MCP discovery for the two primary resources uses bounded `trajectory_search_trajectory` and `survey_run_search_survey_run` tools, returning deterministic lightweight pages with a total count, default limit 100, and maximum limit 500. Their unbounded full-list REST actions remain available for existing clients but are deliberately not registered as MCP tools.

Trajectory extrapolation MCP submissions use the same `Mode` and `CurveType` discriminators as REST/OpenAPI. The closed mutation schema excludes server-derived timestamps, state, endpoint snapshots, revisions, solved sections, and sampled stations. Agents poll `trajectory_extrapolation_case_get_status` and retrieve completed samples through the chunk-count and zero-based chunk tools.

Target-landing MCP submissions are likewise closed: callers supply the source trajectory, convex plane target, curve and landing-attitude choices, lead, confidence, and optional Maximum Landing Curvature. Timestamps, source revision/fingerprint, calculation state, calculation samples, mesh, contours, solved sections, and normalized curve-specific control paths are server-derived. Completed sample controls contain exact interpolated inclination, SI curvature/build/turn rates, toolface, and length normalized from steering start zero to target one; they are calculated from the solved DotNetLibraries section interpolation rather than reconstructed from sparse survey stations. Poll `target_landing_case_get_status` before retrieving the completed case.

Trajectory and SurveyRun each expose a read-only `GET {id}/ExternalReferences` validation and `POST ExternalReferenceAudit` diagnostic. Audits accept `All` or an explicit unique UUID selection, order records deterministically by UUID, and return at most 100 results per page. The validator checks the configured Field, Cluster, Well and WellBore services and, for SurveyRuns, SurveyInstrument. A confirmed 404 is `Invalid`; missing configuration, transport failures, non-success dependency responses, and malformed or mismatched responses are `Unavailable` and are never treated as proof of invalid data. Optional unlinked references are permitted, while required empty WellBore or SurveyInstrument UUIDs are invalid. These diagnostics never participate in writes or alter stored records.

PUT and DELETE operations for trajectories, survey runs, saved batch imports, interpolated trajectories, realization and aggregation cases, ellipse calculations, and both minimum-distance calculation families require `expectedModifiedUtc`. Copy this opaque value from the latest `LastModificationDate`; stale mutations return HTTP 409 with `error: stale_write`. The same rule applies to identity and feature-category definitions.

Creates for trajectories, survey runs, saved batch imports, interpolated trajectories, realization and aggregation cases, and both minimum-distance calculation families assign CreationDate and the initial LastModificationDate on the server, overriding submitted timestamp values. Primary Trajectory reads expose CreationDate, or the Unix epoch when both timestamps are absent, as the effective revision for legacy records.

Successful MCP calls return the HTTP-compatible status and any controller payload as structured JSON plus a text fallback. Validation, not-found, conflict, and unexpected failures are returned as genuine MCP errors with stable sanitized envelopes. Server exceptions are logged but their messages and stack details are not exposed to callers.

Global anti-collision create and update operations enqueue the separation-factor calculation and return immediately with `CalculationState=Queued`. The MCP workflow is `global_anti_collisions_post` → repeated `global_anti_collisions_get_status` calls → `global_anti_collisions_get_by_id` after `Completed`; delete the durable job explicitly when its result is no longer needed. Its closed submission schema accepts only configuration fields and requires a non-empty string ID, confidence in `(0, 0.999]`, exactly one non-empty reference trajectory/well-path UUID, and at least one unique non-empty comparison trajectory UUID. REST enforces that same confidence interval and returns `invalid_confidence_factor` for violations. The maximum is shared with the fixed octree encoding confidence, so the narrow phase cannot request an uncertainty volume broader than the candidate scan. Calculation state, progress, messages, relevant measured-depth ranges, and separation-factor profiles are server-derived and rejected at the MCP binding boundary when submitted. Results document reference/comparison MD in SI metres, dimensionless separation factors, nullable relevant ranges, and potentially disjoint profile intervals. PUT requires matching route/body IDs, rejects replacement while that job is queued or running, and never performs an implicit upsert. The worker reports preparation stages and completed comparison-trajectory counts, persists terminal results, and resumes queued/running records after a normal service restart. Missing resources, duplicates, queue failures, calculation failures, and persistence failures are surfaced with stable outcomes.

`POST Trajectory/BatchExport` creates an all-data or selected backup. Selection is dependency-closed: trajectories pull in their survey runs, and survey runs pull in parent runs. Backup schema version 2 also includes the complete immutable anti-collision policy library and historical Field assignments; schema-version-1 documents remain accepted. `POST Trajectory/BatchRestore` validates resources, catalog dependencies, policy revisions, and non-overlapping assignment history before one atomic write. Its MCP schema requires `FailIfExists` or `ReplaceExisting`, `MapExisting` or `MapOrCreateMissing`, and an explicit `AllowNormalizedNameMapping` decision. Exact catalog and option UUID matching is the safe default; normalized-name mapping occurs only when that flag is `true`.

Anti-collision policies are immutable revisions grouped by `PolicyID`; revision number and creation time are server-derived. A Field assignment points to one exact revision and uses a non-overlapping half-open UTC validity interval. Updates and future-only deletes require the opaque `LastModificationDate`; active and historical assignments cannot be deleted. An entire policy family can be deleted atomically only when none of its revisions is referenced by any current or historical Field assignment; deletion requires the latest revision UUID so a concurrent new revision produces a stale-write conflict. Rules use unique explicit priorities greater than or equal to 1 and first-match semantics, AND conditions within a rule, and require one final unconditional default. Each rule enforces dimensionless `AlertThreshold > AlarmThreshold > 0`. Age, identity, and feature conditions inspect only the comparison trajectory and its WellBore/Well/Slot/Cluster hierarchy. Missing dependencies or age evidence are `Indeterminate`, never false. The current Cluster contract provides Slot features but no Slot identity catalog, so Slot identity predicates explicitly evaluate as `Indeterminate`. The worker freezes the effective assignment, policy revision, evaluation time, comparison evidence, and resulting classification into the durable calculation result.

Persisted calculation cases and results have no age-based retention policy. The service never deletes them automatically after 90 days (or any other age); removal requires an explicit delete request.

Optional registration with an external MCP hub is configured in `appsettings.json` and is disabled by default.

## Local execution and dependency configuration

The service uses the `/trajectory/api` path base. For integration tests, launch it on `http://localhost:8080`; the generated client calls `http://localhost:8080/Trajectory/api/` and routing is case-insensitive in the deployed ingress.

External-reference diagnostics read `FieldHostURL`, `ClusterHostURL`, `WellHostURL`, `WellBoreHostURL`, and `SurveyInstrumentHostURL`. Those diagnostic calls return dependency unavailability as `Unavailable` and never block or mutate a Trajectory or SurveyRun write. Survey correction separately reads `EarthGravityHostURL` and `EarthMagneticFieldHostURL`; an unavailable required correction dependency causes calculation to fail while preserving the submitted observation. Development values point to the public development host, and the Helm chart supplies in-cluster OSDC service URLs in production.

The databases and usage history are relative to the service working directory and are mounted under the durable `/home` volume in containers. Use an isolated test working directory when running destructive integration tests; never clear a developer or deployed database to make a test repeatable.

## Shared identities and features

`TrajectoryIdentity` and `TrajectoryFeatureCategory` are common catalogs for both Survey Run and Trajectory resources. Catalog CRUD uses optimistic concurrency through `expectedModifiedUtc`. Referenced definitions and options cannot be deleted, and resource writes reject missing catalog references, duplicate assignment UUIDs, unsupported validity dates, invalid periods, and overlapping assignments in exclusive categories.

Catalogs are stored in `Trajectory.db`, like the sibling DigiWells microservices. This gives resource and catalog restore genuine all-or-nothing SQLite transaction semantics. On the first version-2 startup, definitions from the former `TrajectoryCatalog.db` are copied without removing or modifying that file.

### Target-landing uncertainty display

`GET TargetLandingCase/{id}/UncertaintyDisplayData` is the lazy companion to the compact target-landing display projection. It calculates only MD-keyed perpendicular ellipse parameters for the source trajectory and sampled lead at the case confidence. It omits duplicate stations, horizontal and vertical ellipses, and extreme paths; landing endpoints already carry their target-plane projected ellipse in the ordinary display result.
