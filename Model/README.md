# Model

`Model` contains the main Trajectory domain model and trajectory calculation logic used by the service under `OSDC.Drilling.Trajectory.Model`.

## Responsibility

This project defines the core model types and computational behavior for trajectory data and related interpolation and calculation workflows.

It is the main implementation project behind the Trajectory service. It does not own database or HTTP behavior; those concerns belong to `Service`.

## Main Features

- trajectory domain objects and persistence models
- trajectory interpolation cases
- stochastic trajectory realization cases
- shared identity and feature catalog models, with assignments on both survey runs and trajectories
- versioned backup/restore contract types for dependency-closed survey-run and trajectory documents
- deterministic bounded search-result contracts for trajectory and survey-run discovery
- typed octree-index health and provenance states for lightweight REST and MCP inspection
- read-only Trajectory and SurveyRun external-reference validation and bounded-audit request/result contracts, with distinct `Valid`, `Invalid`, and `Unavailable` states

Persisted and wire-level engineering quantities use SI units. Depths and vertical coordinates are metres relative to WGS84; alternative depth references are UI presentation transformations and must be converted back before persistence.

Field, Cluster, Well, WellBore, WellBore Architecture, Rig, and Survey Instrument identifiers are identifiers owned by other microservices. The model carries those UUIDs without embedding the external resources. Trajectory and SurveyRun validation/audit result types report confirmed missing references separately from an unavailable dependency.

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
