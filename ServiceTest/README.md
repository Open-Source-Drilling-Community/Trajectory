# ServiceTest

`ServiceTest` contains automated tests for the Trajectory service API.

## Responsibility

This project exercises the Trajectory service through its generated client and also contains self-contained persistence, validation, and MCP-contract tests.

`StoredTrajectoryEllipseTests` uses isolated SQLite data and a local instrument stub to verify that 0.95-confidence ellipses retain completed geometry while replaying authoritative uncertainty, and reject changed source attitudes.

Tests in `Tests.cs` and `McpServerHttpTests.cs` are integration-style and host the real REST and Streamable HTTP MCP pipelines in process. The remaining fixtures construct their own collaborators or isolated databases.

Service-level tests cover SurveyRun and Trajectory persistence/contracts, catalog and backup rules, every durable calculation family, light-data polling, asynchronous calculation state changes, chunked results, external references, anti-collision indexing/policies, and REST/MCP parity.

## Dependencies

`ServiceTest` depends on:

- `ModelSharedOut`
- `NUnit`
- `Microsoft.NET.Test.Sdk`

## Running Tests

Run the self-contained suite directly:

```bash
dotnet test ServiceTest/ServiceTest.csproj
```

The integration fixtures reset legacy process-wide manager caches before creating their hosts so persistence tests cannot leave stale database bindings behind.

## Solution Role

- validates the external API contract and behavior of `Service`
- uses the generated client types from `ModelSharedOut`
- complements `ModelTest`, which targets the model layer directly

## MCP coverage

`McpServerHttpTests.cs` exercises MCP initialization, tool discovery, successful structured/text responses, and representative calls against the in-process service. It also verifies that missing inputs, unknown arguments, missing resources, an empty planned/actual octree selection, and caller-supplied separation-factor calculation state produce stable sanitized MCP errors.

`McpToolRegistrationTests.cs` runs without a live service and guards the MCP contract: the REST-backed tools including read-only usage statistics and the physical uncertainty-envelope evaluator, underscore-only unique names, strict input and success-output schemas, titles and safety annotations, non-empty UUIDs, unknown-argument rejection, optimistic concurrency on durable core mutations, explicit catalog-mapping restore policy, bounded primary-resource search and external-reference audits, chunk-upload/commit guidance, and asynchronous calculation semantics. It checks the resource-specific lineage-aware ellipse tools, all four trajectory-extrapolation discriminators, the target-landing edit/display projections and closed mutation/result boundary, the closed same-WellBore directional-control submission and chunk workflow, the geosteering departure/drilled-length extent, and the anti-collision policy age/identity/feature discriminator, effective-dated assignment workflow, server-derived policy-result exclusion, scan-to-separation-factor handoff, terminal result schemas, SI measured-depth ranges, and dimensionless separation factors.

It also requires explicit domain workflow guidance for interpolation, aggregation, realization, both minimum-distance families, standalone and resource-owned ellipses, batch import, extrapolation, target landing, and directional-control evaluation, preventing a newly reflected operation from being published with only a generic CRUD description.

`TrajectoryExternalReferenceValidatorTests.cs` verifies successful Field/Cluster/Well/WellBore/SurveyInstrument resolution, per-page deduplication, missing-resource classification, unavailable dependency handling, optional unlinked references, and rejection of empty required references.

`TrajectoryBatchServiceTests.cs` verifies dependency-closed trajectory export, parent survey-run inclusion, measurement and station chunk round trips, exact-UUID catalog matching by default, explicit opt-in normalized-name mapping, restore ordering, and all-or-nothing conflict behavior.

`SurveyRunUncertaintyContinuationTests.cs` verifies that Wolff-de Wardt covariance propagation continues the parent transfer matrix into a tied child SurveyRun even when a sidetrack restarts its local measured depth at zero, and that resource-specific ellipse calculations replace stale covariance and interpolate authoritative covariance at requested display depths.

`SurveyRunBitExtrapolationTests.cs`, `SurveyRunCorrectionServiceTests.cs`, and the REST/MCP contract tests cover legacy measured-row defaults, straight single-station extension, all three final-curve continuations, final-row/distance validation, rejection of caller-supplied generated endpoints, correction exclusion, SI metadata, and backup round trips for the frozen bit-extrapolation configuration.

`TrajectoryCatalogMigrationTests.cs` verifies that version-1 through version-8 `Trajectory.db` files are upgraded additively to the current schema without rewriting existing records, that legacy identity and feature rows are copied into the main database, that extrapolation, anti-collision policy, target-landing, and directional-control tables are added at their corresponding migration steps, that target-landing light queries use their covering index instead of the large result-bearing table rows, and that `TrajectoryCatalog.db` remains intact. `DirectionalControlEvaluationPersistenceTests.cs` verifies atomic 250-sample chunk replacement, compact case payloads, and rollback of stale deletes. `AntiCollisionPolicyTests.cs` covers rule validation, indeterminate evidence, first-match classification, immutable revision numbering, non-overlapping Field assignments, and schema-version-2 backup/restore.

`OctreePersistenceTests.cs` verifies the lossless version-1 to version-2 `GlobalAntiCollision.db` migration and its integrity-checked backup, atomic rollback of a failed replacement, exact and combined planned/actual/definitive overlap filtering, status/provenance/count reporting, classification changes, and complete indexed deletion of a trajectory's state and bucket memberships.

`OctreeEnvelopeCoverageTests.cs` characterizes the spatial broad phase. It verifies detection of a crossing between longitudinal mesh samples, strict concentric containment, entry through an uncertainty-volume end, and continuous relevant-MD range coverage.

`OctreeCoverageBenchmarkTests.cs` is an explicit, read-only benchmark against the development Trajectory API. It compares the production one-cell-padded solid swept-AABB cover at depth 22 with the same representation at depths 20, 21, and 23 using fresh isolated SQLite databases. Every representation is compacted without crossing the database cache depth. It is excluded from normal CI runs. Run it deliberately with:

```powershell
dotnet test .\ServiceTest\ServiceTest.csproj --filter FullyQualifiedName~OctreeCoverageBenchmarkTests --configuration Release --logger "console;verbosity=detailed" -- NUnit.ExplicitMode=Relaxed
```

The corrected 2026-09-06 run over 15 development trajectories found 81 candidate pairs at depth 23 and 90 at depth 22. Depth 23 used 104,513 compacted codes and a 3.9 MiB fresh database; depth 22 used 35,373 codes and 3.4 MiB. Across one scan per reference trajectory, persisted search took about 54 seconds at depth 23 and 5 seconds at depth 22. A separate production Valhall slot-coordinate analysis, assuming one occupied well per each of 119 defined slots, estimated that depth 22 would add an average 5.63 slot-proximity candidates per reference well relative to depth 23, concentrated at DP and WP. Depth 22 is retained as the compromise between broad-phase speed and downstream separation-factor work. Treat these figures as dataset-specific benchmark snapshots, not fixed performance guarantees.

`SqlConnectionManagerSafetyTests.cs` is also self-contained. It verifies transactional fresh creation, lossless adoption of an exact legacy schema, and fail-closed handling of malformed and newer databases. To run only it and the MCP registration checks:

```bash
dotnet test ServiceTest/ServiceTest.csproj --filter "FullyQualifiedName~SqlConnectionManagerSafetyTests|FullyQualifiedName~McpToolRegistrationTests"
```

The service must be available at the test base URL (port 8080 in the current local setup) before running the MCP HTTP tests.

Semantic contract tests validate MD/TVD aliases and SI quantities, identifier lookup and relationship scope, discovery metadata, catalogue noun/role/reference kinds, and the absence of a WGS84-depth annotation on an arbitrary vector Z. REST/merged schema tests verify that generation preserves these bindings.

TrajectoryStationEvaluationTests verify numerical station interpolation, source-list preservation, rejection of extrapolation/nonfinite inputs, invalid ordering and unfinished calculations. MCP registration tests check the typed UUID, required SI MD key and read-only behavior. Provider station interpolation remains authoritative; consumers do not interpolate individual scalar properties.


Station-evaluation tests also verify that interpolated canonical Riemannian coordinates produce equivalent finite WGS84 latitude/longitude for spatial consumers.

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


Contract checks cover removed heavy routes/tools, bounded pagination and strict invalid-argument rejection, and omission of engineering collections from discovery. Persistence tests exercise ordering, filters, totals, continuation, empty results, and retained selected reads. Run transport tests against isolated test storage.

`DiscoveryConcurrencyTests` verifies WAL discovery during an active writer and target-landing discovery through indexed metadata, including filters, pagination, and source-revision staleness without parsing engineering documents.

`TrajectoryRecalculationSafetyTests` covers explicit geographic-to-Riemannian wellhead conversion, horizontal covariance against WGS84 scale factors, invalid inputs, main-wellbore and sidetrack calculation with/without uncertainty replay, preservation of chunked and inline geometry on calculation/save failure, atomic successful replacement, and a failed first calculation without fabricated results.
