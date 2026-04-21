using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Trajectory.WebPages;

public interface ITrajectoryWebPagesConfiguration :
    IFieldHostURL,
    IClusterHostURL,
    IRigHostURL,
    IWellHostURL,
    IWellBoreHostURL,
    IWellBoreArchitectureHostURL,
    ITrajectoryHostURL,
    IUnitConversionHostURL
{
}
