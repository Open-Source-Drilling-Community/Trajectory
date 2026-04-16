# ServiceTest

`ServiceTest` contains automated tests for the Trajectory service API.

## Responsibility

This project exercises the Trajectory service through its generated client and verifies API behavior such as create, read, update, and delete operations.

These tests are integration-style tests rather than pure unit tests.

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
