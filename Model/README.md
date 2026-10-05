# Model

`Model` contains the main Trajectory domain model and trajectory calculation logic used by the service under `OSDC.Drilling.Trajectory.Model`.

## Responsibility

This project defines the core model types and computational behavior for trajectory data and related interpolation and calculation workflows.

It is the main implementation project behind the Trajectory service. It does not own database or HTTP behavior; those concerns belong to `Service`.

## Main Features

- trajectory domain objects and persistence models
- trajectory interpolation cases
- durable trajectory extrapolation cases for fixed-length continuation, reconnection to another trajectory, and constrained multi-section well paths
- durable target-landing cases for uncertainty-aware landing into convex oriented planar targets
- stochastic trajectory realization cases
- shared identity and feature catalog models, with assignments on both survey runs and trajectories
- versioned backup/restore contract types for dependency-closed survey-run/trajectory documents and the immutable anti-collision policy/Field-assignment history
- deterministic bounded search-result contracts for trajectory and survey-run discovery
- typed octree-index health/provenance plus transient asynchronous search request, status, and result contracts; scan requests select planned and/or actual trajectories and can restrict results to definitive trajectories
- read-only Trajectory and SurveyRun external-reference validation and bounded-audit request/result contracts, with distinct `Valid`, `Invalid`, and `Unavailable` states

Persisted and wire-level engineering quantities use SI units. Depths and vertical coordinates are metres relative to WGS84; alternative depth references are UI presentation transformations and must be converted back before persistence.

`SurveyStationEllipseCalculation` accepts a complete standalone station history or stations whose covariance is already complete. A partial Wolff-de Wardt or ISCWSA station list is rejected because terminal covariance alone does not retain the propagation state needed for continuation. Wolff-de Wardt requires its non-persisted transfer matrix, while ISCWSA requires the preceding error-source correlation and continuous-propagation accumulators. Resource-owned calculations use the service's SurveyRun- or Trajectory-specific endpoints, which reconstruct the complete lineage before invoking this model.

Horizontal ellipses remain projected on the North-East plane and perpendicular ellipses remain normal to the local borehole direction. All vertical ellipses in one calculation are projected on one stable vertical-section curtain defined by the first-to-last horizontal displacement (with the first available azimuth as the zero-displacement fallback). The vertical plane therefore does not rotate with the instantaneous station azimuth, which is poorly conditioned near a vertical trajectory and formerly produced artificial station-to-station jumps in the displayed vertical semi-axes.

Field, Cluster, Well, WellBore, WellBore Architecture, Rig, and Survey Instrument identifiers are identifiers owned by other microservices. The model carries those UUIDs without embedding the external resources. Trajectory and SurveyRun validation/audit result types report confirmed missing references separately from an unavailable dependency.

`OctreeSearchJobRequest`, `OctreeSearchJobStatus`, and `OctreeSearchJobResult` support the non-blocking anti-collision candidate scan. A request identifies one reference trajectory and its planned/actual/definitive filters. Status carries a server-generated job UUID, state, measured progress, stage message, and terminal candidate count; the terminal result contains unique overlapping trajectory UUIDs. This state is transient and derived—the service owns queueing, retention, and validation.

## Trajectory extrapolation

`TrajectoryExtrapolationCase` starts at the last complete station of a calculated source trajectory. Every case uses an SI-metre sampling interval (30 m by default) and exactly one discriminated specification:

`DirectionalControlEvaluationCase` compares the steering response observed on an actual trajectory with the first section of the reconnect command that would have been proposed toward a reference trajectory in the same WellBore. At each configurable actual-MD interval (10 m by default), the reconnect target is the closest reference point plus the correction length (60 m by default); the reconnect always uses the shortest azimuth branch, and no command delay or lead-in is assumed. The actual interval is fitted with the same exact circular-arc, constant-build-and-turn, or constant-curvature-and-toolface model selected for the case. Residual pairs are evaluated jointly, toolface differences use wrapped circular angles, and robust change-point segmentation produces depth bundles separated by invalid gaps larger than the configured hard limit (100 m by default). Each bundle stores empirical P10/P50/P90, mean, standard deviation, median absolute deviation, range, and histogram for both linked command components.

- `FixedLength` extends straight or continues a circular-arc, constant-build-and-turn, or constant-curvature-and-toolface fit to the last source interval.
- `ReconnectToTrajectory` optionally continues the final source curve for a non-negative lead-in, then finds the closest point from that effective steering start, advances the reference measured depth, interpolates the target position and tangent, and solves a two-section connection of the selected curve family.
- `WellPath` solves a heterogeneous sequence of circular-arc, constant-build-and-turn, and constant-curvature-and-toolface sections. Each section has a stable UUID, at least one constraint, no prefix may be overdetermined, and the complete path must supply exactly `3 × section count` constraints.
- `Geosteering` optionally continues the current source curve for a lead-in and then solves two named RSS steering sections to a target WGS84 vertical depth, inclination, and true-north azimuth. Its closed extent union is either overall departure plus bearing, measured from the final source station, or steering length plus the positive upstream/downstream steering-length ratio. Steering length is the sum of the two steering sections and excludes the lead-in. Circular-arc and constant-curvature/toolface pairs share curvature; build/turn pairs match spatial curvature at their junction. Persisted cases that still contain the retired `OverallDrilledLength` member are upgraded by subtracting their lead-in, preserving their original geometry.

Inputs and persisted outputs use metres, radians, and radians per metre. Every sampled extrapolation station carries curve-family-consistent DLS, BUR, TR, toolface, and cumulative vertical section, and inherits the last defined survey instrument on the source trajectory. Position uncertainty starts with the final source-station uncertainty and continues using that instrument. Wolff-de Wardt continuation replays the source trajectory to reconstruct its non-persisted 6×3 transfer matrix `A`; ISCWSA continuation likewise replays the source trajectory to retain error-source correlation and continuous-propagation accumulators. Endpoint covariance alone cannot recover either state. If the required instrument or complete history is unavailable, the frozen source covariance is retained without fabricated growth. Solved sections carry explicit `LeadInContinuation`, `UpstreamSteeringSection`, `DownstreamSteeringSection`, fixed-extension, or well-path roles. When a constrained well path cannot be solved, `CalculationMessage` identifies the affected section and constraint, the remaining unknowns, and residual or sensitivity information where available. Completed cases freeze the source endpoint, optional target, source/reference revisions, and solved section parameters for auditability.

## Target landing

`TargetLandingCase` references one calculated trajectory and owns a convex polygon in an oriented target plane. The plane is represented by `CurvilinearPoint3D`: the canonical Cartesian origin is Riemannian North/East plus WGS84 TVD in metres, the equivalent WGS84 latitude/longitude are radians, and its forward normal uses WGS84-geodetic inclination and true-north azimuth. Either geographic or Riemannian origin input is accepted, both forms are materialized, and inconsistent dual input is rejected. Polygon vertices are stored as Cartesian plane metres; polar coordinates are presentation only. A completed calculation freezes the source trajectory's terminal station and sampled lead path separately from the post-lead steering start so clients can distinguish and display the source, lead, and designed landing geometry.

The calculator continues the source trend through the lead length, then samples the complete target with a conforming triangular seed mesh and invokes the shared `TargetAxisPath` solver for circular-arc, constant-build-and-turn, or constant-curvature/toolface sections. Free landing attitude uses one section; `PerpendicularToTargetPlane` uses the corresponding two-section solution and retains the shortest forward drilling-relevant result. The optional Maximum Landing Curvature applies only to these new sections. A position-only build/turn target can have several roots: the solver retains the fast conventional root when it complies, otherwise it selects the shortest alternative root whose peak curvature complies. The inverse iteration uses the same 0.25 metre Cartesian tolerance as contour refinement. When every root exceeds the limit, the solver returns the rejected geometric section together with a curvature-specific status, avoiding a second unconstrained solve merely to classify and render the sample. For build/turn sections the peak is checked along the full inclination interval with `sqrt(B² + (T sin i)²)`. A curvature-rejected driller-target sample stops after the exact section and controls have established that result; it does not interpolate display stations or propagate uncertainty that cannot alter either contour.

Each landing result also carries a normalized model-level control path sampled directly from the solved `ArcSection` objects with the curve-specific DotNetLibraries interpolation. Every point retains the exact interpolated inclination as well as its controls. BT points retain the section's signed constant build and turn rates. CTC points use the exact constant-curvature/toolface propagation and its local build and turn values. A CTC solution that approaches within 3 degrees of either vertical direction anywhere along its exact section is rejected because toolface and turn rate become ill-conditioned there; this near-vertical rule does not apply to CA or BT curves. Turn rate is not otherwise a landing-path rejection criterion. Circular-arc points retain constant curvature while transforming the fixed arc plane into the exact local toolface, build, and turn at each interpolated attitude; the arc's start/reference toolface is not incorrectly repeated along the path. Normalized length is zero at the steering start and one at the target. These authoritative controls drive the cylindrical and build/turn Web UI plots; survey-station differencing is not used to reconstruct them. The UI uses the exact inclination to split CA and BT control-space traces within 3 degrees of vertical, avoiding a false chord across the coordinate singularity without rejecting or changing the valid Cartesian path.

Geological targets project the requested position covariance into the target plane and retain only sampled positions whose complete confidence ellipse lies inside the specified polygon. Wolff-de Wardt and ISCWSA propagation replay the source history through both lead and landing sections as one chain, preserving their respective propagation state. The persisted result distinguishes the specified target, uncertainty-safe driller contours, and curvature/geometry-reachable contours; contours are extracted from a conforming triangle mesh rather than inferred with a potentially unsafe convex hull. Keeping the seed mesh conforming prevents hanging refinement edges from being mistaken for outer target edges; cached bisection evaluations then locate every CA, BT, or CTC reachable/unreachable or uncertainty-safe transition to the same 0.25 metre tolerance. Source revisions, the contour algorithm version, and an input fingerprint mark completed cases stale after relevant changes.


## Survey measurement references

`SurveyMeasurement` preserves the instrument-facing observation separately from the canonical trajectory angles. `ObservedInclination` and `ObservedAzimuth` retain the original values; `Inclination` is the corrected angle from the local WGS84 geodetic-down axis (opposite the outward ellipsoid normal) and `Azimuth` is corrected to true north. Station reference fields may inherit the SurveyRun defaults. Each measurement has a stable UUID, an optional UTC acquisition time, and a correction record containing the applied angular differences, evaluation position/depth/time, status, source, dependency-model provenance, and algorithm version.

`SurveyRun.BitExtrapolation` optionally adds exactly one terminal bit station. `CalculateFromLastMeasurement` continues the last measured circular-arc/minimum-curvature, constant-build-and-turn, or constant-curvature-and-toolface segment by a positive frozen `MeasurementToolToBitDistance` in SI metres; a single measured station is extended straight at its inclination and azimuth. `LastStationAlreadyExtrapolated` instead requires exactly the final submitted row to have `Origin = Extrapolated` and requires its MD increment to equal that distance. Legacy rows default to `Measured`. Extrapolated rows carry no observation time or reference correction and are not new instrument observations.

A SurveyRun may define a complete UTC acquisition interval for historical files. When a magnetic observation has no station time, its correction uses the interval midpoint and records that choice. Gravity-vertical and magnetic-north transformations use the full Earth Gravity and Earth Magnetic Field vectors in the local north-east-down frame. The calculated `SurveyStation` remains the canonical trajectory abstraction and is unchanged.
## Trajectory Realizations

Trajectory realization generation is implemented by `TrajectoryRealizationCase`.

A realization case references a trajectory, selects a number of realizations, and uses the wellbore position uncertainty covariance matrices on the survey stations to generate possible trajectory geometries. Before model calculation, the service rematerializes the trajectory from SurveyRuns whose complete uncertainty ancestry has been replayed; it fails closed if that lineage cannot be reconstructed. The reference trajectory can be coarsened before realization generation using `CoarseningMaximumDistance`, which defaults to `0.1` m.

Each realization is generated from one normalized Gaussian draw. The draw is applied in the local covariance frame of each survey station. The resulting points are completed into `MD`, inclination, and azimuth using the minimum curvature method, then the full trajectory is recalculated from `MD`, inclination, and azimuth so derived values such as vertical section, DLS, BUR, and TUR are populated.

The mirror alternatives caused by covariance eigenvector sign ambiguity are filtered by checking that `CompleteFromXYZ` followed by `CompleteFromSIA` reconstructs the candidate point. Among valid alternatives, the selected candidate is the one whose tangent is closest to the original reference station tangent. Tangents are compared as 3D unit vectors, which avoids azimuth wrap-around problems at `0` and `2*pi`.

If a realization attempt cannot be completed, the model draws a new realization for the same realization number. The retry count is bounded; repeated failures cause the calculation to fail with a calculation message.

## Dependencies

`Model` depends on:

- `ModelSharedIn`
- `OSDC.DotnetLibraries.Drilling.Section`
- `OSDC.DotnetLibraries.Drilling.Surveying`
- `OSDC.DotnetLibraries.General.DataManagement` 2.2 or later for the common identity/feature interfaces

## Solution Role

- `Service` uses `Model` to expose the Trajectory API.
- `ModelTest` validates the model behavior and computations.
- `ModelSharedIn` provides generated upstream dependency types consumed by `Model`.

## Notes

This project also contains DocFX-related files used for documentation generation.

`SurveyStationEllipseCalculation.CalculatePerpendicularOnly` supports compact three-dimensional uncertainty displays without calculating unused horizontal, vertical, or extreme-TVD results.
