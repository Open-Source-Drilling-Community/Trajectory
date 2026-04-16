# Trajectory Service

`Service` is the ASP.NET Core microservice for Trajectory.

It exposes the Trajectory API and depends on the `Model` project for the domain model and computation logic.

## Container

The service is packaged as the Docker image:

`norcedrillingtrajectoryservice`

It is published under the `digiwells` organization:

https://hub.docker.com/?namespace=digiwells

## Endpoints

OpenAPI / Swagger:

https://dev.digiwells.no/Trajectory/api/swagger

https://app.digiwells.no/Trajectory/api/swagger

Trajectory API:

https://dev.digiwells.no/Trajectory/api/Trajectory

https://app.digiwells.no/Trajectory/api/Trajectory

## Related Projects

- `Model` contains the main model and trajectory calculation logic used by the service.
- `ModelSharedOut` contains generated client-side types and service schemas for consumers.
- `WebPages` contains the reusable Razor UI pages for Trajectory and TrajectoryInterpolation.
- `WebApp` is the host application that renders the UI using `WebPages`.

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

**Andrew Holsaeter**, *NORCE Energy Modelling and Automation*

**Lucas Volpi**, *NORCE Energy Modelling and Automation*
