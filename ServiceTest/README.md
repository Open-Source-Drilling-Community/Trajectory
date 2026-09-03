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

`McpServerHttpTests.cs` exercises MCP initialization, tool discovery, and representative calls against a running service. The MCP client dependency and ASP.NET Core framework reference in this project support those live protocol tests.

`McpToolRegistrationTests.cs` runs without a live service and guards the generated MCP contract: 118 non-statistics tools, underscore-only unique names, explicit object schemas, detailed descriptions, shared identity/feature catalog concurrency, chunk-upload/commit guidance, calculation polling, and SI-unit annotations.

`SqlConnectionManagerSafetyTests.cs` is also self-contained. It verifies transactional fresh creation, lossless adoption of an exact legacy schema, and fail-closed handling of malformed and newer databases. Run both self-contained groups without a live API with:

```bash
dotnet test ServiceTest/ServiceTest.csproj --filter "FullyQualifiedName~SqlConnectionManagerSafetyTests|FullyQualifiedName~McpToolRegistrationTests"
```

The service must be available at the test base URL (port 8080 in the current local setup) before running the MCP HTTP tests.
