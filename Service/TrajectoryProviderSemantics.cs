using System.Reflection;
using System.Text.Json.Nodes;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using Catalogue = OSDC.DotnetLibraries.Drilling.SemanticCatalogue.SemanticCatalogue;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>Reviewed Trajectory semantic bindings shared by REST and MCP contracts.</summary>
internal static class TrajectoryProviderSemantics
{
    private static readonly IReadOnlyDictionary<string, (string Concept, string? Role, string? Reference)> Aliases =
        new Dictionary<string, (string, string?, string?)>(StringComparer.Ordinal)
        {
            ["DrilledLength"] = (Concepts.MeasuredDepth, null, null),
            ["Wgs84Depth"] = (Concepts.EllipsoidalDepth, null, Concepts.Wgs84),
            ["GeodeticInclination"] = (Concepts.WellboreInclination, null, Concepts.Wgs84DownwardNormal),
            ["TrueNorthAzimuth"] = (Concepts.WellboreAzimuth, null, Concepts.TrueNorthClockwise),
            ["Curvature"] = (Concepts.WellboreCurvature, null, null),
            ["Length"] = (Concepts.PhysicalLengthExtent, null, null),
            ["Inclination"] = (Concepts.WellboreInclination, null, Concepts.Wgs84DownwardNormal),
            ["ToolfaceOrientation"] = (Concepts.Toolface, null, null),
            ["BuildUpRate"] = (Concepts.BuildRate, null, null),
            ["HorizontalDistance"] = (Concepts.DepartureDistance, null, null),
            ["ProportionStandard"] = (Concepts.ConfidenceFactor, null, null),
            ["AppliedInclinationCorrection"] = (Concepts.AppliedInclinationCorrection, null, null),
            ["AppliedAzimuthCorrection"] = (Concepts.AppliedAzimuthCorrection, null, null),
            ["MagneticDeclination"] = (Concepts.MagneticDeclination, null, null),
            ["Acceleration"] = (Concepts.GravityAcceleration, null, null),
            ["GeodeticLatitude"] = (Concepts.Latitude, null, Concepts.Wgs84),
            ["GeodeticLongitude"] = (Concepts.Longitude, null, Concepts.Wgs84),
            ["CalculationMessage"] = (Concepts.CalculationDiagnosticMessage, null, null),
            ["TrajectoryAggregationCaseLight"] = (Concepts.TrajectoryAggregationCase, Concepts.CalculationStatusProjection, null),
            ["TrajectoryRealizationCaseLight"] = (Concepts.TrajectoryRealizationCase, Concepts.CalculationStatusProjection, null),
            ["TrajectoryExtrapolationCaseLight"] = (Concepts.TrajectoryExtrapolationCase, Concepts.CalculationStatusProjection, null),
            ["TargetLandingCaseLight"] = (Concepts.TargetLandingCase, Concepts.CalculationStatusProjection, null),
            ["DirectionalControlEvaluationCaseLight"] = (Concepts.DirectionalControlEvaluationCase, Concepts.CalculationStatusProjection, null),
            ["TrajectoryMinimumDistanceCalculationLight"] = (Concepts.MinimumDistanceCalculation, Concepts.CalculationStatusProjection, null),
            ["SurveyRunMinimumDistanceCalculationLight"] = (Concepts.MinimumDistanceCalculation, Concepts.CalculationStatusProjection, null),
            ["GlobalAntiCollisionCalculationStatus"] = (Concepts.GlobalAntiCollisionCalculation, Concepts.CalculationStatusProjection, null),
            ["InterpolatedTrajectoryLight"] = (Concepts.InterpolatedTrajectory, Concepts.CalculationStatusProjection, null),
            ["InterpolatedTrajectory"] = (Concepts.InterpolatedTrajectory, null, null),
            ["DirectionalControlEvaluationSampleChunk"] = (Concepts.CalculationResultChunk, Concepts.ServerDerivedCalculationResult, null),
            ["TrajectoryAggregationDistanceResultChunk"] = (Concepts.CalculationResultChunk, Concepts.ServerDerivedCalculationResult, null),
            ["TrajectoryRealizationChunk"] = (Concepts.CalculationResultChunk, Concepts.ServerDerivedCalculationResult, null),
            ["TrajectoryMinimumDistanceResultChunk"] = (Concepts.CalculationResultChunk, Concepts.ServerDerivedCalculationResult, null),
            ["SurveyRunMinimumDistanceResultChunk"] = (Concepts.CalculationResultChunk, Concepts.ServerDerivedCalculationResult, null),
            ["SurveyStationChunk"] = (Concepts.CalculationResultChunk, Concepts.ServerDerivedCalculationResult, null),
            ["SurveyPointChunk"] = (Concepts.CalculationResultChunk, Concepts.ServerDerivedCalculationResult, null)
        };

    private static readonly IReadOnlyDictionary<string, string> CalculationConceptsByController =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["TrajectoryAggregationCase"] = Concepts.TrajectoryAggregationCase,
            ["TrajectoryRealizationCase"] = Concepts.TrajectoryRealizationCase,
            ["TrajectoryExtrapolationCase"] = Concepts.TrajectoryExtrapolationCase,
            ["TargetLandingCase"] = Concepts.TargetLandingCase,
            ["DirectionalControlEvaluationCase"] = Concepts.DirectionalControlEvaluationCase,
            ["TrajectoryMinimumDistanceCalculation"] = Concepts.MinimumDistanceCalculation,
            ["SurveyRunMinimumDistanceCalculation"] = Concepts.MinimumDistanceCalculation,
            ["SurveyStationEllipseCalculation"] = Concepts.SurveyStationEllipseCalculation,
            ["GlobalAntiCollisions"] = Concepts.GlobalAntiCollisionCalculation,
            ["InterpolatedTrajectory"] = Concepts.InterpolatedTrajectory
        };

    private static readonly IReadOnlyDictionary<string, string> ConceptsByName = typeof(Concepts)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .ToDictionary(field => field.Name, field => (string)field.GetRawConstantValue()!, StringComparer.Ordinal);

    public static JsonObject? For(string binding)
    {
        (string Concept, string? Role, string? Reference) value;
        if (!Aliases.TryGetValue(binding, out value))
        {
            if (!ConceptsByName.TryGetValue(binding, out string? concept)) return null;
            value = (concept, null, null);
        }
        return Metadata(value.Concept, value.Role, value.Reference);
    }

    public static JsonObject? ForType(Type type) =>
        SemanticMetadata.For(type) ?? For(type.Name);

    public static JsonObject? ForProperty(PropertyInfo property) =>
        SemanticMetadata.For(property) ?? For(property.Name);

    public static JsonObject? ForOperation(string controller, MethodInfo method)
    {
        if (!CalculationConceptsByController.TryGetValue(controller, out string? concept)) return null;
        string action = method.Name;
        string? role = action switch
        {
            _ when action.StartsWith("Post", StringComparison.Ordinal) =>
                controller == "SurveyStationEllipseCalculation"
                    ? Concepts.ImmediateCalculationSubmission
                    : Concepts.QueuedCalculationSubmission,
            _ when action.StartsWith("Put", StringComparison.Ordinal) => Concepts.QueuedCalculationReplacement,
            "GetStatus" => Concepts.CalculationStatusRetrieval,
            _ when action.Contains("Chunk", StringComparison.Ordinal) &&
                   !action.Contains("ChunkCount", StringComparison.Ordinal) => Concepts.CalculationResultChunkRetrieval,
            _ when action.Contains("ChunkCount", StringComparison.Ordinal) ||
                   action.Contains("DisplayData", StringComparison.Ordinal) => Concepts.CalculationResultRetrieval,
            _ when action.Contains("Light", StringComparison.Ordinal) => Concepts.CalculationStatusRetrieval,
            _ when action.StartsWith("Get", StringComparison.Ordinal) => Concepts.CalculationCaseRetrieval,
            _ => null
        };
        return role is null ? null : Metadata(concept, role, null);
    }

    private static JsonObject Metadata(string concept, string? role, string? reference)
    {
        var catalogue = Catalogue.Default;
        var definition = catalogue.Get(concept);
        var result = new JsonObject
        {
            ["catalogue"] = catalogue.Document.Id,
            ["catalogueVersion"] = catalogue.Document.Version,
            ["concept"] = concept,
            ["curationStatus"] = definition.Status.ToString(),
            ["assertionSource"] = "provider-binding-registry",
            ["requiredContext"] = new JsonArray(catalogue.RequiredContext(concept).Select(x => (JsonNode?)JsonValue.Create(x)).ToArray())
        };
        if (role != null) result["role"] = role;
        if (reference != null) result["reference"] = reference;
        if (catalogue.SiUnit(concept) is string unit) result["siUnit"] = unit;
        if (catalogue.Quantity(concept) is QuantityIdentity quantity)
        {
            result["physicalQuantityStatus"] = "resolved";
            result["physicalQuantity"] = new JsonObject
            {
                ["catalogue"] = quantity.Catalogue, ["id"] = quantity.Id.ToString(),
                ["name"] = quantity.Name, ["siUnitName"] = quantity.SiUnitName
            };
        }
        return result;
    }
}
