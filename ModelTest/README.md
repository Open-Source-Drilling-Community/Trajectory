# ModelTest

`ModelTest` is the NUnit test project reserved for the `OSDC.Drilling.Trajectory.Model` project.

## Responsibility

This project is intended to validate the trajectory domain model and computation logic implemented in `Model`.

It is the unit-test project for model-level behavior.

Model-level test coverage should include trajectory interpolation and trajectory realization behavior, especially coarsening, covariance-based realization generation, mirror-candidate selection, retry behavior, and minimum-curvature completion.

## Dependencies

`ModelTest` depends on:

- `Model`
- `NUnit`
- `Microsoft.NET.Test.Sdk`
- `coverlet.collector`

## Solution Role

- validates the core logic in `Model`
- complements `ServiceTest`, which exercises the API surface instead of the model layer directly

## Running Tests

Run the tests with:

```bash
dotnet test ModelTest/ModelTest.csproj
```

## Notes

The project currently contains no discoverable test cases; `dotnet test` therefore reports that no tests are available. Model behavior is also exercised indirectly by service and anti-collision verification, but new model-level regression tests should be added here rather than relying only on integration coverage.
