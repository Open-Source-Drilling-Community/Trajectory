# ModelSharedOut

`ModelSharedOut` manages generated shared models and client-side service contract types for consumers of the Trajectory service. Its generated namespace is `OSDC.Drilling.Trajectory.ModelShared`.

## Responsibility

This project stores OpenAPI schemas and generates C# classes that are used by downstream consumers of the Trajectory API.

It includes the Trajectory service schema together with other related schemas needed by clients and reusable UI components.

The generated output includes client types and methods for trajectory realization cases, including light data, full case data, and chunked realization retrieval. It also contains the versioned batch export/restore document, conflict and catalog policies (including the explicit normalized-name-mapping opt-in), the two dependency-aware backup endpoints, bounded trajectory/survey-run search, single-record external-reference validation and bounded audit results, optimistic-concurrency parameters for durable mutations, and the typed octree index status/provenance and filtered overlap-search contracts exposed by `GetOctreeIndexStatusAsync` and `SearchOctreeIndexAsync`.

## Dependencies

`ModelSharedOut` depends on:

- `Microsoft.OpenApi.Readers`
- `NSwag.CodeGeneration.CSharp`
- `Microsoft.CodeAnalysis.CSharp`

## Solution Role

- `WebPages` depends on `ModelSharedOut`.
- client-facing generated types and schemas are produced here for use outside the core service implementation.
- trajectory realization UI pages use the generated chunk endpoints to avoid loading large realization sets through the light case lists.

## Notes

- The project is configured as an executable because it includes code-generation tooling.
- It contains helper code related to generated pseudo-constructors and schema processing.
- `TrajectoryMergedModel.cs`, `PseudoConstructors.cs`, and the merged JSON/OpenAPI artifacts carry the OSDC namespace and should be regenerated together after service-contract changes.
- Lightweight projection classes intentionally do not receive pseudo-constructors; generated aggregate constructors initialize collections of light projections as empty lists.
- `expectedModifiedUtc` is generated with the round-trip (`O`) date-time format. Callers must still treat it as an opaque value copied from the latest `LastModificationDate`.

## Regeneration

For a Trajectory REST contract change, first build `Service` so its build target refreshes `json-schemas/TrajectoryFullName.json`. Refresh other dependency schemas from their owning repositories when those contracts change. Then run from the repository root:

```powershell
dotnet build .\Service\Service.csproj
dotnet run --project .\ModelSharedOut\ModelSharedOut.csproj
```

Accept the overwrite prompt after reviewing the inputs. The generator updates `TrajectoryMergedModel.cs`, `PseudoConstructors.cs`, and `Service/wwwroot/json-schema/TrajectoryMergedModel.json` together. Build the full solution and run the contract tests after generation; do not patch the generated C# client directly.
