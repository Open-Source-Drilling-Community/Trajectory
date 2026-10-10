using System.Reflection;
using System.Text.Json.Nodes;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Math;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using Catalogue = OSDC.DotnetLibraries.Drilling.SemanticCatalogue.SemanticCatalogue;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>Reviewed Trajectory semantic bindings shared by REST and MCP contracts.</summary>
internal static class TrajectoryProviderSemantics
{
    private static readonly IReadOnlyDictionary<string, (string Concept, string? Role, string? Reference)> Aliases =
        new Dictionary<string, (string, string?, string?)>(StringComparer.Ordinal)
        {
            ["DrilledLength"] = (Concepts.AlongHoleDepth, null, null),
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
            ["MetaInfo"] = (Concepts.ResourceMetadata, null, null),
            ["Name"] = (Concepts.ResourceName, null, null),
            ["Description"] = (Concepts.ResourceDescription, null, null),
            ["CreationDate"] = (Concepts.Instant, Concepts.CreationTime, Concepts.Utc),
            ["LastModificationDate"] = (Concepts.Instant, Concepts.LastModificationTime, Concepts.Utc),
            ["CalculationType"] = (Concepts.TrajectoryCalculationMethod, null, null),
            ["MDStep"] = (Concepts.InterpolationInterval, null, null),
            ["StartAbscissa"] = (Concepts.AlongHoleDepth, null, null),
            ["DLS"] = (Concepts.WellboreCurvature, null, null),
            ["BUR"] = (Concepts.BuildRate, null, null),
            ["TUR"] = (Concepts.TurnRate, null, null),
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
        if (Catalogue.Default.Get(value.Concept).Kind != SemanticKind.Noun) return null;
        return Metadata(value.Concept, value.Role, value.Reference);
    }

    public static JsonObject? ForType(Type type) =>
        typeof(TrajectoryLight).IsAssignableFrom(type) ? Metadata(Concepts.Trajectory, null, null) :
        typeof(SurveyRunLight).IsAssignableFrom(type) ? Metadata(Concepts.SurveyRun, null, null) :
        type == typeof(TrajectoryCalculationType) ? Metadata(Concepts.TrajectoryCalculationMethod, null, null) :
        SemanticMetadata.For(type) ?? For(type.Name);

    public static JsonObject? ForProperty(PropertyInfo property)
    {
        // ReflectedType retains the actual survey context for inherited X/Y/Z/Abscissa.
        // The same CLR names on an arbitrary vector or curvilinear object do not establish a datum.
        Type? owner = property.ReflectedType ?? property.DeclaringType;
        if (owner != null && typeof(Point3DGlobalCoordinates).IsAssignableFrom(owner))
        {
            var binding = property.Name switch
            {
                "Abscissa" or "MD" when typeof(SurveyPoint).IsAssignableFrom(owner) => (Concepts.AlongHoleDepth, Concepts.Wgs84AlongHoleOrigin),
                "X" or "RiemannianNorth" => (Concepts.RiemannianNorth, Concepts.Wgs84RiemannianCoordinates),
                "Y" or "RiemannianEast" => (Concepts.RiemannianEast, Concepts.Wgs84RiemannianCoordinates),
                "Z" or "TVD" => (Concepts.TrueVerticalDepth, Concepts.Wgs84),
                "Latitude" => (Concepts.Latitude, Concepts.Wgs84),
                "Longitude" => (Concepts.Longitude, Concepts.Wgs84),
                "Inclination" => (Concepts.WellboreInclination, Concepts.Wgs84DownwardNormal),
                "Azimuth" => (Concepts.WellboreAzimuth, Concepts.TrueNorthClockwise),
                _ => ((string?)null, (string?)null)
            };
            if (binding.Item1 is { } concept)
            {
                var result = Metadata(concept, null, binding.Item2);
                if (property.Name is "MD" or "TVD" or "RiemannianNorth" or "RiemannianEast")
                    result["valueAliasOf"] = property.Name switch { "MD" => "Abscissa", "TVD" => "Z", "RiemannianNorth" => "X", _ => "Y" };
                return result;
            }
        }
        if (owner == typeof(SurveyMeasurement) && property.Name == "MD") return Metadata(Concepts.AlongHoleDepth, null, null);
        if (owner == typeof(InterpolatedTrajectory) && property.Name == "InterpolationStep")
            return Metadata(Concepts.InterpolationInterval, Concepts.CalculationInput, null);
        if (owner == typeof(SurveyStationEllipseResult)) return property.Name switch {
            "MD" => Metadata(Concepts.AlongHoleDepth, null, Concepts.Wgs84AlongHoleOrigin),
            "HorizontalEllipse" => Metadata(Concepts.HorizontalUncertaintyEllipse, null, null),
            "VerticalEllipse" => Metadata(Concepts.VerticalUncertaintyEllipse, null, null),
            "PerpendicularEllipse" => Metadata(Concepts.PerpendicularUncertaintyEllipse, null, null), _ => null };
        if (owner == typeof(SurveyStationEllipse))
        {
            if (property.Name == "OrientationAngle")return Metadata(Concepts.UncertaintyEllipseOrientation, null, null);
            if (property.Name is "SemiMajorAxis" or "SemiMinorAxis")
                return Metadata(Concepts.PhysicalLengthExtent, property.Name == "SemiMajorAxis" ? Concepts.SemiMajorAxis : Concepts.SemiMinorAxis, null);
        }
        if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?) || IsIdentifierCollection(property.PropertyType))
        {
            string? resource = ResourceForIdentifier(property.Name);
            return Identifier(resource);
        }
        return SemanticMetadata.For(property) ?? For(property.Name);
    }

    public static JsonObject? ForParameter(string controller, ParameterInfo parameter)
    {
        if(controller == "Trajectory") {
            if(parameter.Name == "alongHoleDepth")return Metadata(Concepts.AlongHoleDepth, null,
                parameter.Member.Name=="GetReferencedTrajectoryStation"?null:Concepts.Wgs84AlongHoleOrigin);
            if(parameter.Name == "confidenceFactor")return Metadata(Concepts.ConfidenceFactor, null, null);
            if(parameter.Name == "originWgs84Depth") {
                var origin=Metadata(Concepts.EllipsoidalDepth, Concepts.SourceReference, Concepts.Wgs84);
                origin["keyOriginFor"]="/alongHoleDepth";return origin;
            }
        }
        string? resource = parameter.Name == "id" ? ResourceForController(controller) : ResourceForIdentifier(parameter.Name ?? "");
        if ((parameter.ParameterType == typeof(Guid) || parameter.ParameterType == typeof(Guid?) || IsIdentifierCollection(parameter.ParameterType)) && resource != null)
            return Identifier(resource);
        if (parameter.Name == "expectedModifiedUtc")
            return Metadata(Concepts.Instant, Concepts.LastModificationTime, Concepts.Utc);
        return null;
    }

    public static string? ResourceForController(string controller) => controller switch
    {
        "Trajectory" => Concepts.Trajectory,
        "SurveyRun" => Concepts.SurveyRun,
        _ => CalculationConceptsByController.GetValueOrDefault(controller)
    };

    private static string? ResourceForIdentifier(string name) => name switch
    {
        "TrajectoryID" or "SourceTrajectoryID" or "ReferenceTrajectoryID" or "ComparisonTrajectoryID" or "trajectoryId" or "TrajectoryIDs" or "ComparisonTrajectoryIDs" or "trajectoryIds" => Concepts.Trajectory,
        "SurveyRunID" or "ParentSurveyRunID" or "surveyRunId" or "SurveyRunIDs" or "surveyRunIds" => Concepts.SurveyRun,
        "WellBoreID" or "wellBoreId" => Concepts.WellBore,
        "WellID" or "wellId" => Concepts.Well,
        "FieldID" or "fieldId" => Concepts.Field,
        "ClusterID" or "clusterId" => Concepts.WellCluster,
        _ => null
    };

    private static JsonObject Identifier(string? resource)
    {
        var metadata = Metadata(Concepts.ResourceIdentifier, null, null);
        // A typed UUID remains an identifier noun, not a resource object or a datum.
        if (resource != null) metadata["resourceType"] = resource;
        return metadata;
    }

    public static JsonObject IdentifierForResource(string resource) => Identifier(resource);

    public static bool IsIdentifierCollection(Type type) => type != typeof(string) &&
        type.GetInterfaces().Append(type).Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>) && t.GetGenericArguments()[0] == typeof(Guid));

    public static void AttachMcpPropertyMetadata(JsonObject schema, JsonObject metadata, Type type)
    {
        var target = schema;
        if (IsIdentifierCollection(type))
        {
            var array = schema["type"]?.ToString() == "array" ? schema :
                (schema["anyOf"] as JsonArray)?.OfType<JsonObject>().SingleOrDefault(v => v["type"]?.ToString() == "array");
            if (array?["items"] is JsonObject item) target = item;
        }
        target[SemanticMetadata.ExtensionName] = metadata;
    }

    public static string? PropertyDescription(Type owner, string name)
    {
        if(owner==typeof(VerticalSurveyUncertaintyEllipse))return name switch {
            "MajorAxis"=>"Full major-axis diameter, twice the shared uncertainty ellipse semi-major axis, in SI metres.",
            "MinorAxis"=>"Full minor-axis diameter, twice the shared uncertainty ellipse semi-minor axis, in SI metres.",
            "OrientationAngle"=>"Radians in the fixed vertical-section ellipse axis convention: major-axis direction (-sin(phi),cos(phi)) in (section distance, positive-down TVD). An axis is unoriented, modulo pi. This is not an azimuth relative to north or the wellbore tangent.",_=>null};
        if(owner==typeof(HorizontalSurveyUncertaintyEllipse))return name switch {
            "MajorAxis"=>"Full major-axis diameter, twice the shared uncertainty ellipse semi-major axis, in SI metres.",
            "MinorAxis"=>"Full minor-axis diameter, twice the shared uncertainty ellipse semi-minor axis, in SI metres.",
            "OrientationAngle"=>"Radians clockwise from true north toward east for the horizontal ellipse major axis. An axis is unoriented, modulo pi.",_=>null};
        if(owner==typeof(TrajectoryVerticalEllipseEvaluation) && name=="VerticalSectionAzimuth")return "True-north-clockwise section azimuth in radians, selected from the first to the last horizontally separated source trajectory stations. The full source path determines the plane, including when sampling one MD.";
        if (typeof(Point3DGlobalCoordinates).IsAssignableFrom(owner)) return name switch
        {
            "Abscissa" or "MD" when typeof(SurveyPoint).IsAssignableFrom(owner) => "Measured depth (MD), the curvilinear coordinate along this trajectory/survey run, in SI metres from its declared MD origin. MD and Abscissa are aliases. This is not true vertical depth; a vertical datum offset alone does not change an MD origin.",
            "Z" or "TVD" => "True vertical depth (TVD) in SI metres, positive downward relative to the WGS84 ellipsoid in persisted Trajectory survey data. Z and TVD are aliases; this is not measured depth or an elevation.",
            "X" or "RiemannianNorth" => "WGS84 Riemannian north coordinate in SI metres: signed meridian arc from the equator, north positive. X and RiemannianNorth are aliases; this is not an arbitrary projected northing.",
            "Y" or "RiemannianEast" => "WGS84 Riemannian east coordinate in SI metres: signed arc along the latitude parallel from Greenwich, east positive. Y and RiemannianEast are aliases; this is not an arbitrary projected easting.",
            _ => null
        };
        return null;
    }

    public static JsonObject? ForOperation(string controller, MethodInfo method)
    {
        if (controller == "UncertaintyEnvelope" && method.Name == "Evaluate")
            return Metadata(Concepts.CircularlyDilatedUncertaintyEnvelope, Concepts.CircularUncertaintyEnvelopeDilation, null);
        if (!CalculationConceptsByController.TryGetValue(controller, out string? concept))
        {
            if (ResourceForController(controller) is not { } resource) return null;
            string resourceAction = method.Name;
            string resourceRole = resourceAction.Contains("Station", StringComparison.Ordinal) || resourceAction.Contains("Interpolate", StringComparison.Ordinal) ||
                          resourceAction.Contains("Validate", StringComparison.Ordinal) || resourceAction.Contains("Audit", StringComparison.Ordinal)
                ? Concepts.StatelessEvaluation
                : resourceAction.Contains("GetAll", StringComparison.Ordinal) || resourceAction.Contains("Search", StringComparison.Ordinal)
                    ? Concepts.ResourceCollectionRetrieval
                    : resourceAction.StartsWith("Get", StringComparison.Ordinal) ? Concepts.ResourceRetrieval
                    : resourceAction.StartsWith("Post", StringComparison.Ordinal) ? Concepts.ResourceCreation
                    : resourceAction.StartsWith("Put", StringComparison.Ordinal) ? Concepts.ResourceReplacement
                    : resourceAction.StartsWith("Patch", StringComparison.Ordinal) ? Concepts.ResourcePartialUpdate
                    : resourceAction.StartsWith("Delete", StringComparison.Ordinal) ? Concepts.ResourceDeletion
                    : Concepts.ResourceOperation;
            return Metadata(resource, resourceRole, null);
        }
        string action = method.Name;
        string? role = action switch
        {
            _ when action.StartsWith("Post", StringComparison.Ordinal) =>
                controller == "SurveyStationEllipseCalculation"
                    ? Concepts.ImmediateCalculationSubmission
                    : Concepts.QueuedCalculationSubmission,
            _ when action.StartsWith("Put", StringComparison.Ordinal) => Concepts.QueuedCalculationReplacement,
            _ when action.StartsWith("Delete", StringComparison.Ordinal) => Concepts.CalculationCaseDeletion,
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
        if (definition.Kind != SemanticKind.Noun) throw new InvalidOperationException("A value binding must identify a noun: " + concept);
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
