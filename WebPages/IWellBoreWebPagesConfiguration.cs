using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace OSDC.Drilling.Trajectory.WebPages;

public interface IWellBoreWebPagesConfiguration :
    IWellBoreHostURL,
    IWellHostURL,
    IClusterHostURL,
    IFieldHostURL,
    IRigHostURL,
    IUnitConversionHostURL
{
}
