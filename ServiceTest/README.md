# ServiceTest

`ServiceTest` contains automated tests for the Trajectory service API.

## Responsibility

This project exercises the Trajectory service through its generated client and also contains self-contained persistence, validation, and MCP-contract tests.

Tests in `Tests.cs` and `McpServerHttpTests.cs` are integration-style and require a running service. The remaining fixtures construct their own collaborators or isolated databases and do not require the HTTP service.

Service-level tests should cover trajectory realization case CRUD operations, light-data polling, asynchronous calculation state changes, and chunked retrieval of realized trajectories.

## Dependencies

`ServiceTest` depends on:

- `ModelSharedOut`
- `NUnit`
- `Microsoft.NET.Test.Sdk`

## Runtime Expectation

The tests expect a Trajectory service instance to be running locally at:

`http://localhost:8080/`

The test code configures the generated client against:

`http://localhost:8080/Trajectory/api/`

## Running Tests

Start the service first, then run:

```bash
dotnet test ServiceTest/ServiceTest.csproj
```

Run self-contained tests while the service is stopped with:

```powershell
dotnet test .\ServiceTest\ServiceTest.csproj --filter "FullyQualifiedName!~ServiceTest.Tests&FullyQualifiedName!~McpServerHttpTests"
```

For the full suite, launch `Service` at port 8080 from an isolated working directory whose sibling `home` directory contains disposable test databases, then run the unfiltered command. This avoids touching the repository's `home` data or any deployed persistent volume.

## Solution Role

- validates the external API contract and behavior of `Service`
- uses the generated client types from `ModelSharedOut`
- complements `ModelTest`, which targets the model layer directly

## MCP coverage

`McpServerHttpTests.cs` exercises MCP initialization, tool discovery, successful structured/text responses, and representative calls against a running service. It also verifies that missing inputs, unknown arguments, missing resources, an empty planned/actual octree selection, and caller-supplied separation-factor calculation state produce stable sanitized MCP errors.

`McpToolRegistrationTests.cs` runs without a live service and guards the MCP contract: 130 REST-backed tools, underscore-only unique names, strict input and success-output schemas, titles and safety annotations, non-empty UUIDs, unknown-argument rejection, optimistic concurrency on durable core mutations, explicit catalog-mapping restore policy, bounded primary-resource search and external-reference audits, chunk-upload/commit guidance, and asynchronous-only octree filtering, status, provenance, and overlap-search semantics. It also checks the scan-to-separation-factor handoff, transient scan retention, mutually valid reference choices, unique non-empty comparison selections, server-derived-field exclusion, terminal result schemas, SI measured-depth ranges, and dimensionless separation factors.

`TrajectoryExternalReferenceValidatorTests.cs` verifies successful Field/Cluster/Well/WellBore/SurveyInstrument resolution, per-page deduplication, missing-resource classification, unavailable dependency handling, optional unlinked references, and rejection of empty required references.

`TrajectoryBatchServiceTests.cs` verifies dependency-closed trajectory export, parent survey-run inclusion, measurement and station chunk round trips, exact-UUID catalog matching by default, explicit opt-in normalized-name mapping, restore ordering, and all-or-nothing conflict behavior.

`TrajectoryCatalogMigrationTests.cs` verifies that a version-1 `Trajectory.db` is upgraded to version 2 without rewriting existing records, that legacy identity and feature rows are copied into the main database, and that `TrajectoryCatalog.db` remains intact.

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
