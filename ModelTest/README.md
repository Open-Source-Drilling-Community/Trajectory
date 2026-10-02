# ModelTest

`ModelTest` is the NUnit test project reserved for the `OSDC.Drilling.Trajectory.Model` project.

## Responsibility

This project is intended to validate the trajectory domain model and computation logic implemented in `Model`.

It is the unit-test project for model-level behavior.

Model-level test coverage includes trajectory interpolation, extrapolation, aggregation, uncertainty continuity, composition, and trajectory realization behavior, especially aggregation period-boundary handling, coarsening, covariance-based realization generation, mirror-candidate selection, retry behavior, and minimum-curvature completion.

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

`TrajectoryUncertaintyCompositionTests.cs` verifies that trajectory materialization preserves the parent tie-in covariance and the already-continued station covariance supplied by its SurveyRun sections.

`TrajectoryAggregationCalculatorTests.cs` verifies that overlapping constant-period boundaries map to exactly one trajectory interval and that the production-shaped U3 trajectory aggregates successfully.

`SurveyStationEllipseCalculationTests.cs` verifies the confidence interval and ensures that a partial Wolff-de Wardt station list without its propagation history is rejected instead of being restarted from covariance alone.
