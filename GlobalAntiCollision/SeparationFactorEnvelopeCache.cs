using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Math;
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
        private readonly double? _referenceMinimumMD;
        private readonly double? _comparisonMinimumMD;
        private readonly SeparationFactorEnvelopeCache? _sharedReferenceCache;
        private readonly object _syncLock = new();
        private readonly Dictionary<double, List<UncertaintyEllipse>?> _referenceCache = [];
        private readonly Dictionary<double, List<UncertaintyEllipse>?> _comparisonCache = [];
        private readonly Dictionary<int, CandidateComparisonIndices> _candidateComparisonIndicesCache = [];
        private readonly Dictionary<UncertaintyEllipse, List<SurveyPoint>> _rotatedEllipseCache = [];
        private readonly Dictionary<(double SeparationFactor, int SegmentIndex), List<LineSegment3D>?> _comparisonLineSegmentCache = [];
        private readonly Dictionary<(double SeparationFactor, int ReferenceEllipseIndex), ReferenceIntersectionGeometry?> _referenceGeometryCache = [];
        private readonly int _meshSectorCount;
        private List<UncertaintyEllipse>? _referenceZeroScaleEllipses;
        private List<UncertaintyEllipse>? _referenceUnitScaleEllipses;
        private List<UncertaintyEllipse>? _comparisonZeroScaleEllipses;
        private List<UncertaintyEllipse>? _comparisonUnitScaleEllipses;
        private bool _referenceZeroScaleCreated;
        private bool _referenceUnitScaleCreated;
        private bool _comparisonZeroScaleCreated;
        private bool _comparisonUnitScaleCreated;
        private bool _referenceSurveyPrepared;
        private bool _comparisonSurveyPrepared;

        public SeparationFactorEnvelopeCache(
            List<SurveyStation> surveysRef,
            List<SurveyStation> surveysCmp,
            double confidenceFactor,
            UncertaintyEnvelope.ErrorModelType errorModelTypeRef,
            UncertaintyEnvelope.ErrorModelType errorModelTypeCmp,
            int? meshSectorCount = null,
            double? referenceMinimumMD = null,
            double? comparisonMinimumMD = null,
            SeparationFactorEnvelopeCache? sharedReferenceCache = null)
        {
            _surveysRef = surveysRef;
            _surveysCmp = surveysCmp;
            _confidenceFactor = confidenceFactor;
            _errorModelTypeRef = errorModelTypeRef;
            _errorModelTypeCmp = errorModelTypeCmp;
            _referenceMinimumMD = NormalizeMinimumMD(referenceMinimumMD);
            _comparisonMinimumMD = NormalizeMinimumMD(comparisonMinimumMD);
            _sharedReferenceCache = sharedReferenceCache;
            _meshSectorCount = meshSectorCount ?? PerpendicularEllipseEnvelopeBuilder.DefaultMeshSectorCount;
            ComparisonMeshLongitudinalLength = GetPairComparisonMeshLongitudinalLength(_surveysRef, _surveysCmp);
            if (ComparisonMeshLongitudinalLength.HasValue)
            {
                double minimumComparisonMeshLength = SeparationFactorCalculations.MinimumComparisonMeshLongitudinalLength;
                if ((_surveysRef.Count >= SeparationFactorCalculations.DenseReferenceSurveyStationCount ||
                    _surveysCmp.Count >= SeparationFactorCalculations.DenseReferenceSurveyStationCount ||
                    ComparisonMeshLongitudinalLength.Value < SeparationFactorCalculations.DenseReferenceComparisonMeshLongitudinalLength) &&
                    SeparationFactorCalculations.DenseReferenceComparisonMeshLongitudinalLength > minimumComparisonMeshLength)
                {
                    minimumComparisonMeshLength = SeparationFactorCalculations.DenseReferenceComparisonMeshLongitudinalLength;
                }

                if (minimumComparisonMeshLength > 0)
                {
                    ComparisonMeshLongitudinalLength = Math.Max(
                        ComparisonMeshLongitudinalLength.Value,
                        minimumComparisonMeshLength);
                }
            }
        }

        public double? ComparisonMeshLongitudinalLength { get; }

        public bool IsValid => ComparisonMeshLongitudinalLength.HasValue && ComparisonMeshLongitudinalLength.Value > 0;

        public double? ComparisonMinimumMD => _comparisonMinimumMD;

        public bool IsReferenceMDIncluded(double md) =>
            !_referenceMinimumMD.HasValue || md >= _referenceMinimumMD.Value;

        public bool TryGetEllipses(double separationFactor, out List<UncertaintyEllipse>? ellipseRef, out List<UncertaintyEllipse>? ellipseCmp)
        {
            double cacheKey = NormalizeScale(separationFactor);
            ellipseRef = GetOrCreateReferenceEllipses(cacheKey);
            ellipseCmp = GetOrCreateComparisonEllipses(cacheKey);
            return ellipseRef != null && ellipseCmp != null;
        }

        public CandidateComparisonIndices GetCandidateComparisonIndices(int ellipseRefIndex)
        {
            lock (_syncLock)
            {
                if (_candidateComparisonIndicesCache.TryGetValue(ellipseRefIndex, out CandidateComparisonIndices? cached))
                {
                    return cached;
                }
            }

            if (!TryGetEllipses(SeparationFactorCalculations.MaxSeparationFactor, out List<UncertaintyEllipse>? ellipseRef, out List<UncertaintyEllipse>? ellipseCmp) ||
                ellipseRef == null ||
                ellipseCmp == null)
            {
                CandidateComparisonIndices empty = new([], []);
                lock (_syncLock)
                {
                    _candidateComparisonIndicesCache[ellipseRefIndex] = empty;
                }
                return empty;
            }

            Bounds? reducedReferenceBounds = GetReducedReferenceBounds(ellipseRef, ellipseRefIndex);
            if (reducedReferenceBounds == null)
            {
                CandidateComparisonIndices empty = new([], []);
                lock (_syncLock)
                {
                    _candidateComparisonIndicesCache[ellipseRefIndex] = empty;
                }
                return empty;
            }

            List<int> pointIndices = [];
            for (int i = 0; i < ellipseCmp.Count; i++)
            {
                var boundingBox = ellipseCmp[i].BoundingBox;
                if (boundingBox != null &&
                    IsEllipseAtOrAfterMinimumMD(ellipseCmp[i], _comparisonMinimumMD) &&
                    BoundsOverlap(reducedReferenceBounds, boundingBox))
                {
                    pointIndices.Add(i);
                }
            }

            List<int> segmentIndices = [];
            for (int i = 0; i < ellipseCmp.Count - 1; i++)
            {
                var firstBoundingBox = ellipseCmp[i].BoundingBox;
                var secondBoundingBox = ellipseCmp[i + 1].BoundingBox;
                if (firstBoundingBox == null || secondBoundingBox == null)
                {
                    continue;
                }
                if (!IsEllipseAtOrAfterMinimumMD(ellipseCmp[i], _comparisonMinimumMD) ||
                    !IsEllipseAtOrAfterMinimumMD(ellipseCmp[i + 1], _comparisonMinimumMD))
                {
                    continue;
                }

                Bounds? joinedBounds = Bounds.Join(firstBoundingBox, secondBoundingBox);
                if (joinedBounds != null && BoundsOverlap(reducedReferenceBounds, joinedBounds))
                {
                    segmentIndices.Add(i);
                }
            }

            CandidateComparisonIndices results = new(pointIndices, segmentIndices);
            lock (_syncLock)
            {
                _candidateComparisonIndicesCache[ellipseRefIndex] = results;
            }
            return results;
        }

        public List<SurveyPoint> GetOrCreateRotatedEllipseVertices(UncertaintyEllipse ellipse)
        {
            lock (_syncLock)
            {
                if (_rotatedEllipseCache.TryGetValue(ellipse, out List<SurveyPoint>? cached))
                {
                    return cached;
                }
            }

            List<SurveyPoint> rotated = SeparationFactorCalculations.RotateEllipse(ellipse);
            lock (_syncLock)
            {
                _rotatedEllipseCache[ellipse] = rotated;
            }
            return rotated;
        }

        public List<LineSegment3D>? GetOrCreateComparisonLineSegments(double separationFactor, int segmentIndex)
        {
            double cacheKey = NormalizeScale(separationFactor);
            var lineSegmentCacheKey = (cacheKey, segmentIndex);
            lock (_syncLock)
            {
                if (_comparisonLineSegmentCache.TryGetValue(lineSegmentCacheKey, out List<LineSegment3D>? cached))
                {
                    return cached;
                }
            }

            List<UncertaintyEllipse>? ellipseCmp = GetOrCreateComparisonEllipses(cacheKey);
            if (ellipseCmp == null ||
                segmentIndex < 0 ||
                segmentIndex >= ellipseCmp.Count - 1 ||
                ellipseCmp[segmentIndex].EllipseVertices == null ||
                ellipseCmp[segmentIndex + 1].EllipseVertices == null)
            {
                lock (_syncLock)
                {
                    _comparisonLineSegmentCache[lineSegmentCacheKey] = null;
                }
                return null;
            }

            List<SurveyPoint> currentVertices = GetOrCreateRotatedEllipseVertices(ellipseCmp[segmentIndex]);
            List<SurveyPoint> nextVertices = GetOrCreateRotatedEllipseVertices(ellipseCmp[segmentIndex + 1]);
            if (currentVertices.Count == 0 || currentVertices.Count != nextVertices.Count)
            {
                lock (_syncLock)
                {
                    _comparisonLineSegmentCache[lineSegmentCacheKey] = null;
                }
                return null;
            }

            List<LineSegment3D> lineSegments = [];
            for (int i = 0; i < currentVertices.Count - 1; i++)
            {
                lineSegments.Add(new LineSegment3D(currentVertices[i], nextVertices[i]));
            }

            lock (_syncLock)
            {
                _comparisonLineSegmentCache[lineSegmentCacheKey] = lineSegments;
            }
            return lineSegments;
        }

        public ReferenceIntersectionGeometry? GetOrCreateReferenceGeometry(
            double separationFactor,
            int referenceEllipseIndex,
            List<UncertaintyEllipse> ellipseRef)
        {
            if (_sharedReferenceCache != null)
            {
                return _sharedReferenceCache.GetOrCreateReferenceGeometry(separationFactor, referenceEllipseIndex, ellipseRef);
            }

            double cacheKey = NormalizeScale(separationFactor);
            var geometryCacheKey = (cacheKey, referenceEllipseIndex);
            lock (_syncLock)
            {
                if (_referenceGeometryCache.TryGetValue(geometryCacheKey, out ReferenceIntersectionGeometry? cached))
                {
                    return cached;
                }
            }

            ReferenceIntersectionGeometry? geometry = SeparationFactorCalculations.CreateReferenceIntersectionGeometry(
                ellipseRef,
                referenceEllipseIndex,
                GetOrCreateRotatedEllipseVertices);
            lock (_syncLock)
            {
                _referenceGeometryCache[geometryCacheKey] = geometry;
            }

            return geometry;
        }

        private List<UncertaintyEllipse>? GetOrCreateReferenceEllipses(double separationFactor)
        {
            if (_sharedReferenceCache != null)
            {
                return _sharedReferenceCache.GetOrCreateReferenceEllipses(separationFactor);
            }

            lock (_syncLock)
            {
                if (_referenceCache.TryGetValue(separationFactor, out List<UncertaintyEllipse>? cached))
                {
                    return cached;
                }
            }

            List<UncertaintyEllipse>? ellipses = CreateScaledReferenceEllipses(separationFactor);
            lock (_syncLock)
            {
                _referenceCache[separationFactor] = ellipses;
            }
            return ellipses;
        }

        private List<UncertaintyEllipse>? GetOrCreateComparisonEllipses(double separationFactor)
        {
            lock (_syncLock)
            {
                if (_comparisonCache.TryGetValue(separationFactor, out List<UncertaintyEllipse>? cached))
                {
                    return cached;
                }
            }

            if (!ComparisonMeshLongitudinalLength.HasValue)
            {
                lock (_syncLock)
                {
                    _comparisonCache[separationFactor] = null;
                }
                return null;
            }

            List<UncertaintyEllipse>? ellipses = CreateScaledComparisonEllipses(separationFactor);
            lock (_syncLock)
            {
                _comparisonCache[separationFactor] = ellipses;
            }
            return ellipses;
        }

        private List<UncertaintyEllipse>? CreateScaledReferenceEllipses(double separationFactor)
        {
            List<UncertaintyEllipse>? zeroScale = GetOrCreateReferenceScaleAnchor(0.0);
            List<UncertaintyEllipse>? unitScale = GetOrCreateReferenceScaleAnchor(1.0);
            return CreateScaledEllipses(zeroScale, unitScale, separationFactor);
        }

        private List<UncertaintyEllipse>? CreateScaledComparisonEllipses(double separationFactor)
        {
            List<UncertaintyEllipse>? zeroScale = GetOrCreateComparisonScaleAnchor(0.0);
            List<UncertaintyEllipse>? unitScale = GetOrCreateComparisonScaleAnchor(1.0);
            return CreateScaledEllipses(zeroScale, unitScale, separationFactor);
        }

        private List<UncertaintyEllipse>? GetOrCreateReferenceScaleAnchor(double separationFactor)
        {
            if (separationFactor == 0.0)
            {
                lock (_syncLock)
                {
                    if (!_referenceZeroScaleCreated)
                    {
                        _referenceZeroScaleEllipses = BuildReferenceEllipses(separationFactor);
                        _referenceZeroScaleCreated = true;
                    }

                    return _referenceZeroScaleEllipses;
                }
            }

            lock (_syncLock)
            {
                if (!_referenceUnitScaleCreated)
                {
                    _referenceUnitScaleEllipses = BuildReferenceEllipses(separationFactor);
                    _referenceUnitScaleCreated = true;
                }

                return _referenceUnitScaleEllipses;
            }
        }

        private List<UncertaintyEllipse>? GetOrCreateComparisonScaleAnchor(double separationFactor)
        {
            if (separationFactor == 0.0)
            {
                lock (_syncLock)
                {
                    if (!_comparisonZeroScaleCreated)
                    {
                        _comparisonZeroScaleEllipses = BuildComparisonEllipses(separationFactor);
                        _comparisonZeroScaleCreated = true;
                    }

                    return _comparisonZeroScaleEllipses;
                }
            }

            lock (_syncLock)
            {
                if (!_comparisonUnitScaleCreated)
                {
                    _comparisonUnitScaleEllipses = BuildComparisonEllipses(separationFactor);
                    _comparisonUnitScaleCreated = true;
                }

                return _comparisonUnitScaleEllipses;
            }
        }

        private List<UncertaintyEllipse>? BuildReferenceEllipses(double separationFactor)
        {
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
            return ok ? ellipses : null;
        }

        private List<UncertaintyEllipse>? BuildComparisonEllipses(double separationFactor)
        {
            if (!ComparisonMeshLongitudinalLength.HasValue)
            {
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
                ComparisonMeshLongitudinalLength.Value,
                out List<UncertaintyEllipse>? ellipses);
            return ok ? ellipses : null;
        }

        private List<UncertaintyEllipse>? CreateScaledEllipses(
            List<UncertaintyEllipse>? zeroScaleEllipses,
            List<UncertaintyEllipse>? unitScaleEllipses,
            double separationFactor)
        {
            if (zeroScaleEllipses == null ||
                unitScaleEllipses == null ||
                zeroScaleEllipses.Count != unitScaleEllipses.Count)
            {
                return null;
            }

            List<UncertaintyEllipse> scaledEllipses = new(unitScaleEllipses.Count);
            for (int i = 0; i < unitScaleEllipses.Count; i++)
            {
                UncertaintyEllipse? scaled = CreateScaledEllipse(zeroScaleEllipses[i], unitScaleEllipses[i], separationFactor);
                if (scaled == null)
                {
                    return null;
                }

                scaledEllipses.Add(scaled);
            }

            return scaledEllipses;
        }

        private static UncertaintyEllipse? CreateScaledEllipse(
            UncertaintyEllipse zeroScaleEllipse,
            UncertaintyEllipse unitScaleEllipse,
            double separationFactor)
        {
            if (zeroScaleEllipse.EllipseRadii == null ||
                unitScaleEllipse.EllipseRadii == null ||
                zeroScaleEllipse.EllipseRadii[0] is not double zeroFirstRadius ||
                unitScaleEllipse.EllipseRadii[0] is not double unitFirstRadius ||
                zeroScaleEllipse.EllipseRadii[1] is not double zeroSecondRadius ||
                unitScaleEllipse.EllipseRadii[1] is not double unitSecondRadius ||
                unitScaleEllipse.EllipseCenter == null)
            {
                return null;
            }

            Vector2D radii = new();
            radii[0] = ScaleValue(zeroFirstRadius, unitFirstRadius, separationFactor);
            radii[1] = ScaleValue(zeroSecondRadius, unitSecondRadius, separationFactor);
            UncertaintyEllipse scaledEllipse = new()
            {
                EllipseCenter = unitScaleEllipse.EllipseCenter,
                EllipseOrientationAngle = unitScaleEllipse.EllipseOrientationAngle,
                EllipseRadii = radii,
            };

            if (!TryCreateScaledVertices(zeroScaleEllipse, unitScaleEllipse, separationFactor, scaledEllipse))
            {
                return null;
            }

            return scaledEllipse;
        }

        private static bool TryCreateScaledVertices(
            UncertaintyEllipse zeroScaleEllipse,
            UncertaintyEllipse unitScaleEllipse,
            double separationFactor,
            UncertaintyEllipse scaledEllipse)
        {
            if (zeroScaleEllipse.EllipseVertices == null ||
                unitScaleEllipse.EllipseVertices == null ||
                zeroScaleEllipse.EllipseVertices.Count != unitScaleEllipse.EllipseVertices.Count ||
                !TryGetCoordinates(scaledEllipse.EllipseCenter, out double centerX, out double centerY, out double centerZ))
            {
                return scaledEllipse.DiscretizeEllipse(unitScaleEllipse.EllipseVertices?.Count - 1 ?? 0);
            }

            scaledEllipse.BoundingBox = new(centerX, centerY, centerZ, centerX, centerY, centerZ);
            scaledEllipse.EllipseVertices = new(unitScaleEllipse.EllipseVertices.Count);
            for (int i = 0; i < unitScaleEllipse.EllipseVertices.Count; i++)
            {
                SurveyPoint zeroScaleVertex = zeroScaleEllipse.EllipseVertices[i];
                SurveyPoint unitScaleVertex = unitScaleEllipse.EllipseVertices[i];
                if (!TryGetCoordinates(zeroScaleVertex, out double zeroX, out double zeroY, out double zeroZ) ||
                    !TryGetCoordinates(unitScaleVertex, out double unitX, out double unitY, out double unitZ))
                {
                    return false;
                }

                SurveyPoint scaledPoint = new()
                {
                    X = ScaleValue(zeroX, unitX, separationFactor),
                    Y = ScaleValue(zeroY, unitY, separationFactor),
                    Z = ScaleValue(zeroZ, unitZ, separationFactor),
                };
                scaledEllipse.EllipseVertices.Add(scaledPoint);
                UpdateBoundingBox(scaledEllipse.BoundingBox, scaledPoint);
            }

            return true;
        }

        private static double ScaleValue(double zeroScaleValue, double unitScaleValue, double separationFactor)
        {
            return zeroScaleValue + separationFactor * (unitScaleValue - zeroScaleValue);
        }

        private static void UpdateBoundingBox(BoundingBox3D boundingBox, SurveyPoint point)
        {
            if (!TryGetCoordinates(point, out double x, out double y, out double z))
            {
                return;
            }

            if (x < boundingBox.MinX) boundingBox.MinX = x;
            if (x > boundingBox.MaxX) boundingBox.MaxX = x;
            if (y < boundingBox.MinY) boundingBox.MinY = y;
            if (y > boundingBox.MaxY) boundingBox.MaxY = y;
            if (z < boundingBox.MinZ) boundingBox.MinZ = z;
            if (z > boundingBox.MaxZ) boundingBox.MaxZ = z;
        }

        private static double NormalizeScale(double separationFactor)
        {
            return Math.Round(separationFactor, 6, MidpointRounding.AwayFromZero);
        }

        private static double? NormalizeMinimumMD(double? minimumMD)
        {
            return minimumMD.HasValue && Numeric.IsDefined(minimumMD.Value)
                ? minimumMD.Value
                : null;
        }

        private static bool IsEllipseAtOrAfterMinimumMD(
            UncertaintyEllipse ellipse,
            double? minimumMD)
        {
            return !minimumMD.HasValue ||
                (TryGetCoordinate(ellipse.EllipseCenter, static point => point.MD, out double md) &&
                 md >= minimumMD.Value);
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

        private static bool BoundsOverlap(Bounds left, Bounds right)
        {
            return left.MinX <= right.MaxX &&
                left.MaxX >= right.MinX &&
                left.MinY <= right.MaxY &&
                left.MaxY >= right.MinY &&
                left.MinZ <= right.MaxZ &&
                left.MaxZ >= right.MinZ;
        }

        private static bool BoundsOverlap(Bounds left, BoundingBox3D right)
        {
            return left.MinX <= right.MaxX &&
                left.MaxX >= right.MinX &&
                left.MinY <= right.MaxY &&
                left.MaxY >= right.MinY &&
                left.MinZ <= right.MaxZ &&
                left.MaxZ >= right.MinZ;
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

        private static double? GetPairComparisonMeshLongitudinalLength(
            List<SurveyStation> surveysRef,
            List<SurveyStation> surveysCmp)
        {
            double? minimumReferenceMD = MinimumMDBetweenSurveyStations(surveysRef);
            double? minimumComparisonMD = MinimumMDBetweenSurveyStations(surveysCmp);
            if (!minimumReferenceMD.HasValue || !minimumComparisonMD.HasValue)
            {
                return null;
            }

            return Math.Max(minimumReferenceMD.Value, minimumComparisonMD.Value) /
                SeparationFactorCalculations.MinNumberInterpolations;
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

    public sealed class ReferenceIntersectionGeometry
    {
        public ReferenceIntersectionGeometry(
            Bounds bounds,
            List<Plane3D> planesAbove,
            List<Plane3D> planesBelow,
            List<Triangle3D> trianglesAbove,
            List<Triangle3D> trianglesBelow)
        {
            Bounds = bounds;
            PlanesAbove = planesAbove;
            PlanesBelow = planesBelow;
            TrianglesAbove = trianglesAbove;
            TrianglesBelow = trianglesBelow;
        }

        public Bounds Bounds { get; }

        public List<Plane3D> PlanesAbove { get; }

        public List<Plane3D> PlanesBelow { get; }

        public List<Triangle3D> TrianglesAbove { get; }

        public List<Triangle3D> TrianglesBelow { get; }
    }
}
