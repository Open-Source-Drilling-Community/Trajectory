# Model

`Model` contains the main Trajectory domain model and trajectory calculation logic used by the service.

## Responsibility

This project defines the core model types and computational behavior for trajectory data and related interpolation and calculation workflows.

It is the main implementation project behind the Trajectory service.

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
