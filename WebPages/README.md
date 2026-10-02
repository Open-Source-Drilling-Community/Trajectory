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
- trajectory extrapolation, including fixed continuation, reference-trajectory reconnection, and constrained well-path solving
- survey-run and trajectory minimum-distance calculations
- `AntiCollisionScan`, for filtered octree candidate discovery and separation-factor tables and profiles
- supporting UI components used by those pages
- `TrajectoryIdentities` and `TrajectoryFeatures`
- `TrajectoryBackupRestore`, for dependency-aware JSON backup and restore
- `StatisticsTrajectory`, for refreshable summary and per-endpoint usage statistics

## Purpose

This package makes the Trajectory, TrajectoryInterpolation, and TrajectoryRealization pages reusable from another ASP.NET Core Blazor host application without copying the page source into that host.

Trajectory and survey-run plots offer `Field` and `Cartographic` position references when the selected resource resolves to a Field with a persisted reference point. The Field offset comes from the authoritative Field contract, while the cartographic offset is calculated through the Field coordinate-conversion API. Unavailable references fall back to WGS84 rather than presenting or relabelling zero-offset coordinates.

The current Rig and rotary-table depth reference come from the latest chronological WellBore `RigJob`. Mobile-rig jobs use their job-owned Gaussian drill-floor depth; Platform Rig jobs resolve the depth owned by the Rig. `RigJobs = []` is authoritative and suppresses inference from Cluster data, while `RigJobs = null` retains the legacy direct-`RigID`/Cluster fallback during migration. Only the depth mean is reference-transformed; its standard uncertainty remains a length.


## Survey-run observations and import

The SurveyRun editor displays and edits the observed angles, per-station reference overrides, UTC measurement time, applied corrections, canonical geodetic/true-north angles, and correction status. Inclination reference, azimuth reference, and measurement time can also be applied to every station in one operation; individual rows remain editable afterward, and an empty bulk time clears every station time. Run defaults select geodetic versus gravity vertical, true versus magnetic north, geomagnetic model policy, and an optional UTC acquisition interval. Changing a run default, geomagnetic model, acquisition interval, or bulk station setting invalidates affected computed corrections and clears the stale calculated trajectory until it is recalculated. Manual corrections are retained as explicit overrides.

SurveyRun, Trajectory, and interpolated-Trajectory uncertainty views send the owning resource UUID to the corresponding lineage-aware ellipse endpoint. The service therefore rebuilds authoritative parent SurveyRun uncertainty and replaces stale or partial covariance before the shared table displays horizontal, vertical, and perpendicular ellipses. Every vertical ellipse in one result uses the same first-to-last vertical-section curtain, rather than rotating with the poorly conditioned instantaneous azimuth near vertical inclination. Standalone extrapolation continues to submit its explicit source history because its requested stations extend beyond the source trajectory.

The editor also offers an optional terminal bit extrapolation. Users select server calculation from the last measured curve or declare that the final imported row is already extrapolated, then enter the unit-aware measurement-tool-to-bit distance. Measurement and calculated-station tables show explicit origin chips. Bulk reference and time operations skip extrapolated rows, and an automatically generated bit row is displayed only in the calculated survey result.

Field, Cluster, Well, WellBore, and optional Trajectory/Survey Run selection is implemented by the shared `TrajectoryResourceFilter` component across the Survey Run, Trajectory, batch-import, extrapolation, aggregation, interpolation, realization, minimum-distance, and anti-collision workflows. Every level is a searchable autocomplete with case-insensitive partial-name matching, and downstream choices are constrained by the selected hierarchy. Rig job is deliberately separate from that ownership hierarchy. Pages with active depth-reference conversion use `RigJobSelector`; it selects the job active at the Survey Run acquisition date or at the latest constituent Survey Run date of a Trajectory, falling back to the nearest dated job and then the latest job when no date exists. Changing the rig job immediately changes the rotary-table WGS84 depth used by the shared unit/reference component without changing persisted survey or trajectory data.

Confirmation, unsaved-change, deletion, restore, and short text-entry dialogs use the shared `TrajectoryDialogOptions.Compact` configuration. They are capped at MudBlazor's extra-small responsive width and do not expand to the viewport width.

Direct and batch imports can read an optional UTC timestamp column (or fixed-width field), declare gravity-vertical and/or magnetic-north source data, and supply a per-run acquisition interval when individual times are unavailable. Batch configurations and exported association descriptions retain these settings. Survey-measurement TSV export includes raw readings, corrected values, timestamps, correction state, evaluation context, and dependency-model hashes so it is suitable for audit rather than only trajectory reconstruction.

After a Trajectory is saved, the editor polls its background calculation state and then reloads the calculated station chunks. The calculated table and plots therefore refresh when calculation completes even when their expansion panel was already open when Save was selected.

## Trajectory extrapolation UI

The `TrajectoryExtrapolation` page selects a calculated source trajectory through searchable Field, Cluster, Well, WellBore, and Trajectory controls, each supporting case-insensitive partial-name matching, and accepts a display-unit-aware interpolation interval. Directly below those choices it loads the source trajectory's final calculated station as the extrapolation starting reference, including MD, inclination, azimuth, TVD, North/East, DLS, BUR, TR, and vertical section. It supports all four service modes and polls the lightweight status until completion. All editable engineering values use the same unit-adornment input component, so the unit/reference appears consistently beside the value and refreshes after either a unit-system or reference-system change. Reconnect mode selects its reference trajectory with the same searchable Field/Cluster/Well/WellBore/Trajectory hierarchy. Its lead-in is applied before finding the effective closest reference point, after which the complete requested reference-MD advance is added; the standard editor fixes its azimuth branch to zero for the shortest rotation. Reconnect and geosteering editors expose the optional current-curve lead-in. Geosteering uses a closed choice between overall departure/bearing and steering length; the latter is the combined length of the two steering sections, excludes the lead-in, and is divided using a bounded upstream steering-section length ratio expressed as a percentage and defaulting to 50%. The editor fixes the azimuth branch to the shortest rotation. Target TVD follows the page's active depth reference, and its displayed unit/reference suffix changes with that selection while the persisted value remains WGS84 depth in SI metres. End azimuth and departure bearing similarly display the active TN, GN, or MN suffix and convert through the page's active azimuth reference while remaining true-north radians in the persisted contract. Grid convergence is obtained through the Field service using its highest-ranked executable position-applicable datum transformation. Overall departure is measured from the final source station. The well-path editor presents Length, inclination, azimuth, vertical depth, North, East, circular-arc curvature/start toolface, build/turn rate, and constant-curvature/toolface columns; only the parameter pair belonging to the selected curve type is editable. A live counter shows the required `3 × n` constraint total. Immediately below the WellPath constraint table, a read-only result table presents every solved section with its calculated length, end MD and position/attitude, and the resolved parameter pair for its curve family; values that were supplied as input constraints are shown in bold, while solver-derived values use normal weight. After calculation, the editor also loads the chunked interpolated extrapolation into the standard fixed-height survey-station table: MD, inclination, azimuth, TVD, North/East, DLS, BUR, TR, and vertical section. The source, extrapolation, and reconnect target are drawn in the vertical view using one curtain defined by the source-plus-extrapolation horizontal direction, so the reconnect point has one consistent vertical-section coordinate rather than retaining the target trajectory's unrelated local projection. The shared uncertainty-ellipse component follows that table; it accepts a `ProportionStandard` confidence factor greater than zero and no greater than 0.999 and displays horizontal, vertical, and perpendicular ellipse projections based on uncertainty continued from the source trajectory endpoint. For Wolff-de Wardt results it supplies the source-station history so the service can reconstruct and continue the transfer matrix rather than restart at zero. The horizontal ellipse orientation is labeled as an azimuth and follows the active TN, GN, or MN reference; the other projection orientations remain plane angles. Values and headings follow the application unit system and active depth, position, and azimuth references. Actions above that table can save its stations atomically as a planned Survey Run or export them as ASCII. Saving asks only for a name, assigns the run to the source trajectory's WellBore hierarchy, and inherits the survey instrument and parent link from the last usable source Survey Run. Export opens the browser's native Save As picker directly, suggests a filename, and initially selects Downloads; browsers without the File System Access API fall back to their normal download handling. Calculated Survey Run, calculated Trajectory, and extrapolation exports share one tab-separated ten-column formatter and therefore use the same headings, ordering, display units, and active depth/position/azimuth references.

## Trajectory aggregation UI

The aggregation editor uses its unit-aware interpolation step when sampling the fitted section chain. The completed result is available as the standard fixed-height survey-station table with MD, inclination, azimuth, TVD, North/East, DLS, BUR, TR, and vertical section. The same actions and tab-separated formatter as trajectory extrapolation can export that table through the browser's native Save As flow or create it atomically as a planned Survey Run on the source WellBore. For that run, the editor sums the measured-depth coverage of every source-trajectory section by survey instrument and selects the instrument with the greatest total coverage; the longest contributing source Survey Run supplies the parent link.

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

When the reference trajectory's Field has an effective policy assignment, the service overrides the interactive confidence for the final calculation and freezes the exact assignment/revision. The results page shows each comparison trajectory's matched rule, Alert/Alarm thresholds, worst classification, and any indeterminate reason. `AntiCollisionPolicies` maintains the immutable revision library and future/historical Field assignment timeline. Rule age values use the shared duration unit selector while canonical storage remains SI seconds.

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
- register a singleton `ITrajectoryReferenceDataCache` implementation; the standalone WebApp also runs its one-minute background refresh worker
- register an `ITrajectoryWebPagesConfiguration` implementation
- ensure the generated Trajectory client and OSDC unit-conversion components are available

## Configuration

The pages depend on injected `ITrajectoryAPIUtils` and `ITrajectoryReferenceDataCache` services. The reference-data cache supplies a shared snapshot of Fields, Clusters, Wells, WellBores, Rigs, Survey Instruments, and lightweight WellBore Architectures so navigation does not re-download those catalogs. A host should refresh the snapshot periodically and retain the last successful snapshot on transient failures.

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

`TrajectoryIdentities` and `TrajectoryFeatures` manage the catalogs shared by survey runs and trajectories. The feature page follows the common resource-service catalog layout: a compact category/options grid, bulk selection and deletion, consistent add/save/reload actions, validation, and deletion confirmation. `IdentityFeatureAssignments` is embedded in both resource editors and enforces each category's option and validity-period shape through the service API.

## Backup and restore

`TrajectoryBackupRestore` lets users select survey runs and trajectories or back up everything. A selected trajectory automatically brings along all survey runs used by its sections, while parent survey runs and relevant catalog definitions are also included. Backup schema version 2 always includes the complete anti-collision policy library and Field assignment audit history. Restore previews record counts and offers explicit record-conflict and catalog-resolution policies before sending the complete document to the service. Catalog UUIDs are matched exactly by default; mapping compatible definitions with different UUIDs by normalized name requires a separate warning-bearing opt-in.

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
