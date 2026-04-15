using Microsoft.AspNetCore.Identity.Data;
using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.UnitConversion.DrillingRazorMudComponents;
using System.Runtime.InteropServices;

public static class DataUtils
{
    // default values
    public static string FLOATING_COLOUR = "rgba(70, 50, 240, 0.86)";
    public static string FLOATING_COLOUR_DEEP = "rgba(232, 230, 241, 0.86)";
    // unit management
    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
        public static string? DepthReferenceName { get; set; } = "Rotary table";
        public static string? PositionReferenceName { get; set; } = "Well-head";
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
    }

    public static void ApplyTrajectoryReferenceValues(Guid? trajectoryID, List<TrajectoryLight>? trajectoryList, List<WellBore>? wellBores, List<Well>? wells, List<Cluster>? clusters, List<Rig>? rigs)
    {
        DataUtils.GroundMudLineDepthReferenceSource.GroundMudLineDepthReference = 0;
        DataUtils.SeaWaterLevelDepthReferenceSource.SeaWaterLevelDepthReference = 0;
        DataUtils.RotaryTableDepthReferenceSource.RotaryTableDepthReference = 0;
        DataUtils.WellHeadPositionReferenceSource.WellHeadNorthPositionReference = 0;
        DataUtils.WellHeadPositionReferenceSource.WellHeadEastPositionReference = 0;
        DataUtils.CartographicGridPositionReferenceSource.CartographicGridNorthPositionReference = 0;
        DataUtils.CartographicGridPositionReferenceSource.CartographicGridEastPositionReference = 0;
        DataUtils.LeaseLinePositionReferenceSource.LeaseLineNorthPositionReference = 0;
        DataUtils.LeaseLinePositionReferenceSource.LeaseLineEastPositionReference = 0;
        DataUtils.ClusterPositionReferenceSource.ClusterNorthPositionReference = 0;
        DataUtils.ClusterPositionReferenceSource.ClusterEastPositionReference = 0;
        TrajectoryLight? trajectory = null;
        if (trajectoryList != null && trajectoryID != null)
        {
            foreach (var t in trajectoryList)
            {
                if (t != null && t.MetaInfo != null && t.MetaInfo.ID == trajectoryID)
                {
                    trajectory = t;
                    break;
                }
            }
        }
        if (trajectory != null)
        {
            WellBore? wellBore = null;
            if (wellBores != null)
            {
                foreach (var wb in wellBores)
                {
                    if ( wb != null && wb.MetaInfo != null && wb.MetaInfo.ID == trajectory.WellBoreID) 
                    {
                        wellBore = wb;
                        break;
                    }
                }
            }
            Rig? rig = null;
            if (rigs != null && wellBore != null && wellBore.RigID != null)
            {
                foreach (var r in rigs)
                {
                    if (r != null && r.MetaInfo != null && r.MetaInfo.ID == wellBore.RigID)
                    {
                        rig = r;
                        break;
                    }
                }
            }
            Well? well = null;
            if (wells != null && wellBore != null && wellBore.WellID != null)
            {
                foreach (var w in wells)
                {
                    if (w != null && w.MetaInfo != null && w.MetaInfo.ID == wellBore.WellID)
                    {
                        well = w;
                        break;
                    }
                }
            }
            Guid? slotID = FindSlotIdFromWellBoreHierarchy(wellBore, wellBores, wells);
            if (well == null && wells != null && slotID != null)
            {
                foreach (var w in wells)
                {
                    if (w != null && w.SlotID == slotID)
                    {
                        well = w;
                        break;
                    }
                }
            }
            Cluster? cluster = null;
            if (clusters != null && well != null && well.ClusterID != null) 
            {
                foreach (var c in clusters)
                {
                    if (c != null && c.MetaInfo != null && c.MetaInfo.ID == well.ClusterID)
                    {
                        cluster = c; 
                        break;
                    }
                }
            }
            Slot? slot = FindSlot(cluster, slotID);
            if (rig == null && rigs != null && cluster != null && cluster.IsFixedPlatform && cluster.RigID != null)
            {
                foreach (var r in rigs)
                {
                    if (r != null && r.MetaInfo != null && r.MetaInfo.ID == cluster.RigID)
                    {
                        rig = r;
                        break;
                    }
                }
            }
            if (cluster != null && cluster.GroundMudLineDepth != null && cluster.GroundMudLineDepth.GaussianValue != null && cluster.GroundMudLineDepth.GaussianValue.Mean != null)
            {
                ApplyGroundMudLineDepthWGS84(cluster.GroundMudLineDepth.GaussianValue.Mean);
            }
            if (cluster != null && cluster.TopWaterDepth != null && cluster.TopWaterDepth.GaussianValue != null && cluster.TopWaterDepth.GaussianValue.Mean != null)
            {
                ApplyTopWaterDepthWGS84(cluster.TopWaterDepth.GaussianValue.Mean);
            }
            if (rig != null && rig.DrillFloorElevation != null)
            {
                ApplyRotaryTableDepthnWGS84(rig.DrillFloorElevation);
            }
            if (slot != null && 
                slot.Latitude != null && slot.Latitude.GaussianValue != null && slot.Latitude.GaussianValue.Mean != null &&
                slot.Longitude != null && slot.Longitude.GaussianValue != null && slot.Longitude.GaussianValue.Mean != null)
            {
                OSDC.DotnetLibraries.Drilling.Surveying.SurveyPoint surveyPoint = new ();
                surveyPoint.Latitude = slot.Latitude.GaussianValue.Mean;
                surveyPoint.Longitude = slot.Longitude.GaussianValue.Mean;
                if (surveyPoint.RiemannianNorth != null && surveyPoint.RiemannianEast != null)
                {
                    DataUtils.WellHeadPositionReferenceSource.WellHeadNorthPositionReference = -surveyPoint.RiemannianNorth;
                    DataUtils.WellHeadPositionReferenceSource.WellHeadEastPositionReference = -surveyPoint.RiemannianEast;
                }
            }
            if (cluster != null && 
                cluster.ReferenceLatitude != null && cluster.ReferenceLatitude.GaussianValue != null && cluster.ReferenceLatitude.GaussianValue.Mean != null &&
                cluster.ReferenceLongitude != null && cluster.ReferenceLongitude.GaussianValue != null && cluster.ReferenceLongitude.GaussianValue.Mean != null)
            {
                OSDC.DotnetLibraries.Drilling.Surveying.SurveyPoint surveyPoint = new ();
                surveyPoint.Latitude = cluster.ReferenceLatitude.GaussianValue.Mean;
                surveyPoint.Longitude = cluster.ReferenceLongitude.GaussianValue.Mean;
                if (surveyPoint.RiemannianNorth != null && surveyPoint.RiemannianEast != null)
                {
                    DataUtils.ClusterPositionReferenceSource.ClusterNorthPositionReference = -surveyPoint.RiemannianNorth;
                    DataUtils.ClusterPositionReferenceSource.ClusterEastPositionReference = -surveyPoint.RiemannianEast;
                }
            }
        }
    }

    private static Guid? FindSlotIdFromWellBoreHierarchy(WellBore? wellBore, List<WellBore>? wellBores, List<Well>? wells)
    {
        if (wellBore == null || wellBores == null || wells == null)
        {
            return null;
        }

        HashSet<Guid> visitedWellBoreIds = [];
        WellBore? currentWellBore = wellBore;
        while (currentWellBore?.MetaInfo?.ID != null && visitedWellBoreIds.Add(currentWellBore.MetaInfo.ID))
        {
            Well? currentWell = FindWellById(wells, currentWellBore.WellID);
            if (currentWell?.SlotID != null)
            {
                return currentWell.SlotID;
            }

            currentWellBore = FindWellBoreById(wellBores, currentWellBore.ParentWellBoreID);
        }

        return null;
    }

    private static Well? FindWellById(List<Well>? wells, Guid? wellID)
    {
        if (wells == null || wellID == null)
        {
            return null;
        }

        foreach (Well? well in wells)
        {
            if (well?.MetaInfo?.ID == wellID)
            {
                return well;
            }
        }

        return null;
    }

    private static WellBore? FindWellBoreById(List<WellBore>? wellBores, Guid? wellBoreID)
    {
        if (wellBores == null || wellBoreID == null)
        {
            return null;
        }

        foreach (WellBore? wellBore in wellBores)
        {
            if (wellBore?.MetaInfo?.ID == wellBoreID)
            {
                return wellBore;
            }
        }

        return null;
    }

    private static Slot? FindSlot(Cluster? cluster, Guid? slotID)
    {
        if (cluster?.Slots == null || slotID == null)
        {
            return null;
        }

        foreach (KeyValuePair<string, Slot> entry in cluster.Slots)
        {
            if (entry.Value?.ID == slotID)
            {
                return entry.Value;
            }
        }

        return null;
    }

    public static void ApplyGroundMudLineDepthWGS84(double? val)
    {
        if (val != null)
        {
            DataUtils.GroundMudLineDepthReferenceSource.GroundMudLineDepthReference = -val;
        }
    }

    public static void ApplyTopWaterDepthWGS84(double? val)
    {
        if (val != null)
        {
            DataUtils.SeaWaterLevelDepthReferenceSource.SeaWaterLevelDepthReference = -val;
        }
    }
    public static void ApplyRotaryTableDepthnWGS84(double? val)
    {
        if (val != null)
        {
            DataUtils.RotaryTableDepthReferenceSource.RotaryTableDepthReference = -val;
        }
    }
    public static GroundMudLineDepthReferenceSource GroundMudLineDepthReferenceSource { get; set; } = new GroundMudLineDepthReferenceSource();
    public static SeaWaterLevelDepthReferenceSource SeaWaterLevelDepthReferenceSource { get; set; } = new SeaWaterLevelDepthReferenceSource();
    public static RotaryTableDepthReferenceSource RotaryTableDepthReferenceSource { get; set; } = new RotaryTableDepthReferenceSource();
    public static WellHeadPositionReferenceSource WellHeadPositionReferenceSource { get; set; } = new WellHeadPositionReferenceSource();
    public static CartographicGridPositionReferenceSource CartographicGridPositionReferenceSource { get; set; } = new CartographicGridPositionReferenceSource();
    public static LeaseLinePositionReferenceSource LeaseLinePositionReferenceSource { get; set; } = new LeaseLinePositionReferenceSource();
    public static ClusterPositionReferenceSource ClusterPositionReferenceSource { get; set; } = new ClusterPositionReferenceSource();

    public static void UpdateUnitSystemName(string value) => UnitAndReferenceParameters.UnitSystemName = value;
    public static void UpdateDepthReferenceName(string value) => UnitAndReferenceParameters.DepthReferenceName = value;
    public static void UpdatePositionReferenceName(string value) => UnitAndReferenceParameters.PositionReferenceName = value;

    public static string[] COLORSCALE = ["black", "blue", "grey", "red", "orange", "green", "yellow", "pink", "brown", "purple"];

    /// <summary>
    /// 
    /// </summary>
    /// <param nameList="">name of each curve in the list to plot</param>
    /// <param modeFlagList="">modeFlag of each curve in the list to plot (1 = lines; 2 = markers)</param>
    /// <param colorList="">color of each curve in the list of curves to plot</param>
    /// <param northValuesList="">North values for the list of curves to plot</param>
    /// <param eastValuesList="">East values for the list of curves to plot</param>
    /// <param TVDValuesList="">TVD values for the list of curves to plot</param>
    /// <param verticalSectionValuesList="">Vertical section values for the list of curves to plot</param>
    /// <param trajectoryList=""></param>
    public static void UpdatePlots(
        List<string> nameList,
        List<int> modeFlagList,
        List<string> colorList,
        List<List<object>> northValuesList,
        List<List<object>> eastValuesList,
        List<List<object>> TVDValuesList,
        List<List<object>> verticalSectionValuesList,
        List<Trajectory> trajectoryList
        )
    {
        if (trajectoryList is { })
        {
            //clear data from scatter plot
            nameList.Clear();
            modeFlagList.Clear();
            colorList.Clear();
            northValuesList.Clear();
            eastValuesList.Clear();
            TVDValuesList.Clear();
            verticalSectionValuesList.Clear();

            //generate only one curve for the current trajectory
            for (int k = 0; k < trajectoryList.Count; ++k)
            {
                if (trajectoryList[k].SurveyStationList is { Count: > 2 } traj)
                {
                    //////////////////////////////////////
                    /// Interpolated trajectory (lines) //
                    //////////////////////////////////////
                    //Retrieve and compute data points for interpolated trajectory (plotted as lines)
                    List<object> northValues = [];
                    List<object> eastValues = [];
                    List<object> tvdValues = [];
                    List<object> vSectValues = [];

                    SurveyStation? prevPoint = traj.First();
                    //double vSect = 0.0;
                    foreach (var point in traj)
                    {
                        if (point is { } &&
                            point.X is { } x &&
                            point.Y is { } y &&
                            point.Z is { } z)
                        {
                            northValues.Add(x);
                            eastValues.Add(y);
                            tvdValues.Add(z);
                            if (point.VerticalSection is { } verticalSection)
                            {
                                vSectValues.Add(verticalSection);
                            }
                            else if (point.Abscissa is { } abscissa)
                            {
                                vSectValues.Add(abscissa);
                            }
                            else
                            {
                                vSectValues.Add(null!);
                            }
                            prevPoint = point;
                        }
                    }
                    northValuesList.Add(northValues);
                    eastValuesList.Add(eastValues);
                    TVDValuesList.Add(tvdValues);
                    verticalSectionValuesList.Add(vSectValues);
                    nameList.Add(string.IsNullOrEmpty(trajectoryList[k].Name) ? "trajectory" : trajectoryList[k].Name);
                    modeFlagList.Add(1); // 1=lines, 2=markers, 3=lines+markers
                    colorList.Add(k < COLORSCALE.Length ? COLORSCALE[k] : "black");
                }
            }
        }
    }
}
public class GroundMudLineDepthReferenceSource : IGroundMudLineDepthReferenceSource
{
    public double? GroundMudLineDepthReference { get; set; }
}

public class RotaryTableDepthReferenceSource : IRotaryTableDepthReferenceSource
{
    public double? RotaryTableDepthReference { get; set; }
}

public class SeaWaterLevelDepthReferenceSource : ISeaWaterLevelDepthReferenceSource
{
    public double? SeaWaterLevelDepthReference { get; set; }
}

public class WellHeadPositionReferenceSource : IWellHeadPositionReferenceSource
{
    public double? WellHeadNorthPositionReference { get; set; }
    public double? WellHeadEastPositionReference { get; set; }
}

public class CartographicGridPositionReferenceSource : ICartographicGridPositionReferenceSource
{
    public double? CartographicGridNorthPositionReference { get; set; }
    public double? CartographicGridEastPositionReference { get; set; }
}

public class LeaseLinePositionReferenceSource : ILeaseLinePositionReferenceSource
{
    public double? LeaseLineNorthPositionReference { get; set; }
    public double? LeaseLineEastPositionReference { get; set; }
}

public class ClusterPositionReferenceSource : IClusterPositionReferenceSource
{
    public double? ClusterNorthPositionReference { get; set; }
    public double? ClusterEastPositionReference { get; set; }
}
