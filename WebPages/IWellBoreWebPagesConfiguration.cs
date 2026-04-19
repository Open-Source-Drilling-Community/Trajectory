using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Trajectory.WebPages;

public interface IWellBoreWebPagesConfiguration :
    IWellBoreHostURL,
    IWellHostURL,
    IClusterHostURL,
    IFieldHostURL,
    IRigHostURL,
    IUnitConversionHostURL
{
}
