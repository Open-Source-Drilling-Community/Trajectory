# OSDC.Drilling.Trajectory.WebPages

This release targets MudBlazor 9.9.0 and the matching OSDC shared web component packages.

`OSDC.Drilling.Trajectory.WebPages` is a Razor class library that contains the Trajectory UI pages extracted from the main Trajectory web application.

It currently provides routed pages for:

- `SurveyRun` and batch survey-run import
- `TrajectoryMain`
- `TrajectoryEdit`
- `TrajectoryInterpolatedMain`
- `TrajectoryInterpolationEdit`
- `TrajectoryRealizationMain`
- `TrajectoryRealizationEdit`
- trajectory aggregation
- survey-run and trajectory minimum-distance calculations
- `AntiCollisionScan`, for filtered octree candidate discovery and separation-factor tables and profiles
- supporting UI components used by those pages
- `TrajectoryIdentities` and `TrajectoryFeatures`
- `TrajectoryBackupRestore`, for dependency-aware JSON backup and restore
- `StatisticsTrajectory`, for refreshable summary and per-endpoint usage statistics

## Purpose

This package makes the Trajectory, TrajectoryInterpolation, and TrajectoryRealization pages reusable from another ASP.NET Core Blazor host application without copying the page source into that host.

Trajectory and survey-run plots offer `Field` and `Cartographic` position references when the selected resource resolves to a Field with a persisted reference point. The Field offset comes from the authoritative Field contract, while the cartographic offset is calculated through the Field coordinate-conversion API. Unavailable references fall back to WGS84 rather than presenting or relabelling zero-offset coordinates.

## Trajectory Realization UI

The trajectory realization page lets a user create stochastic realization cases from an existing reference trajectory. The reference trajectory is selected through the field, cluster, well, wellbore, and trajectory selectors.

The edit page supports:

- realization count, limited to 1000 in the UI
- calculation status and progress polling
- advanced options for random seed and coarsening threshold
- loading realized trajectories through chunked service endpoints
- 3D, horizontal projection, and vertical section plots
- a configurable maximum number of displayed realizations, defaulting to 50
- export of realized trajectories to a user-selected file

Exported columns per realization are `MD`, `Incl`, `Az`, `TVD`, `North`, `East`, `DLS`, `BUR`, `TUR`, and `VSect`. The export dialog lets the user choose separator, units and references, and whether realizations are written side by side or one after another.

## Anti-collision scan

`AntiCollisionScan` selects a reference trajectory through the standard Field, Cluster, Well, WellBore, and Trajectory hierarchy. Each selector supports case-insensitive matching on any part of the displayed name. Pseudo-clusters belonging to single wells are omitted from the Cluster selector while those wells remain selectable at Well level. The scan can include planned trajectories, actual trajectories, or both, and can restrict candidates to definitive trajectories.

Candidate discovery uses the service's persistent conservative uncertainty-volume octree and requires the reference index to be current. Its one-cell-padded swept-AABB cover is deliberately broad: a candidate may be a false positive, while the subsequent separation-factor calculation establishes the relevant measured-depth ranges and actual safety factors. The scan is queued server-side; the page polls a lightweight status endpoint and displays real bucket-loading and exact-intersection progress, then retrieves the candidate UUIDs only after completion. Users can select all or some candidates and choose the separation-factor confidence before calculating. The editor uses the shared Unit Reference system's `ProportionStandard` quantity, defaults to 95%, and prevents values above the octree encoding confidence of 99.9%. The canonical API value remains a dimensionless proportion. That calculation is also queued server-side, so either phase may last several minutes without depending on one long HTTP response. The page polls its progress and retrieves the full result only once calculation completes. Results are shown as either every reference survey depth (with empty cells where no comparison was needed) or only depths with at least one result, and as color-coded interactive curves with positive depth downward. The graph can fit its depth axis to the union of all separation intervals or extend it across the complete reference trajectory. Disjoint intervals share a legend group, so one legend click toggles every interval for a trajectory.

## Dependencies

The package compiles the generated Trajectory DTO/client sources from `ModelSharedOut` into the package and depends on:

- `OSDC.DotnetLibraries.Drilling.Surveying`
- `OSDC.DotnetLibraries.Drilling.WebAppUtils`
- `Plotly.Blazor`

`OSDC.DotnetLibraries.Drilling.WebAppUtils` supplies MudBlazor and the OSDC unit-conversion components transitively. A consuming host must still register their runtime services and configuration.

## Host Application Requirements

The consuming application is expected to:

- reference this package
- configure routing so the assembly containing `OSDC.Drilling.Trajectory.WebPages` components is discovered
- provide the required MudBlazor services
- register `AddHttpClient()` because the pages use `IHttpClientFactory`
- load the Plotly.Blazor static assets
- register an `ITrajectoryAPIUtils` implementation in dependency injection
- register an `ITrajectoryWebPagesConfiguration` implementation
- ensure the generated Trajectory client and OSDC unit-conversion components are available

## Configuration

The pages depend on an injected `ITrajectoryAPIUtils` service.

The streamlined design is to register:

- a host-side `ITrajectoryWebPagesConfiguration`
- the concrete `TrajectoryAPIUtils`

`ITrajectoryWebPagesConfiguration` extends the following host URL interfaces from `OSDC.DotnetLibraries.Drilling.WebAppUtils`:

- `IFieldHostURL`
- `IClusterHostURL`
- `IRigHostURL`
- `IWellHostURL`
- `IWellBoreHostURL`
- `IWellBoreArchitectureHostURL`
- `ITrajectoryHostURL`
- `IUnitConversionHostURL`
- `ISurveyInstrumentHostURL`

It also requires `EarthMagneticFieldHostURL` and `VerticalDatumHostURL` string properties for survey corrections and depth-reference presentation. The standalone WebApp additionally configures the Earth Gravity and cartographic/geodetic calculator pages it hosts.

The host application is responsible for supplying those endpoint values through its configuration object.

## Notes

This package contains the UI pages and page-specific support code. It does not by itself provide the service backend.

The package, assembly, and static-web-asset base identity are all `OSDC.Drilling.Trajectory.WebPages`. For example, the 3D camera helper is loaded from `_content/OSDC.Drilling.Trajectory.WebPages/scatter3dCameraPersistence.js`. The NuGet publishing workflow produces `OSDC.Drilling.Trajectory.WebPages.<version>.nupkg`.

## Identities and features

`TrajectoryIdentities` and `TrajectoryFeatures` manage the catalogs shared by survey runs and trajectories. `IdentityFeatureAssignments` is embedded in both resource editors and enforces each category's option and validity-period shape through the service API.

## Backup and restore

`TrajectoryBackupRestore` lets users select survey runs and trajectories or back up everything. A selected trajectory automatically brings along all survey runs used by its sections, while parent survey runs and relevant catalog definitions are also included. Restore previews both record counts and offers explicit record-conflict and catalog-resolution policies before sending the complete document to the service. Catalog UUIDs are matched exactly by default; mapping compatible definitions with different UUIDs by normalized name requires a separate warning-bearing opt-in.

## Usage statistics

`StatisticsTrajectory` follows the shared OSDC resource-service layout. It provides refresh and failure states, total and current-UTC-day request counts, the tracked-endpoint count, the service's last-save time, and a sortable table showing HTTP method, operation, daily and lifetime totals, and last use.

## Mean-sea-level depth references

Trajectory editing resolves mean-sea-level depth references through `MslDepthReferenceUtils`. The editor uses the configured Vertical Datum service data when presenting and updating trajectory interpolation values.

Canonical service values remain SI metres relative to WGS84. Changing the UI depth reference changes both the displayed value and unit label; values are converted back to WGS84 before submission.

## Packaging

Build before packing so the Razor static-web-assets manifest and generated DTO sources are current:

```powershell
dotnet build .\WebPages\WebPages.csproj --configuration Release
dotnet pack .\WebPages\WebPages.csproj --configuration Release --no-build
```

After a service contract change, regenerate `ModelSharedOut` before building this package.
