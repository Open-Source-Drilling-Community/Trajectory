using System;
using System.Collections.Generic;
using System.Linq;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Math;
using OSDC.DotnetLibraries.General.Octree;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace NORCE.Drilling.GlobalAntiCollision
{
    public static class SeparationFactorCalculations
    {
        #region Code copied from SeparationFactorCalculations in TrajectoryUncertaintyDisplayApp
        /// <summary>
        /// 
        /// </summary>
        public static double MaxSeparationFactor { get; set; } = 5.0;
        /// <summary>
        /// 
        /// </summary>
        public static int MinNumberInterpolations { get; set; } = 3;

        public static List<SeparationFactorPoint> CalculateSeparationFactor(List<SurveyStation>? surveysRef, List<SurveyStation>? surveysCmp, int k, double confidenceFactor, UncertaintyEnvelope.ErrorModelType errorModelTypeRef = UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt, UncertaintyEnvelope.ErrorModelType errorModelTypeCmp = UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt)
        {
            if (surveysRef == null || surveysCmp == null || surveysRef.Count == 0 || surveysCmp.Count == 0)
            {
                return [];
            }

            // The number of ellipses for the comparison well should be higher than the number for the reference. This is why we initially used the numberOfInterpolationsMultiplier. But the new approach is to extract the minimum MD distance between
            // ellipseRef survey stations and use that when interpolating ellipseCmp
            // But in some cases at Ullrigg, (e.g. U2 vs U1), the density of survey points is quite different. Therefore we get different results depending on what we choose as the reference well.
            // If something is changed here, please also verify that the start index sent to the Intersect method remains correct. The code should also be merged with the original code in ProximitySafetyFactorView
            int numberOfInterpolationsMultiplier = 3;
            // Try to account for variation in MD between survey stations
            double? minRef = MinimumMDBetweenSurveyStations(surveysRef);
            if (minRef != null)
            {
                minRef = minRef / MinNumberInterpolations;
                double? maxCmp = MaximumMDBetweenSurveyStations(surveysCmp);
                if (maxCmp != null)
                {
                    numberOfInterpolationsMultiplier = System.Math.Max(numberOfInterpolationsMultiplier, (int)System.Math.Ceiling(maxCmp.Value / minRef.Value) + 1);
                }
                else
                {
                    return [];
                }
            }
            else
            {
                return [];
            }

            UncertaintyEnvelope uncertaintyEnvelopeRef = new()
            {
                ErrorModel = errorModelTypeRef,
                SurveyStationList = surveysRef,
            };
            UncertaintyEnvelope uncertaintyEnvelopeCmp = new()
            {
                ErrorModel = errorModelTypeCmp,
                SurveyStationList = surveysCmp,
            };

            List<SeparationFactorPoint> separationFactors = [];
            // Since the Intersect loops over ellipseRef and k is an index on the survey stations, we need to use k * MinNumberInterpolations as start index
            for (int i = 0; i < MinNumberInterpolations; i++)
            {
                double minSeparationFactor = 0.01;
                var maxSeparationFactor = MaxSeparationFactor;

                uncertaintyEnvelopeRef.ConfidenceFactor = confidenceFactor;
                uncertaintyEnvelopeRef.ScalingFactor = maxSeparationFactor;
                // Should we set this:
                //uncertaintyEnvelopeCmp.MeshLongitudinalCount = MinNumberInterpolations;
                bool okRef = uncertaintyEnvelopeRef.Calculate();

                uncertaintyEnvelopeCmp.ConfidenceFactor = confidenceFactor;
                uncertaintyEnvelopeCmp.ScalingFactor = maxSeparationFactor;
                // Should we set this:
                //uncertaintyEnvelopeCmp.MeshLongitudinalCount = numberOfInterpolationsMultiplier * MinNumberInterpolations;
                uncertaintyEnvelopeCmp.MeshLongitudinalLength = minRef.Value;
                bool okCmp = uncertaintyEnvelopeCmp.Calculate();

                List<UncertaintyEllipse>? ellipseRef = okRef ? uncertaintyEnvelopeRef.MeshedEllipseList : null; //surveysRef.GetPlainUncertaintyEnvelope(confidenceFactor, maxSeparationFactor, boreholeRadiusRefList, MinNumberInterpolations);
                List<UncertaintyEllipse>? ellipseCmp = okCmp ? uncertaintyEnvelopeCmp.MeshedEllipseList : null; //surveysCmp.GetPlainUncertaintyEnvelope(confidenceFactor, maxSeparationFactor, boreholeRadiusCmpList, numberOfInterpolationsMultiplier * MinNumberInterpolations, minRef);
                if (ellipseRef != null && ellipseCmp != null)
                {
                    var ellipseRefIndex = k * MinNumberInterpolations + i;
                    if (ellipseRefIndex >= ellipseRef.Count)
                    {
                        break;
                    }
                    if (!TryGetMD(ellipseRef[ellipseRefIndex], out double refMD))
                    {
                        continue;
                    }
                    if (!Intersect(ellipseRef, ellipseCmp, ellipseRefIndex, out double cmpMD))
                    {
                        // Undefined numbers may create problems for json serialization
                        cmpMD = -1.0;
                        separationFactors.Add(new SeparationFactorPoint(refMD, cmpMD, maxSeparationFactor));
                    }
                    else
                    {
                        uncertaintyEnvelopeRef.ScalingFactor = minSeparationFactor;
                        uncertaintyEnvelopeCmp.ScalingFactor = minSeparationFactor;
                        okRef = uncertaintyEnvelopeRef.Calculate();
                        okCmp = uncertaintyEnvelopeCmp.Calculate();
                        ellipseRef = okRef ? uncertaintyEnvelopeRef.MeshedEllipseList : null; //surveysRef.GetPlainUncertaintyEnvelope(confidenceFactor, minSeparationFactor, boreholeRadiusRefList, MinNumberInterpolations);
                        ellipseCmp = okCmp ? uncertaintyEnvelopeCmp.MeshedEllipseList : null; //surveysCmp.GetPlainUncertaintyEnvelope(confidenceFactor, minSeparationFactor, boreholeRadiusCmpList, numberOfInterpolationsMultiplier * MinNumberInterpolations, minRef);
                        if (ellipseRef != null && ellipseCmp != null)
                        {
                            // Since the Intersect loops over ellipseRef and k is an index on the survey stations, we need to use k * MinNumberInterpolations as start index
                            if (Intersect(ellipseRef, ellipseCmp, ellipseRefIndex, out cmpMD))
                            {
                                if (Numeric.IsUndefined(cmpMD))
                                {
                                    if (!TryGetMD(surveysCmp[0], out cmpMD))
                                    {
                                        cmpMD = -1.0;
                                    }
                                }
                                separationFactors.Add(new SeparationFactorPoint(refMD, cmpMD, minSeparationFactor));
                            }
                            else
                            {
                                // dichotomy
                                int cc = 0;
                                do
                                {
                                    double separationFactor = 0.5 * (minSeparationFactor + maxSeparationFactor);
                                    uncertaintyEnvelopeRef.ScalingFactor = separationFactor;
                                    uncertaintyEnvelopeCmp.ScalingFactor = separationFactor;
                                    okRef = uncertaintyEnvelopeRef.Calculate();
                                    okCmp = uncertaintyEnvelopeCmp.Calculate();
                                    ellipseRef = okRef ? uncertaintyEnvelopeRef.MeshedEllipseList : null; //surveysRef.GetPlainUncertaintyEnvelope(confidenceFactor, separationFactor, boreholeRadiusRefList, MinNumberInterpolations);
                                    ellipseCmp = okCmp ? uncertaintyEnvelopeCmp.MeshedEllipseList : null; //surveysCmp.GetPlainUncertaintyEnvelope(confidenceFactor, separationFactor, boreholeRadiusCmpList, numberOfInterpolationsMultiplier * MinNumberInterpolations, minRef);
                                    if (ellipseRef != null && ellipseCmp != null)
                                    {
                                        double cMD;
                                        // Since the Intersect loops over ellipseRef and k is an index on the survey stations, we need to use k * MinNumberInterpolations as start index
                                        if (Intersect(ellipseRef, ellipseCmp, ellipseRefIndex, out cMD))
                                        {
                                            maxSeparationFactor = separationFactor;
                                        }
                                        else
                                        {
                                            minSeparationFactor = separationFactor;
                                        }
                                        if (Numeric.IsDefined(cMD))
                                        {
                                            cmpMD = cMD;
                                        }
                                    }
                                    else
                                    {
                                        // If we cannot calculate the uncertainty envelope, we break the loop
                                        break;
                                    }
                                } while (System.Math.Abs(maxSeparationFactor - minSeparationFactor) > 0.01 && cc++ < 50);
                                if (Numeric.IsUndefined(cmpMD))
                                {
                                    if (!TryGetMD(surveysCmp[0], out cmpMD))
                                    {
                                        cmpMD = -1.0;
                                    }
                                }
                                separationFactors.Add(new SeparationFactorPoint(refMD, cmpMD, 0.5 * (minSeparationFactor + maxSeparationFactor)));
                            }
                        }
                    }
                }
            }
            return separationFactors;
        }

        /// <summary>
        /// Find the maximum MD-delta between two survey's
        /// </summary>
        private static double? MaximumMDBetweenSurveyStations(List<SurveyStation>? listOfSurveyStations)
        {
            double? maxDeltaMD = null;
            if (listOfSurveyStations != null && listOfSurveyStations.Count > 1)
            {
                for (int i = 0; i < listOfSurveyStations.Count - 1; i++)
                {
                    var deltaMD = listOfSurveyStations[i + 1].MD - listOfSurveyStations[i].MD; //Originally MdWGS84
                    if (Numeric.IsDefined(deltaMD) && (maxDeltaMD == null || Numeric.GT(deltaMD, maxDeltaMD)))
                    {
                        maxDeltaMD = deltaMD;
                    }
                }
            }
            return maxDeltaMD;
        }

        /// <summary>
        /// Find the minimum MD-delta between two survey's
        /// </summary>
        private static double? MinimumMDBetweenSurveyStations(List<SurveyStation>? listOfSurveyStations)
        {
            double? minDeltaMD = null;
            if (listOfSurveyStations != null && listOfSurveyStations.Count > 1)
            {
                for (int i = 0; i < listOfSurveyStations.Count - 1; i++)
                {
                    var deltaMD = listOfSurveyStations[i + 1].MD - listOfSurveyStations[i].MD; //Originally MdWGS84
                    if (Numeric.IsDefined(deltaMD) && (minDeltaMD == null || Numeric.LT(deltaMD, minDeltaMD)))
                    {
                        minDeltaMD = deltaMD;
                    }
                }
            }
            return minDeltaMD;
        }

        private static bool Intersect(List<UncertaintyEllipse> ellipseRef, List<UncertaintyEllipse> ellipseCmp, int k, out double cmpMD)
        {
            cmpMD = Numeric.UNDEF_DOUBLE;
            if (k >= 0 && k < ellipseRef.Count - 1 && ellipseRef.Count > 2)
            {
                List<UncertaintyEllipse> reducedEllipseRef = new List<UncertaintyEllipse>();
                if (k == 0)
                {
                    reducedEllipseRef.Add(ellipseRef[0]);
                    reducedEllipseRef.Add(ellipseRef[1]);
                    reducedEllipseRef.Add(ellipseRef[2]);
                }
                else if (k == ellipseRef.Count - 1)
                {
                    reducedEllipseRef.Add(ellipseRef[ellipseRef.Count - 3]);
                    reducedEllipseRef.Add(ellipseRef[ellipseRef.Count - 2]);
                    reducedEllipseRef.Add(ellipseRef[ellipseRef.Count - 1]);
                }
                else
                {
                    reducedEllipseRef.Add(ellipseRef[k - 1]);
                    reducedEllipseRef.Add(ellipseRef[k]);
                    reducedEllipseRef.Add(ellipseRef[k + 1]);
                }
                Bounds reducedEllipseRefBounds = GetBounds(reducedEllipseRef);
                if (reducedEllipseRefBounds.MaxX >= reducedEllipseRefBounds.MinX &&
                    reducedEllipseRefBounds.MaxY >= reducedEllipseRefBounds.MinY &&
                    reducedEllipseRefBounds.MaxZ >= reducedEllipseRefBounds.MinZ)
                {
                    // We need to split into two set of closed volumes, otherwise some points which are actually inside the volume may fall outside.
                    // A point in the upper volume may be outside the side-planes of the lower volume, therefore we treat them separately
                    Pair<List<Plane3D>?, List<Triangle3D?>?> planesAndTrianglesAbove = ExtractBoundingPlanes(reducedEllipseRef.GetRange(0, 2));
                    Pair<List<Plane3D>?, List<Triangle3D?>?> planesAndTrianglesBelow = ExtractBoundingPlanes(reducedEllipseRef.GetRange(1, 2));
                    List<Plane3D>? planesAbove = planesAndTrianglesAbove.Left;
                    List<Plane3D>? planesBelow = planesAndTrianglesBelow.Left;
                    if (planesAbove != null && planesBelow != null)
                    {
                        #region Check if any of the points on the ellipseCmp ellipses are inside the volumes defined by the planes
                        for (int i = 0; i < ellipseCmp.Count; i++)
                        {
                            if (TryGetPoint3D(ellipseCmp[i].EllipseCenter, out Point3D center) && reducedEllipseRefBounds.Contains(center))
                            {
                                if (IsInside(center, planesAbove) || IsInside(center, planesBelow))
                                {
                                    if (TryGetMD(ellipseCmp[i], out double ellipseCmpMd))
                                    {
                                        cmpMD = ellipseCmpMd;
                                    }
                                    return true;
                                }
                            }

                            List<SurveyPoint>? ellipseVertices = ellipseCmp[i].EllipseVertices;
                            if (ellipseVertices != null)
                            {
                                for (int j = 0; j < ellipseVertices.Count; j++)
                                {
                                    if (reducedEllipseRefBounds.Contains(ellipseVertices[j]))
                                    {
                                        if (IsInside(ellipseVertices[j], planesAbove) || IsInside(ellipseVertices[j], planesBelow))
                                        {
                                            if (TryGetMD(ellipseCmp[i], out double ellipseCmpMd))
                                            {
                                                cmpMD = ellipseCmpMd;
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Check if any of the line segments between two ellipseCmp ellipses intersects any of the triangles that define the envelope of ellipseRef
                        List<Triangle3D?>? trianglesAbove = planesAndTrianglesAbove.Right;
                        List<Triangle3D?>? trianglesBelow = planesAndTrianglesBelow.Right;
                        if (trianglesAbove != null && trianglesBelow != null)
                        {
                            List<LineSegment3D> lineSegments = new List<LineSegment3D>();
                            for (int i = 0; i < ellipseCmp.Count - 1; i++)
                            {
                                if (ellipseCmp[i].EllipseVertices != null && ellipseCmp[i + 1].EllipseVertices != null)
                                {
                                    Bounds reducedEllipseCmpBounds = GetBounds(ellipseCmp.GetRange(i, 2));
                                    if (reducedEllipseRefBounds.Intersects(reducedEllipseCmpBounds))
                                    {
                                        // Rotate the cmp ellipses
                                        List<SurveyPoint> ellipseCmpCoordinatesRotated = RotateEllipse(ellipseCmp[i]);
                                        List<SurveyPoint> ellipseCmpCoordinatesNextRotated = RotateEllipse(ellipseCmp[i + 1]);
                                        lineSegments.Clear();
                                        // Avoid the duplicated points at the end of the lists
                                        for (int j = 0; j < ellipseCmpCoordinatesRotated.Count - 1; j++)
                                        {
                                            lineSegments.Add(new LineSegment3D(ellipseCmpCoordinatesRotated[j], ellipseCmpCoordinatesNextRotated[j]));
                                        }
                                        foreach (LineSegment3D lineSegment in lineSegments)
                                        {
                                            foreach (Triangle3D? triangle in trianglesAbove)
                                            {
                                                if (triangle != null && lineSegment.IntersectsTriangle(triangle))
                                                {
                                                    if (TryGetMD(ellipseCmp[i], out double ellipseCmpMd))
                                                    {
                                                        cmpMD = ellipseCmpMd;
                                                    }
                                                    return true;
                                                }
                                            }
                                            foreach (Triangle3D? triangle in trianglesBelow)
                                            {
                                                if (triangle != null && lineSegment.IntersectsTriangle(triangle))
                                                {
                                                    if (TryGetMD(ellipseCmp[i], out double ellipseCmpMd))
                                                    {
                                                        cmpMD = ellipseCmpMd;
                                                    }
                                                    return true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Return the bounding box containing the set of ellipses
        /// </summary>
        /// <param name="ellipses"></param>
        /// <returns></returns>
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

        private static Pair<List<Plane3D>?, List<Triangle3D?>?> ExtractBoundingPlanes(List<UncertaintyEllipse> ellipses)
        {
            if (ellipses != null && ellipses.Count >= 2)
            {
                List<Plane3D> planes = new List<Plane3D>();
                List<Triangle3D?> triangles = new List<Triangle3D?>();
                // Top and bottom plane
                int last = ellipses.Count - 1;
                List<SurveyPoint>? firstVertices = ellipses[0].EllipseVertices;
                List<SurveyPoint>? lastVertices = ellipses[last].EllipseVertices;
                if (firstVertices != null && firstVertices.Count > 2 &&
                    lastVertices != null && lastVertices.Count > 2 &&
                    firstVertices.Count == lastVertices.Count)
                {
                    int idx1 = firstVertices.Count / 3;
                    int idx2 = 2 * firstVertices.Count / 3;
                    planes.Add(new Plane3D(firstVertices[0], firstVertices[idx2], firstVertices[idx1]));
                    planes.Add(new Plane3D(lastVertices[0], lastVertices[idx1], lastVertices[idx2]));
                    // We don't extract the triangles for the top and bottom plane since these are not part of the envelope. Add null to keep the planes and triangles at the same length
                    triangles.Add(null);
                    triangles.Add(null);
                }
                // Sides planes - here we try to match the coordinates of the two ellipses such that corresponding indexes refer to points which are 'aligned'. We also need to account for reverse indexing and inclination above 90 deg.
                for (int i = 0; i < ellipses.Count - 1; i++)
                {
                    List<SurveyPoint>? currentVertices = ellipses[i].EllipseVertices;
                    List<SurveyPoint>? nextVertices = ellipses[i + 1].EllipseVertices;
                    if (currentVertices != null &&
                        nextVertices != null &&
                        currentVertices.Count > 0 &&
                        currentVertices.Count == nextVertices.Count)
                    {
                        // Rotate the ellipses
                        List<SurveyPoint> ellipseCoordinatesRotated = RotateEllipse(ellipses[i]);
                        List<SurveyPoint> ellipseCoordinatesNextRotated = RotateEllipse(ellipses[i + 1]);

                        // Test code - move ellipses to origo
                        //List<Point3D> testReversed = new List<Point3D>();
                        //foreach (Point3D p in ellipseCoordinatesRotated) { testReversed.Add(new Point3D(p.X - ellipses[i].X, p.Y - ellipses[i].Y, p.Z - ellipses[i].Z)); }
                        //List<Point3D> testReversedNext = new List<Point3D>();
                        //foreach (Point3D p in ellipseCoordinatesNextRotated) { testReversedNext.Add(new Point3D(p.X - ellipses[i + 1].X, p.Y - ellipses[i + 1].Y, p.Z - ellipses[i + 1].Z)); }

                        // Add planes - The first and last index of the list contains the same point to connect the ellipse
                        for (int j = 0; j < ellipseCoordinatesRotated.Count - 1; j++)
                        {
                            Triangle3D triangle1 = new Triangle3D(ellipseCoordinatesRotated[j], ellipseCoordinatesRotated[j + 1], ellipseCoordinatesNextRotated[j]);
                            Triangle3D triangle2 = new Triangle3D(ellipseCoordinatesNextRotated[j + 1], ellipseCoordinatesNextRotated[j], ellipseCoordinatesRotated[j + 1]);
                            planes.Add(new Plane3D(triangle1.Vertex1, triangle1.Vertex2, triangle1.Vertex3));
                            planes.Add(new Plane3D(triangle2.Vertex1, triangle2.Vertex2, triangle2.Vertex3));
                            triangles.Add(triangle1);
                            triangles.Add(triangle2);
                        }
                    }
                }
                return new Pair<List<Plane3D>?, List<Triangle3D?>?>(planes, triangles);
            }
            else
            {
                return new Pair<List<Plane3D>?, List<Triangle3D?>?>(null, null);
            }
        }

        private static List<SurveyPoint> RotateEllipse(UncertaintyEllipse ellipse)
        {
            // Rotate the ellipse coordinates
            List<SurveyPoint> ellipseCoordinatesRotated;
            List<SurveyPoint>? ellipseCoordinates = ellipse.EllipseVertices;
            if (ellipseCoordinates == null || ellipseCoordinates.Count == 0)
            {
                return [];
            }
            int startIndex = FindIndex(ellipse, out bool reverse);
            if (startIndex > 0)
            {
                ellipseCoordinatesRotated = new List<SurveyPoint>();
                for (int j = 0; j < ellipseCoordinates.Count; j++)
                {
                    int indexRotated = (startIndex + j) % ellipseCoordinates.Count;
                    // Avoid the duplicated point at the last index
                    if (indexRotated != ellipseCoordinates.Count - 1)
                    {
                        ellipseCoordinatesRotated.Add(ellipseCoordinates[indexRotated]);
                    }
                }
                // Duplicate the first point to complete the ellipse
                ellipseCoordinatesRotated.Add(ellipseCoordinatesRotated[0]);
            }
            else
            {
                ellipseCoordinatesRotated = ellipseCoordinates;
            }

            // Reverse ellipses which use reverse indexing
            if (reverse)
            {
                ellipseCoordinatesRotated.Reverse();
            }
            return ellipseCoordinatesRotated;
        }

        private static int FindIndex(UncertaintyEllipse ellipse, out bool reverse)
        {
            List<SurveyPoint>? ellipseCoordinates = ellipse.EllipseVertices;
            if (ellipseCoordinates == null || ellipseCoordinates.Count == 0 || !TryGetCoordinate(ellipse.EllipseCenter, static point => point.X, out double centerX))
            {
                reverse = false;
                return -1;
            }
            int signNegPosIndex = -1;
            int signPosNegIndex = -1;

            // Check for single point (no uncertainty) - select 0
            bool allXEqual = true;
            bool allYEqual = true;
            bool allZEqual = true;
            foreach (SurveyPoint p in ellipseCoordinates)
            {
                if (p.X != ellipseCoordinates[0].X)
                {
                    allXEqual = false;
                }
                if (p.Y != ellipseCoordinates[0].Y)
                {
                    allYEqual = false;
                }
                if (p.Z != ellipseCoordinates[0].Z)
                {
                    allZEqual = false;
                }
            }
            bool allEqual = allXEqual && allYEqual && allZEqual;
            if (allEqual)
            {
                // It doesn't matter where we start
                reverse = false;
                return 0;
            }

            if (allXEqual)
            {
                // Select MaxY
                int indexMaxY = FindMaxCoordinateIndex(ellipseCoordinates, static point => point.Y);
                if (indexMaxY >= 0 && indexMaxY < ellipseCoordinates.Count - 1)
                {
                    if (TryGetCoordinate(ellipseCoordinates[indexMaxY], static point => point.Z, out double currentZ) &&
                        TryGetCoordinate(ellipseCoordinates[indexMaxY + 1], static point => point.Z, out double nextZ) &&
                        currentZ < nextZ)
                    {
                        reverse = true;
                    }
                    else if (TryGetCoordinate(ellipseCoordinates[indexMaxY], static point => point.Z, out currentZ) &&
                             TryGetCoordinate(ellipseCoordinates[indexMaxY + 1], static point => point.Z, out nextZ) &&
                             currentZ > nextZ)
                    {
                        reverse = false;
                    }
                    else
                    {
                        reverse = false;
                    }
                }
                else
                {
                    reverse = false;
                }
                return indexMaxY;
            }
            if (allYEqual)
            {
                // Select MaxZ
                int indexMaxZ = FindMaxCoordinateIndex(ellipseCoordinates, static point => point.Z);
                if (indexMaxZ >= 0 && indexMaxZ < ellipseCoordinates.Count - 1)
                {
                    if (TryGetCoordinate(ellipseCoordinates[indexMaxZ], static point => point.X, out double currentX) &&
                        TryGetCoordinate(ellipseCoordinates[indexMaxZ + 1], static point => point.X, out double nextX) &&
                        currentX < nextX)
                    {
                        reverse = true;
                    }
                    else if (TryGetCoordinate(ellipseCoordinates[indexMaxZ], static point => point.X, out currentX) &&
                             TryGetCoordinate(ellipseCoordinates[indexMaxZ + 1], static point => point.X, out nextX) &&
                             currentX > nextX)
                    {
                        reverse = false;
                    }
                    else
                    {
                        reverse = false;
                    }
                }
                else
                {
                    reverse = false;
                }
                return indexMaxZ;
            }

            // Search for the change of sign in X
            for (int i = 0; i < ellipseCoordinates.Count - 1; i++)
            {
                if (TryGetCoordinate(ellipseCoordinates[i], static point => point.X, out double currentXValue) &&
                    TryGetCoordinate(ellipseCoordinates[i + 1], static point => point.X, out double nextXValue) &&
                    ((currentXValue - centerX < 0 && nextXValue - centerX > 0) || (currentXValue - centerX == 0 && nextXValue - centerX > 0)))
                {
                    signNegPosIndex = i + 1;
                }
                if (TryGetCoordinate(ellipseCoordinates[i], static point => point.X, out currentXValue) &&
                    TryGetCoordinate(ellipseCoordinates[i + 1], static point => point.X, out nextXValue) &&
                    ((currentXValue - centerX > 0 && nextXValue - centerX < 0) || (currentXValue - centerX > 0 && nextXValue - centerX == 0)))
                {
                    signPosNegIndex = i;
                }
            }
            int index = -1;
            if (signNegPosIndex >= 0 && signPosNegIndex >= 0)
            {
                if (TryGetCoordinate(ellipse.EllipseCenter, static point => point.Inclination, out double inclination) && inclination < Numeric.PI / 2.0)
                {
                    if (TryCompareCoordinate(ellipseCoordinates[signNegPosIndex], ellipseCoordinates[signPosNegIndex], static point => point.Y, out int comparison) && comparison > 0)
                    {
                        index = signNegPosIndex;
                        reverse = true;
                    }
                    else if (TryCompareCoordinate(ellipseCoordinates[signPosNegIndex], ellipseCoordinates[signNegPosIndex], static point => point.Y, out comparison) && comparison > 0)
                    {
                        index = signPosNegIndex;
                        reverse = false;
                    }
                    else
                    {
                        if (TryCompareCoordinate(ellipseCoordinates[signNegPosIndex], ellipseCoordinates[signPosNegIndex], static point => point.Z, out comparison) && comparison > 0)
                        {
                            index = signNegPosIndex;
                            reverse = true;
                        }
                        else if (TryCompareCoordinate(ellipseCoordinates[signPosNegIndex], ellipseCoordinates[signNegPosIndex], static point => point.Z, out comparison) && comparison > 0)
                        {
                            index = signPosNegIndex;
                            reverse = false;
                        }
                        else
                        {
                            reverse = false;
                        }
                    }
                }
                else if (TryGetCoordinate(ellipse.EllipseCenter, static point => point.Inclination, out inclination) && inclination > Numeric.PI / 2.0)
                {
                    if (TryCompareCoordinate(ellipseCoordinates[signNegPosIndex], ellipseCoordinates[signPosNegIndex], static point => point.Y, out int comparison) && comparison < 0)
                    {
                        index = signNegPosIndex;
                        reverse = true;
                    }
                    else if (TryCompareCoordinate(ellipseCoordinates[signPosNegIndex], ellipseCoordinates[signNegPosIndex], static point => point.Y, out comparison) && comparison < 0)
                    {
                        index = signPosNegIndex;
                        reverse = false;
                    }
                    else
                    {
                        if (TryCompareCoordinate(ellipseCoordinates[signNegPosIndex], ellipseCoordinates[signPosNegIndex], static point => point.Z, out comparison) && comparison > 0)
                        {
                            index = signNegPosIndex;
                            reverse = true;
                        }
                        else if (TryCompareCoordinate(ellipseCoordinates[signPosNegIndex], ellipseCoordinates[signNegPosIndex], static point => point.Z, out comparison) && comparison > 0)
                        {
                            index = signPosNegIndex;
                            reverse = false;
                        }
                        else
                        {
                            reverse = false;
                        }
                    }
                }
                else
                {
                    if (TryCompareCoordinate(ellipseCoordinates[signNegPosIndex], ellipseCoordinates[signPosNegIndex], static point => point.Z, out int comparison) && comparison > 0)
                    {
                        index = signNegPosIndex;
                        reverse = true;
                    }
                    else if (TryCompareCoordinate(ellipseCoordinates[signPosNegIndex], ellipseCoordinates[signNegPosIndex], static point => point.Z, out comparison) && comparison > 0)
                    {
                        index = signPosNegIndex;
                        reverse = false;
                    }
                    else
                    {
                        reverse = false;
                    }
                }
            }
            else
            {
                reverse = false;
            }
            return index;
        }

        /// <summary>
        /// test if a point is inside a convex volume defined by a list of planes. Only works with convex volumes
        /// </summary>
        /// <param name="point"></param>
        /// <param name="planes"></param>
        /// <returns></returns>
        private static bool IsInside(Point3D point, List<Plane3D> planes)
        {
            foreach (Plane3D plane in planes)
            {
                if (!plane.IsInside(point))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TryGetMD(UncertaintyEllipse ellipse, out double md)
        {
            return TryGetCoordinate(ellipse.EllipseCenter, static point => point.MD, out md);
        }

        private static bool TryGetMD(SurveyStation surveyStation, out double md)
        {
            return TryGetCoordinate(surveyStation, static point => point.MD, out md);
        }

        private static bool TryGetPoint3D(SurveyPoint? surveyPoint, out Point3D point3D)
        {
            if (TryGetCoordinates(surveyPoint, out double x, out double y, out double z))
            {
                point3D = new Point3D(x, y, z);
                return true;
            }

            point3D = null!;
            return false;
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

        private static int FindMaxCoordinateIndex(List<SurveyPoint> points, Func<SurveyPoint, double?> selector)
        {
            int index = -1;
            double maxValue = Numeric.MIN_DOUBLE;
            for (int i = 0; i < points.Count; i++)
            {
                if (TryGetCoordinate(points[i], selector, out double candidate) && (index < 0 || candidate > maxValue))
                {
                    index = i;
                    maxValue = candidate;
                }
            }

            return index;
        }

        private static bool TryCompareCoordinate(SurveyPoint left, SurveyPoint right, Func<SurveyPoint, double?> selector, out int comparison)
        {
            comparison = 0;
            if (!TryGetCoordinate(left, selector, out double leftValue) || !TryGetCoordinate(right, selector, out double rightValue))
            {
                return false;
            }

            comparison = leftValue.CompareTo(rightValue);
            return true;
        }

        #endregion

    }
}
