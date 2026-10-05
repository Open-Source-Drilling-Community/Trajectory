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
- target-landing design into convex driller or uncertainty-reduced geological targets
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

The SurveyRun editor displays and edits the observed angles, per-station reference overrides, measurement time, applied corrections, canonical geodetic/true-north angles, and correction status. Inclination reference, azimuth reference, and measurement time can also be applied to every station in one operation; individual rows remain editable afterward, and an empty bulk time clears every station time. Run defaults select geodetic versus gravity vertical, true versus magnetic north, geomagnetic model policy, and an optional acquisition interval. Times are presented and interpreted using the selected time reference, while the service contract retains canonical UTC values. Changing a run default, geomagnetic model, acquisition interval, or bulk station setting invalidates affected computed corrections and clears the stale calculated trajectory until it is recalculated. Manual corrections are retained as explicit overrides.

SurveyRun, Trajectory, and interpolated-Trajectory uncertainty views send the owning resource UUID to the corresponding lineage-aware ellipse endpoint. The service therefore rebuilds authoritative parent SurveyRun uncertainty and replaces stale or partial covariance before the shared table displays horizontal, vertical, and perpendicular ellipses. Every vertical ellipse in one result uses the same first-to-last vertical-section curtain, rather than rotating with the poorly conditioned instantaneous azimuth near vertical inclination. Standalone extrapolation continues to submit its explicit source history because its requested stations extend beyond the source trajectory.

The editor also offers an optional terminal bit extrapolation. Users select server calculation from the last measured curve or declare that the final imported row is already extrapolated, then enter the unit-aware measurement-tool-to-bit distance. Measurement and calculated-station tables show explicit origin chips. Bulk reference and time operations skip extrapolated rows, and an automatically generated bit row is displayed only in the calculated survey result.

Field, Cluster, Well, WellBore, and optional Trajectory/Survey Run selection is implemented by the shared `TrajectoryResourceFilter` component across the Survey Run, Trajectory, batch-import, extrapolation, aggregation, interpolation, realization, minimum-distance, and anti-collision workflows. Every level is a searchable autocomplete with case-insensitive partial-name matching, and downstream choices are constrained by the selected hierarchy. Rig job is deliberately separate from that ownership hierarchy. Pages with active depth-reference conversion use `RigJobSelector`; it selects the job active at the Survey Run acquisition date or at the latest constituent Survey Run date of a Trajectory, falling back to the nearest dated job and then the latest job when no date exists. Changing the rig job immediately changes the rotary-table WGS84 depth used by the shared unit/reference component without changing persisted survey or trajectory data.

Confirmation, unsaved-change, deletion, restore, and short text-entry dialogs use the shared `TrajectoryDialogOptions.Compact` configuration. They are capped at MudBlazor's extra-small responsive width and do not expand to the viewport width.

Direct and batch imports can read an optional timestamp column (or fixed-width field) in the selected time reference, declare gravity-vertical and/or magnetic-north source data, and supply a per-run acquisition interval when individual times are unavailable. Batch configurations and exported association descriptions retain these settings. Survey-measurement TSV export includes raw readings, corrected values, timestamps in the selected time reference, correction state, evaluation context, and dependency-model hashes so it is suitable for audit rather than only trajectory reconstruction.

After a Trajectory is saved, the editor polls its background calculation state and then reloads the calculated station chunks. The calculated table and plots therefore refresh when calculation completes even when their expansion panel was already open when Save was selected.

## Trajectory extrapolation UI

The `TrajectoryExtrapolation` page selects a calculated source trajectory through searchable Field, Cluster, Well, WellBore, and Trajectory controls, each supporting case-insensitive partial-name matching, and accepts a display-unit-aware interpolation interval. Directly below those choices it loads the source trajectory's final calculated station as the extrapolation starting reference, including MD, inclination, azimuth, TVD, North/East, DLS, BUR, TR, and vertical section. It supports all four service modes and polls the lightweight status until completion. All editable engineering values use the same unit-adornment input component, so the unit/reference appears consistently beside the value and refreshes after either a unit-system or reference-system change. Reconnect mode selects its reference trajectory with the same searchable Field/Cluster/Well/WellBore/Trajectory hierarchy. Its lead-in is applied before finding the effective closest reference point, after which the complete requested reference-MD advance is added; the standard editor fixes its azimuth branch to zero for the shortest rotation. Reconnect and geosteering editors expose the optional current-curve lead-in. Geosteering uses a closed choice between overall departure/bearing and steering length; the latter is the combined length of the two steering sections, excludes the lead-in, and is divided using a bounded upstream steering-section length ratio expressed as a percentage and defaulting to 50%. The editor fixes the azimuth branch to the shortest rotation. Target TVD follows the page's active depth reference, and its displayed unit/reference suffix changes with that selection while the persisted value remains WGS84 depth in SI metres. End azimuth and departure bearing similarly display the active TN, GN, or MN suffix and convert through the page's active azimuth reference while remaining true-north radians in the persisted contract. Grid convergence is obtained through the Field service using its highest-ranked executable position-applicable datum transformation. Overall departure is measured from the final source station. The well-path editor presents Length, inclination, azimuth, vertical depth, North, East, circular-arc curvature/start toolface, build/turn rate, and constant-curvature/toolface columns; only the parameter pair belonging to the selected curve type is editable. A live counter shows the required `3 × n` constraint total. Immediately below the WellPath constraint table, a read-only result table presents every solved section with its calculated length, end MD and position/attitude, and the resolved parameter pair for its curve family; values that were supplied as input constraints are shown in bold, while solver-derived values use normal weight. After calculation, the editor also loads the chunked interpolated extrapolation into the standard fixed-height survey-station table: MD, inclination, azimuth, TVD, North/East, DLS, BUR, TR, and vertical section. The source, extrapolation, and reconnect target are drawn in the vertical view using one curtain defined by the source-plus-extrapolation horizontal direction, so the reconnect point has one consistent vertical-section coordinate rather than retaining the target trajectory's unrelated local projection. The shared uncertainty-ellipse component follows that table; it accepts a `ProportionStandard` confidence factor greater than zero and no greater than 0.999 and displays horizontal, vertical, and perpendicular ellipse projections based on uncertainty continued from the source trajectory endpoint. For Wolff-de Wardt and ISCWSA results it supplies the source-station history so the service can reconstruct and continue the complete propagation state rather than restart at zero. The horizontal ellipse orientation is labeled as an azimuth and follows the active TN, GN, or MN reference; the other projection orientations remain plane angles. Values and headings follow the application unit system and active depth, position, and azimuth references. Actions above that table can save its stations atomically as a planned Survey Run or export them as ASCII. Saving asks only for a name, assigns the run to the source trajectory's WellBore hierarchy, and inherits the survey instrument and parent link from the last usable source Survey Run. Export opens the browser's native Save As picker directly, suggests a filename, and initially selects Downloads; browsers without the File System Access API fall back to their normal download handling. Calculated Survey Run, calculated Trajectory, and extrapolation exports share one tab-separated ten-column formatter and therefore use the same headings, ordering, display units, and active depth/position/azimuth references.

## Trajectory aggregation UI

The aggregation editor uses its unit-aware interpolation step when sampling the fitted section chain. The completed result is available as the standard fixed-height survey-station table with MD, inclination, azimuth, TVD, North/East, DLS, BUR, TR, and vertical section. The same actions and tab-separated formatter as trajectory extrapolation can export that table through the browser's native Save As flow or create it atomically as a planned Survey Run on the source WellBore. For that run, the editor sums the measured-depth coverage of every source-trajectory section by survey instrument and selects the instrument with the greatest total coverage; the longest contributing source Survey Run supplies the parent link.

## Target landing UI

The case list consumes only the server's scalar target-landing and trajectory light projections. Those projections are read from dedicated database columns without deserializing complete trajectory or target-landing result JSON. Opening an existing case first loads `EditData`, making all inputs visible without downloading calculation samples or mesh triangles, then loads the bounded `DisplayData` projection independently for the graphs and boundary table. Edit/display responses omit null members from repeated nested objects. Save and Save as submit only the editable projection, and the generated client omits its null server-result members, so prior calculated samples, contours, stations, and fingerprints are never uploaded.

The `TargetLanding` page uses the shared searchable Field/Cluster/Well/WellBore/Trajectory selector and separate date-aware Rig-job selector. Reference-dependent values are withheld until the selected trajectory hierarchy, Rig job, Field datum, and cartographic sources have been resolved; changing that source context explicitly reformats inputs and Cartesian plots even when their canonical SI values have not changed. The target plane origin always shows synchronized North/East and latitude/longitude coordinates: North/East use the active position reference, latitude/longitude use the active geodetic reference, and TVD uses the active depth reference. The normal azimuth uses the active azimuth reference. Every polygon row shows Plane X, Plane Y, radial distance, and angle together while the contract stores Cartesian metres. The polygon table uses compact unit-aware inputs and displays the active length or angle unit once in each column heading. Editing X or Y immediately recalculates radial distance and angle; editing radial distance or angle immediately recalculates X and Y. Plane Y follows projected vertical upward (East for a horizontal plane), Plane X completes the plane frame (North for a horizontal plane), and angle zero follows positive Plane Y with positive rotation toward positive Plane X, consistent with the toolface convention. Controls select driller/geological target, CA/BT/CTC curve family, free or perpendicular landing attitude, lead length, confidence in `(0, 0.999]`, and optional Maximum Landing Curvature.

Completed cases plot the specified target, every uncertainty-safe driller contour, and every curvature/geometry-reachable contour at equal plane scale. Excluded calculation samples are hidden. Reachable outer regions have a translucent green fill, while nested loops are classified by contour nesting and shown as orange curvature-excluded islands rather than being incorrectly filled as additional reachable regions. CA, CTC, and BT boundary solutions are mapped to a translucent unit-radius cylinder: toolface is the angular coordinate, curvature is height, and normalized landing length is radius. Marker-free paths consume the authoritative model-level controls sampled directly from each solved curve with its DotNetLibraries interpolation. Every control retains its exact interpolated inclination. BT retains its signed build/turn rates, CTC uses its exact constant curvature/toolface controls, and CA transforms its fixed arc plane into the varying local toolface instead of repeating the start/reference value. CTC roots that approach within 3 degrees of either vertical direction are excluded because their toolface and turn rate are ill-conditioned; CA and BT paths have no corresponding inclination exclusion, and turn rate is not otherwise a rejection criterion. Instead, their cylindrical and build/turn traces are split within 3 degrees of either vertical direction so the plots do not draw a false chord or include singular turn rates in their bounds; the valid Cartesian path remains complete. For every perpendicular-attitude landing curve, including BT, build/turn control traces also stop and restart at the duplicate normalized-length sample marking the section junction, preventing Plotly from drawing a false connector between different section commands. Because the inverse calculation can have several valid roots, neighboring target-boundary vertices are split into continuous solution families when their landing paths diverge materially. Families use dark, high-contrast colors in the cylindrical, build/turn, and Cartesian views and in the boundary-command table. Solid endpoint traces close only when one family is continuous around the complete target contour; partial mapped boundary segments stop at a branch switch instead of inventing a connection or merging distinct solution roots. Cartesian X/Y titles are suppressed. A central curvature axis spans zero to 120% of the maximum plotted curvature and carries a marker for the source trajectory's terminal curvature. The following build/turn control-space view uses turn rate on X, normalized landing length on Y, and build rate on Z, consuming the same authoritative controls without survey-station reconstruction. Each sampled control path begins with the lead-point command at normalized length zero and reaches the boundary at one. Build and turn use the active curvature unit, and their bounds have 20% padding on each side with a minimum SI padding of 0.5 degrees per 30 metres. Results calculated before inclination-aware normalized controls were introduced show an explicit recalculation warning instead of potentially misleading control plots; Save and calculate must run against the current service to populate them. A separate Cartesian 3D view loads the source trajectory through its chunk endpoints and displays it with the specified and reachable target boundaries, every sampled boundary landing path, and a thick orange sampled lead path rendered last from the final source survey to the steering start. Legacy results without sampled lead stations fall back to a direct endpoint bridge so the connection remains visible until recalculation. Its North/East/TVD axes follow the active position and depth references, with TVD increasing downward. The result table is limited to reachable boundary vertices and reports contour and family numbers, plane coordinates, landing length, peak curvature, and explicit unit-aware command columns. A free-attitude case shows one command pair and a perpendicular-attitude case shows two; BT uses build/turn columns while CA and CTC use curvature/toolface columns. Every applicable heading displays the active length, curvature, or angle unit. Each interpolated contour vertex uses the nearest reachable evaluated solution from its bisection bracket. Save and calculate keeps the detailed editor open, displays the server-side queued/running state, message, percentage, and progress bar at the top of the page, and reloads the completed case so contours, samples, and concurrency metadata update in place. Existing cases also provide Save as: it asks for a new name, posts a new UUID containing only the current editable inputs, queues an independent calculation, and keeps the editor on the new copy without overwriting the source case or transferring its calculated result. Closing the editor merges its latest persisted light record directly into the already loaded table; it does not repeat the complete light-case and trajectory-name requests, and any active calculation continues through the single-case status poll. Unsaved draft fields are never copied into the table. The calculation continues on the server if the editor is closed. If a calculation finishes between loading and saving, the editor verifies that every editable input still matches the loaded server version and then retries once with the new opaque revision token; it never retries over configuration changed by another editor.

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

After a reference Field is selected, the scan offers every policy assignment belonging to that Field and preselects the currently effective one when available. Clearing the selector explicitly requests no policy. With an assignment selected, its immutable revision supplies the read-only confidence factor; the service validates the Field relationship and freezes the applied assignment, policy revision and evaluation. The results page shows each comparison trajectory's matched rule, Alert/Alarm thresholds, worst classification, and any indeterminate reason. Separation-factor values below the applicable Alert threshold are orange and values below the Alarm threshold are red. Profile sections with no exceeded level are dashed, Alert sections are solid, and Alarm sections are thick solid; the exact crossing depth is linearly interpolated wherever a sampled profile interval crosses either threshold, and captions beside the table and graph explain the conventions. The overlapping-trajectory table uses one selection-column header checkbox to select or clear every candidate. `AntiCollisionPolicies` maintains the immutable revision library and future/historical Field assignment timeline. The policy library is grouped by end-user policy name, shows the distinct Fields whose current or historical assignments use any revision, and presents each policy's immutable revisions in a selectable nested table; internal policy UUIDs are not displayed. A policy with no Field assignments can be deleted as one guarded operation together with all its revisions. A selected revision is shown read-only, and only the latest revision can be switched into the editor to create the next immutable revision, preserving a linear history. Rule priorities start at 1. Alert and Alarm separation-factor thresholds use plain text inputs without numeric spinner controls. The complete page is hosted within the shared unit/reference context required by its confidence and duration inputs. Field assignment uses separate policy-family and revision selectors: the Field and policy support case-insensitive partial-name filtering, while revision filtering matches its number, name, or description and is restricted to the selected policy. All assignment selectors use nullable UI state so an unselected value is shown as an empty prompt rather than the all-zero UUID. Rule age values use the shared duration unit selector while canonical storage remains SI seconds.

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
- call `AddTrajectoryWebPages()` to register `ITrajectoryAPIUtils` and the default on-demand reference-data provider
- optionally register an `ITrajectoryReferenceDataCache` implementation before `AddTrajectoryWebPages()` when the host maintains shared lists; the standalone WebApp supplies a singleton cache and one-minute background refresh worker
- register an `ITrajectoryWebPagesConfiguration` implementation
- ensure the generated Trajectory client and OSDC unit-conversion components are available

## Configuration

The pages depend on injected `ITrajectoryAPIUtils` and `ITrajectoryReferenceDataCache` services. `AddTrajectoryWebPages()` uses `TryAdd` registrations: it installs `TrajectoryAPIUtils` and `DirectTrajectoryReferenceDataProvider` only when the host has not supplied replacements. The direct provider acquires Fields, Clusters, Wells, WellBores, Rigs, Survey Instruments, lightweight WellBore Architectures, lightweight Trajectories, and lightweight Survey Runs when a page requests them, so the reusable pages work in applications that do not maintain global lists. A host that does maintain those lists can register its own cache implementation first; every page then uses that cache through the same interface. Pages use targeted trajectory or survey-run refreshes after mutations and while polling active calculations.

The streamlined design is to register:

- a host-side `ITrajectoryWebPagesConfiguration`
- `services.AddTrajectoryWebPages()`

For a cached host, register the host's `ITrajectoryReferenceDataCache` before calling `AddTrajectoryWebPages()`. The cache should refresh the complete snapshot periodically and retain the last successful snapshot on transient failures.

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

`StatisticsTrajectory` follows the shared OSDC resource-service layout. It provides refresh and failure states, total and current-day request counts in the selected time reference, the tracked-endpoint count, the service's last-save time, and a sortable table showing HTTP method, operation, daily and lifetime totals, and last use.

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

The Cartesian target-landing graph has independent switches for reference-trajectory perpendicular ellipses, lead-extension perpendicular ellipses, and landing ellipses projected in the target plane. Reference and lead data are fetched lazily through the compact uncertainty-display endpoint. The UI deliberately offers no perpendicular-ellipse display along every extended landing path.
