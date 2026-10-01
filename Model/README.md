# Model

`Model` contains the main Trajectory domain model and trajectory calculation logic used by the service under `OSDC.Drilling.Trajectory.Model`.

## Responsibility

This project defines the core model types and computational behavior for trajectory data and related interpolation and calculation workflows.

It is the main implementation project behind the Trajectory service. It does not own database or HTTP behavior; those concerns belong to `Service`.

## Main Features

- trajectory domain objects and persistence models
- trajectory interpolation cases
- durable trajectory extrapolation cases for fixed-length continuation, reconnection to another trajectory, and constrained multi-section well paths
- stochastic trajectory realization cases
- shared identity and feature catalog models, with assignments on both survey runs and trajectories
- versioned backup/restore contract types for dependency-closed survey-run/trajectory documents and the immutable anti-collision policy/Field-assignment history
- deterministic bounded search-result contracts for trajectory and survey-run discovery
- typed octree-index health/provenance plus transient asynchronous search request, status, and result contracts; scan requests select planned and/or actual trajectories and can restrict results to definitive trajectories
- read-only Trajectory and SurveyRun external-reference validation and bounded-audit request/result contracts, with distinct `Valid`, `Invalid`, and `Unavailable` states

Persisted and wire-level engineering quantities use SI units. Depths and vertical coordinates are metres relative to WGS84; alternative depth references are UI presentation transformations and must be converted back before persistence.

Field, Cluster, Well, WellBore, WellBore Architecture, Rig, and Survey Instrument identifiers are identifiers owned by other microservices. The model carries those UUIDs without embedding the external resources. Trajectory and SurveyRun validation/audit result types report confirmed missing references separately from an unavailable dependency.

`OctreeSearchJobRequest`, `OctreeSearchJobStatus`, and `OctreeSearchJobResult` support the non-blocking anti-collision candidate scan. A request identifies one reference trajectory and its planned/actual/definitive filters. Status carries a server-generated job UUID, state, measured progress, stage message, and terminal candidate count; the terminal result contains unique overlapping trajectory UUIDs. This state is transient and derived—the service owns queueing, retention, and validation.

## Trajectory extrapolation

`TrajectoryExtrapolationCase` starts at the last complete station of a calculated source trajectory. Every case uses an SI-metre sampling interval (30 m by default) and exactly one discriminated specification:

- `FixedLength` extends straight or continues a circular-arc, constant-build-and-turn, or constant-curvature-and-toolface fit to the last source interval.
- `ReconnectToTrajectory` optionally continues the final source curve for a non-negative lead-in, then finds the closest point from that effective steering start, advances the reference measured depth, interpolates the target position and tangent, and solves a two-section connection of the selected curve family.
- `WellPath` solves a heterogeneous sequence of circular-arc, constant-build-and-turn, and constant-curvature-and-toolface sections. Each section has a stable UUID, at least one constraint, no prefix may be overdetermined, and the complete path must supply exactly `3 × section count` constraints.
- `Geosteering` optionally continues the current source curve for a lead-in and then solves two named RSS steering sections to a target WGS84 vertical depth, inclination, and true-north azimuth. Its closed extent union is either overall departure plus bearing, measured from the final source station, or overall drilled length plus the positive upstream/downstream steering-length ratio. Overall drilled length includes the lead-in. Circular-arc and constant-curvature/toolface pairs share curvature; build/turn pairs match spatial curvature at their junction.

Inputs and persisted outputs use metres, radians, and radians per metre. Every sampled extrapolation station carries curve-family-consistent DLS, BUR, TR, toolface, and cumulative vertical section; generated samples deliberately carry no fabricated covariance. Solved sections carry explicit `LeadInContinuation`, `UpstreamSteeringSection`, `DownstreamSteeringSection`, fixed-extension, or well-path roles. Completed cases freeze the source endpoint, optional target, source/reference revisions, and solved section parameters for auditability.


## Survey measurement references

`SurveyMeasurement` preserves the instrument-facing observation separately from the canonical trajectory angles. `ObservedInclination` and `ObservedAzimuth` retain the original values; `Inclination` is the corrected angle from the local WGS84 geodetic-down axis (opposite the outward ellipsoid normal) and `Azimuth` is corrected to true north. Station reference fields may inherit the SurveyRun defaults. Each measurement has a stable UUID, an optional UTC acquisition time, and a correction record containing the applied angular differences, evaluation position/depth/time, status, source, dependency-model provenance, and algorithm version.

`SurveyRun.BitExtrapolation` optionally adds exactly one terminal bit station. `CalculateFromLastMeasurement` continues the last measured circular-arc/minimum-curvature, constant-build-and-turn, or constant-curvature-and-toolface segment by a positive frozen `MeasurementToolToBitDistance` in SI metres; a single measured station is extended straight at its inclination and azimuth. `LastStationAlreadyExtrapolated` instead requires exactly the final submitted row to have `Origin = Extrapolated` and requires its MD increment to equal that distance. Legacy rows default to `Measured`. Extrapolated rows carry no observation time or reference correction and are not new instrument observations.

A SurveyRun may define a complete UTC acquisition interval for historical files. When a magnetic observation has no station time, its correction uses the interval midpoint and records that choice. Gravity-vertical and magnetic-north transformations use the full Earth Gravity and Earth Magnetic Field vectors in the local north-east-down frame. The calculated `SurveyStation` remains the canonical trajectory abstraction and is unchanged.
## Trajectory Realizations

Trajectory realization generation is implemented by `TrajectoryRealizationCase`.

A realization case references a trajectory, selects a number of realizations, and uses the wellbore position uncertainty covariance matrices on the survey stations to generate possible trajectory geometries. The reference trajectory can be coarsened before realization generation using `CoarseningMaximumDistance`, which defaults to `0.1` m.

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
