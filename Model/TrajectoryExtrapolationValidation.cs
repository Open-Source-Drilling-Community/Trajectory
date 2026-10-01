using OSDC.DotnetLibraries.General.Common;
using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Model
{
    public static class TrajectoryExtrapolationValidation
    {
        public static List<string> Validate(TrajectoryExtrapolationCase? value)
        {
            List<string> errors = [];
            if (value == null)
            {
                errors.Add("case_required");
                return errors;
            }
            if (value.MetaInfo == null || value.MetaInfo.ID == System.Guid.Empty) errors.Add("case_id_required");
            if (value.SourceTrajectoryID == System.Guid.Empty) errors.Add("source_trajectory_required");
            if (!DefinedPositive(value.InterpolationInterval)) errors.Add("interpolation_interval_must_be_positive");
            if (!System.Enum.IsDefined(value.Mode)) errors.Add("mode_invalid");
            UpgradeLegacyGeosteeringExtent(value);

            switch (value.Specification)
            {
                case FixedLengthExtrapolationSpecification fixedLength when value.Mode == TrajectoryExtrapolationMode.FixedLength:
                    if (!DefinedPositive(fixedLength.Length)) errors.Add("extension_length_must_be_positive");
                    if (!System.Enum.IsDefined(fixedLength.ExtensionType)) errors.Add("extension_type_invalid");
                    break;
                case ReconnectTrajectoryExtrapolationSpecification reconnect when value.Mode == TrajectoryExtrapolationMode.ReconnectToTrajectory:
                    if (reconnect.ReferenceTrajectoryID == System.Guid.Empty) errors.Add("reference_trajectory_required");
                    if (reconnect.ReferenceTrajectoryID == value.SourceTrajectoryID) errors.Add("reference_trajectory_must_differ_from_source");
                    if (!DefinedPositive(reconnect.ReferenceMDAdvance)) errors.Add("reference_md_advance_must_be_positive");
                    if (!DefinedPositive(reconnect.JunctionCurvatureRatio)) errors.Add("junction_curvature_ratio_must_be_positive");
                    if (!System.Enum.IsDefined(reconnect.CurveType)) errors.Add("curve_type_invalid");
                    if (!DefinedNonNegative(reconnect.LeadInLength)) errors.Add("lead_in_length_must_be_non_negative");
                    break;
                case WellPathExtrapolationSpecification wellPath when value.Mode == TrajectoryExtrapolationMode.WellPath:
                    ValidateWellPath(wellPath, errors);
                    break;
                case GeosteeringTrajectoryExtrapolationSpecification geosteering when value.Mode == TrajectoryExtrapolationMode.Geosteering:
                    ValidateGeosteering(geosteering, errors);
                    break;
                case null:
                    errors.Add("specification_required");
                    break;
                default:
                    errors.Add("specification_does_not_match_mode");
                    break;
            }
            return errors;
        }

        /// <summary>
        /// Converts the retired total-length representation into the length of the two steering
        /// sections. This keeps persisted pre-rename cases geometrically unchanged.
        /// </summary>
        public static void UpgradeLegacyGeosteeringExtent(TrajectoryExtrapolationCase value)
        {
            if (value.Specification is not GeosteeringTrajectoryExtrapolationSpecification
                {
                    Extent: DrilledLengthGeosteeringExtentConstraint drilled
                } geosteering ||
                drilled.LegacyOverallDrilledLength is not double legacyOverallLength)
            {
                return;
            }

            if (!DefinedPositive(drilled.SteeringLength))
            {
                drilled.SteeringLength = legacyOverallLength - geosteering.LeadInLength;
            }
            drilled.LegacyOverallDrilledLength = null;
        }

        private static void ValidateGeosteering(GeosteeringTrajectoryExtrapolationSpecification value, List<string> errors)
        {
            if (!DefinedNonNegative(value.LeadInLength)) errors.Add("lead_in_length_must_be_non_negative");
            if (!Defined(value.TargetVerticalDepth)) errors.Add("target_vertical_depth_required");
            if (!Defined(value.EndInclination) || value.EndInclination < 0.0 || value.EndInclination > System.Math.PI)
                errors.Add("end_inclination_out_of_range");
            if (!Defined(value.EndAzimuth)) errors.Add("end_azimuth_required");
            if (!System.Enum.IsDefined(value.CurveType)) errors.Add("curve_type_invalid");
            switch (value.Extent)
            {
                case DepartureGeosteeringExtentConstraint departure:
                    if (!DefinedPositive(departure.DepartureDistance)) errors.Add("departure_distance_must_be_positive");
                    if (!Defined(departure.DepartureBearing)) errors.Add("departure_bearing_required");
                    break;
                case DrilledLengthGeosteeringExtentConstraint drilled:
                    if (!DefinedPositive(drilled.SteeringLength)) errors.Add("steering_length_must_be_positive");
                    if (!DefinedPositive(drilled.SteeringLengthRatio)) errors.Add("steering_length_ratio_must_be_positive");
                    break;
                case null:
                    errors.Add("geosteering_extent_required");
                    break;
                default:
                    errors.Add("geosteering_extent_invalid");
                    break;
            }
        }

        private static void ValidateWellPath(WellPathExtrapolationSpecification value, List<string> errors)
        {
            if (value.SectionList is not { Count: > 0 })
            {
                errors.Add("well_path_section_required");
                return;
            }

            int total = 0;
            int running = 0;
            HashSet<System.Guid> ids = [];
            for (int index = 0; index < value.SectionList.Count; index++)
            {
                WellPathSectionSpecification? section = value.SectionList[index];
                if (section == null)
                {
                    errors.Add($"section_{index}_required");
                    continue;
                }
                if (section.SectionID == System.Guid.Empty || !ids.Add(section.SectionID)) errors.Add($"section_{index}_id_invalid");
                int count = CommonConstraintCount(section);
                switch (section)
                {
                    case CircularArcWellPathSectionSpecification circular:
                        count += Defined(circular.Curvature) ? 1 : 0;
                        count += Defined(circular.StartToolface) ? 1 : 0;
                        if (Defined(circular.Curvature) && circular.Curvature < 0.0) errors.Add($"section_{index}_curvature_must_be_non_negative");
                        break;
                    case ConstantBuildAndTurnWellPathSectionSpecification buildTurn:
                        count += Defined(buildTurn.BuildRate) ? 1 : 0;
                        count += Defined(buildTurn.TurnRate) ? 1 : 0;
                        break;
                    case ConstantCurvatureAndToolfaceWellPathSectionSpecification curvatureToolface:
                        count += Defined(curvatureToolface.Curvature) ? 1 : 0;
                        count += Defined(curvatureToolface.Toolface) ? 1 : 0;
                        if (Defined(curvatureToolface.Curvature) && curvatureToolface.Curvature < 0.0) errors.Add($"section_{index}_curvature_must_be_non_negative");
                        break;
                    default:
                        errors.Add($"section_{index}_curve_type_invalid");
                        break;
                }
                if (Defined(section.Length) && section.Length <= 0.0) errors.Add($"section_{index}_length_must_be_positive");
                if (Defined(section.EndInclination) && (section.EndInclination < 0.0 || section.EndInclination > System.Math.PI))
                    errors.Add($"section_{index}_inclination_out_of_range");
                if (count == 0) errors.Add($"section_{index}_requires_a_constraint");
                running += count;
                total += count;
                if (running > 3 * (index + 1)) errors.Add($"section_{index}_prefix_overdetermined");
            }
            if (total != 3 * value.SectionList.Count) errors.Add("well_path_requires_exactly_three_constraints_per_section");
        }

        private static int CommonConstraintCount(WellPathSectionSpecification value) =>
            (Defined(value.Length) ? 1 : 0) +
            (Defined(value.EndInclination) ? 1 : 0) +
            (Defined(value.EndAzimuth) ? 1 : 0) +
            (Defined(value.EndVerticalDepth) ? 1 : 0) +
            (Defined(value.EndNorth) ? 1 : 0) +
            (Defined(value.EndEast) ? 1 : 0);

        private static bool Defined(double? value) => value is double number && Numeric.IsDefined(number);
        private static bool DefinedPositive(double value) => Numeric.IsDefined(value) && value > 0.0;
        private static bool DefinedNonNegative(double value) => Numeric.IsDefined(value) && value >= 0.0;
    }
}
