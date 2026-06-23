using System.Net.Http.Json;
using System.Text.Json;
using ModelShared = NORCE.Drilling.Trajectory.ModelShared;

namespace NORCE.Drilling.Trajectory.WebPages;

public static class MslDepthReferenceUtils
{
    public static Task<double?> ResolveMeanSeaLevelDepthReferenceForTrajectoryAsync(
        ITrajectoryAPIUtils api,
        Guid? trajectoryId,
        IEnumerable<ModelShared.TrajectoryLight>? trajectories,
        IEnumerable<ModelShared.WellBore>? wellBores,
        IEnumerable<ModelShared.Well>? wells,
        IEnumerable<ModelShared.Cluster>? clusters)
    {
        ModelShared.TrajectoryLight? trajectory = trajectories?.FirstOrDefault(item => item?.MetaInfo?.ID == trajectoryId);
        return ResolveMeanSeaLevelDepthReferenceForWellBoreAsync(api, trajectory?.WellBoreID, wellBores, wells, clusters);
    }

    public static Task<double?> ResolveMeanSeaLevelDepthReferenceForSurveyRunAsync(
        ITrajectoryAPIUtils api,
        Guid? surveyRunId,
        IEnumerable<ModelShared.SurveyRunLight>? surveyRuns,
        IEnumerable<ModelShared.WellBore>? wellBores,
        IEnumerable<ModelShared.Well>? wells,
        IEnumerable<ModelShared.Cluster>? clusters)
    {
        ModelShared.SurveyRunLight? surveyRun = surveyRuns?.FirstOrDefault(item => item?.MetaInfo?.ID == surveyRunId);
        return ResolveMeanSeaLevelDepthReferenceForWellBoreAsync(api, surveyRun?.WellBoreID, wellBores, wells, clusters);
    }

    public static Task<double?> ResolveMeanSeaLevelDepthReferenceForWellBoreAsync(
        ITrajectoryAPIUtils api,
        Guid? wellBoreId,
        IEnumerable<ModelShared.WellBore>? wellBores,
        IEnumerable<ModelShared.Well>? wells,
        IEnumerable<ModelShared.Cluster>? clusters)
    {
        ModelShared.WellBore? wellBore = wellBores?.FirstOrDefault(item => item?.MetaInfo?.ID == wellBoreId);
        ModelShared.WellBore? rootWellBore = ResolveRootWellBore(wellBore, wellBores);
        ModelShared.Well? well = wells?.FirstOrDefault(item => item?.MetaInfo?.ID == rootWellBore?.WellID);
        ModelShared.Cluster? cluster = clusters?.FirstOrDefault(item => item?.MetaInfo?.ID == well?.ClusterID);
        ModelShared.Slot? slot = ResolveSlot(well, cluster, clusters);

        return CalculateMeanSeaLevelDepthReferenceAsync(
            api.HttpClientVerticalDatum,
            api.HostNameVerticalDatum,
            api.HostBasePathVerticalDatum,
            slot?.Latitude?.GaussianValue?.Mean ?? cluster?.ReferenceLatitude?.GaussianValue?.Mean,
            slot?.Longitude?.GaussianValue?.Mean ?? cluster?.ReferenceLongitude?.GaussianValue?.Mean);
    }

    private static ModelShared.WellBore? ResolveRootWellBore(ModelShared.WellBore? wellBore, IEnumerable<ModelShared.WellBore>? wellBores)
    {
        ModelShared.WellBore? current = wellBore;
        HashSet<Guid> visitedIds = new();
        while (current?.IsSidetrack == true &&
            current.ParentWellBoreID is Guid parentId &&
            parentId != Guid.Empty &&
            visitedIds.Add(parentId))
        {
            ModelShared.WellBore? parent = wellBores?.FirstOrDefault(item => item?.MetaInfo?.ID == parentId);
            if (parent == null)
            {
                break;
            }

            current = parent;
        }

        return current;
    }

    private static ModelShared.Slot? ResolveSlot(ModelShared.Well? well, ModelShared.Cluster? selectedCluster, IEnumerable<ModelShared.Cluster>? clusters)
    {
        if (well?.SlotID is not Guid slotId)
        {
            return null;
        }

        ModelShared.Cluster? cluster = selectedCluster;
        cluster ??= clusters?.FirstOrDefault(item => item?.MetaInfo?.ID == well.ClusterID);
        cluster ??= clusters?.FirstOrDefault(item => item?.Slots?.Values.Any(slot => slot?.ID == slotId) == true);
        return cluster?.Slots?.Values.FirstOrDefault(slot => slot?.ID == slotId);
    }

    private static async Task<double?> CalculateMeanSeaLevelDepthReferenceAsync(HttpClient client, string hostName, string hostBasePath, double? latitude, double? longitude)
    {
        if (latitude == null || longitude == null)
        {
            return null;
        }

        Guid orderId = Guid.NewGuid();
        object order = new
        {
            MetaInfo = new { ID = orderId, HttpHostName = hostName, HttpHostBasePath = hostBasePath, HttpEndPoint = "VerticalDatumOrder/" },
            Name = $"MSL reference {orderId}",
            Description = "Temporary MSL-to-WGS84 conversion.",
            CreationDate = DateTimeOffset.UtcNow,
            LastModificationDate = DateTimeOffset.UtcNow,
            VerticalDatum = new
            {
                MetaInfo = new { ID = Guid.NewGuid(), HttpHostName = hostName, HttpHostBasePath = hostBasePath, HttpEndPoint = "VerticalDatum/" },
                Name = $"MSL reference {orderId}",
                Description = "Temporary MSL-to-WGS84 conversion.",
                CreationDate = DateTimeOffset.UtcNow,
                LastModificationDate = DateTimeOffset.UtcNow,
                DatumSet = new[] { new { Latitude = latitude.Value, Longitude = longitude.Value, GenericVerticalDatum = 0 } },
                ConversionFrom = "FromMeanSeaLevel",
                Type = "Raw"
            }
        };

        try
        {
            using HttpResponseMessage postResponse = await client.PostAsJsonAsync("VerticalDatumOrder", order);
            postResponse.EnsureSuccessStatusCode();

            using JsonDocument document = await client.GetFromJsonAsync<JsonDocument>($"VerticalDatumOrder/{orderId}") ?? throw new InvalidOperationException("VerticalDatumOrder response was empty.");
            JsonElement datumSet = document.RootElement.GetProperty("VerticalDatum").GetProperty("DatumSet");
            if (datumSet.GetArrayLength() == 0 ||
                !datumSet[0].TryGetProperty("VerticalDatumWGS64", out JsonElement valueElement) ||
                valueElement.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return -valueElement.GetDouble();
        }
        finally
        {
            try
            {
                await client.DeleteAsync($"VerticalDatumOrder/{orderId}");
            }
            catch
            {
                // Best-effort cleanup of a temporary calculation order.
            }
        }
    }
}
