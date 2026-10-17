# Trajectory WebApp

`WebApp` is the ASP.NET Core Blazor host application for Trajectory under `OSDC.Drilling.Trajectory.WebApp`.

It provides the application shell, host-owned `/Home` route, startup configuration, routing, and static assets for the UI. Survey Run, Trajectory, interpolation, aggregation, realization, extrapolation, target landing, directional-control evaluation, distance/ellipse, anti-collision, catalog, batch-import, backup/restore, display, and usage-statistics pages are provided by the `WebPages` Razor class library.

The anti-collision scan provides case-insensitive partial-name search in its Field, Cluster, Well, WellBore, and Trajectory selectors. It submits both octree candidate discovery and selected separation-factor comparisons as background service jobs. Each phase polls a lightweight status endpoint and shows measured progress, allowing multi-minute work to continue without an HTTP request timeout; candidate UUIDs and complete calculation results are downloaded only after their respective jobs complete. Its separation-factor graph can show either the union of calculated depth intervals or the whole reference trajectory. The Anti-collision Policies page creates immutable revisions and effective-dated Field assignments; completed results display the frozen policy and per-comparison Alert/Alarm classification.

The host maintains one application-wide reference-data snapshot for Fields, Clusters, Wells, WellBores, Rigs, Survey Instruments, lightweight WellBore Architectures, lightweight Trajectories, and lightweight Survey Runs. Pages reuse this snapshot instead of downloading the same lists whenever the user navigates between workflows. A background worker refreshes all lists concurrently every minute. Trajectory and Survey Run mutations use targeted refresh operations so their shared lists become current without reloading the external catalogs. Snapshot publication is atomic, and a failed refresh leaves the last successful snapshot available, so a temporary dependency outage does not erase working reference data. Full catalog responses are used because UUID-only polling cannot detect renames or hierarchy changes with the currently published dependency contracts.

Trajectory and Survey Run workflows use one shared searchable ownership selector for Field, Cluster, Well, WellBore, and the optional Trajectory or Survey Run. Each level accepts partial names and constrains the choices below it. Rig job is selected separately on depth-reference-aware pages: the default is the job active at the Survey Run acquisition date or the latest constituent Survey Run date of a Trajectory, and changing it updates the rotary-table depth reference used for display conversions.

## Container

The host application is packaged as the Docker image:

`docker.io/digiwells/osdcdrillingtrajectorywebappclient:stable`

It is published under the `digiwells` organization:

https://hub.docker.com/?namespace=digiwells

## UI Endpoint

The web application is available at:

https://dev.digiwells.no/Trajectory/webapp/Trajectory

https://app.digiwells.no/Trajectory/webapp/Trajectory

https://awe.web.intra.norceresearch.no/Trajectory/webapp/Trajectory

The backing service OpenAPI endpoint is available at:

https://dev.digiwells.no/Trajectory/api/swagger

https://app.digiwells.no/Trajectory/api/swagger

https://awe.web.intra.norceresearch.no/Trajectory/api/swagger

## Project Relationship

- `WebApp` hosts and configures the UI.
- `WebPages` contains the reusable Razor pages and page-specific support components.
- `ModelSharedOut` provides the generated service client types used by `WebPages`.

## Navigation

Home is the first side-menu entry, followed by Survey Management and Survey Calculations. A dedicated top-level Anti-collision Management group comes immediately after Survey Calculations and contains the octree-backed Anti-collision Scan, immutable policy/Field-assignment administration, Survey Run Distance, and Trajectory Distance. The remaining menu exposes the shared Identities and Features catalogs, trajectory interpolation/realization/aggregation/extrapolation calculations, batch import and dependency-aware Backup / Restore, reporting views, contextual data, calculators, and endpoint-level Usage Statistics. The Trajectory Extrapolation editor supports lead-in-aware reconnect and geosteering workflows, including overall departure/bearing and steering-length/section-ratio constraints. Steering length excludes the separate lead-in continuation. Its interpolated result can be exported with the shared calculated-survey ASCII structure or saved atomically as a planned Survey Run in the source WellBore, inheriting the last usable source Survey Run's instrument. The Survey Run editor supports an optional, explicitly marked terminal bit extrapolation using a unit-aware measurement-tool-to-bit distance. Survey Run and Trajectory editors both assign definitions from the shared catalogs. Contextual data is ordered Field, Cluster, Well, WellBore, Rig, and Survey Instrument. Calculators provide cartographic conversion, MSL/WGS84 vertical-datum conversion, gravitational and magnetic vectors, and unit conversion.

Survey Calculations also includes Directional Control Evaluation. Its reusable page compares an actual trajectory with reconnect commands toward a reference trajectory in the same WellBore, monitors the server-side calculation after navigation, and retrieves large interval results by page while keeping case discovery lightweight.

Trajectory and survey-run visualizations expose `Field` and `Cartographic` position references when the selected hierarchy resolves to a Field with a persisted reference point. Cartographic coordinates are resolved through the Field coordinate-conversion API; unavailable reference values fall back to WGS84.

The Home page is the user-facing capability index. It documents the canonical SI/WGS84 contract, links directly to every owned workflow, explains queued calculations and chunked results, and identifies REST/OpenAPI, the the current MCP tools plus `ping`, and the reusable Razor package as equivalent access paths to the same persisted service state.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

## Current shared-page dependencies

The host consumes the local `OSDC.Drilling.Trajectory.WebPages` project and the published OSDC WebPages packages for the migrated sibling services, including `OSDC.Drilling.WellBoreArchitecture.WebPages`.

The renamed Helm chart is `charts/osdcdrillingtrajectorywebappclient`. TCP startup, readiness, and liveness probes keep ingress traffic away from a replacement pod until the Blazor server is accepting connections. All production dependency URLs are supplied as Helm-managed environment variables and point to the corresponding OSDC Kubernetes services. `TrajectoryHostURL` points to `http://osdctrajectoryservice/`. The public routes remain `/Trajectory/webapp` and `/Trajectory/api` because they identify the domain resource rather than the owning organization.

## Hosting requirements and local execution

The host registers server-side Blazor, MudBlazor, `AddHttpClient()`, `ITrajectoryWebPagesConfiguration`, the singleton `ITrajectoryReferenceDataCache` and its background refresh worker, and the imported OSDC WebPages services. It registers the singleton cache before calling `AddTrajectoryWebPages()`, so the reusable package's `TryAdd` fallback does not replace the application-wide cache. Hosts without a cache receive the package's on-demand direct provider instead. Route discovery includes the required reusable assemblies without giving those packages a generic `/Home` route; `/Home` remains owned by this WebApp.

Configuration must supply the Trajectory, Field, Cluster, Rig, Well, WellBore, WellBore Architecture, Survey Instrument, Unit Conversion, Cartographic Projection, Earth Geodesy, Earth Gravity, Earth Magnetic Field, and Earth Vertical Datum URLs. Development settings use public DigiWells URLs and production/Helm settings use in-cluster service names.

Run locally with:

```powershell
dotnet run --project .\WebApp\WebApp.csproj
```

The application always uses `/trajectory/webapp` as its path base, so test links beneath that prefix rather than only at the site root.

## Bounded light discovery

Resource and catalog discovery returns light pages, with `offset` defaulting to 0 and `limit` defaulting to 50 (allowed range 1–200). Names and relevant resource-reference filters are evaluated on the server. Unbounded full-object `HeavyData` routes and tools, including filtered full-object lists, have been removed. Existing UUID and HTTP-metadata listings remain available; use named light pages for routine discovery.

REST routes and MCP tool names (relative to the service's existing API path base):

- `/AntiCollisionPolicyRevision/LightData` / `anti_collision_policy_revision_discover`
- `/DirectionalControlEvaluationCase/LightData` / `directional_control_evaluation_case_discover`
- `/InterpolatedTrajectory/LightData` / `interpolated_trajectory_discover`
- `/SurveyRunBatchImport/LightData` / `survey_run_batch_import_discover`
- `/SurveyRun/LightData` / `survey_run_discover`
- `/SurveyRunMinimumDistanceCalculation/LightData` / `survey_run_minimum_distance_calculation_discover`
- `/TargetLandingCase/LightData` / `target_landing_case_discover`
- `/TrajectoryAggregationCase/LightData` / `trajectory_aggregation_case_discover`
- `/Trajectory/LightData` / `trajectory_discover`
- `/TrajectoryExtrapolationCase/LightData` / `trajectory_extrapolation_case_discover`
- `/TrajectoryFeatureCategory/LightData` / `trajectory_feature_category_discover`
- `/TrajectoryIdentity/LightData` / `trajectory_identity_discover`
- `/TrajectoryMinimumDistanceCalculation/LightData` / `trajectory_minimum_distance_calculation_discover`
- `/TrajectoryRealizationCase/LightData` / `trajectory_realization_case_discover`
- `/SurveyStationEllipseCalculation/LightData` / `survey_station_ellipse_calculation_discover`
- `/FieldAntiCollisionPolicyAssignment/LightData` / `field_anti_collision_policy_assignment_discover`
- `/GlobalAntiCollisions/LightData` / `global_anti_collisions_discover`
- `/Octrees/LightData` / `octrees_discover`

Pages contain `Items`, `Offset`, `Limit`, `TotalCount`, and `NextOffset`; `NextOffset` is null on the final page. Offset pages are individually consistent reads, not a snapshot spanning requests: concurrent inserts/deletes can change later pages. Refresh discovery when the underlying collection changes. EarthGeodesy's existing search pages instead expose `Items[].Value`, `Offset`, `Limit`, and `TotalCount`.

Single-record reads, result/chunk reads, and deliberate backup/export/restore operations are preserved. Backups can still contain complete resources and should be treated as explicit bulk operations.

This is a breaking REST/MCP contract change: update generated clients, UIs, and external integrations together with the service release. Removed heavy-list names are not compatibility aliases. No persisted engineering shapes, database names, or storage volumes are changed. Historical usage counters are retained; bounded discovery has a separate additive `DiscoveryRequestsPerDay` history in resource services.


Tables/selectors use light discovery. Editors retrieve the selected complete resource; category options load only for chosen or already assigned definitions. Slot context uses paginated light locations, and rig-history context reads the selected wellbore. Backup selection uses discovery while the dedicated export/restore APIs retain full backup semantics.
