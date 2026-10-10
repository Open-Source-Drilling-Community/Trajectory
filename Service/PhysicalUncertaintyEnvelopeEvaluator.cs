using System;
using System.Linq;
using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.Service;

internal static class PhysicalUncertaintyEnvelopeEvaluator
{
    internal static bool TryEvaluate(PhysicalUncertaintyEnvelopeRequest? request,
        out PhysicalUncertaintyEnvelopeEvaluation? result, out string? error)
    {
        result = null;
        error = null;
        if (request is null || !Finite(request.UncertaintySemiMajorAxis, request.UncertaintySemiMinorAxis,
                request.UncertaintyOrientationAngle, request.Inclination, request.Azimuth, request.BoreholeDiameter) ||
            request.UncertaintySemiMajorAxis <= 0 || request.UncertaintySemiMinorAxis <= 0 ||
            request.UncertaintySemiMajorAxis < request.UncertaintySemiMinorAxis || request.BoreholeDiameter < 0)
        {
            error = "Ellipse axes, station attitude and borehole diameter must be finite; axes must be positive and ordered and diameter nonnegative.";
            return false;
        }
        if (request.Projection == UncertaintyProjectionPlane.Vertical &&
            (request.VerticalSectionAzimuth is not double verticalSectionAzimuth || !double.IsFinite(verticalSectionAzimuth)))
        {
            error = "Vertical-section azimuth is required for a vertical projection.";
            return false;
        }

        double sourceTheta = ToCartesianAngle(request.Projection, request.UncertaintyOrientationAngle);
        Matrix2 source = EllipseMatrix(request.UncertaintySemiMajorAxis, request.UncertaintySemiMinorAxis, sourceTheta);
        var projected = ProjectCrossSection(request);
        Matrix2 physical = EllipseMatrix(projected.Major, projected.Minor, ToCartesianAngle(request.Projection, projected.Angle));
        Matrix2 combined = MinimumDeterminantOuterBound(source, physical);
        var eig = Eigen(combined);
        result = new PhysicalUncertaintyEnvelopeEvaluation
        {
            Projection = request.Projection,
            AppliedBoreholeDiameter = request.BoreholeDiameter,
            ProjectedBoreholeCrossSection = new PlanarEllipse
            {
                SemiMajorAxis = projected.Major, SemiMinorAxis = projected.Minor,
                OrientationAngle = NormalizeAxis(projected.Angle)
            },
            CombinedEnvelope = new PlanarEllipse
            {
                SemiMajorAxis = Math.Sqrt(Math.Max(0, eig.MajorValue)),
                SemiMinorAxis = Math.Sqrt(Math.Max(0, eig.MinorValue)),
                OrientationAngle = NormalizeAxis(FromCartesianAngle(request.Projection, eig.MajorAngle))
            }
        };
        return true;
    }

    private static (double Major, double Minor, double Angle) ProjectCrossSection(PhysicalUncertaintyEnvelopeRequest request)
    {
        double radius = request.BoreholeDiameter / 2;
        return request.Projection switch
        {
            UncertaintyProjectionPlane.Horizontal =>
                (radius, radius * Math.Abs(Math.Cos(request.Inclination)), request.Azimuth - Math.PI / 2),
            UncertaintyProjectionPlane.Vertical => ProjectVertical(radius, request.Inclination, request.Azimuth,
                request.VerticalSectionAzimuth!.Value),
            _ => (radius, radius, 0)
        };
    }

    private static (double Major, double Minor, double Angle) ProjectVertical(double radius, double inclination,
        double azimuth, double verticalSectionAzimuth)
    {
        double delta = azimuth - verticalSectionAzimuth;
        double minor = radius * Math.Abs(Math.Sin(inclination) * Math.Sin(delta));
        double angle = Math.Atan2(Math.Cos(inclination), Math.Sin(inclination) * Math.Cos(delta));
        return (radius, minor, angle);
    }

    private static Matrix2 MinimumDeterminantOuterBound(Matrix2 first, Matrix2 second)
    {
        if (second.A == 0 && second.B == 0 && second.C == 0) return first;
        const double golden = 0.6180339887498948482;
        double left = -20, right = 20;
        double x1 = right - golden * (right - left), x2 = left + golden * (right - left);
        double f1 = Determinant(Bound(first, second, Math.Exp(x1)));
        double f2 = Determinant(Bound(first, second, Math.Exp(x2)));
        for (int i = 0; i < 120; i++)
        {
            if (f1 <= f2) { right = x2; x2 = x1; f2 = f1; x1 = right - golden * (right - left); f1 = Determinant(Bound(first, second, Math.Exp(x1))); }
            else { left = x1; x1 = x2; f1 = f2; x2 = left + golden * (right - left); f2 = Determinant(Bound(first, second, Math.Exp(x2))); }
        }
        return Bound(first, second, Math.Exp((left + right) / 2));
    }

    private static Matrix2 Bound(Matrix2 first, Matrix2 second, double beta) =>
        first * (1 + 1 / beta) + second * (1 + beta);

    private static Matrix2 EllipseMatrix(double major, double minor, double theta)
    {
        double c = Math.Cos(theta), s = Math.Sin(theta), a2 = major * major, b2 = minor * minor;
        return new Matrix2(a2 * c * c + b2 * s * s, (a2 - b2) * c * s, a2 * s * s + b2 * c * c);
    }

    private static (double MajorValue, double MinorValue, double MajorAngle) Eigen(Matrix2 value)
    {
        double trace = value.A + value.C;
        double root = Math.Sqrt(Math.Max(0, (value.A - value.C) * (value.A - value.C) + 4 * value.B * value.B));
        double major = (trace + root) / 2, minor = (trace - root) / 2;
        double angle = 0.5 * Math.Atan2(2 * value.B, value.A - value.C);
        return (major, minor, angle);
    }

    private static double ToCartesianAngle(UncertaintyProjectionPlane projection, double angle) =>
        projection == UncertaintyProjectionPlane.Vertical ? angle + Math.PI / 2 : angle;
    private static double FromCartesianAngle(UncertaintyProjectionPlane projection, double angle) =>
        projection == UncertaintyProjectionPlane.Vertical ? angle - Math.PI / 2 : angle;
    private static double NormalizeAxis(double angle)
    {
        angle %= Math.PI;
        return angle < 0 ? angle + Math.PI : angle;
    }
    private static bool Finite(params double[] values) => values.All(double.IsFinite);
    private static double Determinant(Matrix2 value) => value.A * value.C - value.B * value.B;
    private readonly record struct Matrix2(double A, double B, double C)
    {
        public static Matrix2 operator +(Matrix2 x, Matrix2 y) => new(x.A + y.A, x.B + y.B, x.C + y.C);
        public static Matrix2 operator *(Matrix2 x, double scale) => new(x.A * scale, x.B * scale, x.C * scale);
    }
}
