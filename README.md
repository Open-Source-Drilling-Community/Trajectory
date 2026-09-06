# Trajectory

The Trajectory repository contains the OSDC Trajectory service, the host web application, and a reusable Razor class library for the Trajectory UI pages. The solution-owned .NET namespace root is `OSDC.Drilling.Trajectory`; the companion anti-collision library uses `OSDC.Drilling.GlobalAntiCollision`.

## Solution Architecture

The solution currently contains:

- `ModelSharedIn`
  - auto-generated C# classes for upstream model dependencies
  - source OpenAPI schemas for Field, Cluster, Well, WellBore, WellBore Architecture, and Survey Instrument
- `Model`
  - domain model and trajectory calculation logic
  - trajectory interpolation and stochastic trajectory realization calculations
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
  - Razor class library containing the Trajectory, TrajectoryInterpolation, and TrajectoryRealization pages and their page-specific support components
  - depends on `ModelSharedOut`
- `WebApp`
  - ASP.NET Core Blazor host application
  - depends on `WebPages`
  - provides the host shell, routing, configuration, and static assets for the UI
- `ModelTest`
  - NUnit project reserved for model and computation tests; it currently has no discoverable cases
- `ServiceTest`
  - self-contained contract/persistence tests and integration tests for the running service API
- `GlobalAntiCollisionTest`
  - executable verification harness for the anti-collision implementation
- `home`
  - local persisted data, including `Trajectory.db`, `GlobalAntiCollision.db`, `SeparationFactorResults.db`, and usage history

## Main Workflows

The repository supports the following main trajectory workflows:

- trajectory creation, editing, storage, and retrieval
- survey-run import, editing, calculation, and chunked station transfer
- trajectory interpolation cases
- stochastic trajectory realization cases based on survey station wellbore position uncertainty
- trajectory aggregation, station-ellipse, and survey-run/trajectory minimum-distance calculations
- automatically maintained global anti-collision octree indexes
- shared identity and feature catalogs
- versioned, dependency-closed backup and atomic restore

Trajectory realization cases are defined from a reference trajectory and a requested number of realizations. The model optionally coarsens the reference trajectory before generation, draws realizations from the covariance-defined uncertainty field, completes the generated points with the minimum curvature method, and stores the resulting realized trajectories as lists of survey points. Large realization sets are persisted and retrieved in chunks.

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

The Helm charts are named `osdcdrillingtrajectoryservice` and `osdcdrillingtrajectorywebappclient`. The service chart deliberately retains the historical `trajectory-claim` PVC and all database filenames. Use `--set persistence.existingClaim=trajectory-claim` for the identity cutover, and do not uninstall the legacy release before verifying the selected cluster, namespace, mounted claim, image digest, and existing record counts. Its `Recreate` strategy prevents overlapping SQLite writers.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

## Current implementation

- The service exposes its REST operations through MCP over streamable HTTP at `/trajectory/api/mcp` and WebSocket at `/trajectory/api/mcp/ws`.
- MCP exposes 130 REST-backed tools plus `ping`; the usage-statistics controller is intentionally excluded. The unbounded full-list operations for trajectories and survey runs remain in REST for compatibility but are replaced in MCP by deterministic `trajectory_search_trajectory` and `survey_run_search_survey_run` pages (default 100, maximum 500) with text and relationship/type filters. Read-only `validate_external_references` tools check one Trajectory or SurveyRun, while the corresponding audit tools check UUID-ordered pages of at most 100 stored records. These checks cover Field, Cluster, Well and WellBore references plus SurveyInstrument for SurveyRuns; missing resources are distinguished from unavailable dependency services. Every tool publishes a title, strict input and success-output schemas, safety annotations, and operation-specific workflow guidance. Schemas enforce non-empty UUIDs, reject unknown arguments, constrain usable backup/restore policies, and document nested models, chunk indexes, identity/feature assignments, nullability, and SI units. Octree candidate discovery is exposed only through its queued scan/status/result workflow and rejects requests that exclude both planned and actual trajectories. Separation-factor submissions require exactly one reference, a valid confidence factor and unique selected comparison UUIDs while rejecting server-derived state/results; callers poll lightweight status and retrieve terminal profiles with explicit relevant MD ranges in SI metres. Successful calls provide structured JSON and text fallback; failures use stable sanitized MCP error envelopes.
- The trajectory editor supports mean-sea-level depth references through the Vertical Datum integration.
- Survey runs and trajectories share extensible identity and feature catalogs. Both editors support assignments; catalog definitions are managed from the `TrajectoryIdentities` and `TrajectoryFeatures` pages.
- The Backup / Restore page creates versioned JSON backups. Survey runs may be selected independently; selecting a trajectory automatically includes its referenced survey runs and their parent chains. Restore validates the complete dependency graph, resolves the shared catalogs, writes survey runs before trajectories, and commits record changes atomically without recalculation. Catalog UUIDs are matched exactly by default; normalized-name mapping requires an explicit opt-in.
- The Usage Statistics page follows the shared resource-service layout with refresh and failure states, responsive summary metrics, and a sortable per-endpoint table containing method, today and total counts, and last use.
- The default identities are `NameForPlanning`, `NameForCompanyReporting`, `NameForRegulatoryReporting`, `Nickname`, and `NameForOperationReporting`. The default feature categories are `SurveyContext`, `BoreholeSectionContext`, `SurveyPurpose`, `TrajectoryPurpose`, `SurveyReferenceStatus`, `AcquisitionMode`, `MeasurementCondition`, `RunningMode`, `DataProcessingState`, `CorrectionApplied`, `QualityStatus`, `QualityIssue`, and `SurveyStationDensity`.
- The WebApp uses the published OSDC shared WebPages packages for Field, Cluster, Rig, Well, WellBore, Survey Instrument, Earth Cartographic Projection, Earth Geodesy, Earth Gravity, Earth Magnetic Field, and Earth Vertical Datum.
- The reusable UI package identity is `OSDC.Drilling.Trajectory.WebPages`. All first-party reusable UI dependencies use their OSDC package identities.
- Production dependency URLs are also expressed as Helm-managed environment variables and point to the OSDC Kubernetes services. The stable public resource routes remain `/Trajectory/api` and `/Trajectory/webapp`.
- `Trajectory.db` schema version 2 stores survey/trajectory records and their shared identity and feature catalogs together. Version-0/1 databases are upgraded transactionally by adding the two catalog tables and copying rows from a validated legacy `TrajectoryCatalog.db`; the legacy file is retained as a rollback copy. Existing domain rows are not rewritten. Unknown, malformed, incomplete, or newer schemas stop startup without deleting or repairing data.
- `GlobalAntiCollision.db` database schema version 2 uses an indexed `(octree depth, coarse code, trajectory UUID)` membership table plus one state row per trajectory. Spatial-index algorithm version 3 stores a compact, one-cell-padded conservative swept-AABB cover of each 99.9%-confidence uncertainty volume at detailed depth 22; it fills segment interiors and end regions so strict containment and end entry are not missed. `TrajectoryType` and `IsDefinitive` are derived from the authoritative trajectory and used only for spatial-search filtering. Version-1 database rows are preserved through a verified, transactional migration after a timestamped integrity-checked backup is created. Older derived indexes become stale through their provenance hash and are rebuilt automatically without rewriting trajectory data. Cache replacements and deletions are atomic, trajectory writes maintain the cache automatically, and startup reconciliation repairs missing, outdated, or orphaned derived entries. The REST/MCP status operation exposes currentness, source timestamp, algorithm version, confidence factor, calculation hash, bucket count, and detailed-code count.
- Global anti-collision REST/MCP calculations are queued and executed by a background worker instead of holding the initiating HTTP request open. POST/PUT return the queued representation immediately; callers poll the lightweight `GET GlobalAntiCollisions/{id}/Status` operation and retrieve the full result once the state is `Completed`. Progress is reported by preparation stage and completed comparison-trajectory count, interrupted jobs resume after a normal service restart, PUT never silently creates a missing record, and all string-ID database operations are parameterized.
- The WebApp includes an Anti-collision Scan workflow with a case-insensitive partial-name search in each Field/Cluster/Well/WellBore/Trajectory selector, planned/actual and definitive filtering, selectable octree candidates, sparse or all-reference-depth separation-factor tables, and grouped interactive profiles with measured depth positive downward. The graph depth axis can cover either the union of calculated separation intervals or the complete reference trajectory. Both octree candidate discovery and separation-factor calculation run as server-side jobs with regularly polled stage/progress updates, so a multi-minute operation does not hold one HTTP request open.
- Persisted calculation cases and results are durable and are not automatically deleted after 90 days or any other age. They are removed only through explicit delete operations.

## Build, generation, and tests

From the repository root, restore and build the explicit solution:

```powershell
dotnet restore .\Trajectory.sln
dotnet build .\Trajectory.sln --no-restore
```

Public-contract changes require rebuilding `Service` to refresh `ModelSharedOut/json-schemas/TrajectoryFullName.json`, then running `ModelSharedOut` and accepting its overwrite prompt. This regenerates `TrajectoryMergedModel.cs`, `PseudoConstructors.cs`, and `Service/wwwroot/json-schema/TrajectoryMergedModel.json`; generated client files must not be repaired by hand.

Self-contained service tests can run without a server. The generated-client and MCP transport integration tests expect the service at `http://localhost:8080/` with its `/Trajectory/api` path base. See the project READMEs for the precise commands and isolation requirements.
