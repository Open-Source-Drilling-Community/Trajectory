# ModelSharedOut

`ModelSharedOut` manages generated shared models and client-side service contract types for consumers of the Trajectory service.

## Responsibility

This project stores OpenAPI schemas and generates C# classes that are used by downstream consumers of the Trajectory API.

It includes the Trajectory service schema together with other related schemas needed by clients and reusable UI components.

## Dependencies

`ModelSharedOut` depends on:

- `Microsoft.OpenApi.Readers`
- `NSwag.CodeGeneration.CSharp`
- `Microsoft.CodeAnalysis.CSharp`

## Solution Role

- `WebPages` depends on `ModelSharedOut`.
- client-facing generated types and schemas are produced here for use outside the core service implementation.

## Notes

- The project is configured as an executable because it includes code-generation tooling.
- It contains helper code related to generated pseudo-constructors and schema processing.
