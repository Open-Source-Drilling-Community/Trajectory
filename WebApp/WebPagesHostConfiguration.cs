using NORCE.Drilling.Trajectory.WebPages;

namespace NORCE.Drilling.Trajectory.WebApp;

public class WebPagesHostConfiguration :
    ITrajectoryWebPagesConfiguration,
    NORCE.Drilling.WellBoreArchitecture.WebPages.IWellBoreArchitectureWebPagesConfiguration,
    NORCE.Drilling.Rig.WebPages.IRigWebPagesConfiguration,
    NORCE.Drilling.WellBore.WebPages.IWellBoreWebPagesConfiguration,
    NORCE.Drilling.Well.WebPages.IWellWebPagesConfiguration,
    NORCE.Drilling.Cluster.WebPages.IClusterWebPagesConfiguration,
    NORCE.Drilling.Field.WebPages.IFieldWebPagesConfiguration,
    NORCE.Drilling.CartographicProjection.WebPages.ICartographicProjectionWebPagesConfiguration,
    NORCE.Drilling.GeodeticDatum.WebPages.IGeodeticDatumWebPagesConfiguration,
    NORCE.Drilling.SurveyInstrument.WebPages.ISurveyInstrumentWebPagesConfiguration,
    NORCE.Drilling.EarthGeomagneticField.WebPages.IEarthGeomagneticFieldWebPagesConfiguration
{
    public string FieldHostURL { get; set; } = string.Empty;
    public string ClusterHostURL { get; set; } = string.Empty;
    public string RigHostURL { get; set; } = string.Empty;
    public string WellHostURL { get; set; } = string.Empty;
    public string WellBoreHostURL { get; set; } = string.Empty;
    public string WellBoreArchitectureHostURL { get; set; } = string.Empty;
    public string TrajectoryHostURL { get; set; } = string.Empty;
    public string CartographicProjectionHostURL { get; set; } = string.Empty;
    public string GeodeticDatumHostURL { get; set; } = string.Empty;
    public string UnitConversionHostURL { get; set; } = string.Empty;
    public string SurveyInstrumentHostURL { get; set; } = string.Empty;
    public string EarthMagneticFieldHostURL { get; set; } = string.Empty;
}
