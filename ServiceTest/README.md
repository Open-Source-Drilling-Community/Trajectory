# ServiceTest

`ServiceTest` contains automated tests for the Trajectory service API.

## Responsibility

This project exercises the Trajectory service through its generated client and verifies API behavior such as create, read, update, and delete operations.

These tests are integration-style tests rather than pure unit tests.

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

## Solution Role

- validates the external API contract and behavior of `Service`
- uses the generated client types from `ModelSharedOut`
- complements `ModelTest`, which targets the model layer directly

## MCP coverage

`McpServerHttpTests.cs` exercises MCP initialization, tool discovery, successful structured/text responses, and representative calls against a running service. It also verifies that missing inputs, unknown arguments, and missing resources produce stable sanitized MCP errors.

`McpToolRegistrationTests.cs` runs without a live service and guards the MCP contract: 121 REST-backed tools, underscore-only unique names, strict input and success-output schemas, titles and safety annotations, non-empty UUIDs, unknown-argument rejection, optimistic concurrency on durable core mutations, explicit catalog-mapping restore policy, bounded primary-resource search, chunk-upload/commit guidance, calculation polling, SI-unit annotations, and octree filtering/status/provenance semantics.

`TrajectoryBatchServiceTests.cs` verifies dependency-closed trajectory export, parent survey-run inclusion, measurement and station chunk round trips, exact-UUID catalog matching by default, explicit opt-in normalized-name mapping, restore ordering, and all-or-nothing conflict behavior.

`TrajectoryCatalogMigrationTests.cs` verifies that a version-1 `Trajectory.db` is upgraded to version 2 without rewriting existing records, that legacy identity and feature rows are copied into the main database, and that `TrajectoryCatalog.db` remains intact.

`OctreePersistenceTests.cs` verifies the lossless version-1 to version-2 `GlobalAntiCollision.db` migration and its integrity-checked backup, atomic rollback of a failed replacement, trajectory-type/definitive filtering, status/provenance/count reporting, classification changes, and complete indexed deletion of a trajectory's state and bucket memberships.

`SqlConnectionManagerSafetyTests.cs` is also self-contained. It verifies transactional fresh creation, lossless adoption of an exact legacy schema, and fail-closed handling of malformed and newer databases. Run both self-contained groups without a live API with:

```bash
dotnet test ServiceTest/ServiceTest.csproj --filter "FullyQualifiedName~SqlConnectionManagerSafetyTests|FullyQualifiedName~McpToolRegistrationTests"
```

The service must be available at the test base URL (port 8080 in the current local setup) before running the MCP HTTP tests.
