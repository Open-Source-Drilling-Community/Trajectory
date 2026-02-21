using NORCE.Drilling.Trajectory.ModelShared;

public static class DataUtils
{
    // default values
    public static string FLOATING_COLOUR = "rgba(70, 50, 240, 0.86)";
    public static string FLOATING_COLOUR_DEEP = "rgba(232, 230, 241, 0.86)";
    // unit management
    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
        public static string? DepthReferenceName { get; set; }
        public static string? PositionReferenceName { get; set; }
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
    }

    public static void UpdateUnitSystemName(string val)
    {
        UnitAndReferenceParameters.UnitSystemName = (string)val;
    }

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
    /// <param trajectoryList=""></param>
    public static void UpdatePlots(
        List<string> nameList,
        List<int> modeFlagList,
        List<string> colorList,
        List<List<object>> northValuesList,
        List<List<object>> eastValuesList,
        List<List<object>> TVDValuesList,
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
            // vSectValuesList.Clear();

            //generate only one curve for the current trajectory
            for (int k = 0; k < trajectoryList.Count; ++k)
            {
                if (trajectoryList[k].SurveyStationList is { Count: > 2 } ssList &&
                    trajectoryList[k].InterpolatedTrajectory is { Count: > 2 } traj)
                {
                    //////////////////////////////////////
                    /// Interpolated trajectory (lines) //
                    //////////////////////////////////////
                    //Retrieve and compute data points for interpolated trajectory (plotted as lines)
                    List<object> northValues = [];
                    List<object> eastValues = [];
                    List<object> tvdValues = [];
                    List<object> vSectValues = [];

                    SurveyPoint? prevPoint = traj[0];
                    //double vSect = 0.0;
                    for (int i = 0; i < traj.Count; ++i)
                    {
                        SurveyPoint? point = traj[i];
                        if (point is { } &&
                            point.X is { } x &&
                            point.Y is { } y &&
                            point.Z is { } z)
                        {
                            northValues.Add(x);
                            eastValues.Add(y);
                            tvdValues.Add(z);
                            //vSect += System.Math.Sqrt(System.Math.Pow((double)(point.X - prevPoint.X), 2.0) + System.Math.Pow((double)(point.Y - prevPoint.Y), 2.0));
                            //vSectValues.Add(vSect);
                            prevPoint = point;
                        }
                    }
                    northValuesList.Add(northValues);
                    eastValuesList.Add(eastValues);
                    TVDValuesList.Add(tvdValues);
                    // vSectValuesList.Add(vSectValues);
                    nameList.Add(string.IsNullOrEmpty(trajectoryList[k].Name) ? "traj (interp)" : trajectoryList[k].Name + "(interp)");
                    modeFlagList.Add(1); // 1=lines, 2=markers, 3=lines+markers
                    colorList.Add(k < COLORSCALE.Length ? COLORSCALE[k] : "black");

                    /////////////////////////////////////
                    /// SurveyStation points (markers) //
                    /////////////////////////////////////
                    //Retrieve and compute data points for SurveyStation (plotted as markers)
                    List<object> northValues2 = [];
                    List<object> eastValues2 = [];
                    List<object> tvdValues2 = [];
                    //List<object> vSectValues2 = [];

                    SurveyStation prevStation = ssList[0];
                    // vSect = 0.0;
                    for (int i = 0; i < ssList.Count; ++i)
                    {
                        SurveyStation? station = ssList[i];
                        if (station is { } &&
                            station.X is { } x &&
                            station.Y is { } y &&
                            station.Z is { } z)
                        {
                            northValues2.Add(x);
                            eastValues2.Add(y);
                            tvdValues2.Add(z);
                            // vSect += System.Math.Sqrt(System.Math.Pow((double)(point.X - prevPoint.X), 2.0) + System.Math.Pow((double)(point.Y - prevPoint.Y), 2.0));
                            // vSectValues2.Add(vSect);
                            prevStation = station;
                        }
                    }
                    northValuesList.Add(northValues2);
                    eastValuesList.Add(eastValues2);
                    TVDValuesList.Add(tvdValues2);
                    // vSectValuesList.Add(vSectValues2);
                    nameList.Add(string.IsNullOrEmpty(trajectoryList[k].Name) ? "traj (survey stations)" : trajectoryList[k].Name + "(survey stations)");
                    modeFlagList.Add(2); // 1=lines, 2=markers, 3=lines+markers
                    colorList.Add(k < COLORSCALE.Length ? COLORSCALE[k] : "black");
                }
            }
        }
    }
}