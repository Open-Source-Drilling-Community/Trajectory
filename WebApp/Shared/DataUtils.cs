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

   
}