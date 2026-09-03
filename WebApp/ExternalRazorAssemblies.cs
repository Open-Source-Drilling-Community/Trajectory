using System.Reflection;
using OSDC.Drilling.Trajectory.WebPages;

namespace OSDC.Drilling.Trajectory.WebApp;

public static class ExternalRazorAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(TrajectoryMain).Assembly,
        typeof(OSDC.Drilling.WellBoreArchitecture.WebPages.Pages.WellBoreArchitectureMain).Assembly,
        typeof(OSDC.Drilling.Rig.WebPages.Pages.RigMain).Assembly,
        typeof(OSDC.Drilling.WellBore.WebPages.WellBoreMain).Assembly,
        typeof(OSDC.Drilling.Well.WebPages.WellMain).Assembly,
        typeof(OSDC.Drilling.Cluster.WebPages.ClusterMain).Assembly,
        typeof(OSDC.Drilling.Field.WebPages.Field).Assembly,
        typeof(OSDC.Drilling.EarthCartographicProjection.WebPages.ProjectionDefinitions).Assembly,
        typeof(OSDC.Drilling.SurveyInstrument.WebPages.SurveyInstrumentMain).Assembly,
        typeof(OSDC.UnitConversion.WebPages.SingleUnitConversionMain).Assembly,
    ];
}
