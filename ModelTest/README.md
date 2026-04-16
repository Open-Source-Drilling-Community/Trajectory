# ModelTest

`ModelTest` contains automated tests for the `Model` project.

## Responsibility

This project is intended to validate the trajectory domain model and computation logic implemented in `Model`.

It is the unit-test project for model-level behavior.

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

The current test project structure is in place and references the model correctly, but the active test surface is currently minimal.
