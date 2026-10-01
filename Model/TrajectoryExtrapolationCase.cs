using OSDC.DotnetLibraries.Drilling.Section;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.Trajectory.Model
{
    public enum TrajectoryExtrapolationMode
    {
        FixedLength = 0,
        ReconnectToTrajectory = 1,
        WellPath = 2,
        Geosteering = 3
    }

    public enum FixedLengthExtrapolationType
    {
        Straight = 0,
        ContinueCircularArc = 1,
        ContinueConstantBuildAndTurn = 2,
        ContinueConstantCurvatureAndToolface = 3
    }

    public enum ExtrapolationCurveType
    {
        CircularArc = 0,
        ConstantBuildAndTurn = 1,
        ConstantCurvatureAndToolface = 2
    }

    public enum TrajectoryExtrapolationSectionRole
    {
        Unspecified = 0,
        FixedLengthExtension = 1,
        LeadInContinuation = 2,
        UpstreamSteeringSection = 3,
        DownstreamSteeringSection = 4,
        WellPathSection = 5
    }

    public class TrajectoryExtrapolationCaseLight
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }
        public Guid SourceTrajectoryID { get; set; }
        public TrajectoryExtrapolationMode Mode { get; set; }
        public CalculationState CalculationState { get; set; } = CalculationState.Queued;
        public double CalculationProgress { get; set; }
        public string? CalculationMessage { get; set; }
    }

    public class TrajectoryExtrapolationCase : TrajectoryExtrapolationCaseLight
    {
        public const double DefaultInterpolationInterval = 30.0;

        /// <summary>Sampling interval in canonical SI metres along hole.</summary>
        public double InterpolationInterval { get; set; } = DefaultInterpolationInterval;

        /// <summary>
        /// Exactly one concrete specification is required. Its JSON discriminator is named Mode and must
        /// agree with the case Mode.
        /// </summary>
        public TrajectoryExtrapolationSpecification? Specification { get; set; }

        /// <summary>Frozen source endpoint used by the completed calculation.</summary>
        public SurveyStation? StartStation { get; set; }

        /// <summary>Frozen reconnect target, when reconnecting to a reference trajectory.</summary>
        public SurveyStation? TargetStation { get; set; }

        public double? ClosestReferenceMD { get; set; }
        public double? TargetReferenceMD { get; set; }
        public DateTimeOffset? SourceTrajectoryRevision { get; set; }
        public DateTimeOffset? ReferenceTrajectoryRevision { get; set; }
        public List<TrajectoryExtrapolationSolvedSection>? SolvedSectionList { get; set; }
        public List<SurveyStation>? SurveyStationList { get; set; }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "Mode")]
    [JsonDerivedType(typeof(FixedLengthExtrapolationSpecification), "FixedLength")]
    [JsonDerivedType(typeof(ReconnectTrajectoryExtrapolationSpecification), "ReconnectToTrajectory")]
    [JsonDerivedType(typeof(WellPathExtrapolationSpecification), "WellPath")]
    [JsonDerivedType(typeof(GeosteeringTrajectoryExtrapolationSpecification), "Geosteering")]
    public abstract class TrajectoryExtrapolationSpecification
    {
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class FixedLengthExtrapolationSpecification : TrajectoryExtrapolationSpecification
    {
        /// <summary>Extension length in canonical SI metres along hole.</summary>
        public double Length { get; set; }
        public FixedLengthExtrapolationType ExtensionType { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class ReconnectTrajectoryExtrapolationSpecification : TrajectoryExtrapolationSpecification
    {
        public Guid ReferenceTrajectoryID { get; set; }
        /// <summary>Positive distance in SI metres added to the closest reference measured depth.</summary>
        public double ReferenceMDAdvance { get; set; }
        public ExtrapolationCurveType CurveType { get; set; }
        /// <summary>Explicit whole-turn azimuth branch; zero selects the shortest wrapped turn.</summary>
        public int AzimuthBranch { get; set; }
        /// <summary>Junction-curvature ratio for build-and-turn pairs; one gives a smooth join.</summary>
        public double JunctionCurvatureRatio { get; set; } = 1.0;
        /// <summary>
        /// Initial continuation of the source trajectory's final curve before steering can begin, in SI metres.
        /// </summary>
        public double LeadInLength { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class GeosteeringTrajectoryExtrapolationSpecification : TrajectoryExtrapolationSpecification
    {
        /// <summary>Initial continuation of the source trajectory's final curve, in SI metres.</summary>
        public double LeadInLength { get; set; }
        /// <summary>Final WGS84 vertical depth, positive downward and in SI metres.</summary>
        public double TargetVerticalDepth { get; set; }
        /// <summary>Final inclination in SI radians from downward vertical.</summary>
        public double EndInclination { get; set; }
        /// <summary>Final true-north azimuth in SI radians, positive east of north.</summary>
        public double EndAzimuth { get; set; }
        public ExtrapolationCurveType CurveType { get; set; }
        /// <summary>Explicit whole-turn azimuth branch; zero selects the shortest wrapped turn.</summary>
        public int AzimuthBranch { get; set; }
        [JsonRequired]
        public GeosteeringExtentConstraint? Extent { get; set; }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "ExtentType")]
    [JsonDerivedType(typeof(DepartureGeosteeringExtentConstraint), "Departure")]
    [JsonDerivedType(typeof(DrilledLengthGeosteeringExtentConstraint), "DrilledLength")]
    public abstract class GeosteeringExtentConstraint
    {
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class DepartureGeosteeringExtentConstraint : GeosteeringExtentConstraint
    {
        /// <summary>Overall horizontal departure from the final source station, in SI metres.</summary>
        public double DepartureDistance { get; set; }
        /// <summary>Departure bearing in SI radians, positive east of WGS84 true north.</summary>
        public double DepartureBearing { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class DrilledLengthGeosteeringExtentConstraint : GeosteeringExtentConstraint
    {
        /// <summary>Total length from the final source station through the lead-in and both steering sections.</summary>
        public double OverallDrilledLength { get; set; }
        /// <summary>Positive ratio of upstream steering-section length to downstream steering-section length.</summary>
        public double SteeringLengthRatio { get; set; } = 1.0;
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class WellPathExtrapolationSpecification : TrajectoryExtrapolationSpecification
    {
        public List<WellPathSectionSpecification> SectionList { get; set; } = [];
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "CurveType")]
    [JsonDerivedType(typeof(CircularArcWellPathSectionSpecification), "CircularArc")]
    [JsonDerivedType(typeof(ConstantBuildAndTurnWellPathSectionSpecification), "ConstantBuildAndTurn")]
    [JsonDerivedType(typeof(ConstantCurvatureAndToolfaceWellPathSectionSpecification), "ConstantCurvatureAndToolface")]
    public abstract class WellPathSectionSpecification
    {
        public Guid SectionID { get; set; } = Guid.NewGuid();
        /// <summary>Optional along-hole section length in canonical SI metres.</summary>
        public double? Length { get; set; }
        /// <summary>Optional end inclination in SI radians from downward vertical.</summary>
        public double? EndInclination { get; set; }
        /// <summary>Optional end true-north azimuth in SI radians, positive east of north.</summary>
        public double? EndAzimuth { get; set; }
        /// <summary>Optional end WGS84 vertical depth in SI metres, positive downward.</summary>
        public double? EndVerticalDepth { get; set; }
        /// <summary>Optional end local north coordinate in SI metres.</summary>
        public double? EndNorth { get; set; }
        /// <summary>Optional end local east coordinate in SI metres.</summary>
        public double? EndEast { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class CircularArcWellPathSectionSpecification : WellPathSectionSpecification
    {
        /// <summary>Optional circular-arc curvature in SI radians per metre.</summary>
        public double? Curvature { get; set; }
        /// <summary>Optional circular-arc toolface at section start in SI radians.</summary>
        public double? StartToolface { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class ConstantBuildAndTurnWellPathSectionSpecification : WellPathSectionSpecification
    {
        /// <summary>Optional constant inclination build rate in SI radians per metre.</summary>
        public double? BuildRate { get; set; }
        /// <summary>Optional constant azimuth turn rate in SI radians per metre.</summary>
        public double? TurnRate { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class ConstantCurvatureAndToolfaceWellPathSectionSpecification : WellPathSectionSpecification
    {
        /// <summary>Optional constant curvature in SI radians per metre.</summary>
        public double? Curvature { get; set; }
        /// <summary>Optional constant toolface in SI radians.</summary>
        public double? Toolface { get; set; }
    }

    public class TrajectoryExtrapolationSolvedSection
    {
        public Guid SectionID { get; set; }
        public int SectionIndex { get; set; }
        public TrajectoryExtrapolationSectionRole Role { get; set; }
        public ExtrapolationCurveType CurveType { get; set; }
        public double StartMD { get; set; }
        public double EndMD { get; set; }
        public double Length { get; set; }
        public SurveyStation? Start { get; set; }
        public SurveyStation? End { get; set; }
        public double? CircularArcCurvature { get; set; }
        public double? CircularArcStartToolface { get; set; }
        public double? ConstantBuildRate { get; set; }
        public double? ConstantTurnRate { get; set; }
        public double? ConstantCurvature { get; set; }
        public double? ConstantToolface { get; set; }
    }
}
