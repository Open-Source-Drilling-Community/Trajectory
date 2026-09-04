# Trajectory WebApp

`WebApp` is the ASP.NET Core Blazor host application for Trajectory under `OSDC.Drilling.Trajectory.WebApp`.

It provides the application shell, startup configuration, routing, and static assets for the UI. The Trajectory, TrajectoryInterpolation, and TrajectoryRealization pages themselves are now provided by the `WebPages` Razor class library.

## Container

The host application is packaged as the Docker image:

`docker.io/digiwells/osdcdrillingtrajectorywebappclient:stable`

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

## Navigation

The side menu exposes the main trajectory pages, including the shared Identities and Features catalogs, dependency-aware Backup / Restore under Import/Export, and the refreshable endpoint-level Usage Statistics page. Survey Run and Trajectory editors both assign definitions from those catalogs. Contextual data is ordered Field, Cluster, Well, WellBore, Rig, and Survey Instrument.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

## Current shared-page dependencies

The host consumes the local `OSDC.Drilling.Trajectory.WebPages` project and the published OSDC WebPages packages for the migrated sibling services, including `OSDC.Drilling.WellBoreArchitecture.WebPages`.

The renamed Helm chart is `charts/osdcdrillingtrajectorywebappclient`. All production dependency URLs are supplied as Helm-managed environment variables and point to the corresponding OSDC Kubernetes services. `TrajectoryHostURL` points to `http://osdctrajectoryservice/`. The public routes remain `/Trajectory/webapp` and `/Trajectory/api` because they identify the domain resource rather than the owning organization.
