# ModelSharedIn

`ModelSharedIn` manages generated dependency models used by the `Model` project. Its generator and assembly use the `OSDC.Drilling.Trajectory` identity.

## Responsibility

This project stores upstream OpenAPI schema files and generates C# classes from them. The checked-in inputs are `Field.json`, `Cluster.json`, `Well.json`, `WellBore.json`, `WellBoreArchitecture.json`, and `SurveyInstrument.json` under `json-schemas`.

It supports the distributed shared model approach for dependencies that the Trajectory model consumes from other services.

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
