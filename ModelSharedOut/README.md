# ModelSharedOut

`ModelSharedOut` manages generated shared models and client-side service contract types for consumers of the Trajectory service. Its generated namespace is `OSDC.Drilling.Trajectory.ModelShared`.

## Responsibility

This project stores OpenAPI schemas and generates C# classes that are used by downstream consumers of the Trajectory API.

It includes the Trajectory service schema together with other related schemas needed by clients and reusable UI components.

The Field dependency input is the Field-owned `FieldFullName.json` contract rather than the merged dependency bundle. It is processed after transitive dependency bundles so an older embedded `Field` schema cannot replace the authoritative shape (including `ReferencePoint`) during short-name merging. The Earth Geodesy input is likewise pinned from its owning `EarthGeodesyFullName.json` contract so shared short names keep the authoritative geodetic definitions.

The WellBore-owned `WellBoreFullName.json` input is processed after the local
Trajectory service schema. The latter can contain a transitive WellBore copy from
a dependency assembly; it must not override current properties such as `RigJobs`.

The generated output includes client types and methods for trajectory realization and extrapolation cases, including light/status data, full case data, four discriminated extrapolation specifications, the closed departure-versus-steering-length geosteering extent, explicit solved-section roles, heterogeneous well-path sections, and chunked result retrieval. SurveyRun contracts include the optional terminal bit-extrapolation mode, positive SI tool-to-bit distance, and measured-versus-extrapolated row provenance. Resource-specific SurveyRun and Trajectory ellipse endpoints let clients request authoritative lineage replay instead of trusting submitted covariance. It also contains the versioned batch export/restore document, conflict and catalog policies (including the explicit normalized-name-mapping opt-in), the two dependency-aware backup endpoints, bounded trajectory/survey-run search, single-record external-reference validation and bounded audit results, optimistic-concurrency parameters for durable mutations, the typed octree index status/provenance, the asynchronous filtered overlap-search contract, and lightweight polling contracts for long calculations without repeatedly transferring result payloads.

For anti-collision clients, the generated REST sequence is `QueueOctreeSearchAsync` → repeated `GetOctreeSearchStatusAsync` calls → `GetOctreeSearchResultAsync` after completion. The returned candidate UUIDs can be selected for a durable Global Anti-Collision separation-factor request, which is then polled through its lightweight status endpoint before downloading its terminal profile and frozen policy classifications. `AntiCollisionPolicyRevision` is immutable; `FieldAntiCollisionPolicyAssignment` selects one exact revision over a UTC interval with optimistic concurrency. Whole-policy deletion removes every revision only when none is referenced by a current or historical Field assignment and requires the latest revision UUID as a concurrency token. Reference and comparison measured depths are SI metres, age thresholds are SI seconds, and separation factors are dimensionless. The MCP server advertises a narrower creation schema than the generated REST DTO: agents cannot submit server-derived calculation state, policy snapshot, classification, progress, messages, relevant-depth ranges, or results.

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
- The schema merger rewrites discriminator mapping references together with shortened schema names. Polymorphic collections remain typed to their discriminated base so generated clients preserve mixed well-path curve variants.
- The generator post-processes NJsonSchema's System.Text.Json inheritance converter so a read-modify-write round trip emits each polymorphic discriminator exactly once and first. This applies to extrapolation `Mode`, well-path `CurveType`, geosteering `ExtentType`, and other generated discriminated contracts.
- `expectedModifiedUtc` is generated with the round-trip (`O`) date-time format. Callers must still treat it as an opaque value copied from the latest `LastModificationDate`.

## Regeneration

For a Trajectory REST contract change, first build `Service` so its build target refreshes `json-schemas/TrajectoryFullName.json`. Refresh other dependency schemas from their owning repositories when those contracts change. Then run from the repository root:

Dependency refreshes include their current merged service schemas where Trajectory consumes composed APIs. This is especially important for Cluster's `ReferencePoint`, nullable Rig equipment properties, and the WellBore Architecture aggregate contract.

```powershell
dotnet build .\Service\Service.csproj
dotnet run --project .\ModelSharedOut\ModelSharedOut.csproj
```

Accept the overwrite prompt after reviewing the inputs. The generator updates `TrajectoryMergedModel.cs`, `PseudoConstructors.cs`, and `Service/wwwroot/json-schema/TrajectoryMergedModel.json` together. Build the full solution and run the contract tests after generation; do not patch the generated C# client directly.
