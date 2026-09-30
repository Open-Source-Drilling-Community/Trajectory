using System;

namespace OSDC.Drilling.Trajectory.Model
{
    /// <summary>Transforms an observed survey direction into the WGS84 geodetic north-east-down frame.</summary>
    public static class SurveyReferenceCorrectionCalculator
    {
        public static bool TryCorrect(
            double observedInclination,
            double observedAzimuth,
            SurveyInclinationReference inclinationReference,
            SurveyAzimuthReference azimuthReference,
            (double North, double East, double Down)? gravity,
            (double North, double East, double Down)? magneticField,
            out double inclination,
            out double azimuth)
        {
            inclination = default;
            azimuth = default;
            if (!double.IsFinite(observedInclination) || !double.IsFinite(observedAzimuth) ||
                observedInclination < 0.0 || observedInclination > Math.PI)
            {
                return false;
            }

            Vector down = inclinationReference == SurveyInclinationReference.GravityVertical
                ? Normalize(gravity is { } g ? new(g.North, g.East, g.Down) : default)
                : new(0.0, 0.0, 1.0);
            if (!down.IsDefined)
            {
                return false;
            }

            Vector referenceNorth = azimuthReference switch
            {
                SurveyAzimuthReference.TrueNorth => ProjectAndNormalize(new(1.0, 0.0, 0.0), down),
                SurveyAzimuthReference.MagneticNorth when magneticField is { } field =>
                    ProjectAndNormalize(new(field.North, field.East, field.Down), down),
                _ => default
            };
            if (!referenceNorth.IsDefined)
            {
                return false;
            }

            Vector referenceEast = Normalize(Cross(down, referenceNorth));
            double sinInclination = Math.Sin(observedInclination);
            Vector direction =
                down * Math.Cos(observedInclination) +
                referenceNorth * (sinInclination * Math.Cos(observedAzimuth)) +
                referenceEast * (sinInclination * Math.Sin(observedAzimuth));
            direction = Normalize(direction);
            if (!direction.IsDefined)
            {
                return false;
            }

            inclination = Math.Acos(Math.Clamp(direction.Down, -1.0, 1.0));
            azimuth = NormalizeAzimuth(Math.Atan2(direction.East, direction.North));
            return true;
        }

        public static double NormalizeAzimuth(double value)
        {
            double normalized = value % (2.0 * Math.PI);
            return normalized < 0.0 ? normalized + 2.0 * Math.PI : normalized;
        }

        public static double ShortestSignedAngle(double value)
        {
            double normalized = NormalizeAzimuth(value);
            return normalized > Math.PI ? normalized - 2.0 * Math.PI : normalized;
        }

        private static Vector ProjectAndNormalize(Vector value, Vector normal) =>
            Normalize(value - normal * Dot(value, normal));

        private static Vector Cross(Vector left, Vector right) => new(
            left.East * right.Down - left.Down * right.East,
            left.Down * right.North - left.North * right.Down,
            left.North * right.East - left.East * right.North);

        private static double Dot(Vector left, Vector right) =>
            left.North * right.North + left.East * right.East + left.Down * right.Down;

        private static Vector Normalize(Vector value)
        {
            double magnitude = Math.Sqrt(Dot(value, value));
            return double.IsFinite(magnitude) && magnitude > 0.0 ? value * (1.0 / magnitude) : default;
        }

        private readonly record struct Vector(double North, double East, double Down)
        {
            public bool IsDefined => double.IsFinite(North) && double.IsFinite(East) && double.IsFinite(Down) &&
                (North != 0.0 || East != 0.0 || Down != 0.0);
            public static Vector operator +(Vector left, Vector right) => new(left.North + right.North, left.East + right.East, left.Down + right.Down);
            public static Vector operator -(Vector left, Vector right) => new(left.North - right.North, left.East - right.East, left.Down - right.Down);
            public static Vector operator *(Vector value, double factor) => new(value.North * factor, value.East * factor, value.Down * factor);
        }
    }
}
