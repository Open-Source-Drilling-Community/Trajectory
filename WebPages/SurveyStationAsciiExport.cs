using OSDC.UnitConversion.DrillingRazorMudComponents;
using System.Globalization;
using System.Text;

namespace OSDC.Drilling.Trajectory.WebPages;

internal static class SurveyStationAsciiExport
{
    public static string BuildTabSeparated(
        MudUnitAndReferenceChoiceTag units,
        IEnumerable<ModelShared.SurveyStation> stations)
    {
        string[] headers =
        [
            $"Measured Depth [{units.GetDepthUnitLabel("DepthDrilling")}]",
            $"Inclination [{units.GetUnitLabel("PlaneAngleDrilling")}]",
            $"Azimuth [{units.GetAzimuthUnitLabel("PlaneAngleDrilling")}]",
            $"Vertical Depth [{units.GetDepthUnitLabel("DepthDrilling")}]",
            $"{CoordinateLabel(units, PositionDirectionType.North)} [{units.GetPositionUnitLabel("PositionDrilling")}]",
            $"{CoordinateLabel(units, PositionDirectionType.East)} [{units.GetPositionUnitLabel("PositionDrilling")}]",
            $"Vertical Section [{units.GetUnitLabel("DepthDrilling")}]",
            $"DLS [{units.GetUnitLabel("CurvatureDrilling")}]",
            $"BUR [{units.GetUnitLabel("CurvatureDrilling")}]",
            $"TR [{units.GetUnitLabel("CurvatureDrilling")}]"
        ];

        StringBuilder builder = new();
        builder.AppendLine(string.Join('\t', headers.Select(SanitizeField)));
        foreach (ModelShared.SurveyStation station in stations)
        {
            string[] row =
            [
                ConvertDepth(units, station.MD ?? station.Abscissa),
                ConvertQuantity(units, station.Inclination, "PlaneAngleDrilling"),
                ConvertAzimuth(units, station.Azimuth),
                ConvertDepth(units, station.TVD),
                ConvertPosition(units, station.RiemannianNorth, PositionDirectionType.North),
                ConvertPosition(units, station.RiemannianEast, PositionDirectionType.East),
                ConvertQuantity(units, station.VerticalSection, "DepthDrilling"),
                ConvertQuantity(units, station.Curvature, "CurvatureDrilling"),
                ConvertQuantity(units, station.BUR, "CurvatureDrilling"),
                ConvertQuantity(units, station.TUR, "CurvatureDrilling")
            ];
            builder.AppendLine(string.Join('\t', row.Select(SanitizeField)));
        }
        return builder.ToString();
    }

    public static string EnsureTsvFileName(string value, string fallback)
    {
        string sanitized = SanitizeFileName(value, fallback);
        return sanitized.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase)
            ? sanitized
            : sanitized + ".tsv";
    }

    private static string ConvertDepth(MudUnitAndReferenceChoiceTag units, double? value) =>
        value.HasValue ? NormalizeNumber(units.FromWGS84DepthSI(value.Value, "DepthDrilling", false)) : string.Empty;

    private static string ConvertAzimuth(MudUnitAndReferenceChoiceTag units, double? value) =>
        value.HasValue ? NormalizeNumber(units.FromTrueNorthAzimuthSI(value.Value, "PlaneAngleDrilling", false)) : string.Empty;

    private static string ConvertPosition(
        MudUnitAndReferenceChoiceTag units, double? value, PositionDirectionType direction) =>
        value.HasValue
            ? NormalizeNumber(units.FromWGS84PositionSI(value.Value, "PositionDrilling", direction).ToString(CultureInfo.CurrentCulture))
            : string.Empty;

    private static string ConvertQuantity(MudUnitAndReferenceChoiceTag units, double? value, string quantityName) =>
        value.HasValue ? NormalizeNumber(units.FromSI(value.Value, quantityName, false)) : string.Empty;

    private static string CoordinateLabel(MudUnitAndReferenceChoiceTag units, PositionDirectionType direction)
    {
        if (string.Equals(units.PositionReferenceName, "WGS84", StringComparison.OrdinalIgnoreCase))
            return direction == PositionDirectionType.North ? "Riemann North" : "Riemann East";
        if (string.Equals(units.PositionReferenceName, "Cartographic", StringComparison.OrdinalIgnoreCase))
            return direction == PositionDirectionType.North ? "Northing" : "Easting";
        return direction == PositionDirectionType.North ? "North" : "East";
    }

    private static string NormalizeNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        string trimmed = value.Trim().Replace("\u00A0", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);
        if (double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out double parsed) ||
            double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed) ||
            double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands, new CultureInfo("nb-NO"), out parsed))
        {
            return parsed.ToString("G17", CultureInfo.InvariantCulture);
        }
        return trimmed.Replace(',', '.');
    }

    private static string SanitizeField(string? value)
    {
        string sanitized = (value ?? string.Empty)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Replace("°", "deg", StringComparison.Ordinal)
            .Replace("µ", "u", StringComparison.Ordinal)
            .Trim();
        StringBuilder ascii = new(sanitized.Length);
        foreach (char character in sanitized) ascii.Append(character <= sbyte.MaxValue ? character : '?');
        return ascii.ToString();
    }

    private static string SanitizeFileName(string value, string fallback)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = new((value ?? string.Empty).Trim().Select(character => invalid.Contains(character) ? '-' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }
}
