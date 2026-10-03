using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSDC.Drilling.Trajectory.Model
{
    public static class TrajectoryExtrapolationCalculator
    {
        private const int ClosestPointIterations = 48;

        public static bool Calculate(
            TrajectoryExtrapolationCase calculation,
            Trajectory source,
            Func<Guid, Trajectory?> trajectoryProvider)
        {
            List<string> validationErrors = TrajectoryExtrapolationValidation.Validate(calculation);
            if (validationErrors.Count > 0)
            {
                return Fail(calculation, string.Join(", ", validationErrors));
            }
            if (!TryGetOrderedCompleteStations(source, out List<SurveyStation> sourceStations))
            {
                return Fail(calculation, "The source trajectory has no complete calculated station list.");
            }

            SurveyStation start = PrepareStartStation(sourceStations, source.CalculationType);
            // Survey tools are attached to source stations by survey-run section. A terminal
            // interpolated station may not carry one, so inherit the last explicitly defined
            // instrument rather than treating the extrapolation as instrument-free.
            start.SurveyTool ??= sourceStations
                .AsEnumerable()
                .Reverse()
                .Select(station => station.SurveyTool)
                .FirstOrDefault(tool => tool != null);
            calculation.StartStation = new SurveyStation(start);
            calculation.SourceTrajectoryRevision = source.LastModificationDate;
            calculation.TargetStation = null;
            calculation.ClosestReferenceMD = null;
            calculation.TargetReferenceMD = null;
            calculation.ReferenceTrajectoryRevision = null;
            calculation.SolvedSectionList = [];
            calculation.SurveyStationList = [new SurveyStation(start)];
            calculation.CalculationState = CalculationState.Running;
            calculation.CalculationProgress = 0.1;
            calculation.CalculationMessage = "Preparing extrapolation";

            bool success = calculation.Specification switch
            {
                FixedLengthExtrapolationSpecification fixedLength =>
                    CalculateFixedLength(calculation, sourceStations, fixedLength),
                ReconnectTrajectoryExtrapolationSpecification reconnect =>
                    CalculateReconnect(calculation, sourceStations, source.CalculationType, reconnect, trajectoryProvider),
                WellPathExtrapolationSpecification wellPath =>
                    CalculateWellPath(calculation, start, wellPath),
                GeosteeringTrajectoryExtrapolationSpecification geosteering =>
                    CalculateGeosteering(calculation, sourceStations, source.CalculationType, geosteering),
                _ => false
            };

            if (!success)
            {
                calculation.CalculationState = CalculationState.Failed;
                calculation.CalculationProgress = 1.0;
                calculation.CalculationMessage ??= "The extrapolation could not be calculated.";
                calculation.SurveyStationList = [];
                calculation.SolvedSectionList = [];
                return false;
            }

            ContinueSourceUncertainty(calculation.SurveyStationList!, start, sourceStations);

            calculation.CalculationState = CalculationState.Completed;
            calculation.CalculationProgress = 1.0;
            calculation.CalculationMessage = null;
            return true;
        }

        private static bool CalculateFixedLength(
            TrajectoryExtrapolationCase calculation,
            List<SurveyStation> sourceStations,
            FixedLengthExtrapolationSpecification specification)
        {
            SurveyStation start = sourceStations[^1];
            if (specification.ExtensionType == FixedLengthExtrapolationType.Straight)
            {
                return CalculateStraight(calculation, start, specification.Length);
            }
            if (sourceStations.Count < 2)
            {
                calculation.CalculationMessage = "Continuing a curve requires two distinct source stations.";
                return false;
            }

            SurveyStation previous = sourceStations[^2];
            if (previous.MD is not double previousMd || start.MD is not double startMd || !Numeric.GT(startMd, previousMd))
            {
                calculation.CalculationMessage = "The final two source stations do not have increasing measured depths.";
                return false;
            }

            ArcSection? section = specification.ExtensionType switch
            {
                FixedLengthExtrapolationType.ContinueCircularArc => ExtendCircularArc(previous, start, specification.Length),
                FixedLengthExtrapolationType.ContinueConstantBuildAndTurn => ExtendBuildAndTurn(previous, start, specification.Length),
                FixedLengthExtrapolationType.ContinueConstantCurvatureAndToolface => ExtendCurvatureAndToolface(previous, start, specification.Length),
                _ => null
            };
            if (section == null)
            {
                calculation.CalculationMessage = "The final source interval could not be represented by the selected curve.";
                return false;
            }

            ExtrapolationCurveType curveType = ToCurveType(specification.ExtensionType);
            AddSamples(calculation.SurveyStationList!, section, startMd, (double)section.End.Abscissa!, calculation.InterpolationInterval);
            calculation.SolvedSectionList = [CreateSolvedSection(Guid.NewGuid(), 0,
                TrajectoryExtrapolationSectionRole.FixedLengthExtension, curveType, start, section.End, section)];
            return calculation.SurveyStationList!.Count > 1;
        }

        private static bool CalculateStraight(TrajectoryExtrapolationCase calculation, SurveyStation start, double length)
        {
            if (!Complete(start))
            {
                calculation.CalculationMessage = "The final source station is incomplete.";
                return false;
            }
            double inclination = start.Inclination!.Value;
            double azimuth = start.Azimuth!.Value;
            double sinInclination = System.Math.Sin(inclination);
            SurveyStation end = new()
            {
                MD = start.MD + length,
                Inclination = inclination,
                Azimuth = azimuth,
                RiemannianNorth = start.RiemannianNorth + length * sinInclination * System.Math.Cos(azimuth),
                RiemannianEast = start.RiemannianEast + length * sinInclination * System.Math.Sin(azimuth),
                TVD = start.TVD + length * System.Math.Cos(inclination),
                Curvature = 0.0,
                Toolface = 0.0,
                BUR = 0.0,
                TUR = 0.0
            };
            ConstantCurvatureAndToolfaceArcSection section = new(ToPoint(start), ToPoint(end));
            section.CTCCurve.Length = length;
            section.CTCCurve.Curvature = 0.0;
            section.CTCCurve.Toolface = 0.0;
            if (!section.CalculateLDT())
            {
                calculation.CalculationMessage = "The straight extension could not be calculated.";
                return false;
            }
            int firstExtensionStationIndex = calculation.SurveyStationList!.Count;
            AddSamples(calculation.SurveyStationList, section, start.MD!.Value, end.MD!.Value, calculation.InterpolationInterval);
            for (int index = firstExtensionStationIndex; index < calculation.SurveyStationList.Count; index++)
            {
                // A straight continuation has no dogleg, build, or turn. Stamp the exact values
                // explicitly because section interpolation may carry diagnostics from its curved
                // source endpoint into an otherwise straight zero-curvature segment.
                calculation.SurveyStationList[index].Curvature = 0.0;
                calculation.SurveyStationList[index].Toolface = 0.0;
                calculation.SurveyStationList[index].BUR = 0.0;
                calculation.SurveyStationList[index].TUR = 0.0;
            }
            calculation.SolvedSectionList = [CreateSolvedSection(Guid.NewGuid(), 0,
                TrajectoryExtrapolationSectionRole.FixedLengthExtension,
                ExtrapolationCurveType.ConstantCurvatureAndToolface, start, section.End, section)];
            return true;
        }

        internal static ArcSection? ExtendCircularArc(SurveyStation previous, SurveyStation last, double extension)
        {
            CircularArcSection fitted = new(ToPoint(previous), EndpointAttitude(last));
            if (!fitted.CalculateSIA()) return null;
            CircularArcSection extended = new(ToPoint(previous), new TrajectoryPoint3D());
            extended.Circle.Length = last.MD!.Value - previous.MD!.Value + extension;
            extended.Circle.Curvature = fitted.Circle.Curvature;
            extended.Circle.ReferenceToolface = fitted.Circle.ReferenceToolface;
            return extended.CalculateLDT() ? extended : null;
        }

        internal static ArcSection? ExtendBuildAndTurn(SurveyStation previous, SurveyStation last, double extension)
        {
            BuildAndTurnArcSection fitted = new(ToPoint(previous), EndpointAttitude(last));
            if (!fitted.CalculateSIA()) return null;
            BuildAndTurnArcSection extended = new(ToPoint(previous), new TrajectoryPoint3D());
            extended.BuildAndTurn.Length = last.MD!.Value - previous.MD!.Value + extension;
            extended.BuildAndTurn.BUR = fitted.BuildAndTurn.BUR;
            extended.BuildAndTurn.TR = fitted.BuildAndTurn.TR;
            return extended.CalculateLBT() ? extended : null;
        }

        internal static ArcSection? ExtendCurvatureAndToolface(SurveyStation previous, SurveyStation last, double extension)
        {
            ConstantCurvatureAndToolfaceArcSection fitted = new(ToPoint(previous), EndpointAttitude(last));
            if (!fitted.CalculateSIA()) return null;
            ConstantCurvatureAndToolfaceArcSection extended = new(ToPoint(previous), new TrajectoryPoint3D());
            extended.CTCCurve.Length = last.MD!.Value - previous.MD!.Value + extension;
            extended.CTCCurve.Curvature = fitted.CTCCurve.Curvature;
            extended.CTCCurve.Toolface = fitted.CTCCurve.Toolface;
            return extended.CalculateLDT() ? extended : null;
        }

        private static bool CalculateReconnect(
            TrajectoryExtrapolationCase calculation,
            List<SurveyStation> sourceStations,
            OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType sourceCalculationType,
            ReconnectTrajectoryExtrapolationSpecification specification,
            Func<Guid, Trajectory?> trajectoryProvider)
        {
            if (!TryApplyLeadIn(calculation, sourceStations, sourceCalculationType,
                    specification.LeadInLength, out SurveyStation start))
                return false;
            Trajectory? reference = trajectoryProvider(specification.ReferenceTrajectoryID);
            if (!TryGetOrderedCompleteStations(reference, out List<SurveyStation> referenceStations))
            {
                calculation.CalculationMessage = "The reference trajectory has no complete calculated station list.";
                return false;
            }
            if (!TryFindClosestReferenceMD(start, referenceStations, reference!.CalculationType, out double closestMd))
            {
                calculation.CalculationMessage = "A closest point on the reference trajectory could not be found.";
                return false;
            }
            double targetMd = closestMd + specification.ReferenceMDAdvance;
            double lastReferenceMd = referenceStations[^1].MD!.Value;
            if (targetMd > lastReferenceMd || !SurveyStation.InterpolateAtAbscissa(referenceStations, targetMd, out SurveyStation? target, reference.CalculationType) || target == null)
            {
                calculation.CalculationMessage = "The requested target measured depth lies beyond the reference trajectory.";
                return false;
            }

            calculation.ClosestReferenceMD = closestMd;
            calculation.TargetReferenceMD = targetMd;
            calculation.TargetStation = new SurveyStation(target);
            calculation.ReferenceTrajectoryRevision = reference.LastModificationDate;
            TrajectoryPoint3D from = ToPoint(start);
            TrajectoryPoint3D to = new()
            {
                X = target.RiemannianNorth,
                Y = target.RiemannianEast,
                Z = target.TVD,
                Inclination = target.Inclination,
                Azimuth = target.Azimuth
            };

            bool solved = TryCalculatePositionTargetPair(from, to, specification.CurveType,
                specification.AzimuthBranch, specification.JunctionCurvatureRatio, out ArcSection pair);
            if (!solved || pair.End.Abscissa is not double endMd)
            {
                calculation.CalculationMessage = "The selected double-curve solver could not reach the target position and attitude.";
                return false;
            }

            AddSamples(calculation.SurveyStationList!, pair, start.MD!.Value, endMd, calculation.InterpolationInterval, JunctionMD(pair));
            AppendDoubleSolvedSections(calculation, pair, specification.CurveType);
            return true;
        }

        private static bool CalculateGeosteering(
            TrajectoryExtrapolationCase calculation,
            List<SurveyStation> sourceStations,
            OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType sourceCalculationType,
            GeosteeringTrajectoryExtrapolationSpecification specification)
        {
            SurveyStation originalStart = sourceStations[^1];
            if (!TryApplyLeadIn(calculation, sourceStations, sourceCalculationType,
                    specification.LeadInLength, out SurveyStation steeringStart))
                return false;

            ArcSection pair;
            bool solved;
            switch (specification.Extent)
            {
                case DepartureGeosteeringExtentConstraint departure:
                    TrajectoryPoint3D target = new()
                    {
                        X = originalStart.RiemannianNorth!.Value +
                            departure.DepartureDistance * System.Math.Cos(departure.DepartureBearing),
                        Y = originalStart.RiemannianEast!.Value +
                            departure.DepartureDistance * System.Math.Sin(departure.DepartureBearing),
                        Z = specification.TargetVerticalDepth,
                        Inclination = specification.EndInclination,
                        Azimuth = specification.EndAzimuth
                    };
                    solved = TryCalculatePositionTargetPair(ToPoint(steeringStart), target,
                        specification.CurveType, specification.AzimuthBranch, 1.0, out pair);
                    break;
                case DrilledLengthGeosteeringExtentConstraint drilled:
                    double downstreamLength = drilled.SteeringLength / (1.0 + drilled.SteeringLengthRatio);
                    double upstreamLength = drilled.SteeringLength - downstreamLength;
                    solved = TryCalculateDrilledLengthPair(ToPoint(steeringStart), upstreamLength,
                        downstreamLength, specification, out pair);
                    break;
                default:
                    calculation.CalculationMessage = "A geosteering extent is required.";
                    return false;
            }

            if (!solved || pair.End.Abscissa is not double endMd)
            {
                calculation.CalculationMessage = "No feasible two-section geosteering solution was found.";
                return false;
            }

            calculation.TargetStation = FromPoint(pair.End);
            AddSamples(calculation.SurveyStationList!, pair, steeringStart.MD!.Value, endMd,
                calculation.InterpolationInterval, JunctionMD(pair));
            AppendDoubleSolvedSections(calculation, pair, specification.CurveType);
            return true;
        }

        private static bool TryCalculatePositionTargetPair(TrajectoryPoint3D from, TrajectoryPoint3D to,
            ExtrapolationCurveType curveType, int azimuthBranch, double junctionCurvatureRatio,
            out ArcSection pair)
        {
            switch (curveType)
            {
                case ExtrapolationCurveType.CircularArc:
                    DoubleArcs circular = new() { Start = from, End = to };
                    pair = circular;
                    return circular.CalculateXYZ();
                case ExtrapolationCurveType.ConstantBuildAndTurn:
                    DoubleBuildAndTurnArcs buildTurn = new(from, to);
                    pair = buildTurn;
                    return buildTurn.CalculateXYZ(azimuthBranch, junctionCurvatureRatio);
                case ExtrapolationCurveType.ConstantCurvatureAndToolface:
                    DoubleConstantCurvatureAndToolfaceArcs curvatureToolface = new(from, to);
                    pair = curvatureToolface;
                    return curvatureToolface.CalculateXYZ(azimuthBranch);
                default:
                    pair = null!;
                    return false;
            }
        }

        private static bool TryCalculateDrilledLengthPair(TrajectoryPoint3D from,
            double upstreamLength, double downstreamLength,
            GeosteeringTrajectoryExtrapolationSpecification specification, out ArcSection pair)
        {
            switch (specification.CurveType)
            {
                case ExtrapolationCurveType.CircularArc:
                    bool circularSolved = DoubleSectionByDrilledLengthSolver.TryCalculateCircularArcs(from,
                        upstreamLength, downstreamLength, specification.TargetVerticalDepth,
                        specification.EndInclination, specification.EndAzimuth, out DoubleArcs circular);
                    pair = circular;
                    return circularSolved;
                case ExtrapolationCurveType.ConstantBuildAndTurn:
                    bool buildTurnSolved = DoubleSectionByDrilledLengthSolver.TryCalculateBuildAndTurnArcs(from,
                        upstreamLength, downstreamLength, specification.TargetVerticalDepth,
                        specification.EndInclination, specification.EndAzimuth, specification.AzimuthBranch,
                        out DoubleBuildAndTurnArcs buildTurn);
                    pair = buildTurn;
                    return buildTurnSolved;
                case ExtrapolationCurveType.ConstantCurvatureAndToolface:
                    bool curvatureToolfaceSolved = DoubleSectionByDrilledLengthSolver.TryCalculateConstantCurvatureAndToolfaceArcs(from,
                        upstreamLength, downstreamLength, specification.TargetVerticalDepth,
                        specification.EndInclination, specification.EndAzimuth,
                        out DoubleConstantCurvatureAndToolfaceArcs curvatureToolface);
                    pair = curvatureToolface;
                    return curvatureToolfaceSolved;
                default:
                    pair = null!;
                    return false;
            }
        }

        private static bool TryApplyLeadIn(TrajectoryExtrapolationCase calculation,
            List<SurveyStation> sourceStations,
            OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType calculationType,
            double leadInLength,
            out SurveyStation steeringStart)
        {
            SurveyStation start = sourceStations[^1];
            steeringStart = start;
            if (Numeric.EQ(leadInLength, 0.0)) return true;

            ArcSection? leadIn;
            ExtrapolationCurveType curveType;
            if (sourceStations.Count == 1)
            {
                leadIn = CreateStraightSection(start, leadInLength);
                curveType = ExtrapolationCurveType.ConstantCurvatureAndToolface;
            }
            else
            {
                SurveyStation previous = sourceStations[^2];
                if (previous.MD is not double previousMd || start.MD is not double startMd || !Numeric.GT(startMd, previousMd))
                {
                    calculation.CalculationMessage = "The final two source stations do not define an increasing interval for the lead-in.";
                    return false;
                }
                (leadIn, curveType) = calculationType switch
                {
                    OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType.ConstantBuildAndTurnMethod =>
                        (ExtendBuildAndTurn(previous, start, leadInLength), ExtrapolationCurveType.ConstantBuildAndTurn),
                    OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType.ConstantCurvatureAndToolfaceMethod =>
                        (ExtendCurvatureAndToolface(previous, start, leadInLength), ExtrapolationCurveType.ConstantCurvatureAndToolface),
                    _ => (ExtendCircularArc(previous, start, leadInLength), ExtrapolationCurveType.CircularArc)
                };
            }
            if (leadIn?.End.Abscissa is not double endMd)
            {
                calculation.CalculationMessage = "The source trajectory's final curve could not be continued for the lead-in.";
                return false;
            }
            AddSamples(calculation.SurveyStationList!, leadIn, start.MD!.Value, endMd,
                calculation.InterpolationInterval);
            steeringStart = FromPoint(leadIn.End);
            calculation.SolvedSectionList!.Add(CreateSolvedSection(Guid.NewGuid(),
                calculation.SolvedSectionList.Count, TrajectoryExtrapolationSectionRole.LeadInContinuation,
                curveType, start, leadIn.End, leadIn));
            return true;
        }

        private static ArcSection? CreateStraightSection(SurveyStation start, double length)
        {
            ConstantCurvatureAndToolfaceArcSection section = new(ToPoint(start), new TrajectoryPoint3D());
            section.CTCCurve.Length = length;
            section.CTCCurve.Curvature = 0.0;
            section.CTCCurve.Toolface = 0.0;
            return section.CalculateLDT() ? section : null;
        }

        private static bool CalculateWellPath(
            TrajectoryExtrapolationCase calculation,
            SurveyStation start,
            WellPathExtrapolationSpecification specification)
        {
            ComplexPath path = new() { Start = ToPoint(start) };
            foreach (WellPathSectionSpecification input in specification.SectionList)
            {
                ComplexPathSection section = path.AddSection(ToSectionCurveType(input));
                section.Length = input.Length;
                section.End.Inclination = input.EndInclination;
                section.End.Azimuth = input.EndAzimuth;
                section.End.Z = input.EndVerticalDepth;
                section.End.X = input.EndNorth;
                section.End.Y = input.EndEast;
                switch (input)
                {
                    case CircularArcWellPathSectionSpecification circular:
                        section.Curvature = circular.Curvature;
                        section.Toolface = circular.StartToolface;
                        break;
                    case ConstantBuildAndTurnWellPathSectionSpecification buildTurn:
                        section.BUR = buildTurn.BuildRate;
                        section.TurnRate = buildTurn.TurnRate;
                        break;
                    case ConstantCurvatureAndToolfaceWellPathSectionSpecification curvatureToolface:
                        section.Curvature = curvatureToolface.Curvature;
                        section.Toolface = curvatureToolface.Toolface;
                        break;
                }
            }
            if (!path.Calculate())
            {
                calculation.CalculationMessage = path.FailureDescription;
                return false;
            }

            calculation.SolvedSectionList = [];
            for (int index = 0; index < path.SolvedSections.Count; index++)
            {
                ArcSection section = path.SolvedSections[index];
                Guid sectionId = specification.SectionList[index].SectionID;
                ExtrapolationCurveType curveType = ToExtrapolationCurveType(specification.SectionList[index]);
                AddSamples(calculation.SurveyStationList!, section, section.Start.Abscissa!.Value, section.End.Abscissa!.Value, calculation.InterpolationInterval);
                calculation.SolvedSectionList.Add(CreateSolvedSection(sectionId, index,
                    TrajectoryExtrapolationSectionRole.WellPathSection, curveType,
                    FromPoint(section.Start), section.End, section));
            }
            return true;
        }

        private static bool TryFindClosestReferenceMD(
            SurveyStation point,
            List<SurveyStation> reference,
            OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType method,
            out double closestMd)
        {
            closestMd = 0.0;
            double bestDistanceSquared = double.PositiveInfinity;
            for (int index = 1; index < reference.Count; index++)
            {
                double low = reference[index - 1].MD!.Value;
                double high = reference[index].MD!.Value;
                for (int iteration = 0; iteration < ClosestPointIterations; iteration++)
                {
                    double third = (high - low) / 3.0;
                    double left = low + third;
                    double right = high - third;
                    if (DistanceSquaredAt(reference, left, point, method) <= DistanceSquaredAt(reference, right, point, method)) high = right;
                    else low = left;
                }
                double md = 0.5 * (low + high);
                double distanceSquared = DistanceSquaredAt(reference, md, point, method);
                if (distanceSquared < bestDistanceSquared ||
                    (Numeric.EQ(distanceSquared, bestDistanceSquared) && md < closestMd))
                {
                    bestDistanceSquared = distanceSquared;
                    closestMd = md;
                }
            }
            return Numeric.IsDefined(bestDistanceSquared) && !double.IsPositiveInfinity(bestDistanceSquared);
        }

        private static double DistanceSquaredAt(List<SurveyStation> reference, double md, SurveyStation point,
            OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType method)
        {
            if (!SurveyStation.InterpolateAtAbscissa(reference, md, out SurveyStation? candidate, method) || candidate == null) return double.PositiveInfinity;
            double dx = candidate.RiemannianNorth!.Value - point.RiemannianNorth!.Value;
            double dy = candidate.RiemannianEast!.Value - point.RiemannianEast!.Value;
            double dz = candidate.TVD!.Value - point.TVD!.Value;
            return dx * dx + dy * dy + dz * dz;
        }

        internal static void AddSamples(List<SurveyStation> output, ArcSection section, double startMd, double endMd,
            double interval, double? requiredMd = null)
        {
            SortedSet<double> measuredDepths = [];
            for (double md = startMd + interval; md < endMd; md += interval) measuredDepths.Add(md);
            if (requiredMd is double junction && junction > startMd && junction < endMd) measuredDepths.Add(junction);
            measuredDepths.Add(endMd);
            foreach (double md in measuredDepths)
            {
                CurvilinearPoint3D point = section.InterpolateAtMD(md);
                if (point != null && Numeric.IsDefined(point.Abscissa) && Numeric.IsDefined(point.X) &&
                    Numeric.IsDefined(point.Y) && Numeric.IsDefined(point.Z))
                {
                    SurveyStation station = FromPoint(point);
                    if (output.Count == 0 || !Numeric.EQ(output[^1].MD, station.MD))
                    {
                        if (output.Count > 0)
                        {
                            // Use the same curve-specific completion routines as ordinary trajectory
                            // calculations. This fills DLS, BUR, TR, toolface, and vertical section.
                            CompleteDerivedValues(output[^1], station, CalculationType(section));
                        }
                        output.Add(station);
                    }
                }
            }
        }

        private static OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType CalculationType(ArcSection section) =>
            section switch
            {
                BuildAndTurnArcSection or DoubleBuildAndTurnArcs =>
                    OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType.ConstantBuildAndTurnMethod,
                ConstantCurvatureAndToolfaceArcSection or DoubleConstantCurvatureAndToolfaceArcs =>
                    OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType.ConstantCurvatureAndToolfaceMethod,
                _ => OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType.MinimumCurvatureMethod
            };

        private static void CompleteDerivedValues(
            SurveyStation previous,
            SurveyStation station,
            OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType calculationType)
        {
            double? north = station.RiemannianNorth;
            double? east = station.RiemannianEast;
            double? tvd = station.TVD;
            previous.CompleteFromSIA(station, calculationType);

            // Sampling has already established the authoritative section geometry. Completion is
            // used for the curve diagnostics only, so retain those sampled coordinates exactly.
            station.RiemannianNorth = north;
            station.RiemannianEast = east;
            station.TVD = tvd;
            if (previous.VerticalSection is double previousVerticalSection &&
                previous.RiemannianNorth is double previousNorth &&
                previous.RiemannianEast is double previousEast &&
                north is double currentNorth && east is double currentEast)
            {
                station.VerticalSection = previousVerticalSection + System.Math.Sqrt(
                    (currentNorth - previousNorth) * (currentNorth - previousNorth) +
                    (currentEast - previousEast) * (currentEast - previousEast));
            }
        }

        private static void AppendDoubleSolvedSections(TrajectoryExtrapolationCase calculation,
            ArcSection pair, ExtrapolationCurveType type)
        {
            TrajectoryPoint3D intermediate = pair switch
            {
                DoubleArcs value => value.Intermediate,
                DoubleBuildAndTurnArcs value => value.Intermediate,
                DoubleConstantCurvatureAndToolfaceArcs value => value.Intermediate,
                _ => throw new InvalidOperationException("Unsupported double section.")
            };
            ArcSection first = BuildHalf(pair, pair.Start, intermediate, true);
            ArcSection second = BuildHalf(pair, intermediate, pair.End, false);
            int firstIndex = calculation.SolvedSectionList?.Count ?? 0;
            calculation.SolvedSectionList ??= [];
            calculation.SolvedSectionList.Add(CreateSolvedSection(Guid.NewGuid(), firstIndex,
                TrajectoryExtrapolationSectionRole.UpstreamSteeringSection,
                type, FromPoint(first.Start), first.End, first));
            calculation.SolvedSectionList.Add(CreateSolvedSection(Guid.NewGuid(), firstIndex + 1,
                TrajectoryExtrapolationSectionRole.DownstreamSteeringSection,
                type, FromPoint(second.Start), second.End, second));
        }

        private static ArcSection BuildHalf(ArcSection pair, TrajectoryPoint3D start, TrajectoryPoint3D end, bool upstream)
        {
            if (pair is DoubleArcs circularPair)
            {
                CircularArcSection result = new(start, end);
                result.Circle.Length = end.Abscissa - start.Abscissa;
                result.Circle.Curvature = circularPair.DoubleArcCurve.Curvature;
                result.Circle.ReferenceToolface = upstream ? circularPair.DoubleArcCurve.UpstreamReferenceToolface : circularPair.DoubleArcCurve.DownstreamReferenceToolface;
                return result;
            }
            if (pair is DoubleBuildAndTurnArcs buildTurnPair)
            {
                BuildAndTurnArcSection result = new(start, end);
                result.BuildAndTurn.Length = end.Abscissa - start.Abscissa;
                result.BuildAndTurn.BUR = upstream ? buildTurnPair.DoubleBuildAndTurnCurve.UpstreamBUR : buildTurnPair.DoubleBuildAndTurnCurve.DownstreamBUR;
                result.BuildAndTurn.TR = upstream ? buildTurnPair.DoubleBuildAndTurnCurve.UpstreamTR : buildTurnPair.DoubleBuildAndTurnCurve.DownstreamTR;
                return result;
            }
            DoubleConstantCurvatureAndToolfaceArcs curvatureToolfacePair = (DoubleConstantCurvatureAndToolfaceArcs)pair;
            ConstantCurvatureAndToolfaceArcSection ctc = new(start, end);
            ctc.CTCCurve.Length = end.Abscissa - start.Abscissa;
            ctc.CTCCurve.Curvature = curvatureToolfacePair.DoubleCTCCurve.Curvature;
            ctc.CTCCurve.Toolface = upstream ? curvatureToolfacePair.DoubleCTCCurve.UpstreamToolface : curvatureToolfacePair.DoubleCTCCurve.DownstreamToolface;
            return ctc;
        }

        private static TrajectoryExtrapolationSolvedSection CreateSolvedSection(Guid id, int index,
            TrajectoryExtrapolationSectionRole role, ExtrapolationCurveType type,
            SurveyStation start, TrajectoryPoint3D end, ArcSection section)
        {
            TrajectoryExtrapolationSolvedSection result = new()
            {
                SectionID = id,
                SectionIndex = index,
                Role = role,
                CurveType = type,
                StartMD = start.MD!.Value,
                EndMD = end.Abscissa!.Value,
                Length = end.Abscissa.Value - start.MD.Value,
                Start = WithoutUncertainty(start),
                End = FromPoint(end)
            };
            switch (section)
            {
                case CircularArcSection circular:
                    result.CircularArcCurvature = circular.Circle.Curvature;
                    result.CircularArcStartToolface = circular.Circle.ReferenceToolface;
                    break;
                case BuildAndTurnArcSection buildTurn:
                    result.ConstantBuildRate = buildTurn.BuildAndTurn.BUR;
                    result.ConstantTurnRate = buildTurn.BuildAndTurn.TR;
                    break;
                case ConstantCurvatureAndToolfaceArcSection curvatureToolface:
                    result.ConstantCurvature = curvatureToolface.CTCCurve.Curvature;
                    result.ConstantToolface = curvatureToolface.CTCCurve.Toolface;
                    break;
            }
            return result;
        }

        internal static bool TryGetOrderedCompleteStations(Trajectory? trajectory, out List<SurveyStation> stations)
        {
            stations = trajectory?.SurveyStationList?.Where(Complete).OrderBy(value => value.MD).ToList() ?? [];
            return stations.Count > 0;
        }

        internal static SurveyStation PrepareStartStation(
            List<SurveyStation> sourceStations,
            OSDC.DotnetLibraries.Drilling.Surveying.TrajectoryCalculationType calculationType)
        {
            SurveyStation start = new(sourceStations[^1]);
            if (sourceStations.Count > 1 &&
                (start.VerticalSection == null || start.Curvature == null || start.BUR == null || start.TUR == null))
            {
                SurveyStation previous = WithoutUncertainty(sourceStations[^2]);
                previous.VerticalSection ??= VerticalSectionAt(sourceStations, sourceStations.Count - 2);
                SurveyStation completed = WithoutUncertainty(start);
                if (previous.CompleteFromSIA(completed, calculationType))
                {
                    start.VerticalSection ??= completed.VerticalSection;
                    start.Curvature ??= completed.Curvature;
                    start.Toolface ??= completed.Toolface;
                    start.BUR ??= completed.BUR;
                    start.TUR ??= completed.TUR;
                }
            }

            start.VerticalSection ??= VerticalSectionAt(sourceStations, sourceStations.Count - 1);
            if (sourceStations.Count == 1)
            {
                start.Curvature ??= 0.0;
                start.Toolface ??= 0.0;
                start.BUR ??= 0.0;
                start.TUR ??= 0.0;
            }
            return start;
        }

        /// <summary>
        /// Continues position uncertainty from the frozen final source station. The extrapolation
        /// is not a new observation, so its local error-model covariance is treated as an increment
        /// to the source endpoint covariance rather than restarting the well at zero uncertainty.
        /// </summary>
        internal static void ContinueSourceUncertainty(
            List<SurveyStation> stations,
            SurveyStation sourceEndpoint,
            IReadOnlyList<SurveyStation>? sourceHistory = null)
        {
            if (stations.Count == 0)
            {
                return;
            }

            SurveyInstrument? surveyTool = sourceEndpoint.SurveyTool;
            foreach (SurveyStation station in stations)
            {
                station.SurveyTool = surveyTool;
            }

            if (surveyTool?.ModelType is SurveyInstrumentModelType.MWD_WolffDeWardt or
                SurveyInstrumentModelType.Gyro_WolffDeWardt &&
                sourceHistory is { Count: > 1 } &&
                ContinueWolffDeWardtUncertainty(stations, sourceHistory, surveyTool))
            {
                return;
            }

            if (surveyTool?.ModelType is SurveyInstrumentModelType.MWD_ISCWSA or
                SurveyInstrumentModelType.Gyro_ISCWSA &&
                sourceHistory is { Count: > 1 } &&
                ContinueIscwsaUncertainty(stations, sourceHistory, surveyTool))
            {
                return;
            }

            if (sourceEndpoint.Covariance == null)
            {
                CalculateUncertainty(stations, surveyTool);
                return;
            }

            SymmetricMatrix3x3 sourceCovariance = CopyCovariance(sourceEndpoint.Covariance);
            Vector3D? sourceBias = CopyVector(sourceEndpoint.Bias);

            // Carry the source endpoint uncertainty even when its instrument definition is not
            // available. In that case no defensible incremental growth can be calculated.
            foreach (SurveyStation station in stations)
            {
                station.Covariance = CopyCovariance(sourceCovariance);
                station.Bias = CopyVector(sourceBias);
                station.CalculateEigenProperties();
            }

            // ISCWSA systematic, global, well-by-well, random and continuous-mode terms retain
            // accumulators that cannot be recovered from the endpoint covariance. Without the
            // complete source history, preserving the frozen endpoint covariance is safer than
            // fabricating an independent incremental result.
            if (surveyTool == null ||
                surveyTool.ModelType is SurveyInstrumentModelType.MWD_ISCWSA or
                SurveyInstrumentModelType.Gyro_ISCWSA)
            {
                return;
            }

            if (surveyTool == null || stations.Count < 2)
            {
                return;
            }

            List<SurveyStation> incrementalStations = stations.Select(station =>
            {
                SurveyStation incremental = new(station)
                {
                    SurveyTool = surveyTool,
                    Covariance = null,
                    Bias = null,
                    EigenValues = null,
                    EigenVectors = null
                };
                return incremental;
            }).ToList();

            bool calculated = CalculateUncertainty(incrementalStations, surveyTool);

            if (!calculated)
            {
                return;
            }

            SymmetricMatrix3x3? incrementalBaseline = incrementalStations[0].Covariance;
            Vector3D? incrementalBiasBaseline = incrementalStations[0].Bias;
            for (int index = 0; index < stations.Count; index++)
            {
                if (incrementalStations[index].Covariance is not { } incrementalCovariance)
                {
                    continue;
                }

                stations[index].Covariance = AddCovariance(
                    sourceCovariance, incrementalCovariance, incrementalBaseline);
                stations[index].Bias = AddVectors(
                    sourceBias, incrementalStations[index].Bias, incrementalBiasBaseline);
                stations[index].CalculateEigenProperties();
            }
        }

        /// <summary>
        /// Replays the Wolff-de Wardt transfer matrix through the source trajectory before
        /// extending it. Its 6x3 A matrix is the propagation state; the terminal covariance
        /// alone cannot be inverted to recover that state.
        /// </summary>
        private static bool ContinueWolffDeWardtUncertainty(
            List<SurveyStation> extrapolationStations,
            IReadOnlyList<SurveyStation> sourceHistory,
            SurveyInstrument extrapolationTool)
        {
            List<SurveyStation> replay = sourceHistory
                .Select(station => new SurveyStation(station)
                {
                    Covariance = null,
                    Bias = null,
                    EigenValues = null,
                    EigenVectors = null
                })
                .ToList();
            if (replay.Count == 0)
            {
                return false;
            }

            SurveyInstrument? lastDefinedTool = null;
            foreach (SurveyStation station in replay)
            {
                lastDefinedTool = station.SurveyTool ?? lastDefinedTool;
                station.SurveyTool = lastDefinedTool;
            }
            if (replay.Any(station => station.SurveyTool == null))
            {
                return false;
            }

            for (int index = 1; index < extrapolationStations.Count; index++)
            {
                replay.Add(new SurveyStation(extrapolationStations[index])
                {
                    SurveyTool = extrapolationTool,
                    Covariance = null,
                    Bias = null,
                    EigenValues = null,
                    EigenVectors = null
                });
            }

            try
            {
                if (!CovarianceCalculatorWolffDeWardt.Calculate(replay))
                {
                    return false;
                }
            }
            catch
            {
                // A legacy or incomplete source history may not be replayable. The caller retains
                // the frozen endpoint covariance and falls back to the older incremental path.
                return false;
            }

            int sourceEndIndex = sourceHistory.Count - 1;
            CopyUncertainty(replay[sourceEndIndex], extrapolationStations[0], extrapolationTool);
            for (int index = 1; index < extrapolationStations.Count; index++)
            {
                CopyUncertainty(replay[sourceEndIndex + index], extrapolationStations[index], extrapolationTool);
            }
            return true;
        }

        /// <summary>
        /// Replays the complete ISCWSA error-source accumulators through the source trajectory
        /// before extending it. Endpoint covariance alone loses correlation-mode and continuous
        /// propagation state, so an incremental calculation starting at the endpoint is invalid.
        /// </summary>
        private static bool ContinueIscwsaUncertainty(
            List<SurveyStation> extrapolationStations,
            IReadOnlyList<SurveyStation> sourceHistory,
            SurveyInstrument extrapolationTool)
        {
            List<SurveyStation> replay = sourceHistory
                .Select(station => new SurveyStation(station)
                {
                    Covariance = null,
                    Bias = null,
                    EigenValues = null,
                    EigenVectors = null
                })
                .ToList();
            if (replay.Count == 0) return false;

            SurveyInstrument? lastDefinedTool = null;
            foreach (SurveyStation station in replay)
            {
                lastDefinedTool = station.SurveyTool ?? lastDefinedTool;
                station.SurveyTool = lastDefinedTool;
            }
            if (replay.Any(station => station.SurveyTool == null)) return false;

            for (int index = 1; index < extrapolationStations.Count; index++)
            {
                replay.Add(new SurveyStation(extrapolationStations[index])
                {
                    SurveyTool = extrapolationTool,
                    Covariance = null,
                    Bias = null,
                    EigenValues = null,
                    EigenVectors = null
                });
            }

            try
            {
                if (!CovarianceCalculatorISCWSA.Calculate(replay)) return false;
            }
            catch
            {
                return false;
            }

            int sourceEndIndex = sourceHistory.Count - 1;
            CopyUncertainty(replay[sourceEndIndex], extrapolationStations[0], extrapolationTool);
            for (int index = 1; index < extrapolationStations.Count; index++)
                CopyUncertainty(replay[sourceEndIndex + index], extrapolationStations[index], extrapolationTool);
            return true;
        }

        private static void CopyUncertainty(
            SurveyStation source,
            SurveyStation target,
            SurveyInstrument surveyTool)
        {
            target.SurveyTool = surveyTool;
            target.Covariance = source.Covariance == null ? null : CopyCovariance(source.Covariance);
            target.Bias = CopyVector(source.Bias);
            target.CalculateEigenProperties();
        }

        private static bool CalculateUncertainty(List<SurveyStation> stations, SurveyInstrument? surveyTool)
        {
            if (surveyTool == null)
            {
                return false;
            }

            foreach (SurveyStation station in stations)
            {
                station.SurveyTool = surveyTool;
            }

            try
            {
                return surveyTool.ModelType switch
                {
                    SurveyInstrumentModelType.MWD_WolffDeWardt or SurveyInstrumentModelType.Gyro_WolffDeWardt =>
                        CovarianceCalculatorWolffDeWardt.Calculate(stations),
                    SurveyInstrumentModelType.MWD_ISCWSA or SurveyInstrumentModelType.Gyro_ISCWSA =>
                        CovarianceCalculatorISCWSA.Calculate(stations),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private static SymmetricMatrix3x3 CopyCovariance(SymmetricMatrix3x3 source)
        {
            SymmetricMatrix3x3 result = new();
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    result[row, column] = source[row, column];
                }
            }
            return result;
        }

        private static SymmetricMatrix3x3 AddCovariance(
            SymmetricMatrix3x3 source,
            SymmetricMatrix3x3 increment,
            SymmetricMatrix3x3? incrementBaseline)
        {
            SymmetricMatrix3x3 result = new();
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    result[row, column] = (source[row, column] ?? 0.0) +
                        (increment[row, column] ?? 0.0) -
                        (incrementBaseline?[row, column] ?? 0.0);
                }
            }
            return result;
        }

        private static Vector3D? CopyVector(Vector3D? source) => source == null
            ? null
            : new Vector3D { X = source.X, Y = source.Y, Z = source.Z };

        private static Vector3D? AddVectors(Vector3D? source, Vector3D? increment, Vector3D? incrementBaseline)
        {
            if (source == null && increment == null)
            {
                return null;
            }
            return new Vector3D
            {
                X = (source?.X ?? 0.0) + (increment?.X ?? 0.0) - (incrementBaseline?.X ?? 0.0),
                Y = (source?.Y ?? 0.0) + (increment?.Y ?? 0.0) - (incrementBaseline?.Y ?? 0.0),
                Z = (source?.Z ?? 0.0) + (increment?.Z ?? 0.0) - (incrementBaseline?.Z ?? 0.0)
            };
        }

        private static double VerticalSectionAt(List<SurveyStation> stations, int endIndex)
        {
            double verticalSection = stations[0].VerticalSection ?? 0.0;
            for (int index = 1; index <= endIndex; index++)
            {
                if (stations[index - 1].RiemannianNorth is double previousNorth &&
                    stations[index - 1].RiemannianEast is double previousEast &&
                    stations[index].RiemannianNorth is double north &&
                    stations[index].RiemannianEast is double east)
                {
                    verticalSection += System.Math.Sqrt(
                        (north - previousNorth) * (north - previousNorth) +
                        (east - previousEast) * (east - previousEast));
                }
            }
            return verticalSection;
        }

        private static bool Complete(SurveyStation value) =>
            value != null && Numeric.IsDefined(value.MD) && Numeric.IsDefined(value.Inclination) &&
            Numeric.IsDefined(value.Azimuth) && Numeric.IsDefined(value.RiemannianNorth) &&
            Numeric.IsDefined(value.RiemannianEast) && Numeric.IsDefined(value.TVD);

        internal static TrajectoryPoint3D ToPoint(SurveyStation value) => new()
        {
            Abscissa = value.MD,
            Inclination = value.Inclination,
            Azimuth = value.Azimuth,
            X = value.RiemannianNorth,
            Y = value.RiemannianEast,
            Z = value.TVD,
            VerticalSection = value.VerticalSection,
            Curvature = value.Curvature,
            Toolface = value.Toolface,
            BUR = value.BUR,
            TUR = value.TUR
        };

        private static TrajectoryPoint3D EndpointAttitude(SurveyStation value) => new()
        {
            Abscissa = value.MD,
            Inclination = value.Inclination,
            Azimuth = value.Azimuth
        };

        internal static SurveyStation FromPoint(CurvilinearPoint3D value)
        {
            TrajectoryPoint3D? trajectoryPoint = value as TrajectoryPoint3D;
            return new SurveyStation
            {
                MD = value.Abscissa,
                Inclination = value.Inclination,
                Azimuth = value.Azimuth,
                RiemannianNorth = value.X,
                RiemannianEast = value.Y,
                TVD = value.Z,
                VerticalSection = trajectoryPoint?.VerticalSection,
                Curvature = trajectoryPoint?.Curvature,
                Toolface = trajectoryPoint?.Toolface,
                BUR = trajectoryPoint?.BUR,
                TUR = trajectoryPoint?.TUR
            };
        }

        private static SurveyStation WithoutUncertainty(SurveyStation value) => new()
        {
            MD = value.MD,
            Inclination = value.Inclination,
            Azimuth = value.Azimuth,
            RiemannianNorth = value.RiemannianNorth,
            RiemannianEast = value.RiemannianEast,
            TVD = value.TVD,
            VerticalSection = value.VerticalSection,
            Curvature = value.Curvature,
            Toolface = value.Toolface,
            BUR = value.BUR,
            TUR = value.TUR,
            BoreholeRadius = value.BoreholeRadius
        };

        private static double? JunctionMD(ArcSection pair) => pair switch
        {
            DoubleArcs value => value.Intermediate.Abscissa,
            DoubleBuildAndTurnArcs value => value.Intermediate.Abscissa,
            DoubleConstantCurvatureAndToolfaceArcs value => value.Intermediate.Abscissa,
            _ => null
        };

        private static ExtrapolationCurveType ToCurveType(FixedLengthExtrapolationType value) => value switch
        {
            FixedLengthExtrapolationType.ContinueCircularArc => ExtrapolationCurveType.CircularArc,
            FixedLengthExtrapolationType.ContinueConstantBuildAndTurn => ExtrapolationCurveType.ConstantBuildAndTurn,
            _ => ExtrapolationCurveType.ConstantCurvatureAndToolface
        };

        private static SectionCurveType ToSectionCurveType(WellPathSectionSpecification value) => value switch
        {
            CircularArcWellPathSectionSpecification => SectionCurveType.CircularArc,
            ConstantBuildAndTurnWellPathSectionSpecification => SectionCurveType.ConstantBuildAndTurn,
            ConstantCurvatureAndToolfaceWellPathSectionSpecification => SectionCurveType.ConstantCurvatureAndToolface,
            _ => throw new InvalidOperationException("Unsupported well-path section.")
        };

        private static ExtrapolationCurveType ToExtrapolationCurveType(WellPathSectionSpecification value) => value switch
        {
            CircularArcWellPathSectionSpecification => ExtrapolationCurveType.CircularArc,
            ConstantBuildAndTurnWellPathSectionSpecification => ExtrapolationCurveType.ConstantBuildAndTurn,
            ConstantCurvatureAndToolfaceWellPathSectionSpecification => ExtrapolationCurveType.ConstantCurvatureAndToolface,
            _ => throw new InvalidOperationException("Unsupported well-path section.")
        };

        private static bool Fail(TrajectoryExtrapolationCase calculation, string message)
        {
            calculation.CalculationState = CalculationState.Failed;
            calculation.CalculationProgress = 1.0;
            calculation.CalculationMessage = message;
            calculation.SurveyStationList = [];
            calculation.SolvedSectionList = [];
            return false;
        }
    }
}
