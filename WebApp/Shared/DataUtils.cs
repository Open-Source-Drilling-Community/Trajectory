public static class DataUtils
{
    // default values
    public const double DEFAULT_VALUE = 999.25;
    public static string DEFAULT_NAME_Trajectory = "Default Trajectory Name";
    public static string DEFAULT_DESCR_Trajectory = "Default Trajectory Description";
    public static string DEFAULT_NAME_SurveyStation = "Default SurveyStation Name";
    public static string DEFAULT_DESCR_SurveyStation = "Default SurveyStation Description";

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

    // units and labels
    public static readonly string TrajectorySurveyStationListLabel = "SurveyStationList";
    public static readonly string TrajectoryOutputParamLabel = "TrajectoryOutputParam";
    public static readonly string TrajectoryNameLabel = "Trajectory name";
    public static readonly string TrajectoryDescrLabel = "Trajectory description";
    public static readonly string TrajectoryOutputParamQty = "DepthDrilling";

    public static readonly string SurveyStationNameLabel = "SurveyStation name";
    public static readonly string SurveyStationParamLabel = "SurveyStationParam";
    public static readonly string SurveyStationParamQty = "DepthDrilling";

    public static readonly string SurveyStationTypeLabel = "SurveyStation type";
    public static readonly string DerivedData1Label = "DerivedData1 name";
    public static readonly string DerivedData1ParamLabel = "DerivedData1Param";
    public static readonly string DerivedData1ParamQty = "DepthDrilling";
    public static readonly string DerivedData2Label = "DerivedData2 name";
    public static readonly string DerivedData2ParamLabel = "DerivedData2Param";
    public static readonly string DerivedData2ParamQty = "DepthDrilling";

    public static readonly string InputXValuesTitle = "X value";
    public static readonly string InputXValuesQty = "DepthDrilling";
    public static readonly string InputYValuesTitle = "Y value";
    public static readonly string InputYValuesQty = "Length";
    public static readonly string OutputXValuesTitle = "X value";
    public static readonly string OutputXValuesQty = "DepthDrilling";
    public static readonly string OutputYValuesTitle = "Y value";
    public static readonly string OutputYValuesQty = "Length";
}