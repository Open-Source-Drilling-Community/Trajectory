using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.GlobalAntiCollision
{
    public sealed class SeparationFactorEnvelopeCache
    {
        private readonly List<SurveyStation> _surveysRef;
        private readonly List<SurveyStation> _surveysCmp;
        private readonly double _confidenceFactor;
        private readonly UncertaintyEnvelope.ErrorModelType _errorModelTypeRef;
        private readonly UncertaintyEnvelope.ErrorModelType _errorModelTypeCmp;
        private readonly Dictionary<double, List<UncertaintyEllipse>?> _referenceCache = [];
        private readonly Dictionary<double, List<UncertaintyEllipse>?> _comparisonCache = [];
        private readonly Dictionary<int, CandidateComparisonIndices> _candidateComparisonIndicesCache = [];
        private readonly int _meshSectorCount;
        private bool _referenceSurveyPrepared;
        private bool _comparisonSurveyPrepared;

        public SeparationFactorEnvelopeCache(
            List<SurveyStation> surveysRef,
            List<SurveyStation> surveysCmp,
            double confidenceFactor,
            UncertaintyEnvelope.ErrorModelType errorModelTypeRef,
            UncertaintyEnvelope.ErrorModelType errorModelTypeCmp,
            int? meshSectorCount = null)
        {
            _surveysRef = surveysRef;
            _surveysCmp = surveysCmp;
            _confidenceFactor = confidenceFactor;
            _errorModelTypeRef = errorModelTypeRef;
            _errorModelTypeCmp = errorModelTypeCmp;
            _meshSectorCount = meshSectorCount ?? PerpendicularEllipseEnvelopeBuilder.DefaultMeshSectorCount;
            MinimumReferenceMdStep = MinimumMDBetweenSurveyStations(_surveysRef);
            if (MinimumReferenceMdStep.HasValue)
            {
                MinimumReferenceMdStep /= SeparationFactorCalculations.MinNumberInterpolations;
            }
        }

        public double? MinimumReferenceMdStep { get; }

        public bool IsValid => MinimumReferenceMdStep.HasValue && MinimumReferenceMdStep.Value > 0;

        public bool TryGetEllipses(double separationFactor, out List<UncertaintyEllipse>? ellipseRef, out List<UncertaintyEllipse>? ellipseCmp)
        {
            double cacheKey = NormalizeScale(separationFactor);
            ellipseRef = GetOrCreateReferenceEllipses(cacheKey);
            ellipseCmp = GetOrCreateComparisonEllipses(cacheKey);
            return ellipseRef != null && ellipseCmp != null;
        }

        public CandidateComparisonIndices GetCandidateComparisonIndices(int ellipseRefIndex)
        {
            if (_candidateComparisonIndicesCache.TryGetValue(ellipseRefIndex, out CandidateComparisonIndices? cached))
            {
                return cached;
            }

            if (!TryGetEllipses(SeparationFactorCalculations.MaxSeparationFactor, out List<UncertaintyEllipse>? ellipseRef, out List<UncertaintyEllipse>? ellipseCmp) ||
                ellipseRef == null ||
                ellipseCmp == null)
            {
                CandidateComparisonIndices empty = new([], []);
                _candidateComparisonIndicesCache[ellipseRefIndex] = empty;
                return empty;
            }

            Bounds? reducedReferenceBounds = GetReducedReferenceBounds(ellipseRef, ellipseRefIndex);
            if (reducedReferenceBounds == null)
            {
                CandidateComparisonIndices empty = new([], []);
                _candidateComparisonIndicesCache[ellipseRefIndex] = empty;
                return empty;
            }

            List<int> pointIndices = [];
            for (int i = 0; i < ellipseCmp.Count; i++)
            {
                var boundingBox = ellipseCmp[i].BoundingBox;
                if (boundingBox != null && reducedReferenceBounds.Intersects(boundingBox))
                {
                    pointIndices.Add(i);
                }
            }

            int firstSegmentIndex = -1;
            int lastSegmentIndex = -1;
            for (int i = 0; i < ellipseCmp.Count - 1; i++)
            {
                var firstBoundingBox = ellipseCmp[i].BoundingBox;
                var secondBoundingBox = ellipseCmp[i + 1].BoundingBox;
                if (firstBoundingBox == null || secondBoundingBox == null)
                {
                    continue;
                }

                Bounds? joinedBounds = Bounds.Join(firstBoundingBox, secondBoundingBox);
                if (joinedBounds != null && reducedReferenceBounds.Intersects(joinedBounds))
                {
                    if (firstSegmentIndex < 0)
                    {
                        firstSegmentIndex = i;
                    }
                    lastSegmentIndex = i;
                }
            }

            List<int> segmentIndices = CreateContiguousSegmentIndices(firstSegmentIndex, lastSegmentIndex);
            CandidateComparisonIndices results = new(pointIndices, segmentIndices);
            _candidateComparisonIndicesCache[ellipseRefIndex] = results;
            return results;
        }

        private static List<int> CreateContiguousSegmentIndices(int firstSegmentIndex, int lastSegmentIndex)
        {
            if (firstSegmentIndex < 0 || lastSegmentIndex < firstSegmentIndex)
            {
                return [];
            }

            List<int> segmentIndices = [];
            for (int i = firstSegmentIndex; i <= lastSegmentIndex; i++)
            {
                segmentIndices.Add(i);
            }

            return segmentIndices;
        }

        private List<UncertaintyEllipse>? GetOrCreateReferenceEllipses(double separationFactor)
        {
            if (_referenceCache.TryGetValue(separationFactor, out List<UncertaintyEllipse>? cached))
            {
                return cached;
            }

            bool ok = PerpendicularEllipseEnvelopeBuilder.TryBuildMeshedEllipseList(
                _surveysRef,
                _errorModelTypeRef,
                ref _referenceSurveyPrepared,
                _confidenceFactor,
                separationFactor,
                _meshSectorCount,
                SeparationFactorCalculations.MinNumberInterpolations,
                null,
                out List<UncertaintyEllipse>? ellipses);
            if (!ok)
            {
                ellipses = null;
            }
            _referenceCache[separationFactor] = ellipses;
            return ellipses;
        }

        private List<UncertaintyEllipse>? GetOrCreateComparisonEllipses(double separationFactor)
        {
            if (_comparisonCache.TryGetValue(separationFactor, out List<UncertaintyEllipse>? cached))
            {
                return cached;
            }

            if (!MinimumReferenceMdStep.HasValue)
            {
                _comparisonCache[separationFactor] = null;
                return null;
            }

            bool ok = PerpendicularEllipseEnvelopeBuilder.TryBuildMeshedEllipseList(
                _surveysCmp,
                _errorModelTypeCmp,
                ref _comparisonSurveyPrepared,
                _confidenceFactor,
                separationFactor,
                _meshSectorCount,
                null,
                MinimumReferenceMdStep.Value,
                out List<UncertaintyEllipse>? ellipses);
            if (!ok)
            {
                ellipses = null;
            }
            _comparisonCache[separationFactor] = ellipses;
            return ellipses;
        }

        private static double NormalizeScale(double separationFactor)
        {
            return Math.Round(separationFactor, 6, MidpointRounding.AwayFromZero);
        }

        private static Bounds? GetReducedReferenceBounds(List<UncertaintyEllipse> ellipseRef, int ellipseRefIndex)
        {
            if (ellipseRefIndex < 0 || ellipseRefIndex >= ellipseRef.Count || ellipseRef.Count <= 2)
            {
                return null;
            }

            List<UncertaintyEllipse> reducedEllipseRef = [];
            if (ellipseRefIndex == 0)
            {
                reducedEllipseRef.Add(ellipseRef[0]);
                reducedEllipseRef.Add(ellipseRef[1]);
                reducedEllipseRef.Add(ellipseRef[2]);
            }
            else if (ellipseRefIndex == ellipseRef.Count - 1)
            {
                reducedEllipseRef.Add(ellipseRef[^3]);
                reducedEllipseRef.Add(ellipseRef[^2]);
                reducedEllipseRef.Add(ellipseRef[^1]);
            }
            else
            {
                reducedEllipseRef.Add(ellipseRef[ellipseRefIndex - 1]);
                reducedEllipseRef.Add(ellipseRef[ellipseRefIndex]);
                reducedEllipseRef.Add(ellipseRef[ellipseRefIndex + 1]);
            }

            Bounds bounds = GetBounds(reducedEllipseRef);
            if (bounds.MaxX < bounds.MinX ||
                bounds.MaxY < bounds.MinY ||
                bounds.MaxZ < bounds.MinZ)
            {
                return null;
            }

            return bounds;
        }

        private static Bounds GetBounds(List<UncertaintyEllipse> ellipses)
        {
            double minX = Numeric.MAX_DOUBLE;
            double maxX = Numeric.MIN_DOUBLE;
            double minY = Numeric.MAX_DOUBLE;
            double maxY = Numeric.MIN_DOUBLE;
            double minZ = Numeric.MAX_DOUBLE;
            double maxZ = Numeric.MIN_DOUBLE;
            for (int i = 0; i < ellipses.Count; i++)
            {
                List<SurveyPoint>? vertices = ellipses[i].EllipseVertices;
                if (vertices == null)
                {
                    continue;
                }

                for (int j = 0; j < vertices.Count; j++)
                {
                    if (TryGetCoordinates(vertices[j], out double x, out double y, out double z))
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        if (z < minZ) minZ = z;
                        if (z > maxZ) maxZ = z;
                    }
                }
            }

            return new Bounds(minX, maxX, minY, maxY, minZ, maxZ);
        }

        private static bool TryGetCoordinates(SurveyPoint? surveyPoint, out double x, out double y, out double z)
        {
            x = 0;
            y = 0;
            z = 0;
            return TryGetCoordinate(surveyPoint, static point => point.X, out x) &&
                TryGetCoordinate(surveyPoint, static point => point.Y, out y) &&
                TryGetCoordinate(surveyPoint, static point => point.Z, out z);
        }

        private static bool TryGetCoordinate(SurveyPoint? surveyPoint, Func<SurveyPoint, double?> selector, out double value)
        {
            value = 0;
            if (surveyPoint == null)
            {
                return false;
            }

            double? candidate = selector(surveyPoint);
            if (!candidate.HasValue || !Numeric.IsDefined(candidate.Value))
            {
                return false;
            }

            value = candidate.Value;
            return true;
        }

        private static double? MinimumMDBetweenSurveyStations(List<SurveyStation>? listOfSurveyStations)
        {
            double? minDeltaMD = null;
            if (listOfSurveyStations != null && listOfSurveyStations.Count > 1)
            {
                for (int i = 0; i < listOfSurveyStations.Count - 1; i++)
                {
                    double? deltaMD = listOfSurveyStations[i + 1].MD - listOfSurveyStations[i].MD;
                    if (Numeric.IsDefined(deltaMD) && (minDeltaMD == null || Numeric.LT(deltaMD, minDeltaMD)))
                    {
                        minDeltaMD = deltaMD;
                    }
                }
            }
            return minDeltaMD;
        }
    }

    public sealed class CandidateComparisonIndices
    {
        public CandidateComparisonIndices(List<int> pointIndices, List<int> segmentIndices)
        {
            PointIndices = pointIndices;
            SegmentIndices = segmentIndices;
        }

        public List<int> PointIndices { get; }

        public List<int> SegmentIndices { get; }

        public bool HasCandidates => PointIndices.Count > 0 || SegmentIndices.Count > 0;
    }
}
