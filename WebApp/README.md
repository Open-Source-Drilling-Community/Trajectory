# Trajectory WebApp

`WebApp` is the ASP.NET Core Blazor host application for Trajectory.

It provides the application shell, startup configuration, routing, and static assets for the UI. The Trajectory and TrajectoryInterpolation pages themselves are now provided by the `WebPages` Razor class library.

## Container

The host application is packaged as the Docker image:

`norcedrillingtrajectorywebappclient`

It is published under the `digiwells` organization:

https://hub.docker.com/?namespace=digiwells

## UI Endpoint

The web application is available at:

https://dev.digiwells.no/Trajectory/webapp/Trajectory

https://app.digiwells.no/Trajectory/webapp/Trajectory

The backing service OpenAPI endpoint is available at:

https://dev.digiwells.no/Trajectory/api/swagger

https://app.digiwells.no/Trajectory/api/swagger

## Project Relationship

- `WebApp` hosts and configures the UI.
- `WebPages` contains the reusable Razor pages and page-specific support components.
- `ModelSharedOut` provides the generated service client types used by `WebPages`.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*
