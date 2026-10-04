using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.GlobalAntiCollision;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Globalization;

namespace OSDC.Drilling.Trajectory.Service;

/// <summary>Adds reference-frame and SI semantics that cannot be inferred from CLR property names.</summary>
internal sealed class TrajectorySemanticSchemaFilter : ISchemaFilter
{
    private const string InclinationReferenceDescription =
        "Reference vertical for an observed inclination. GeodeticVertical is the local positive-down axis " +
        "perpendicular to the WGS84 ellipsoid (opposite its outward normal); GravityVertical follows the local " +
        "total-gravity vector; InheritRun uses the survey run default.";

    private const string AzimuthReferenceDescription =
        "North reference for an observed clockwise azimuth. TrueNorth is WGS84 geodetic north projected onto the " +
        "plane perpendicular to the selected vertical; MagneticNorth is the evaluated geomagnetic-field vector " +
        "projected onto that plane; InheritRun uses the survey run default.";

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(TrajectoryExtrapolationCase) &&
            schema.Properties.TryGetValue("Specification", out OpenApiSchema? specification))
        {
            specification.OneOf.Clear();
            specification.Reference = new OpenApiReference
            {
                Id = typeof(TrajectoryExtrapolationSpecification).FullName,
                Type = ReferenceType.Schema
            };
        }

        if (context.Type == typeof(WellPathExtrapolationSpecification) &&
            schema.Properties.TryGetValue("SectionList", out OpenApiSchema? sectionList))
        {
            // Swashbuckle otherwise emits an item-level oneOf which NSwag narrows to the
            // first concrete section. Reference the discriminated base so generated clients
            // retain a heterogeneous section collection.
            sectionList.Items = new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Id = typeof(WellPathSectionSpecification).FullName,
                    Type = ReferenceType.Schema
                }
            };
        }

        if (context.Type == typeof(GeosteeringTrajectoryExtrapolationSpecification) &&
            schema.Properties.TryGetValue("Extent", out OpenApiSchema? extent))
        {
            extent.OneOf.Clear();
            extent.Reference = new OpenApiReference
            {
                Id = typeof(GeosteeringExtentConstraint).FullName,
                Type = ReferenceType.Schema
            };
            Describe(schema, "LeadInLength", "Initial continuation of the source trajectory's final calculated curve before steering starts, in canonical SI metres.", "DrilledLength", "m");
            Describe(schema, "TargetVerticalDepth", "Absolute WGS84 vertical depth target, positive downward in canonical SI metres.", "Wgs84Depth", "m");
            Describe(schema, "EndInclination", "Target inclination from the local WGS84 geodetic-down axis in SI radians.", "GeodeticInclination", "rad");
            Describe(schema, "EndAzimuth", "Target clockwise azimuth from WGS84 geodetic true north in SI radians.", "TrueNorthAzimuth", "rad");
            Describe(schema, "Extent", "Exactly one constraint: overall Departure from the final source-trajectory station, or SteeringLength for the two steering sections.");
            SetMinimum(schema, "LeadInLength", 0.0m);
            SetRange(schema, "EndInclination", 0.0m, (decimal)Math.PI);
        }
        if (context.Type == typeof(ReconnectTrajectoryExtrapolationSpecification))
        {
            Describe(schema, "LeadInLength", "Initial continuation of the source trajectory's final calculated curve before the closest reference point is found and steering starts, in canonical SI metres.", "DrilledLength", "m");
            SetMinimum(schema, "LeadInLength", 0.0m);
        }
        if (context.Type == typeof(TargetLandingCase))
        {
            Describe(schema, "SourceTrajectoryID", "Non-empty UUID of the stored calculated trajectory from whose final station the landing is designed.");
            Describe(schema, "LeadLength", "Initial continuation of the source trajectory's final calculated trend before the newly designed landing sections begin, in canonical SI metres.", "DrilledLength", "m");
            Describe(schema, "MaximumLandingCurvature", "Optional hard curvature limit applied only to newly designed landing sections, in SI radians per metre. The default is 3 degrees per 30 metres.", "Curvature", "rad/m");
            DescribeConfidenceFactor(schema, (decimal)SurveyStationEllipseCalculation.MaximumConfidenceFactor);
            SetMinimum(schema, "LeadLength", 0.0m);
            SetExclusiveMinimum(schema, "MaximumLandingCurvature", 0.0m);
        }
        if (context.Type == typeof(TargetPlaneDefinition))
        {
            schema.Description = "Convex target polygon in an oriented plane. Plane defines the origin and forward normal; Polygon coordinates are canonical Cartesian metres in that plane.";
            Describe(schema, "Plane", "Target-plane origin and forward normal. RiemannianNorth/RiemannianEast and TVD are canonical local WGS84 NED metres; Latitude/Longitude are WGS84 radians; Inclination is from WGS84 geodetic down and Azimuth is clockwise from true north.");
            Describe(schema, "Polygon", "Ordered vertices of a simple convex target polygon in canonical plane Cartesian metres. At least three vertices are required.");
            if (schema.Properties.TryGetValue("Polygon", out OpenApiSchema? polygon)) polygon.MinItems = 3;
        }
        if (context.Type == typeof(TargetPlanePoint))
        {
            Describe(schema, "X", "First Cartesian coordinate in the target plane, in canonical SI metres.", "Length", "m");
            Describe(schema, "Y", "Second Cartesian coordinate in the target plane, positive toward the plane's projected vertical-up axis when the plane is not horizontal, in canonical SI metres.", "Length", "m");
        }
        if (context.Type == typeof(TargetLandingControlPoint))
        {
            schema.Description = "Authoritative curve-specific control state at one normalized position along a solved target-landing path.";
            Describe(schema, "NormalizedLength", "Dimensionless along-hole position over the complete landing path: zero is the steering start after the lead and one is the target boundary.", "LengthRatio", "1");
            Describe(schema, "Inclination", "Exact local trajectory inclination at this control sample, in SI radians. Near vertical, azimuth, toolface and turn rate are ill-conditioned even though the Cartesian path and curvature remain valid.", "Inclination", "rad");
            Describe(schema, "Curvature", "Local non-negative spatial curvature calculated from the defining solved curve, in SI radians per metre.", "Curvature", "rad/m");
            Describe(schema, "Toolface", "Local signed toolface about the borehole tangent, zero at high side and positive toward the right side, in SI radians. Circular-arc values vary from the arc's start/reference toolface.", "ToolfaceOrientation", "rad");
            Describe(schema, "BuildRate", "Signed local inclination derivative with respect to measured length, in SI radians per metre; positive builds inclination and negative drops it.", "BuildUpRate", "rad/m");
            Describe(schema, "TurnRate", "Signed local azimuth derivative with respect to measured length, in SI radians per metre.", "TurnRate", "rad/m");
            SetRange(schema, "NormalizedLength", 0.0m, 1.0m);
            SetRange(schema, "Inclination", 0.0m, (decimal)Math.PI);
            SetMinimum(schema, "Curvature", 0.0m);
        }
        if (context.Type == typeof(DepartureGeosteeringExtentConstraint))
        {
            Describe(schema, "DepartureDistance", "Overall horizontal departure from the final source-trajectory station in canonical SI metres.", "HorizontalDistance", "m");
            Describe(schema, "DepartureBearing", "Bearing of the overall departure, clockwise and positive east of WGS84 true north, in SI radians.", "TrueNorthAzimuth", "rad");
            SetExclusiveMinimum(schema, "DepartureDistance", 0.0m);
        }
        if (context.Type == typeof(DrilledLengthGeosteeringExtentConstraint))
        {
            Describe(schema, "SteeringLength", "Total along-hole length of the upstream and downstream steering sections, excluding LeadInLength, in canonical SI metres.", "DrilledLength", "m");
            Describe(schema, "SteeringLengthRatio", "Dimensionless positive ratio of upstream to downstream steering-section length.", "LengthRatio", "1");
            SetExclusiveMinimum(schema, "SteeringLength", 0.0m);
            SetExclusiveMinimum(schema, "SteeringLengthRatio", 0.0m);
        }

        if (context.Type == typeof(SurveyStationEllipseCalculation))
        {
            DescribeConfidenceFactor(schema,
                (decimal)SurveyStationEllipseCalculation.MaximumConfidenceFactor);
        }
        if (context.Type == typeof(AntiCollisionPolicyRevision) ||
            context.Type == typeof(AntiCollisionPolicyRevisionCreate) ||
            context.Type == typeof(OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision))
        {
            DescribeConfidenceFactor(schema,
                (decimal)OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision.MaximumConfidenceFactor);
        }
        if (context.Type == typeof(OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision))
            Describe(schema, "RequestedPolicyAssignmentID", "Optional caller-selected Field anti-collision policy assignment UUID. Omit it to calculate separation factors without policy classification. The service validates that it belongs to the reference trajectory's Field and derives the applied assignment, immutable policy snapshot, confidence factor and classifications.");

        if (context.Type == typeof(AntiCollisionPolicyRule) &&
            schema.Properties.TryGetValue("Conditions", out OpenApiSchema? conditions))
        {
            conditions.Items = new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Id = typeof(AntiCollisionPolicyCondition).FullName,
                    Type = ReferenceType.Schema
                }
            };
        }

        if (context.Type == typeof(AntiCollisionTrajectoryAgeCondition))
            Describe(schema, "AgeThresholdSeconds", "Comparison trajectory age threshold in canonical SI seconds, evaluated from the oldest defined contributing survey-run acquisition start or station measurement time.", "Duration", "s");
        if (context.Type == typeof(AntiCollisionPolicyRule))
        {
            SetMinimum(schema, "Priority", 1);
            Describe(schema, "AlertThreshold", "Dimensionless separation-factor Alert threshold; it must be greater than AlarmThreshold.", "SeparationFactor", "1");
            Describe(schema, "AlarmThreshold", "Dimensionless separation-factor Alarm threshold.", "SeparationFactor", "1");
        }

        if (context.Type == typeof(SurveyInclinationReference))
        {
            schema.Description = InclinationReferenceDescription;
            return;
        }
        if (context.Type == typeof(SurveyAzimuthReference))
        {
            schema.Description = AzimuthReferenceDescription;
            return;
        }
        if (context.Type == typeof(SurveyGeomagneticModel))
        {
            schema.Description = "Geomagnetic model used for magnetic-north correction: Automatic selects WMM2025 for 2025 or later and IGRF14 for earlier instants; an explicit value pins that model.";
            return;
        }

        if (typeof(SurveyRunLight).IsAssignableFrom(context.Type))
        {
            Describe(schema, "AcquisitionStartUtc", "Earliest known survey-run acquisition instant in UTC; required together with AcquisitionEndUtc when station times are absent for magnetic correction.");
            Describe(schema, "AcquisitionEndUtc", "Latest known survey-run acquisition instant in UTC; required together with AcquisitionStartUtc when station times are absent for magnetic correction.");
            RestrictRunDefault(schema, "DefaultInclinationReference", InclinationReferenceDescription);
            RestrictRunDefault(schema, "DefaultAzimuthReference", AzimuthReferenceDescription);
            Describe(schema, "GeomagneticModel", "Geomagnetic model selection used by measurements whose effective azimuth reference is MagneticNorth.");
        }
        if (context.Type == typeof(SurveyRun))
        {
            Describe(schema, "BitExtrapolation", "Optional terminal extrapolation from the final measurement-tool station to the bit. When present, exactly one terminal calculated or caller-identified extrapolated station is included in the calculated SurveyStationList.");
        }
        if (context.Type == typeof(SurveyRunBitExtrapolation))
        {
            Describe(schema, "Mode", "CalculateFromLastMeasurement derives a terminal station on the server; LastStationAlreadyExtrapolated requires exactly the final submitted row to have Origin Extrapolated.");
            Describe(schema, "MeasurementToolToBitDistance", "Distance-to-bit elevation of the measurement tool relative to the bit front face, positive upward in canonical SI metres; used as the positive along-hole MD increment to the bit.", "DistanceToBit", "m");
            if (schema.Properties.TryGetValue("MeasurementToolToBitDistance", out OpenApiSchema? distance))
            {
                distance.Minimum = 0.0m;
                distance.ExclusiveMinimum = true;
            }
        }
        if (context.Type == typeof(SurveyImportSettings))
        {
            RestrictRunDefault(schema, "DefaultInclinationReference", InclinationReferenceDescription);
            RestrictRunDefault(schema, "DefaultAzimuthReference", AzimuthReferenceDescription);
            Describe(schema, "GeomagneticModel", "Geomagnetic model selection copied to every survey run created by this import.");
        }
        if (context.Type == typeof(SurveyMeasurement))
        {
            Describe(schema, "MeasurementID", "Stable UUID for this measurement, independent of list position.");
            Describe(schema, "Origin", "Measured for an instrument observation; Extrapolated only for the final caller-supplied bit station in LastStationAlreadyExtrapolated mode.");
            Describe(schema, "MD", "Measured or along-hole depth in canonical SI metres.", "MeasuredDepth", "m");
            Describe(schema, "Inclination", "Canonical inclination from the local WGS84 geodetic-down axis in SI radians, after reference correction.", "GeodeticInclination", "rad");
            Describe(schema, "Azimuth", "Canonical clockwise azimuth from WGS84 geodetic true north in SI radians, after reference correction.", "TrueNorthAzimuth", "rad");
            Describe(schema, "ObservedInclination", "Original observed inclination in SI radians before transformation from InclinationReference.", "ObservedInclination", "rad");
            Describe(schema, "ObservedAzimuth", "Original observed clockwise azimuth in SI radians before transformation from AzimuthReference.", "ObservedAzimuth", "rad");
            Describe(schema, "MeasurementTimeUtc", "UTC measurement instant. Required for magnetic correction when the survey run has no complete acquisition interval.");
            Describe(schema, "InclinationReference", InclinationReferenceDescription, "SurveyInclinationReference");
            Describe(schema, "AzimuthReference", AzimuthReferenceDescription, "SurveyAzimuthReference");
            Describe(schema, "Correction", "Frozen correction result and Earth-model provenance used to derive canonical inclination and azimuth.");
        }
        if (context.Type == typeof(SurveyMeasurementCorrection))
        {
            Describe(schema, "AppliedInclinationCorrection", "Signed canonical-minus-observed inclination correction in SI radians.", "PlaneAngle", "rad");
            Describe(schema, "AppliedAzimuthCorrection", "Shortest signed canonical-minus-observed azimuth correction in SI radians.", "PlaneAngle", "rad");
            Describe(schema, "MagneticDeclination", "Evaluated magnetic declination clockwise from geodetic true north in SI radians.", "PlaneAngle", "rad");
            Describe(schema, "GravityNorth", "North component of total gravity in the local WGS84 north-east-down frame, in SI metres per second squared.", "Acceleration", "m/s2");
            Describe(schema, "GravityEast", "East component of total gravity in the local WGS84 north-east-down frame, in SI metres per second squared.", "Acceleration", "m/s2");
            Describe(schema, "GravityDown", "Down component of total gravity in the local WGS84 north-east-down frame, in SI metres per second squared.", "Acceleration", "m/s2");
            Describe(schema, "EvaluatedLatitude", "WGS84 geodetic latitude used for the correction, in SI radians.", "GeodeticLatitude", "rad");
            Describe(schema, "EvaluatedLongitude", "WGS84 geodetic longitude used for the correction, in SI radians.", "GeodeticLongitude", "rad");
            Describe(schema, "EvaluatedDepthWgs84", "Depth used for the correction in SI metres, positive downward from the WGS84 reference ellipsoid.", "Wgs84Depth", "m");
            Describe(schema, "EvaluationTimeUtc", "UTC instant used to evaluate the geomagnetic model.");
            Describe(schema, "TimeMethod", "How EvaluationTimeUtc was selected: station measurement time, survey-run acquisition midpoint, or not required.");
            Describe(schema, "AlgorithmVersion", "Opaque version of the reference-correction algorithm.");
        }
    }

    private static void RestrictRunDefault(OpenApiSchema schema, string propertyName, string description)
    {
        if (!schema.Properties.TryGetValue(propertyName, out OpenApiSchema? property)) return;
        WrapReferenceForOpenApi30Annotations(property);
        property.Description = description + " InheritRun is forbidden for a run-level default.";
        property.Not = new OpenApiSchema { Enum = [new OpenApiString("InheritRun")] };
        property.Extensions["x-osdc-semantic"] = new OpenApiString("RunLevelReferenceDefault");
    }

    private static void SetMinimum(OpenApiSchema schema, string propertyName, decimal minimum)
    {
        if (schema.Properties.TryGetValue(propertyName, out OpenApiSchema? property))
            property.Minimum = minimum;
    }

    private static void SetExclusiveMinimum(OpenApiSchema schema, string propertyName, decimal minimum)
    {
        if (!schema.Properties.TryGetValue(propertyName, out OpenApiSchema? property)) return;
        property.Minimum = minimum;
        property.ExclusiveMinimum = true;
    }

    private static void SetRange(OpenApiSchema schema, string propertyName, decimal minimum, decimal maximum)
    {
        if (!schema.Properties.TryGetValue(propertyName, out OpenApiSchema? property)) return;
        property.Minimum = minimum;
        property.Maximum = maximum;
    }

    private static void DescribeConfidenceFactor(OpenApiSchema schema, decimal maximum)
    {
        Describe(schema, "ConfidenceFactor",
            $"Dimensionless confidence proportion greater than 0 and no greater than {maximum.ToString(CultureInfo.InvariantCulture)}.",
            "ProportionStandard", "1");
        if (!schema.Properties.TryGetValue("ConfidenceFactor", out OpenApiSchema? property)) return;
        property.Minimum = 0.0m;
        property.ExclusiveMinimum = true;
        property.Maximum = maximum;
        property.ExclusiveMaximum = false;
    }

    private static void Describe(OpenApiSchema schema, string propertyName, string description,
        string? semantic = null, string? siUnit = null)
    {
        if (!schema.Properties.TryGetValue(propertyName, out OpenApiSchema? property)) return;
        WrapReferenceForOpenApi30Annotations(property);
        property.Description = description;
        if (semantic != null) property.Extensions["x-osdc-semantic"] = new OpenApiString(semantic);
        if (siUnit != null) property.Extensions["x-si-unit"] = new OpenApiString(siUnit);
    }

    private static void WrapReferenceForOpenApi30Annotations(OpenApiSchema property)
    {
        // OpenAPI 3.0 ignores siblings of $ref. Move the reference into allOf so
        // property-specific descriptions, constraints, and semantic extensions survive.
        if (property.Reference == null) return;
        property.AllOf =
        [
            new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Id = property.Reference.Id,
                    Type = property.Reference.Type
                }
            }
        ];
        property.Reference = null;
    }
}
