using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.WebPages;

internal static class CartographicPositionReferenceUtils
{
    public static async Task ApplyForTrajectoryAsync(
        ITrajectoryAPIUtils api,
        Guid? trajectoryId,
        IEnumerable<TrajectoryLight>? trajectories,
        IEnumerable<Cluster>? clusters,
        IEnumerable<Field>? fields,
        ILogger logger)
    {
        TrajectoryLight? trajectory = trajectories?.FirstOrDefault(item => item?.MetaInfo?.ID == trajectoryId);
        Guid? fieldId = trajectory?.FieldID ?? ResolveClusterFieldId(trajectory?.ClusterID, clusters);
        await ApplyAsync(api, ResolveField(fieldId, fields), logger);
    }

    public static async Task ApplyForSurveyRunAsync(
        ITrajectoryAPIUtils api,
        Guid? surveyRunId,
        IEnumerable<SurveyRunLight>? surveyRuns,
        IEnumerable<Cluster>? clusters,
        IEnumerable<Field>? fields,
        ILogger logger)
    {
        SurveyRunLight? surveyRun = surveyRuns?.FirstOrDefault(item => item?.MetaInfo?.ID == surveyRunId);
        Guid? fieldId = surveyRun?.FieldID ?? ResolveClusterFieldId(surveyRun?.ClusterID, clusters);
        await ApplyAsync(api, ResolveField(fieldId, fields), logger);
    }

    public static async Task ApplyAsync(ITrajectoryAPIUtils api, Field? field, ILogger logger)
    {
        CartographicGridPositionReferenceSource source = DataUtils.CartographicGridPositionReferenceSource;
        source.CartographicGridNorthPositionReference = null;
        source.CartographicGridEastPositionReference = null;

        if (field?.MetaInfo?.ID is not Guid fieldId || fieldId == Guid.Empty ||
            field.ReferencePoint?.Latitude is not double latitude ||
            field.ReferencePoint.Longitude is not double longitude ||
            field.ReferencePoint.RiemannianNorth is not double riemannianNorth ||
            field.ReferencePoint.RiemannianEast is not double riemannianEast)
        {
            ResetUnavailableSelection();
            return;
        }

        try
        {
            FieldCoordinateConversionResponse response = await api.ClientField.ForwardFieldCoordinatesAsync(new FieldForwardConversionRequest
            {
                FieldID = fieldId,
                SourceGeographicReference = FieldGeographicReference.Wgs84,
                ProjectionApplicabilityPolicy = FieldApplicabilityPolicy.AllowUnknown,
                Transformation = new FieldTransformationOptions
                {
                    SelectionPolicy = FieldTransformationSelectionPolicy.FirstAvailable,
                    ApplicabilityPolicy = FieldApplicabilityPolicy.AllowUnknown,
                    DepthPolicy = FieldDepthTransformationPolicy.AllowUntransformedDepthFor2D
                },
                Positions =
                [
                    new FieldForwardConversionPosition
                    {
                        Latitude = latitude,
                        Longitude = longitude,
                        VerticalDepth = field.ReferencePoint.TVD ?? 0
                    }
                ]
            });

            FieldCoordinateConversionPositionResult? result = response.Positions?.FirstOrDefault();
            if (result?.ProjectedCoordinate == null)
            {
                ResetUnavailableSelection();
                return;
            }

            source.CartographicGridNorthPositionReference = result.ProjectedCoordinate.Northing - riemannianNorth;
            source.CartographicGridEastPositionReference = result.ProjectedCoordinate.Easting - riemannianEast;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to resolve the cartographic position reference for field {FieldId}", fieldId);
            ResetUnavailableSelection();
        }
    }

    private static Field? ResolveField(Guid? fieldId, IEnumerable<Field>? fields) =>
        fieldId is Guid id && id != Guid.Empty
            ? fields?.FirstOrDefault(field => field?.MetaInfo?.ID == id)
            : null;

    private static Guid? ResolveClusterFieldId(Guid? clusterId, IEnumerable<Cluster>? clusters) =>
        clusterId is Guid id && id != Guid.Empty
            ? clusters?.FirstOrDefault(cluster => cluster?.MetaInfo?.ID == id)?.FieldID
            : null;

    private static void ResetUnavailableSelection()
    {
        if (string.Equals(DataUtils.UnitAndReferenceParameters.PositionReferenceName, "Cartographic", StringComparison.Ordinal))
        {
            DataUtils.UnitAndReferenceParameters.PositionReferenceName = "WGS84";
        }
    }
}
