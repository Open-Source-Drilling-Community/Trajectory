# Model

`Model` contains the main Trajectory domain model and trajectory calculation logic used by the service.

## Responsibility

This project defines the core model types and computational behavior for trajectory data and related interpolation and calculation workflows.

It is the main implementation project behind the Trajectory service.

## Main Features

- trajectory domain objects and persistence models
- trajectory interpolation cases
- stochastic trajectory realization cases

## Trajectory Realizations

Trajectory realization generation is implemented by `TrajectoryRealizationCase`.

A realization case references a trajectory, selects a number of realizations, and uses the wellbore position uncertainty covariance matrices on the survey stations to generate possible trajectory geometries. The reference trajectory can be coarsened before realization generation using `CoarseningMaximumDistance`, which defaults to `0.1` m.

Each realization is generated from one normalized Gaussian draw. The draw is applied in the local covariance frame of each survey station. The resulting points are completed into `MD`, inclination, and azimuth using the minimum curvature method, then the full trajectory is recalculated from `MD`, inclination, and azimuth so derived values such as vertical section, DLS, BUR, and TUR are populated.

The mirror alternatives caused by covariance eigenvector sign ambiguity are filtered by checking that `CompleteFromXYZ` followed by `CompleteFromSIA` reconstructs the candidate point. Among valid alternatives, the selected candidate is the one whose tangent is closest to the original reference station tangent. Tangents are compared as 3D unit vectors, which avoids azimuth wrap-around problems at `0` and `2*pi`.

If a realization attempt cannot be completed, the model draws a new realization for the same realization number. The retry count is bounded; repeated failures cause the calculation to fail with a calculation message.

## Dependencies

`Model` depends on:

- `ModelSharedIn`
- `OSDC.DotnetLibraries.Drilling.Surveying`

## Solution Role

- `Service` uses `Model` to expose the Trajectory API.
- `ModelTest` validates the model behavior and computations.
- `ModelSharedIn` provides generated upstream dependency types consumed by `Model`.

## Notes

This project also contains DocFX-related files used for documentation generation.
