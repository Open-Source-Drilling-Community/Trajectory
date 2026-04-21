# NORCE.Drilling.Trajectory.WebPages

`NORCE.Drilling.Trajectory.WebPages` is a Razor class library that contains the Trajectory UI pages extracted from the main Trajectory web application.

It currently provides:

- `TrajectoryMain`
- `TrajectoryEdit`
- `TrajectoryInterpolatedMain`
- `TrajectoryInterpolationEdit`
- supporting UI components used by those pages

## Purpose

This package makes the Trajectory and TrajectoryInterpolation pages reusable from another ASP.NET Core Blazor host application without copying the page source into that host.

## Dependencies

The package depends on:

- `NORCE.Drilling.Trajectory.ModelSharedOut`
- `MudBlazor`
- `Plotly.Blazor`
- `OSDC.UnitConversion.DrillingRazorMudComponents`

## Host Application Requirements

The consuming application is expected to:

- reference this package
- configure routing so the assembly containing `NORCE.Drilling.Trajectory.WebPages` components is discovered
- provide the required MudBlazor services
- load the Plotly.Blazor static assets
- register an `ITrajectoryAPIUtils` implementation in dependency injection
- register an `ITrajectoryWebPagesConfiguration` implementation

## Configuration

The pages depend on an injected `ITrajectoryAPIUtils` service.

The streamlined design is to register:

- a host-side `ITrajectoryWebPagesConfiguration`
- the concrete `TrajectoryAPIUtils`

`ITrajectoryWebPagesConfiguration` extends the following host URL interfaces from `OSDC.DotnetLibraries.Drilling.WebAppUtils`:

- `IFieldHostURL`
- `IClusterHostURL`
- `IRigHostURL`
- `IWellHostURL`
- `IWellBoreHostURL`
- `IWellBoreArchitectureHostURL`
- `ITrajectoryHostURL`
- `IUnitConversionHostURL`

The host application is responsible for supplying those endpoint values through its configuration object.

## Notes

This package contains the UI pages and page-specific support code. It does not by itself provide the service backend.
