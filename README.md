# Trajectory

The Trajectory repository contains the Trajectory service, the host web application, and a reusable Razor class library for the Trajectory UI pages.

## Solution Architecture

The solution currently contains:

- `ModelSharedIn`
  - auto-generated C# classes for upstream model dependencies
  - source schemas are stored as JSON files following the OpenAPI standard
- `Model`
  - domain model and trajectory calculation logic
  - trajectory interpolation and stochastic trajectory realization calculations
- `Service`
  - ASP.NET Core microservice exposing the Trajectory API
  - depends on `Model`
  - persists trajectory realization cases and realization chunks in SQLite
- `ModelSharedOut`
  - auto-generated client-side classes and schemas used by consumers of the Trajectory service
  - includes the Trajectory service schema together with other relevant upstream schemas
- `WebPages`
  - Razor class library containing the Trajectory, TrajectoryInterpolation, and TrajectoryRealization pages and their page-specific support components
  - depends on `ModelSharedOut`
- `WebApp`
  - ASP.NET Core Blazor host application
  - depends on `WebPages`
  - provides the host shell, routing, configuration, and static assets for the UI
- `ModelTest`
  - unit tests for the model and computation logic
- `ServiceTest`
  - tests for the service API
- `home`
  - local persisted data, including the SQLite database at `home/Trajectory.db`

## Main Workflows

The repository supports the following main trajectory workflows:

- trajectory creation, editing, storage, and retrieval
- trajectory interpolation cases
- stochastic trajectory realization cases based on survey station wellbore position uncertainty

Trajectory realization cases are defined from a reference trajectory and a requested number of realizations. The model optionally coarsens the reference trajectory before generation, draws realizations from the covariance-defined uncertainty field, completes the generated points with the minimum curvature method, and stores the resulting realized trajectories as lists of survey points. Large realization sets are persisted and retrieved in chunks.

## Security and Confidentiality

Data are persisted as clear text in a single SQLite database hosted in the service container.
Neither authentication nor authorization have been implemented.

Docker containers for the service and host web application are available under the `digiwells` organization:

https://hub.docker.com/?namespace=digiwells

More information about running the containers and mapping the database to a local folder is available here:

https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki

## Deployment

The Trajectory service is available at:

https://dev.digiwells.no/Trajectory/api/Trajectory

https://app.digiwells.no/Trajectory/api/Trajectory

The host web application is available at:

https://dev.digiwells.no/Trajectory/webapp/Trajectory

https://app.digiwells.no/Trajectory/webapp/Trajectory

The OpenAPI schema of the service is available at:

https://dev.digiwells.no/Trajectory/swagger

https://app.digiwells.no/Trajectory/swagger

The service and host web application are deployed as Docker containers using Kubernetes and Helm.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

## Current implementation

- The service exposes its REST operations through MCP over streamable HTTP at `/trajectory/api/mcp` and WebSocket at `/trajectory/api/mcp/ws`.
- MCP tools are generated from 104 non-statistics controller actions; the usage-statistics controller is intentionally excluded. A `ping` tool is also available. Tool metadata now provides operation-specific workflow guidance and complete nested JSON input schemas with UUID formats, enum values, defaults, chunk-index constraints, and SI units.
- The trajectory editor supports mean-sea-level depth references through the Vertical Datum integration.
- The WebApp uses the current shared WebPages packages for Field (1.0.19), Cluster (1.0.12), Cartographic Projection (1.0.8), Geodetic Datum (1.0.7), Earth Geomagnetic Field (1.0.4), Gravitational Field (1.0.3), Well (1.0.11), and WellBore (1.0.12).
