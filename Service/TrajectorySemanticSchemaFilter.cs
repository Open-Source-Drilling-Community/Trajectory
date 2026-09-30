using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OSDC.Drilling.Trajectory.Model;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        if (context.Type == typeof(SurveyImportSettings))
        {
            RestrictRunDefault(schema, "DefaultInclinationReference", InclinationReferenceDescription);
            RestrictRunDefault(schema, "DefaultAzimuthReference", AzimuthReferenceDescription);
            Describe(schema, "GeomagneticModel", "Geomagnetic model selection copied to every survey run created by this import.");
        }
        if (context.Type == typeof(SurveyMeasurement))
        {
            Describe(schema, "MeasurementID", "Stable UUID for this measurement, independent of list position.");
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
