# Trajectory

The Trajectory repository contains the OSDC Trajectory service, the host web application, and a reusable Razor class library for the Trajectory UI pages. The solution-owned .NET namespace root is `OSDC.Drilling.Trajectory`; the companion anti-collision library uses `OSDC.Drilling.GlobalAntiCollision`.

## Solution Architecture

The solution currently contains:

- `ModelSharedIn`
  - auto-generated C# classes for upstream model dependencies
  - source OpenAPI schemas for Field, Cluster, Well, WellBore, WellBore Architecture, Survey Instrument, Earth Gravity, and Earth Magnetic Field
- `Model`
  - domain model and trajectory calculation logic
  - survey correction, interpolation, aggregation, extrapolation, target landing, directional-control evaluation, uncertainty, distance, and stochastic realization calculations
- `Service`
  - ASP.NET Core microservice exposing the Trajectory API
  - depends on `Model`
  - persists the resource, calculation, anti-collision, and usage-statistics state
- `GlobalAntiCollision`
  - octree indexing and spatial candidate-search implementation used by the service
- `ModelSharedOut`
  - auto-generated client-side classes and schemas used by consumers of the Trajectory service
  - includes the Trajectory service schema together with other relevant upstream schemas
- `WebPages`
  - reusable Razor pages for survey/trajectory management, every calculation workflow, anti-collision, import/export, catalogs, displays, and statistics
  - depends on `ModelSharedOut`
- `WebApp`
  - ASP.NET Core Blazor host application
  - depends on `WebPages`
  - provides the host shell, routing, configuration, and static assets for the UI
- `ModelTest`
  - NUnit tests for model and computation behavior, including survey-reference vector transforms
- `ServiceTest`
  - self-contained contract/persistence tests and integration tests for the running service API
- `GlobalAntiCollisionTest`
  - executable verification harness for the anti-collision implementation
- `home`
  - local persisted data, including `Trajectory.db`, `GlobalAntiCollision.db`, `SeparationFactorResults.db`, and usage history

## Main Workflows

The repository supports the following main trajectory workflows:

- trajectory creation, editing, storage, and retrieval
- survey-run import, raw-observation/reference correction, editing, calculation, and chunked station transfer; retained measurements above a resolved tie-in use the tie-in WGS84 position for local Earth-reference evaluation while the calculated survey begins at the tie-in. Wolff-de Wardt and ISCWSA uncertainty follow the complete parent SurveyRun chain through ordinary and sidetrack tie-ins so their propagation state and resulting ellipsoids remain continuous; composed trajectories preserve those station covariances. A run may optionally end at an explicitly marked bit extrapolation: the service either continues the final measured curve by the frozen SI tool-to-bit distance or accepts a final pre-extrapolated row.
- trajectory interpolation cases
- trajectory extrapolation from the last calculated station by fixed continuation, lead-in-aware reference-trajectory reconnection, two-command geosteering to a target depth and attitude, or a constrained multi-section well path; sampled results include DLS, BUR, TR, cumulative vertical section, and position uncertainty continued from the source endpoint for shared confidence-factor ellipse display
- target-landing design cases that continue the source trend through an optional lead, sample a convex oriented target plane with a conforming seed mesh plus bisection-refined transitions, and solve one- or two-section circular-arc, build/turn, or curvature/toolface landings; position-only build/turn targets use the contour's Cartesian tolerance, consider alternative roots when the conventional root exceeds the optional Maximum Landing Curvature, and retain a curvature-rejected root without repeating the inverse solve, while CTC roots approaching within 3 degrees of vertical are rejected and geological targets are reduced by projected position uncertainty
- directional-control evaluation cases that compare the first proposed reconnect section with an exact same-family fit of each actual-trajectory interval, compute linked command residuals, and detect robust depth bundles with empirical P10/P50/P90 distributions; both trajectories are constrained to one WellBore and detailed samples are chunked
- stochastic trajectory realization cases based on survey station wellbore position uncertainty, after lineage-aware rematerialization of every contributing and parent SurveyRun
- trajectory aggregation, station-ellipse, and survey-run/trajectory minimum-distance calculations; resource-specific ellipse routes replay authoritative SurveyRun ancestry and replace stale or partial display covariance
- Trajectory editor save operations poll the background calculation and reload the calculated station chunks, so an already-open calculated table is refreshed when the calculation completes
- automatically maintained global anti-collision octree indexes
- shared identity and feature catalogs
- versioned, dependency-closed backup and atomic restore

Trajectory realization cases are defined from a reference trajectory and a requested number of realizations. The model optionally coarsens the reference trajectory before generation, draws realizations from the covariance-defined uncertainty field, completes the generated points with the minimum curvature method, and stores the resulting realized trajectories as lists of survey points. Large realization sets are persisted and retrieved in chunks.

### Capability map

| Area | Available functionality |
| --- | --- |
| Survey data | SurveyRun CRUD; observed-reference correction; bit extrapolation; batch import; chunked measurements/stations; uncertainty continuation through ordinary and sidetrack tie-ins |
| Trajectories | Multi-SurveyRun composition; calculation; bounded discovery; identities/features; external-reference validation/audit; dependency-closed backup/restore |
| Design and analysis | Interpolation; compact aggregation; stochastic realization; station ellipses; SurveyRun/Trajectory minimum distance; extrapolation; target landing; directional-control evaluation |
| Anti-collision | Automatically reconciled uncertainty-volume octrees; asynchronous candidate scans; separation-factor profiles; immutable policy revisions and effective-dated Field assignments |
| Interfaces | REST/OpenAPI, 176 REST-backed MCP tools plus `ping`, reusable Razor pages, and the standalone Blazor WebApp |

## Semantic contract

Trajectory references `OSDC.DotnetLibraries.Drilling.SemanticCatalogue` 0.16.0. The service applies an explicit provider binding registry to public trajectory, survey, uncertainty, target-landing, extrapolation, directional-control, and anti-collision types. REST/OpenAPI and MCP publish the same structured `x-osdc-semantic` objects, including stable concept URNs, applicable role/reference URNs, catalogue version, and physical-quantity/SI representation where defined. Provider fields remain the authoritative wire contract; semantic metadata describes them and does not alter JSON names, validation, or canonical SI values.

Persisted calculation tools additionally expose the 0.16.0 lifecycle vocabulary. Aggregation, realization, extrapolation, target landing, directional-control evaluation, minimum-distance calculations, global anti-collision, and interpolated trajectories identify queued submission/replacement, case deletion, case retrieval, light status retrieval, and result/chunk retrieval as applicable. Survey-station ellipse calculation identifies its synchronous submission separately. The same reviewed operation roles are generated for REST and Trajectory MCP tools.

## Security and Confidentiality

Data are persisted as clear text in SQLite databases hosted in the service container.
Neither authentication nor authorization have been implemented.

Docker containers for the service and host web application are available under the `digiwells` organization:

https://hub.docker.com/?namespace=digiwells

The migrated images are `docker.io/digiwells/osdcdrillingtrajectoryservice:stable` and `docker.io/digiwells/osdcdrillingtrajectorywebappclient:stable`.

## Deployment

The Trajectory service is available at:

https://dev.digiwells.no/Trajectory/api/Trajectory

https://app.digiwells.no/Trajectory/api/Trajectory

https://awe.web.intra.norceresearch.no/Trajectory/api/Trajectory

The host web application is available at:

https://dev.digiwells.no/Trajectory/webapp/Trajectory

https://app.digiwells.no/Trajectory/webapp/Trajectory

https://awe.web.intra.norceresearch.no/Trajectory/webapp/Trajectory

The merged OpenAPI schema and Swagger UI of the service are available at:

https://dev.digiwells.no/Trajectory/api/swagger

https://app.digiwells.no/Trajectory/api/swagger

https://awe.web.intra.norceresearch.no/Trajectory/api/swagger

The service and host web application are deployed as Docker containers using Kubernetes and Helm.

The Helm charts are named `osdcdrillingtrajectoryservice` and `osdcdrillingtrajectorywebappclient`. The service chart deliberately retains the historical `trajectory-claim` PVC and all database filenames. Use `--set persistence.existingClaim=trajectory-claim` for the identity cutover, and do not uninstall the legacy release before verifying the selected cluster, namespace, mounted claim, image digest, and existing record counts. Its `Recreate` strategy prevents overlapping service pods from writing SQLite. Within the single service process, private connection caches, a bounded busy timeout, and WAL journaling serialize short concurrent writer transactions instead of abandoning background calculations with `SQLITE_LOCKED`.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

## Current implementation

- The service exposes its REST operations through MCP over streamable HTTP at `/trajectory/api/mcp` and WebSocket at `/trajectory/api/mcp/ws`.
- Persisted Trajectory resources and calculation cases receive server-owned creation and initial modification timestamps; caller-provided values are not authoritative. Legacy trajectories without timestamps expose a deterministic effective revision.
- MCP exposes 176 REST-backed tools plus `ping`, including read-only usage statistics. The unbounded full-list operations for trajectories and survey runs remain in REST for compatibility but are replaced in MCP by deterministic bounded searches. Target-landing editing and visualization use dedicated projections so clients do not transfer the full mesh-sampling aggregate. Trajectory extrapolation, target-landing, directional-control evaluation, and anti-collision policy tools publish closed inputs; calculated samples, statistics, revisions, and bundles remain server-derived. Policy mutations expose immutable revisions and optimistic-concurrency-protected, effective-dated Field assignments. Every tool publishes a title, strict input and success-output schemas, safety annotations, and operation-specific workflow guidance. Survey-run schemas distinguish observed gravity/geodetic and magnetic/true-north references from canonical WGS84-geodetic inclination and true-north azimuth. Resource-specific ellipse tools rebuild authoritative SurveyRun ancestry before projecting uncertainty onto requested display stations. Octree candidate discovery uses its queued scan/status/result workflow. Separation-factor submissions reject server-derived state, policy snapshots, classifications, and results; callers poll lightweight status and retrieve terminal profiles with SI measured-depth ranges and dimensionless separation factors.
- The trajectory editor supports mean-sea-level depth references through the Vertical Datum integration.
- Survey runs and trajectories share extensible identity and feature catalogs. Both editors support assignments; catalog definitions are managed from the `TrajectoryIdentities` and `TrajectoryFeatures` pages.
- The Backup / Restore page creates versioned JSON backups. Survey runs may be selected independently; selecting a trajectory automatically includes its referenced survey runs and their parent chains. Schema version 2 also carries the complete immutable anti-collision policy library and historical Field assignments; version-1 documents remain restorable. Restore validates the dependency graph, policy revisions, non-overlapping assignment history, and catalogs before one atomic commit. Catalog UUIDs are matched exactly by default; normalized-name mapping requires an explicit opt-in.
- The Usage Statistics page follows the shared resource-service layout with refresh and failure states, responsive summary metrics, and a sortable per-endpoint table containing method, today and total counts, and last use.
- The default identities are `NameForPlanning`, `NameForCompanyReporting`, `NameForRegulatoryReporting`, `Nickname`, and `NameForOperationReporting`. The default feature categories are `SurveyContext`, `BoreholeSectionContext`, `SurveyPurpose`, `TrajectoryPurpose`, `SurveyReferenceStatus`, `AcquisitionMode`, `MeasurementCondition`, `RunningMode`, `DataProcessingState`, `CorrectionApplied`, `QualityStatus`, `QualityIssue`, and `SurveyStationDensity`.
- The WebApp uses the published OSDC shared WebPages packages for Field, Cluster, Rig, Well, WellBore, Survey Instrument, Earth Cartographic Projection, Earth Geodesy, Earth Gravity, Earth Magnetic Field, and Earth Vertical Datum.
- The reusable UI package identity is `OSDC.Drilling.Trajectory.WebPages`. All first-party reusable UI dependencies use their OSDC package identities.
- Production dependency URLs are also expressed as Helm-managed environment variables and point to the OSDC Kubernetes services. The stable public resource routes remain `/Trajectory/api` and `/Trajectory/webapp`.
- `Trajectory.db` schema version 10 stores survey/trajectory records, trajectory-extrapolation, target-landing, and directional-control evaluation cases, immutable anti-collision policy revisions, effective-dated Field assignments, and shared identity/feature catalogs together. Version-1 through version-9 databases are upgraded transactionally; version 5 target-landing rows gain backfilled scalar light-data and compact edit-data columns, version 6 trajectory rows gain backfilled name/description columns, version 8 adds the target-landing covering index, version 9 adds compact directional-control cases plus separate sample chunks, and version 10 renames the persisted anti-collision `AgeThresholdSeconds` JSON member to the canonical-SI, unit-neutral `AgeThreshold`. Complete engineering records and values remain preserved. The legacy catalog file is retained as a rollback copy. Unknown, malformed, incomplete, or newer schemas stop startup without deleting or repairing data.
- `GlobalAntiCollision.db` database schema version 2 uses an indexed `(octree depth, coarse code, trajectory UUID)` membership table plus one state row per trajectory. Spatial-index algorithm version 3 stores a compact, one-cell-padded conservative swept-AABB cover of each 99.9%-confidence uncertainty volume at detailed depth 22; it fills segment interiors and end regions so strict containment and end entry are not missed. `TrajectoryType` and `IsDefinitive` are derived from the authoritative trajectory and used only for spatial-search filtering. Version-1 database rows are preserved through a verified, transactional migration after a timestamped integrity-checked backup is created. Older derived indexes become stale through their provenance hash and are rebuilt automatically without rewriting trajectory data. Cache replacements and deletions are atomic, trajectory writes maintain the cache automatically, and startup reconciliation repairs missing, outdated, or orphaned derived entries. The REST/MCP status operation exposes currentness, source timestamp, algorithm version, confidence factor, calculation hash, bucket count, and detailed-code count.
- Global anti-collision REST/MCP calculations are queued and executed by a background worker instead of holding the initiating HTTP request open. POST/PUT return the queued representation immediately; callers poll the lightweight `GET GlobalAntiCollisions/{id}/Status` operation and retrieve the full result once the state is `Completed`. `RequestedPolicyAssignmentID` is optional: omitting it performs no policy classification, while supplying it requires an assignment belonging to the reference trajectory's Field and makes the service derive the confidence factor plus a frozen applied-assignment/policy snapshot. Confidence is a canonical dimensionless proportion in `(0, 0.999]`; REST and MCP reject any larger value because the octree broad phase is encoded at 0.999. Progress is reported by preparation stage and completed comparison-trajectory count, interrupted jobs resume after a normal service restart, PUT never silently creates a missing record, and all string-ID database operations are parameterized.
- Anti-collision policies are immutable revisions owned by Trajectory. Fields have non-overlapping half-open UTC assignments to exact revision UUIDs. Rules use unique priorities, first-match semantics, AND their conditions, and end in one unconditional default; each enforces `Alert > Alarm > 0`. Conditions inspect only the comparison trajectory and its WellBore/Well/Slot/Cluster hierarchy. Age uses the oldest defined contributing acquisition start or station measurement time. Missing/future evidence and unavailable dependencies become auditable `Indeterminate` outcomes. Completed calculations freeze the policy, assignment UUID, evaluation instant, comparison context, thresholds, and classification.
- The WebApp includes an Anti-collision Scan workflow with a case-insensitive partial-name search in each Field/Cluster/Well/WellBore/Trajectory selector, planned/actual and definitive filtering, selectable octree candidates, sparse or all-reference-depth separation-factor tables, and grouped interactive profiles with measured depth positive downward. Before calculation, the user chooses confidence through the shared Unit Reference system; it defaults to 95% and is limited to the octree confidence of 99.9%. The graph depth axis can cover either the union of calculated separation intervals or the complete reference trajectory. Both octree candidate discovery and separation-factor calculation run as server-side jobs with regularly polled stage/progress updates, so a multi-minute operation does not hold one HTTP request open.
- The WebApp maintains one atomically published, application-wide snapshot of Fields, Clusters, Wells, WellBores, Rigs, Survey Instruments, lightweight WellBore Architectures, lightweight Trajectories, and lightweight Survey Runs. Pages reuse it instead of issuing duplicate full-list requests. The background worker refreshes it every minute, while trajectory and survey-run mutations trigger targeted refreshes of their respective light lists. The reusable WebPages package uses the same abstraction but registers an on-demand direct provider as a `TryAdd` fallback, so another host can use the pages without maintaining a global cache.
- Trajectory and survey-run plots can use the owning Field's persisted reference point as their `Field` position datum. Their `Cartographic` datum is resolved through the Field coordinate-conversion API, alongside WGS84, cluster, and well-head choices where those references are available; unavailable derived references fall back to WGS84.
- Persisted calculation cases and results are durable and are not automatically deleted after 90 days or any other age. They are removed only through explicit delete operations.
- Completed target-landing samples expose server-derived normalized control paths calculated directly from the solved DotNetLibraries CA, CTC, or BT sections. They carry exact interpolated inclination, SI curvature, signed build and turn rates, and local toolface; circular-arc toolface varies along the arc rather than repeating its start/reference value. These controls drive the cylindrical and build/turn Web UI views without reconstructing commands from sparse survey stations. CA and BT paths remain valid in Cartesian space when they pass near vertical, but the control-space plots split their traces within 3 degrees of either vertical direction because toolface and azimuth-based turn rate are singular there; no chord is drawn across that omitted display interval. Closing the detailed editor merges its persisted light record into the existing case table immediately instead of reloading all cases and trajectory names.

## Build, generation, and tests

From the repository root, restore and build the explicit solution:

```powershell
dotnet restore .\Trajectory.sln
dotnet build .\Trajectory.sln --no-restore
```

Public-contract changes require rebuilding `Service` to refresh `ModelSharedOut/json-schemas/TrajectoryFullName.json`, then running `ModelSharedOut` and accepting its overwrite prompt. This regenerates `TrajectoryMergedModel.cs`, `PseudoConstructors.cs`, and `Service/wwwroot/json-schema/TrajectoryMergedModel.json`; generated client files must not be repaired by hand.

Self-contained service tests can run without a server. The generated-client and MCP transport integration tests expect the service at `http://localhost:8080/` with its `/Trajectory/api` path base. See the project READMEs for the precise commands and isolation requirements.

Trajectory REST and MCP contracts now explicitly bind survey MD/Abscissa, WGS84 TVD/Z, Riemannian coordinates, metadata, UUID lookup arguments and relationship identifiers to SemanticCatalogue 0.16.0. See [semantic bindings](SEMANTIC-BINDINGS.md) for aliases, reference conventions and discovery scope.

`GET Trajectory/{id}/Station?alongHoleDepth=486` evaluates a complete station at the supplied MD in SI metres using the stored trajectory's calculation method. It is read-only, requires a completed trajectory and rejects extrapolation. The corresponding MCP tool exposes the typed trajectory UUID and along-hole-depth key; consumers can project any declared station quantity rather than use a separate operation for TVD, inclination or azimuth.


Station evaluation synchronizes the geographic coordinate cache from canonical Riemannian north/east through the shared WGS84 implementation. Latitude/longitude can therefore feed downstream spatial-field queries even when interpolation populated only X/Y/Z.

## Read-only reference and uncertainty evaluation

`GET Trajectory/{id}/ReferencedStation?alongHoleDepth=350&originWgs84Depth=...` evaluates signed path length from a verified WGS84-depth surface intersection. The path must be complete, monotone in vertical depth and contain both the intersection and requested station. The service uses the stored calculation method and shared survey interpolation; it assumes no path extension. Requested MD, origin depth, native intersection MD and complete canonical station are returned separately.

`GET Trajectory/{id}/VerticalEllipse?alongHoleDepth=500&confidenceFactor=0.95` reconstructs authoritative survey-run uncertainty lineage without persisting a case. It returns full major/minor diameters (SI metres), orientation (SI radians), confidence probability and the selected vertical-section azimuth. The stable section uses the source path's first/last horizontally separated stations. The major-axis direction is `(-sin(phi),cos(phi))` in `(section distance, positive-down TVD)`; axes are unoriented modulo pi. These parameters have explicit REST/MCP catalogue bindings, including quantity and reference conventions.
