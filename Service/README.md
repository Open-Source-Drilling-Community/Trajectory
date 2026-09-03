# Trajectory Service

`Service` is the ASP.NET Core microservice for Trajectory. Its code namespace root is `OSDC.Drilling.Trajectory.Service`.

It exposes the Trajectory API and depends on the `Model` project for the domain model and computation logic.

## Responsibilities

- expose CRUD endpoints for trajectory data
- expose trajectory interpolation cases
- expose trajectory realization cases
- persist data in SQLite
- run trajectory realization calculations asynchronously so long-running cases do not block the request that creates or updates the case

## Container

The service is packaged as the Docker image:

`docker.io/digiwells/osdcdrillingtrajectoryservice:stable`

It is published under the `digiwells` organization:

https://hub.docker.com/?namespace=digiwells

## Endpoints

OpenAPI / Swagger:

https://dev.digiwells.no/Trajectory/api/swagger

https://app.digiwells.no/Trajectory/api/swagger

Trajectory API:

https://dev.digiwells.no/Trajectory/api/Trajectory

https://app.digiwells.no/Trajectory/api/Trajectory

Trajectory realization cases are exposed through:

- `TrajectoryRealizationCase`
- `TrajectoryRealizationCase/LightData`
- `TrajectoryRealizationCase/{id}`
- `TrajectoryRealizationCase/{id}/Realizations/ChunkCount`
- `TrajectoryRealizationCase/{id}/Realizations/Chunks/{chunkIndex}`

The light data endpoint is intended for grids and polling calculation status. Realized trajectories are stored separately in chunks, with 25 realizations per chunk by default, so clients can load large result sets progressively.

## Related Projects

- `Model` contains the main model and trajectory calculation logic used by the service.
- `ModelSharedOut` contains generated client-side types and service schemas for consumers.
- `WebPages` contains the reusable Razor UI pages for Trajectory, TrajectoryInterpolation, and TrajectoryRealization.
- `WebApp` is the host application that renders the UI using `WebPages`.

## Persistence and identity cutover

The service keeps its historical API path (`/Trajectory/api` case-insensitively), database filenames, and `trajectory-claim` storage identity. Its renamed Helm chart is `charts/osdcdrillingtrajectoryservice` and defaults to a `Recreate` deployment strategy. For a new OSDC Helm release that must reuse production data, set `persistence.existingClaim=trajectory-claim` explicitly.

Fresh databases are created transactionally at schema version 1. An exact unversioned legacy schema is adopted by setting only SQLite `user_version`; existing rows are not rewritten. Unexpected tables, missing or malformed columns, and newer schema versions fail startup without automatic deletion or reconstruction.

## Source Code Origin

The original service and host web application solution was generated from a NORCE Drilling and Wells Modelling Team .NET template.

Creation date: `02/12/2025`

Version: `4.0.22`

Template source:

https://github.com/NORCE-DrillingAndWells/Templates

Template documentation:

https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki/.NET-Templates

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

## MCP server

The service publishes its non-statistics REST actions as MCP tools. Tool registration discovers controller actions and preserves support for asynchronous operations, chunked trajectory data, filters, and multi-ID requests. Every tool has an operation-specific description and an explicit JSON input schema, including nested model properties, UUID and date-time formats, enum values, defaults, and SI-unit guidance.

- Streamable HTTP: `/trajectory/api/mcp`
- WebSocket: `/trajectory/api/mcp/ws`
- Published controller tools: 104
- Utility tools: `ping`
- Excluded surface: `TrajectoryUsageStatisticsController`

The descriptions explain the service workflows as well as individual calls. In particular, survey-measurement chunks are uploaded with zero-based indexes and then committed; calculation cases are created and polled through `CalculationState`/`CalculationProgress`; large station, realization, minimum-distance, and aggregation results are retrieved through chunk-count and chunk tools. Unless a field explicitly says otherwise, lengths, depths, coordinates, and distances are metres, angles are radians, and curvature is radians per metre.

Optional registration with an external MCP hub is configured in `appsettings.json` and is disabled by default.
