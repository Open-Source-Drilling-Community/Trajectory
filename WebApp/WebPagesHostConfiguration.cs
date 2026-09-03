using OSDC.Drilling.Trajectory.WebPages;

namespace OSDC.Drilling.Trajectory.WebApp;

public class WebPagesHostConfiguration :
    ITrajectoryWebPagesConfiguration,
    OSDC.Drilling.WellBoreArchitecture.WebPages.IWellBoreArchitectureWebPagesConfiguration,
    OSDC.Drilling.Rig.WebPages.IRigWebPagesConfiguration,
    OSDC.Drilling.WellBore.WebPages.IWellBoreWebPagesConfiguration,
    OSDC.Drilling.Well.WebPages.IWellWebPagesConfiguration,
    OSDC.Drilling.Cluster.WebPages.IClusterWebPagesConfiguration,
    OSDC.Drilling.Field.WebPages.IFieldWebPagesConfiguration,
    OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration,
    OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyWebPagesConfiguration,
    OSDC.Drilling.SurveyInstrument.WebPages.ISurveyInstrumentWebPagesConfiguration,
    OSDC.Drilling.EarthMagneticField.WebPages.IEarthMagneticFieldWebPagesConfiguration,
    OSDC.Drilling.EarthGravity.WebPages.IEarthGravityWebPagesConfiguration,
    OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumWebPagesConfiguration
{
    public string? FieldHostURL { get; set; } = string.Empty;
    public string? ClusterHostURL { get; set; } = string.Empty;
    public string? RigHostURL { get; set; } = string.Empty;
    public string? WellHostURL { get; set; } = string.Empty;
    public string? WellBoreHostURL { get; set; } = string.Empty;
    public string? WellBoreArchitectureHostURL { get; set; } = string.Empty;
    public string? TrajectoryHostURL { get; set; } = string.Empty;
    public string CartographicProjectionHostURL { get; set; } = string.Empty;
    public string GeodeticDatumHostURL { get; set; } = string.Empty;
    public string? UnitConversionHostURL { get; set; } = string.Empty;
    public string? SurveyInstrumentHostURL { get; set; } = string.Empty;
    public string EarthMagneticFieldHostURL { get; set; } = string.Empty;
    public string GravitationalFieldHostURL { get; set; } = string.Empty;
    public string VerticalDatumHostURL { get; set; } = string.Empty;
    public string? EarthCartographicProjectionHostURL
    {
        get => CartographicProjectionHostURL;
        set => CartographicProjectionHostURL = value ?? string.Empty;
    }
    public string? EarthVerticalDatumHostURL
    {
        get => VerticalDatumHostURL;
        set => VerticalDatumHostURL = value ?? string.Empty;
    }

    string OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration.ServiceUrl => CartographicProjectionHostURL;
    string OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration.EarthGeodesyUrl => GeodeticDatumHostURL;
    string OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration.UnitConversionUrl => UnitConversionHostURL ?? string.Empty;
    string OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyWebPagesConfiguration.EarthGeodesyHostURL => GeodeticDatumHostURL;
    string OSDC.Drilling.EarthGravity.WebPages.IEarthGravityWebPagesConfiguration.EarthGravityHostURL => GravitationalFieldHostURL;
    string OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumWebPagesConfiguration.EarthVerticalDatumHostURL => VerticalDatumHostURL;
    string? OSDC.Drilling.WellBore.WebPages.IWellBoreWebPagesConfiguration.VerticalDatumHostURL
    {
        get => VerticalDatumHostURL;
        set => VerticalDatumHostURL = value ?? string.Empty;
    }
}
