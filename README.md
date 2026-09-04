# Trajectory

The Trajectory repository contains the OSDC Trajectory service, the host web application, and a reusable Razor class library for the Trajectory UI pages. The solution-owned .NET namespace root is `OSDC.Drilling.Trajectory`; the companion anti-collision library uses `OSDC.Drilling.GlobalAntiCollision`.

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
  - local persisted data, including `Trajectory.db`, `GlobalAntiCollision.db`, `SeparationFactorResults.db`, and usage history

## Main Workflows

The repository supports the following main trajectory workflows:

- trajectory creation, editing, storage, and retrieval
- trajectory interpolation cases
- stochastic trajectory realization cases based on survey station wellbore position uncertainty

Trajectory realization cases are defined from a reference trajectory and a requested number of realizations. The model optionally coarsens the reference trajectory before generation, draws realizations from the covariance-defined uncertainty field, completes the generated points with the minimum curvature method, and stores the resulting realized trajectories as lists of survey points. Large realization sets are persisted and retrieved in chunks.

## Security and Confidentiality

Data are persisted as clear text in SQLite databases hosted in the service container.
Neither authentication nor authorization have been implemented.

Docker containers for the service and host web application are available under the `digiwells` organization:

https://hub.docker.com/?namespace=digiwells

The migrated images are `docker.io/digiwells/osdcdrillingtrajectoryservice:stable` and `docker.io/digiwells/osdcdrillingtrajectorywebappclient:stable`.

## Deployment

The Trajectory service is available at:

https://dev.digiwells.no/Trajectory/api/Trajectory

https://app.digiwells.no/Trajectory/api/Trajectory

The host web application is available at:

https://dev.digiwells.no/Trajectory/webapp/Trajectory

https://app.digiwells.no/Trajectory/webapp/Trajectory

The OpenAPI schema of the service is available at:

https://dev.digiwells.no/Trajectory/api/swagger

https://app.digiwells.no/Trajectory/api/swagger

The service and host web application are deployed as Docker containers using Kubernetes and Helm.

The Helm charts are named `osdcdrillingtrajectoryservice` and `osdcdrillingtrajectorywebappclient`. The service chart deliberately retains the historical `trajectory-claim` PVC and all database filenames. Use `--set persistence.existingClaim=trajectory-claim` for the identity cutover, and do not uninstall the legacy release before verifying the selected cluster, namespace, mounted claim, image digest, and existing record counts. Its `Recreate` strategy prevents overlapping SQLite writers.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on digitalization, drilling engineering, and geosteering.

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

## Current implementation

- The service exposes its REST operations through MCP over streamable HTTP at `/trajectory/api/mcp` and WebSocket at `/trajectory/api/mcp/ws`.
- MCP tools are generated from 120 non-statistics controller actions; the usage-statistics controller is intentionally excluded. A `ping` tool is also available. Tool metadata provides operation-specific workflow guidance and complete nested JSON input schemas with UUID formats, enum values, defaults, chunk-index constraints, identity/feature assignments, backup/restore policies, and SI units.
- The trajectory editor supports mean-sea-level depth references through the Vertical Datum integration.
- Survey runs and trajectories share extensible identity and feature catalogs. Both editors support assignments; catalog definitions are managed from the `TrajectoryIdentities` and `TrajectoryFeatures` pages.
- The Backup / Restore page creates versioned JSON backups. Survey runs may be selected independently; selecting a trajectory automatically includes its referenced survey runs and their parent chains. Restore validates the complete dependency graph, resolves the shared catalogs, writes survey runs before trajectories, and commits record changes atomically without recalculation.
- The Usage Statistics page follows the shared resource-service layout with refresh and failure states, responsive summary metrics, and a sortable per-endpoint table containing method, today and total counts, and last use.
- The default identities are `NameForPlanning`, `NameForCompanyReporting`, `NameForRegulatoryReporting`, `Nickname`, and `NameForOperationReporting`. The default feature categories are `SurveyContext`, `BoreholeSectionContext`, `SurveyPurpose`, `TrajectoryPurpose`, `SurveyReferenceStatus`, `AcquisitionMode`, `MeasurementCondition`, `RunningMode`, `DataProcessingState`, `CorrectionApplied`, `QualityStatus`, `QualityIssue`, and `SurveyStationDensity`.
- The WebApp uses the published OSDC shared WebPages packages for Field, Cluster, Rig, Well, WellBore, Survey Instrument, Earth Cartographic Projection, Earth Geodesy, Earth Gravity, Earth Magnetic Field, and Earth Vertical Datum.
- The reusable UI package identity is `OSDC.Drilling.Trajectory.WebPages`. All first-party reusable UI dependencies use their OSDC package identities.
- Production dependency URLs are also expressed as Helm-managed environment variables and point to the OSDC Kubernetes services. The stable public resource routes remain `/Trajectory/api` and `/Trajectory/webapp`.
- `Trajectory.db` schema version 2 stores survey/trajectory records and their shared identity and feature catalogs together. Version-0/1 databases are upgraded transactionally by adding the two catalog tables and copying rows from a validated legacy `TrajectoryCatalog.db`; the legacy file is retained as a rollback copy. Existing domain rows are not rewritten. Unknown, malformed, incomplete, or newer schemas stop startup without deleting or repairing data.
