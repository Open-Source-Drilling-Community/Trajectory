# ModelSharedIn

`ModelSharedIn` manages generated dependency models used by the `Model` project. Its generator and assembly use the `OSDC.Drilling.Trajectory` identity.

## Responsibility

This project stores upstream OpenAPI schema files and generates C# classes from them. The checked-in inputs are `Field.json`, `Cluster.json`, `Well.json`, `WellBore.json`, `WellBoreArchitecture.json`, `SurveyInstrument.json`, `EarthGravity.json`, and `EarthMagneticField.json` under `json-schemas`.

It supports the distributed shared model approach for dependencies that the Trajectory model consumes from other services.

The generated contracts supply the ownership hierarchy and tie-in context, survey-instrument error models, and gravity/magnetic reference evaluations used by SurveyRun correction, trajectory composition, uncertainty propagation, target landing, directional-control evaluation, and anti-collision evidence. Trajectory-owned DTOs are not defined here.

## Dependencies

`ModelSharedIn` depends on:

- `Microsoft.OpenApi.Readers`
- `NSwag.CodeGeneration.CSharp`

## Solution Role

- `Model` references `ModelSharedIn`.
- The generated types represent external service contracts needed by the Trajectory model.

## Notes

- The project is configured as an executable because it includes code-generation tooling.
- The source schemas are stored under `json-schemas`.
- Generated dependency types use `OSDC.Drilling.Trajectory.ModelShared`. `MergedModel.cs` and `MergedModel.json` are derived artifacts and must not be hand-edited.

## Regeneration

Refresh each dependency schema from that dependency repository's authoritative service schema, then run from the Trajectory repository root:

```powershell
dotnet run --project .\ModelSharedIn\ModelSharedIn.csproj
```

Answer `Y` only after reviewing the input schemas. Inspect the generated diff for route or short type-name collisions, then build the solution so downstream compilation validates the result.

Refresh each input from its owning repository before running the generator. In particular, use the current Survey Instrument service schema so shared catalogue policies and batch restore DTOs are not silently omitted from the Trajectory model contract.

## Earth reference dependencies

`EarthGravity.json` and `EarthMagneticField.json` are copied from the authoritative `Service/wwwroot/json-schema` output of the Earth Gravity and Earth Magnetic Field repositories. Their adjacent `.sha256` files pin the reviewed inputs. Refresh the schema and checksum together, regenerate this project, and inspect the generated diff; do not hand-edit the generated dependency DTOs.

## Bounded light discovery

Resource and catalog discovery returns light pages, with `offset` defaulting to 0 and `limit` defaulting to 50 (allowed range 1–200). Names and relevant resource-reference filters are evaluated on the server. Unbounded full-object `HeavyData` routes and tools, including filtered full-object lists, have been removed. Existing UUID and HTTP-metadata listings remain available; use named light pages for routine discovery.

REST routes and MCP tool names (relative to the service's existing API path base):

- `/AntiCollisionPolicyRevision/LightData` / `anti_collision_policy_revision_discover`
- `/DirectionalControlEvaluationCase/LightData` / `directional_control_evaluation_case_discover`
- `/InterpolatedTrajectory/LightData` / `interpolated_trajectory_discover`
- `/SurveyRunBatchImport/LightData` / `survey_run_batch_import_discover`
- `/SurveyRun/LightData` / `survey_run_discover`
- `/SurveyRunMinimumDistanceCalculation/LightData` / `survey_run_minimum_distance_calculation_discover`
- `/TargetLandingCase/LightData` / `target_landing_case_discover`
- `/TrajectoryAggregationCase/LightData` / `trajectory_aggregation_case_discover`
- `/Trajectory/LightData` / `trajectory_discover`
- `/TrajectoryExtrapolationCase/LightData` / `trajectory_extrapolation_case_discover`
- `/TrajectoryFeatureCategory/LightData` / `trajectory_feature_category_discover`
- `/TrajectoryIdentity/LightData` / `trajectory_identity_discover`
- `/TrajectoryMinimumDistanceCalculation/LightData` / `trajectory_minimum_distance_calculation_discover`
- `/TrajectoryRealizationCase/LightData` / `trajectory_realization_case_discover`
- `/SurveyStationEllipseCalculation/LightData` / `survey_station_ellipse_calculation_discover`
- `/FieldAntiCollisionPolicyAssignment/LightData` / `field_anti_collision_policy_assignment_discover`
- `/GlobalAntiCollisions/LightData` / `global_anti_collisions_discover`
- `/Octrees/LightData` / `octrees_discover`

Pages contain `Items`, `Offset`, `Limit`, `TotalCount`, and `NextOffset`; `NextOffset` is null on the final page. Offset pages are individually consistent reads, not a snapshot spanning requests: concurrent inserts/deletes can change later pages. Refresh discovery when the underlying collection changes. EarthGeodesy's existing search pages instead expose `Items[].Value`, `Offset`, `Limit`, and `TotalCount`.

Single-record reads, result/chunk reads, and deliberate backup/export/restore operations are preserved. Backups can still contain complete resources and should be treated as explicit bulk operations.

This is a breaking REST/MCP contract change: update generated clients, UIs, and external integrations together with the service release. Removed heavy-list names are not compatibility aliases. No persisted engineering shapes, database names, or storage volumes are changed. Historical usage counters are retained; bounded discovery has a separate additive `DiscoveryRequestsPerDay` history in resource services.


Regenerate from the owning service's freshly compiled OpenAPI, refresh dependent schema snapshots, then regenerate consuming clients/DTOs. Do not edit generated C# to restore removed methods. Keep the hand-authored discovery client extensions alongside generated clients: they iterate bounded light pages when a complete name/reference list is needed. Reference projections are partial display/context objects; retrieve a selected complete record before full-replacement writes or engineering calculations. Schema-name normalization must also rewrite discriminator mappings.
