using System.Globalization;

namespace OSDC.Drilling.Trajectory.WebPages;

/// <summary>
/// Converts between the canonical UTC timestamps used by the service contract and the
/// wall-clock reference selected in <see cref="DataUtils.UnitAndReferenceParameters"/>.
/// </summary>
public static class DateTimeReference
{
    public static bool UsesLocalTime => string.Equals(
        DataUtils.UnitAndReferenceParameters.DateReferenceName,
        "Local Time",
        StringComparison.Ordinal);

    public static DateTime ToDisplayDateTime(DateTimeOffset value) =>
        UsesLocalTime ? value.ToLocalTime().DateTime : value.ToUniversalTime().UtcDateTime;

    public static DateTimeOffset ToCanonicalUtc(DateTime value)
    {
        DateTime unspecified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        return UsesLocalTime
            ? new DateTimeOffset(unspecified, TimeZoneInfo.Local.GetUtcOffset(unspecified)).ToUniversalTime()
            : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    public static string Format(DateTimeOffset? value, string format = "yyyy-MM-dd HH:mm:ss") =>
        value is { } defined
            ? ToDisplayDateTime(defined).ToString(format, CultureInfo.CurrentCulture)
            : string.Empty;

    public static string FormatDate(DateTimeOffset? value, string format = "yyyy-MM-dd") =>
        value is { } defined
            ? ToDisplayDateTime(defined).ToString(format, CultureInfo.CurrentCulture)
            : string.Empty;

    public static bool TryParse(string? value, out DateTimeOffset? canonicalUtc)
    {
        canonicalUtc = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out DateTime parsed) &&
            !DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out parsed))
        {
            return false;
        }

        canonicalUtc = parsed.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(parsed),
            DateTimeKind.Local => new DateTimeOffset(parsed).ToUniversalTime(),
            _ => ToCanonicalUtc(parsed)
        };
        return true;
    }
}
