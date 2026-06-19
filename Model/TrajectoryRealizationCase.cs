using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryRealizationCase : TrajectoryRealizationCaseLight
    {
        public const int MaximumRealizationCount = 1000;
        private const int MaximumAttemptsPerRealization = 5;
        public List<List<SurveyPoint>>? RealizationList { get; set; }

        public bool Calculate(Trajectory trajectory, Action<double, string?>? progress = null)
        {
            CalculationMessage = null;
            RealizationList = null;

            if (RealizationCount <= 0 || RealizationCount > MaximumRealizationCount)
            {
                CalculationMessage = $"Realization count must be between 1 and {MaximumRealizationCount}.";
                return false;
            }

            if (trajectory?.SurveyStationList is not { Count: > 1 } sourceStations)
            {
                CalculationMessage = "The reference trajectory must contain at least two survey stations.";
                return false;
            }

            List<SurveyStation> orderedStations = sourceStations
                .Where(station => station != null)
                .OrderBy(station => station.MD ?? station.Abscissa ?? double.MaxValue)
                .Select(station => new SurveyStation(station))
                .ToList();

            ReferenceStationCount = orderedStations.Count;
            EnsureFirstStationCovariance(orderedStations);
            if (!ValidateStations(orderedStations, out string? validationMessage))
            {
                CalculationMessage = validationMessage;
                return false;
            }

            List<SurveyStation> realizationStations = CoarsenStations(orderedStations, CoarseningMaximumDistance, trajectory.CalculationType);
            CoarsenedStationCount = realizationStations.Count;
            EnsureFirstStationCovariance(realizationStations);
            if (!ValidateStations(realizationStations, out validationMessage))
            {
                CalculationMessage = validationMessage;
                return false;
            }

            Random random = RandomSeed.HasValue ? new Random(RandomSeed.Value) : new Random();
            RealizationList = new List<List<SurveyPoint>>(RealizationCount);
            for (int realizationIndex = 0; realizationIndex < RealizationCount; realizationIndex++)
            {
                List<SurveyPoint>? realization = null;
                int attempt;
                for (attempt = 1; attempt <= MaximumAttemptsPerRealization; attempt++)
                {
                    realization = Realize(realizationStations, random, realizationIndex);
                    if (realization != null)
                    {
                        break;
                    }
                }

                if (realization == null)
                {
                    CalculationMessage = $"Unable to generate realization {realizationIndex + 1} after {MaximumAttemptsPerRealization} attempts.";
                    RealizationList = null;
                    return false;
                }

                RealizationList.Add(realization);
                string message = attempt > 1
                    ? $"Generated realization {realizationIndex + 1} of {RealizationCount} after {attempt} attempts"
                    : $"Generated realization {realizationIndex + 1} of {RealizationCount}";
                progress?.Invoke((realizationIndex + 1.0) / RealizationCount, message);
            }

            return true;
        }

        public static List<SurveyStation> CoarsenStations(
            List<SurveyStation> stations,
            double coarseningMaximumDistance,
            TrajectoryCalculationType calculationType = TrajectoryCalculationType.MinimumCurvatureMethod)
        {
            if (stations.Count <= 2 || !Numeric.IsDefined(coarseningMaximumDistance) || Numeric.LE(coarseningMaximumDistance, 0.0))
            {
                return stations.Select(station => new SurveyStation(station)).ToList();
            }

            List<SurveyStation> result = [new SurveyStation(stations[0])];
            int startIndex = 0;
            while (startIndex < stations.Count - 1)
            {
                int acceptedEndIndex = startIndex + 1;
                for (int candidateEndIndex = startIndex + 2; candidateEndIndex < stations.Count; candidateEndIndex++)
                {
                    if (SegmentWithinTolerance(stations, startIndex, candidateEndIndex, coarseningMaximumDistance, calculationType))
                    {
                        acceptedEndIndex = candidateEndIndex;
                    }
                    else
                    {
                        break;
                    }
                }

                result.Add(new SurveyStation(stations[acceptedEndIndex]));
                startIndex = acceptedEndIndex;
            }

            return result;
        }

        private static bool SegmentWithinTolerance(
            List<SurveyStation> stations,
            int startIndex,
            int endIndex,
            double tolerance,
            TrajectoryCalculationType calculationType)
        {
            SurveyStation start = stations[startIndex];
            SurveyStation end = stations[endIndex];
            if ((start.MD ?? start.Abscissa) is not double startMd ||
                (end.MD ?? end.Abscissa) is not double endMd ||
                !TryGetCoordinates(start, out _, out _, out _) ||
                !TryGetCoordinates(end, out _, out _, out _))
            {
                return false;
            }

            List<SurveyPoint> segment =
            [
                CreatePointForCompletion(start),
                CreatePointForCompletion(end)
            ];
            if (!SurveyPoint.CompleteSurvey(segment, calculationType))
            {
                return false;
            }

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                SurveyStation original = stations[i];
                if ((original.MD ?? original.Abscissa) is not double md ||
                    !TryGetCoordinates(original, out double originalX, out double originalY, out double originalZ))
                {
                    return false;
                }

                SurveyPoint interpolated = new();
                if (!SurveyPoint.InterpolateAtAbscissa(segment, md, interpolated, calculationType) ||
                    !TryGetCoordinates(interpolated, out double interpolatedX, out double interpolatedY, out double interpolatedZ))
                {
                    return false;
                }

                double distance = Math.Sqrt(
                    Square(originalX - interpolatedX) +
                    Square(originalY - interpolatedY) +
                    Square(originalZ - interpolatedZ));
                if (distance > tolerance)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<SurveyPoint>? Realize(List<SurveyStation> stations, Random random, int realizationIndex)
        {
            if (stations.Count == 0)
            {
                return null;
            }

            double[] normalized = [NextGaussian(random), NextGaussian(random), NextGaussian(random)];
            bool debugRealization = realizationIndex + 1 == DebugRealizationNumber;
            if (debugRealization)
            {
                InitializeDebugExport(realizationIndex, normalized);
            }

            List<SurveyPoint> realization = new(stations.Count);
            for (int stationIndex = 0; stationIndex < stations.Count; stationIndex++)
            {
                SurveyStation station = stations[stationIndex];
                List<SurveyPoint>? candidates = CreateRealizationCandidates(station, normalized);
                if (candidates is not { Count: > 0 })
                {
                    return null;
                }

                SurveyPoint? point;
                if (stationIndex == 0)
                {
                    point = candidates[0];
                    point.MD = station.MD;
                    point.Abscissa = station.Abscissa ?? station.MD;
                    point.Inclination = station.Inclination;
                    point.Azimuth = station.Azimuth;
                    point.VerticalSection = station.VerticalSection;
                    realization.Add(point);
                    if (debugRealization)
                    {
                        AppendDebugPoint("raw-covariance-spherical", stationIndex, point, station, candidateIndex: 0, selected: true, smoothnessScore: null);
                        AppendDebugPoint("incremental-md-incl-az", stationIndex, point, station, candidateIndex: 0, selected: true, smoothnessScore: null);
                    }
                    continue;
                }

                point = SelectAndCompleteSmoothCandidate(realization[^1], candidates, debugRealization ? stationIndex : null, station);
                if (point == null)
                {
                    return null;
                }
                point.MD = point.Abscissa;
                point.RiemannianNorth = point.X;
                point.RiemannianEast = point.Y;
                point.TVD = point.Z;
                realization.Add(point);
                if (debugRealization)
                {
                    AppendDebugPoint("incremental-md-incl-az-selected", stationIndex, point, station, candidateIndex: null, selected: true, smoothnessScore: CandidateSmoothnessScore(realization[^2], point));
                }
            }

            List<SurveyPoint> completed = realization
                .Select(point => new SurveyPoint
                {
                    MD = point.MD ?? point.Abscissa,
                    Abscissa = point.Abscissa ?? point.MD,
                    Inclination = point.Inclination,
                    Azimuth = point.Azimuth,
                    VerticalSection = point.VerticalSection
                })
                .ToList();

            if (completed.Count > 0)
            {
                completed[0].X = realization[0].X;
                completed[0].Y = realization[0].Y;
                completed[0].Z = realization[0].Z;
                completed[0].RiemannianNorth = realization[0].X;
                completed[0].RiemannianEast = realization[0].Y;
                completed[0].TVD = realization[0].Z;
            }

            if (!SurveyPoint.CompleteSurvey(completed, TrajectoryCalculationType.MinimumCurvatureMethod))
            {
                return null;
            }

            foreach (SurveyPoint point in completed)
            {
                point.MD ??= point.Abscissa;
                point.RiemannianNorth = point.X;
                point.RiemannianEast = point.Y;
                point.TVD = point.Z;
            }

            if (debugRealization)
            {
                for (int stationIndex = 0; stationIndex < completed.Count; stationIndex++)
                {
                    SurveyStation? referenceStation = stationIndex < stations.Count ? stations[stationIndex] : null;
                    AppendDebugPoint("final-complete-survey", stationIndex, completed[stationIndex], referenceStation, candidateIndex: null, selected: true, smoothnessScore: null);
                }
            }

            return completed;
        }

        private static List<SurveyPoint>? CreateRealizationCandidates(SurveyStation station, double[] normalized)
        {
            if (!TryGetEigen(station, out double[,] eigenVectors, out double[] eigenValues) ||
                !TryGetCoordinates(station, out double centerX, out double centerY, out double centerZ))
            {
                return null;
            }

            double localX = Math.Sqrt(Math.Max(eigenValues[0], 0.0)) * normalized[0];
            double localY = Math.Sqrt(Math.Max(eigenValues[1], 0.0)) * normalized[1];
            double localZ = Math.Sqrt(Math.Max(eigenValues[2], 0.0)) * normalized[2];
            List<SurveyPoint> candidates = new(SignVariants.Length);
            foreach (int[] signs in SignVariants)
            {
                SurveyPoint point = new()
                {
                    X = centerX + GetBias(station, 0) + eigenVectors[0, 0] * signs[0] * localX + eigenVectors[0, 1] * signs[1] * localY + eigenVectors[0, 2] * signs[2] * localZ,
                    Y = centerY + GetBias(station, 1) + eigenVectors[1, 0] * signs[0] * localX + eigenVectors[1, 1] * signs[1] * localY + eigenVectors[1, 2] * signs[2] * localZ,
                    Z = centerZ + GetBias(station, 2) + eigenVectors[2, 0] * signs[0] * localX + eigenVectors[2, 1] * signs[1] * localY + eigenVectors[2, 2] * signs[2] * localZ
                };
                point.RiemannianNorth = point.X;
                point.RiemannianEast = point.Y;
                point.TVD = point.Z;
                candidates.Add(point);
            }

            return candidates;
        }

        private static readonly int[][] SignVariants =
        [
            [1, 1, 1],
            [-1, 1, 1],
            [1, -1, 1],
            [1, 1, -1],
            [-1, -1, 1],
            [-1, 1, -1],
            [1, -1, -1],
            [-1, -1, -1]
        ];

        private static SurveyPoint? SelectAndCompleteSmoothCandidate(SurveyPoint previous, List<SurveyPoint> candidates, int? debugStationIndex = null, SurveyStation? debugReferenceStation = null)
        {
            SurveyPoint? bestCandidate = null;
            double bestTangentScore = double.PositiveInfinity;
            double bestSmoothnessScore = double.PositiveInfinity;
            int bestCandidateIndex = -1;
            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                SurveyPoint candidate = candidates[candidateIndex];
                if (debugStationIndex is int stationIndex)
                {
                    AppendDebugPoint("raw-covariance-spherical-candidate", stationIndex, candidate, debugReferenceStation, candidateIndex, selected: false, smoothnessScore: null);
                }

                SurveyPoint previousCopy = new(previous);
                SurveyPoint completedCandidate = new(candidate);
                if (!previousCopy.CompleteFromXYZ(completedCandidate, TrajectoryCalculationType.MinimumCurvatureMethod))
                {
                    if (debugStationIndex is int failedStationIndex)
                    {
                        AppendDebugPoint("incremental-md-incl-az-candidate-failed", failedStationIndex, completedCandidate, debugReferenceStation, candidateIndex, selected: false, smoothnessScore: null);
                    }
                    continue;
                }

                if (!CompleteFromXYZRoundTrips(previous, candidate, completedCandidate))
                {
                    if (debugStationIndex is int rejectedStationIndex)
                    {
                        AppendDebugPoint("incremental-md-incl-az-candidate-rejected-roundtrip", rejectedStationIndex, completedCandidate, debugReferenceStation, candidateIndex, selected: false, smoothnessScore: null);
                    }
                    continue;
                }

                double tangentScore = CandidateTangentScore(debugReferenceStation, completedCandidate) ?? double.PositiveInfinity;
                double smoothnessScore = CandidateSmoothnessScore(previous, completedCandidate);
                if (debugStationIndex is int completedStationIndex)
                {
                    AppendDebugPoint("incremental-md-incl-az-candidate", completedStationIndex, completedCandidate, debugReferenceStation, candidateIndex, selected: false, smoothnessScore: tangentScore);
                }
                if (tangentScore < bestTangentScore ||
                    (Numeric.EQ(tangentScore, bestTangentScore) && smoothnessScore < bestSmoothnessScore))
                {
                    bestTangentScore = tangentScore;
                    bestSmoothnessScore = smoothnessScore;
                    bestCandidate = completedCandidate;
                    bestCandidateIndex = candidateIndex;
                }
            }

            if (bestCandidate != null && debugStationIndex is int selectedStationIndex)
            {
                AppendDebugPoint("incremental-md-incl-az-candidate-selected", selectedStationIndex, bestCandidate, debugReferenceStation, bestCandidateIndex, selected: true, smoothnessScore: bestTangentScore);
            }

            return bestCandidate;
        }

        private static bool CompleteFromXYZRoundTrips(SurveyPoint previous, SurveyPoint rawCandidate, SurveyPoint completedCandidate)
        {
            if (!TryGetCoordinates(rawCandidate, out double targetX, out double targetY, out double targetZ) ||
                !TryGetCoordinates(previous, out double previousX, out double previousY, out double previousZ) ||
                completedCandidate.Abscissa is not double abscissa ||
                completedCandidate.Inclination is not double inclination ||
                completedCandidate.Azimuth is not double azimuth)
            {
                return false;
            }

            SurveyPoint reconstructed = new()
            {
                Abscissa = abscissa,
                MD = abscissa,
                Inclination = inclination,
                Azimuth = azimuth
            };

            SurveyPoint previousCopy = new(previous);
            if (!previousCopy.CompleteFromSIA(reconstructed, TrajectoryCalculationType.MinimumCurvatureMethod) ||
                !TryGetCoordinates(reconstructed, out double reconstructedX, out double reconstructedY, out double reconstructedZ))
            {
                return false;
            }

            double error = Math.Sqrt(Square(reconstructedX - targetX) + Square(reconstructedY - targetY) + Square(reconstructedZ - targetZ));
            double chord = Math.Sqrt(Square(targetX - previousX) + Square(targetY - previousY) + Square(targetZ - previousZ));
            double tolerance = Math.Max(1e-4, 1e-8 * Math.Max(1.0, chord));
            return error <= tolerance;
        }

        private static double? CandidateTangentScore(SurveyStation? referenceStation, SurveyPoint candidate)
        {
            if (referenceStation?.Inclination is not double referenceInclination ||
                referenceStation.Azimuth is not double referenceAzimuth ||
                candidate.Inclination is not double candidateInclination ||
                candidate.Azimuth is not double candidateAzimuth)
            {
                return null;
            }

            (double x, double y, double z) referenceTangent = TangentVector(referenceInclination, referenceAzimuth);
            (double x, double y, double z) candidateTangent = TangentVector(candidateInclination, candidateAzimuth);
            double dot =
                referenceTangent.x * candidateTangent.x +
                referenceTangent.y * candidateTangent.y +
                referenceTangent.z * candidateTangent.z;
            dot = Math.Min(1.0, Math.Max(-1.0, dot));
            return 1.0 - dot;
        }

        private static (double x, double y, double z) TangentVector(double inclination, double azimuth)
        {
            double sinInclination = Math.Sin(inclination);
            return (
                sinInclination * Math.Cos(azimuth),
                sinInclination * Math.Sin(azimuth),
                Math.Cos(inclination));
        }

        private static double CandidateSmoothnessScore(SurveyPoint previous, SurveyPoint candidate)
        {
            double curvature = Math.Abs(candidate.Curvature ?? 0.0);
            double buildRate = Math.Abs(candidate.BUR ?? 0.0);
            double turnRate = Math.Abs(candidate.TUR ?? 0.0);
            double distance = 0.0;
            if (TryGetCoordinates(previous, out double previousX, out double previousY, out double previousZ) &&
                TryGetCoordinates(candidate, out double candidateX, out double candidateY, out double candidateZ))
            {
                distance = Math.Sqrt(Square(candidateX - previousX) + Square(candidateY - previousY) + Square(candidateZ - previousZ));
            }

            return curvature + buildRate + turnRate + 1e-9 * distance;
        }

        private const int DebugRealizationNumber = 21;
        private const string DebugExportPath = @"C:\OSDC\Trajectory\trajectory-realization-21-generation-debug.tsv";

        private static void InitializeDebugExport(int realizationIndex, double[] normalized)
        {
            StringBuilder builder = new();
            builder.AppendLine("# Temporary trajectory realization generation debug export");
            builder.AppendLine("# RealizationNumberOneBased\t" + (realizationIndex + 1).ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("# RealizationIndexZeroBased\t" + realizationIndex.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("# NormalizedGaussian\t" + string.Join("\t", normalized.Select(value => FormatDebugValue(value))));
            builder.AppendLine(string.Join('\t',
            [
                "Stage",
                "StationIndex",
                "CandidateIndex",
                "Selected",
                "SmoothnessScore",
                "ReferenceMD",
                "ReferenceIncl",
                "ReferenceAz",
                "ReferenceX",
                "ReferenceY",
                "ReferenceZ",
                "MD",
                "Abscissa",
                "Incl",
                "Az",
                "X",
                "Y",
                "Z",
                "TVD",
                "North",
                "East",
                "DLS",
                "BUR",
                "TUR",
                "VSect"
            ]));
            File.WriteAllText(DebugExportPath, builder.ToString());
        }

        private static void AppendDebugPoint(string stage, int stationIndex, SurveyPoint point, SurveyStation? referenceStation, int? candidateIndex, bool selected, double? smoothnessScore)
        {
            List<string> fields =
            [
                stage,
                (stationIndex + 1).ToString(CultureInfo.InvariantCulture),
                candidateIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                selected.ToString(CultureInfo.InvariantCulture),
                FormatDebugValue(smoothnessScore),
                FormatDebugValue(referenceStation?.MD ?? referenceStation?.Abscissa),
                FormatDebugValue(referenceStation?.Inclination),
                FormatDebugValue(referenceStation?.Azimuth),
                FormatDebugValue(referenceStation?.X ?? referenceStation?.RiemannianNorth),
                FormatDebugValue(referenceStation?.Y ?? referenceStation?.RiemannianEast),
                FormatDebugValue(referenceStation?.Z ?? referenceStation?.TVD),
                FormatDebugValue(point.MD),
                FormatDebugValue(point.Abscissa),
                FormatDebugValue(point.Inclination),
                FormatDebugValue(point.Azimuth),
                FormatDebugValue(point.X),
                FormatDebugValue(point.Y),
                FormatDebugValue(point.Z),
                FormatDebugValue(point.TVD),
                FormatDebugValue(point.RiemannianNorth),
                FormatDebugValue(point.RiemannianEast),
                FormatDebugValue(point.Curvature),
                FormatDebugValue(point.BUR),
                FormatDebugValue(point.TUR),
                FormatDebugValue(point.VerticalSection)
            ];
            File.AppendAllText(DebugExportPath, string.Join('\t', fields) + Environment.NewLine);
        }

        private static string FormatDebugValue(double? value) =>
            value is double defined && Numeric.IsDefined(defined)
                ? defined.ToString("G17", CultureInfo.InvariantCulture)
                : string.Empty;

        private static bool ValidateStations(List<SurveyStation> stations, out string? message)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                SurveyStation station = stations[i];
                if (!TryGetCoordinates(station, out _, out _, out _) ||
                    (station.MD ?? station.Abscissa) is not double ||
                    station.Inclination is not double ||
                    station.Azimuth is not double)
                {
                    message = $"Survey station {i + 1} is missing position, MD, inclination, or azimuth.";
                    return false;
                }

                if (!TryGetEigen(station, out _, out _))
                {
                    message = $"Survey station {i + 1} is missing a usable covariance matrix.";
                    return false;
                }
            }

            message = null;
            return true;
        }

        private static void EnsureFirstStationCovariance(List<SurveyStation> stations)
        {
            if (stations.Count < 2 || HasUsableCovariance(stations[0]))
            {
                return;
            }

            bool followingStationsHaveCovariance = stations
                .Skip(1)
                .Any(HasUsableCovariance);
            if (!followingStationsHaveCovariance)
            {
                return;
            }

            OSDC.DotnetLibraries.General.Math.SymmetricMatrix3x3 covariance = new();
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    covariance[row, col] = 0.0;
                }
            }

            stations[0].Covariance = covariance;
            stations[0].EigenVectors = null;
            stations[0].EigenValues = null;
        }

        private static bool HasUsableCovariance(SurveyStation station)
        {
            if (station.Covariance == null)
            {
                return false;
            }

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (station.Covariance[row, col] is double value && Numeric.IsDefined(value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetEigen(SurveyStation station, out double[,] eigenVectors, out double[] eigenValues)
        {
            eigenVectors = new double[3, 3];
            eigenValues = new double[3];
            if (station.Covariance == null)
            {
                return false;
            }

            if ((station.EigenVectors == null || station.EigenValues == null) && !station.CalculateEigenProperties())
            {
                return false;
            }

            if (station.EigenVectors == null || station.EigenValues == null)
            {
                return false;
            }

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (station.EigenVectors[row, col] is not double value || !Numeric.IsDefined(value))
                    {
                        return false;
                    }
                    eigenVectors[row, col] = value;
                }

                if (station.EigenValues[row] is not double eigenValue || !Numeric.IsDefined(eigenValue) || Numeric.LT(eigenValue, 0.0))
                {
                    return false;
                }
                eigenValues[row] = eigenValue;
            }

            return true;
        }

        private static SurveyPoint CreatePointForCompletion(SurveyStation station)
        {
            TryGetCoordinates(station, out double x, out double y, out double z);
            return new SurveyPoint
            {
                X = x,
                Y = y,
                Z = z,
                RiemannianNorth = x,
                RiemannianEast = y,
                TVD = z,
                MD = station.MD,
                Abscissa = station.Abscissa ?? station.MD,
                Inclination = station.Inclination,
                Azimuth = station.Azimuth,
                VerticalSection = station.VerticalSection
            };
        }

        private static bool TryGetCoordinates(SurveyPoint point, out double x, out double y, out double z)
        {
            x = point.X ?? point.RiemannianNorth ?? double.NaN;
            y = point.Y ?? point.RiemannianEast ?? double.NaN;
            z = point.Z ?? point.TVD ?? double.NaN;
            return Numeric.IsDefined(x) && Numeric.IsDefined(y) && Numeric.IsDefined(z);
        }

        private static double GetBias(SurveyStation station, int index)
        {
            return station.Bias?[index] ?? 0.0;
        }

        private static double NextGaussian(Random random)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        private static double Square(double value) => value * value;
    }
}
