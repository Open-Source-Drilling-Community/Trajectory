# ModelTest

`ModelTest` is the NUnit test project reserved for the `OSDC.Drilling.Trajectory.Model` project.

## Responsibility

This project is intended to validate the trajectory domain model and computation logic implemented in `Model`.

It is the unit-test project for model-level behavior.

Model-level test coverage includes trajectory interpolation, extrapolation, aggregation, uncertainty continuity, composition, and trajectory realization behavior, especially aggregation period-boundary and vertical-departure handling, coarsening, covariance-based realization generation, mirror-candidate selection, retry behavior, and minimum-curvature completion.

The suite also covers survey-reference transforms and bit extrapolation, station ellipses, CA/BT/CTC fitting and controls, target-landing reachability, minimum-distance geometry, and the uncertainty continuity needed by anti-collision consumers. This complements service tests for persistence, HTTP/MCP schemas, background queues, and transactional behavior.

Reconnect extrapolation tests also verify that the complete reference measured-depth advance is added after the lead-in has established the effective closest reference point.

Directional-control tests verify exact CA/BT/CTC interval fitting, first-section reconnect comparisons, wrapped toolface residuals, linked two-component bundle segmentation, hard invalid-gap splitting, and same-WellBore validation. `TestData/Ullrigg/U3-MD-Incl-Az.txt` is a permitted copy of the raw Ullrigg text survey; a calibration test generates its reference automatically with Trajectory Aggregation before running the evaluation. No Valhall source data is copied into this repository.

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

`TrajectoryAggregationCalculatorTests.cs` verifies that overlapping constant-period boundaries map to exactly one trajectory interval, that the production-shaped U3 trajectory aggregates without mirroring its vertical departure or overall displacement, and that sampled aggregate stations respect the interpolation step and expose derived DLS, BUR, and TR values.

`SurveyStationEllipseCalculationTests.cs` verifies the confidence interval, ensures that a partial Wolff-de Wardt station list without its propagation history is rejected instead of being restarted from covariance alone, and confirms that changing near-vertical station azimuths cannot rotate the stable vertical-section ellipse curtain.
