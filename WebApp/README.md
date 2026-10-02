# Trajectory WebApp

`WebApp` is the ASP.NET Core Blazor host application for Trajectory under `OSDC.Drilling.Trajectory.WebApp`.

It provides the application shell, host-owned `/Home` route, startup configuration, routing, and static assets for the UI. Survey Run, Trajectory, trajectory extrapolation, anti-collision scan, calculation, catalog, backup/restore, and usage-statistics pages are provided by the `WebPages` Razor class library.

The anti-collision scan provides case-insensitive partial-name search in its Field, Cluster, Well, WellBore, and Trajectory selectors. It submits both octree candidate discovery and selected separation-factor comparisons as background service jobs. Each phase polls a lightweight status endpoint and shows measured progress, allowing multi-minute work to continue without an HTTP request timeout; candidate UUIDs and complete calculation results are downloaded only after their respective jobs complete. Its separation-factor graph can show either the union of calculated depth intervals or the whole reference trajectory. The Anti-collision Policies page creates immutable revisions and effective-dated Field assignments; completed results display the frozen policy and per-comparison Alert/Alarm classification.

The host maintains one application-wide reference-data snapshot for Fields, Clusters, Wells, WellBores, Rigs, Survey Instruments, and lightweight WellBore Architectures. Trajectory pages reuse this snapshot instead of downloading the same catalogs whenever the user navigates between pages. A background worker refreshes all catalogs concurrently every minute. Refresh publication is atomic, and a failed refresh leaves the last successful snapshot available, so a temporary dependency outage does not erase working reference data. Full catalog responses are used because UUID-only polling cannot detect renames or hierarchy changes with the currently published dependency contracts.

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

Home is the first side-menu entry. The menu then exposes Survey Run and Trajectory management, the shared Identities and Features catalogs, the octree-backed Anti-collision Scan, trajectory interpolation/realization/aggregation/extrapolation calculations, batch import and dependency-aware Backup / Restore, reporting views, contextual data, calculators, and endpoint-level Usage Statistics. The Trajectory Extrapolation editor supports lead-in-aware reconnect and geosteering workflows, including overall departure/bearing and steering-length/section-ratio constraints. Steering length excludes the separate lead-in continuation. Its interpolated result can be exported with the shared calculated-survey ASCII structure or saved atomically as a planned Survey Run in the source WellBore, inheriting the last usable source Survey Run's instrument. The Survey Run editor supports an optional, explicitly marked terminal bit extrapolation using a unit-aware measurement-tool-to-bit distance. Survey Run and Trajectory editors both assign definitions from the shared catalogs. Contextual data is ordered Field, Cluster, Well, WellBore, Rig, and Survey Instrument. Calculators provide cartographic conversion, MSL/WGS84 vertical-datum conversion, gravitational and magnetic vectors, and unit conversion.

Trajectory and survey-run visualizations expose `Field` and `Cartographic` position references when the selected hierarchy resolves to a Field with a persisted reference point. Cartographic coordinates are resolved through the Field coordinate-conversion API; unavailable reference values fall back to WGS84.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

## Current shared-page dependencies

The host consumes the local `OSDC.Drilling.Trajectory.WebPages` project and the published OSDC WebPages packages for the migrated sibling services, including `OSDC.Drilling.WellBoreArchitecture.WebPages`.

The renamed Helm chart is `charts/osdcdrillingtrajectorywebappclient`. All production dependency URLs are supplied as Helm-managed environment variables and point to the corresponding OSDC Kubernetes services. `TrajectoryHostURL` points to `http://osdctrajectoryservice/`. The public routes remain `/Trajectory/webapp` and `/Trajectory/api` because they identify the domain resource rather than the owning organization.

## Hosting requirements and local execution

The host registers server-side Blazor, MudBlazor, `AddHttpClient()`, `ITrajectoryWebPagesConfiguration`, `ITrajectoryAPIUtils`, the singleton `ITrajectoryReferenceDataCache` and its background refresh worker, and the imported OSDC WebPages services. Route discovery includes the required reusable assemblies without giving those packages a generic `/Home` route; `/Home` remains owned by this WebApp.

Configuration must supply the Trajectory, Field, Cluster, Rig, Well, WellBore, WellBore Architecture, Survey Instrument, Unit Conversion, Cartographic Projection, Earth Geodesy, Earth Gravity, Earth Magnetic Field, and Earth Vertical Datum URLs. Development settings use public DigiWells URLs and production/Helm settings use in-cluster service names.

Run locally with:

```powershell
dotnet run --project .\WebApp\WebApp.csproj
```

The application always uses `/trajectory/webapp` as its path base, so test links beneath that prefix rather than only at the site root.
