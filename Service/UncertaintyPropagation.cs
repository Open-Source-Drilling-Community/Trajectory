using OSDC.DotnetLibraries.General.Math;
using OSDC.DotnetLibraries.Drilling.Surveying;
namespace OSDC.Drilling.Trajectory.Service;

/// <summary>Combines instrument error with an independent common wellhead translation in N/E/depth metres.</summary>
public static class UncertaintyPropagation
{
    public static bool IsUsable(SymmetricMatrix3x3? covariance)
    {
        if (covariance is null) return false;
        for (int r = 0; r < 3; r++) for (int c = 0; c < 3; c++) if (covariance[c, r] is not double v || !double.IsFinite(v) || covariance[c, r] != covariance[r, c]) return false;
        // Principal minors of a real symmetric 3x3 matrix must be non-negative (roundoff tolerance).
        double a = covariance[0, 0]!.Value, b = covariance[1, 1]!.Value, c3 = covariance[2, 2]!.Value, d = covariance[0, 1]!.Value, e = covariance[0, 2]!.Value, f = covariance[1, 2]!.Value;
        double scale = System.Math.Max(1e-300, System.Math.Max(a, System.Math.Max(b, c3))), eps = 1e-10;
        return a >= 0 && b >= 0 && c3 >= 0 && a * b - d * d >= -eps * scale * scale && a * c3 - e * e >= -eps * scale * scale && b * c3 - f * f >= -eps * scale * scale && a * b * c3 + 2 * d * e * f - a * f * f - b * e * e - c3 * d * d >= -eps * scale * scale * scale;
    }
    public static bool AddIndependentReference(IReadOnlyList<SurveyStation> stations, SymmetricMatrix3x3 reference)
    {
        if (!IsUsable(reference) || stations.Any(s => !IsUsable(s.Covariance))) return false;
        foreach (var station in stations)
        {
            var total = new SymmetricMatrix3x3(); for (int r = 0; r < 3; r++) for (int c = 0; c < 3; c++) total[c, r] = station.Covariance![c, r] + reference[c, r];
            if (!IsUsable(total)) return false; station.Covariance = total; station.CalculateEigenProperties();
        }
        return true;
    }
    public static bool CopyToRetainedGeometry(IReadOnlyList<SurveyStation> retained, IReadOnlyList<SurveyStation> replay)
    {
        if (retained.Count == 0 || retained.Count > replay.Count) return false;
        for (int i = 0; i < retained.Count; i++) if (retained[i].MD != replay[i].MD || retained[i].Inclination != replay[i].Inclination || retained[i].Azimuth != replay[i].Azimuth || !IsUsable(replay[i].Covariance)) return false;
        for (int i = 0; i < retained.Count; i++) { retained[i].Covariance = replay[i].Covariance; retained[i].Bias = replay[i].Bias; retained[i].EigenValues = replay[i].EigenValues; retained[i].EigenVectors = replay[i].EigenVectors; retained[i].SurveyTool = replay[i].SurveyTool; }
        return true;
    }
}
