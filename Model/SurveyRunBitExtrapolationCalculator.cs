using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Math;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Model
{
    public static class SurveyRunBitExtrapolationCalculator
    {
        public const string TerminalAnnotation = "Extrapolated from measurement tool to bit";

        public static bool TryAppendCalculatedStation(
            List<SurveyStation> stations,
            TrajectoryCalculationType calculationType,
            double distance)
        {
            if (stations.Count == 0 || !Numeric.IsDefined(distance) || !Numeric.GT(distance, 0.0))
            {
                return false;
            }

            SurveyStation last = stations[^1];
            if (!IsComplete(last))
            {
                return false;
            }

            SurveyStation? extrapolated = stations.Count == 1
                ? ExtrapolateStraight(last, distance)
                : ExtrapolateLastCurve(stations[^2], last, calculationType, distance);
            if (extrapolated == null)
            {
                return false;
            }

            extrapolated.Annotation = TerminalAnnotation;
            stations.Add(extrapolated);
            return true;
        }

        private static SurveyStation? ExtrapolateStraight(SurveyStation start, double distance)
        {
            double inclination = start.Inclination!.Value;
            double azimuth = start.Azimuth!.Value;
            double sinInclination = Math.Sin(inclination);
            return new SurveyStation
            {
                MD = start.MD + distance,
                Inclination = inclination,
                Azimuth = azimuth,
                RiemannianNorth = start.RiemannianNorth + distance * sinInclination * Math.Cos(azimuth),
                RiemannianEast = start.RiemannianEast + distance * sinInclination * Math.Sin(azimuth),
                TVD = start.TVD + distance * Math.Cos(inclination),
                VerticalSection = start.VerticalSection is { } verticalSection
                    ? verticalSection + distance * sinInclination
                    : null,
                Curvature = 0.0,
                Toolface = 0.0,
                BUR = 0.0,
                TUR = 0.0,
                BoreholeRadius = start.BoreholeRadius
            };
        }

        private static SurveyStation? ExtrapolateLastCurve(
            SurveyStation previous,
            SurveyStation last,
            TrajectoryCalculationType calculationType,
            double distance)
        {
            if (!IsComplete(previous) || previous.MD is not double previousMd || last.MD is not double lastMd ||
                !Numeric.GT(lastMd, previousMd))
            {
                return null;
            }

            ArcSection? section = calculationType switch
            {
                TrajectoryCalculationType.ConstantBuildAndTurnMethod => ExtendBuildAndTurn(previous, last, distance),
                TrajectoryCalculationType.ConstantCurvatureAndToolfaceMethod => ExtendCurvatureAndToolface(previous, last, distance),
                _ => ExtendCircularArc(previous, last, distance)
            };
            if (section?.End is not { Abscissa: not null, Inclination: not null, Azimuth: not null,
                X: not null, Y: not null, Z: not null } end)
            {
                return null;
            }

            SurveyStation result = new()
            {
                MD = end.Abscissa,
                Inclination = end.Inclination,
                Azimuth = end.Azimuth,
                RiemannianNorth = end.X,
                RiemannianEast = end.Y,
                TVD = end.Z,
                VerticalSection = last.VerticalSection is { } verticalSection
                    ? verticalSection + Math.Sqrt(
                        Math.Pow(end.X.Value - last.RiemannianNorth!.Value, 2) +
                        Math.Pow(end.Y.Value - last.RiemannianEast!.Value, 2))
                    : null,
                BoreholeRadius = last.BoreholeRadius
            };

            switch (section)
            {
                case CircularArcSection circular:
                    result.Curvature = circular.Circle.Curvature;
                    result.Toolface = circular.Circle.ReferenceToolface;
                    break;
                case BuildAndTurnArcSection buildAndTurn:
                    result.BUR = buildAndTurn.BuildAndTurn.BUR;
                    result.TUR = buildAndTurn.BuildAndTurn.TR;
                    break;
                case ConstantCurvatureAndToolfaceArcSection curvatureAndToolface:
                    result.Curvature = curvatureAndToolface.CTCCurve.Curvature;
                    result.Toolface = curvatureAndToolface.CTCCurve.Toolface;
                    break;
            }

            return result;
        }

        private static ArcSection? ExtendCircularArc(SurveyStation previous, SurveyStation last, double extension)
        {
            CircularArcSection fitted = new(ToPoint(previous), EndpointAttitude(last));
            if (!fitted.CalculateSIA()) return null;
            CircularArcSection extended = new(ToPoint(previous), new TrajectoryPoint3D());
            extended.Circle.Length = last.MD!.Value - previous.MD!.Value + extension;
            extended.Circle.Curvature = fitted.Circle.Curvature;
            extended.Circle.ReferenceToolface = fitted.Circle.ReferenceToolface;
            return extended.CalculateLDT() ? extended : null;
        }

        private static ArcSection? ExtendBuildAndTurn(SurveyStation previous, SurveyStation last, double extension)
        {
            BuildAndTurnArcSection fitted = new(ToPoint(previous), EndpointAttitude(last));
            if (!fitted.CalculateSIA()) return null;
            BuildAndTurnArcSection extended = new(ToPoint(previous), new TrajectoryPoint3D());
            extended.BuildAndTurn.Length = last.MD!.Value - previous.MD!.Value + extension;
            extended.BuildAndTurn.BUR = fitted.BuildAndTurn.BUR;
            extended.BuildAndTurn.TR = fitted.BuildAndTurn.TR;
            return extended.CalculateLBT() ? extended : null;
        }

        private static ArcSection? ExtendCurvatureAndToolface(SurveyStation previous, SurveyStation last, double extension)
        {
            ConstantCurvatureAndToolfaceArcSection fitted = new(ToPoint(previous), EndpointAttitude(last));
            if (!fitted.CalculateSIA()) return null;
            ConstantCurvatureAndToolfaceArcSection extended = new(ToPoint(previous), new TrajectoryPoint3D());
            extended.CTCCurve.Length = last.MD!.Value - previous.MD!.Value + extension;
            extended.CTCCurve.Curvature = fitted.CTCCurve.Curvature;
            extended.CTCCurve.Toolface = fitted.CTCCurve.Toolface;
            return extended.CalculateLDT() ? extended : null;
        }

        private static bool IsComplete(SurveyStation station) =>
            Numeric.IsDefined(station.MD) && Numeric.IsDefined(station.Inclination) &&
            Numeric.IsDefined(station.Azimuth) && Numeric.IsDefined(station.RiemannianNorth) &&
            Numeric.IsDefined(station.RiemannianEast) && Numeric.IsDefined(station.TVD);

        private static TrajectoryPoint3D ToPoint(SurveyStation station) => new()
        {
            Abscissa = station.MD,
            Inclination = station.Inclination,
            Azimuth = station.Azimuth,
            X = station.RiemannianNorth,
            Y = station.RiemannianEast,
            Z = station.TVD
        };

        private static TrajectoryPoint3D EndpointAttitude(SurveyStation station) => new()
        {
            Abscissa = station.MD,
            Inclination = station.Inclination,
            Azimuth = station.Azimuth
        };
    }
}
