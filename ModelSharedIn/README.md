# ModelSharedIn

`ModelSharedIn` manages generated dependency models used by the `Model` project. Its generator and assembly use the `OSDC.Drilling.Trajectory` identity.

## Responsibility

This project stores upstream OpenAPI schema files and generates C# classes from them.

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
- Generated dependency types use `OSDC.Drilling.Trajectory.ModelShared`; regenerate them from their OpenAPI inputs instead of hand-editing `MergedModel.cs`.
