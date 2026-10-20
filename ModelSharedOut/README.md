# ModelSharedOut

`ModelSharedOut` manages generated shared models and client-side service contract types for consumers of the Trajectory service. Its generated namespace is `OSDC.Drilling.Trajectory.ModelShared`.

The committed source and merged OpenAPI documents preserve the service's structured `x-osdc-semantic` extensions. Regenerate them after changing model bindings or the provider registry.

## Responsibility

This project stores OpenAPI schemas and generates C# classes that are used by downstream consumers of the Trajectory API.

It includes the Trajectory service schema together with other related schemas needed by clients and reusable UI components.

The Field dependency input is the Field-owned `FieldFullName.json` contract rather than the merged dependency bundle. It is processed after transitive dependency bundles so an older embedded `Field` schema cannot replace the authoritative shape (including `ReferencePoint`) during short-name merging. The Earth Geodesy input is likewise pinned from its owning `EarthGeodesyFullName.json` contract so shared short names keep the authoritative geodetic definitions.

The WellBore-owned `WellBoreFullName.json` input is processed after the local
Trajectory service schema. The latter can contain a transitive WellBore copy from
a dependency assembly; it must not override current properties such as `RigJobs`.

The generated output includes client types and methods for trajectory realization, extrapolation, target-landing, and directional-control evaluation cases, including light/status data and chunked large-result retrieval. Directional-control compact cases carry bundle statistics while interval samples use separate zero-based chunks. Target landing includes separate edit/display projections, unrestricted full case data, the source endpoint and sampled-lead snapshots, authoritative normalized curve-control points, and reusable minimum/junction/maximum curvature characteristics. Extrapolation cases expose the same characteristics over their actively steered sections and distinguish lead-in, upstream, and downstream section roles. It also includes four discriminated extrapolation specifications, the closed departure-versus-steering-length geosteering extent, explicit solved-section roles, heterogeneous well-path sections, and chunked result retrieval. SurveyRun contracts include the optional terminal bit-extrapolation mode, positive SI tool-to-bit distance, and measured-versus-extrapolated row provenance. Resource-specific SurveyRun and Trajectory ellipse endpoints let clients request authoritative lineage replay instead of trusting submitted covariance. It also contains the versioned batch export/restore document, conflict and catalog policies (including the explicit normalized-name-mapping opt-in), the two dependency-aware backup endpoints, bounded trajectory/survey-run search, single-record external-reference validation and bounded audit results, optimistic-concurrency parameters for durable mutations, the typed octree index status/provenance, the asynchronous filtered overlap-search contract, and lightweight polling contracts for long calculations without repeatedly transferring result payloads.

For anti-collision clients, the generated REST sequence is `QueueOctreeSearchAsync` → repeated `GetOctreeSearchStatusAsync` calls → `GetOctreeSearchResultAsync` after completion. The returned candidate UUIDs can be selected for a durable Global Anti-Collision separation-factor request, which is then polled through its lightweight status endpoint before downloading its terminal profile and frozen policy classifications. `AntiCollisionPolicyRevision` is immutable; `FieldAntiCollisionPolicyAssignment` selects one exact revision over a UTC interval with optimistic concurrency. Whole-policy deletion removes every revision only when none is referenced by a current or historical Field assignment and requires the latest revision UUID as a concurrency token. Reference and comparison measured depths are SI metres, age thresholds are SI seconds, and separation factors are dimensionless. The MCP server advertises a narrower creation schema than the generated REST DTO: agents cannot submit server-derived calculation state, policy snapshot, classification, progress, messages, relevant-depth ranges, or results.

## Generated API coverage

The generated client covers SurveyRun and Trajectory CRUD/search/chunks, batch import, shared identity and feature catalogs, external-reference validation/audit, dependency-closed backup/restore, interpolation, aggregation, realization, station ellipses, SurveyRun/Trajectory minimum distance, extrapolation, target landing, directional-control evaluation, octree status/repair and queued candidate search, durable global anti-collision calculations, immutable policy revisions, effective-dated Field assignments, and usage statistics. Large calculation families expose light/status records and chunk-count/chunk operations so consumers do not need an unrestricted aggregate payload.

## Dependencies

`ModelSharedOut` depends on:

- `Microsoft.OpenApi.Readers`
- `NSwag.CodeGeneration.CSharp`
- `Microsoft.CodeAnalysis.CSharp`

## Solution Role

- `WebPages` depends on `ModelSharedOut`.
- client-facing generated types and schemas are produced here for use outside the core service implementation.
- reusable calculation pages use generated light/status and chunk endpoints to avoid loading large station, realization, distance, target-landing, and directional-control results through discovery lists.

## Notes

- The project is configured as an executable because it includes code-generation tooling.
- It contains helper code related to generated pseudo-constructors and schema processing.
- `TrajectoryMergedModel.cs`, `PseudoConstructors.cs`, and the merged JSON/OpenAPI artifacts carry the OSDC namespace and should be regenerated together after service-contract changes.
- Lightweight projection classes intentionally do not receive pseudo-constructors; generated aggregate constructors initialize collections of light projections as empty lists.
- The schema merger rewrites discriminator mapping references together with shortened schema names. Polymorphic collections remain typed to their discriminated base so generated clients preserve mixed well-path curve variants.
- The generator post-processes NJsonSchema's System.Text.Json inheritance converter so a read-modify-write round trip emits each polymorphic discriminator exactly once and first. This applies to extrapolation `Mode`, well-path `CurveType`, geosteering `ExtentType`, and other generated discriminated contracts.
- `expectedModifiedUtc` is generated with the round-trip (`O`) date-time format. Callers must still treat it as an opaque value copied from the latest `LastModificationDate`.
- Collection properties, instances, parameters, and response arrays are configured as mutable `List<T>` in the authoritative generator settings; the pseudo-constructor generator handles nested `List<T>` shapes directly.

## Regeneration

For a Trajectory REST contract change, first build `Service` so its build target refreshes `json-schemas/TrajectoryFullName.json`. Refresh other dependency schemas from their owning repositories when those contracts change. Then run from the repository root:

Dependency refreshes include their current merged service schemas where Trajectory consumes composed APIs. This is especially important for Cluster's `ReferencePoint`, nullable Rig equipment properties, and the WellBore Architecture aggregate contract.

```powershell
dotnet build .\Service\Service.csproj
dotnet run --project .\ModelSharedOut\ModelSharedOut.csproj
```

Accept the overwrite prompt after reviewing the inputs. The generator updates `TrajectoryMergedModel.cs`, `PseudoConstructors.cs`, and `Service/wwwroot/json-schema/TrajectoryMergedModel.json` together. Build the full solution and run the contract tests after generation; do not patch the generated C# client directly.

The target-landing client includes the lazy `GetTargetLandingCaseUncertaintyDisplayDataAsync` projection. It returns only MD-keyed perpendicular ellipse parameters needed by the Cartesian display; target-plane landing ellipses remain part of boundary samples in `DisplayData`.

The regenerated full and merged contracts preserve explicit survey-coordinate aliases, their catalogue quantity/reference metadata, and UUID lookup/relationship scope. The generated C# descriptions document these conventions; property shapes are unchanged. See [semantic bindings](../SEMANTIC-BINDINGS.md).

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

Matrix contracts now include a numeric `FormatVersion` (1) and nine nullable row-major `Values`. Covariance is in square metres; eigenvectors are dimensionless. Use the regenerated client or preserve these JSON members when exporting/restoring complete data; legacy dimension-only matrices contain no usable covariance. The version stays numeric under the client string-enum convention.
