using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.General.Statistics;
using OSDC.DotnetLibraries.Drilling.Surveying;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

public static class APIUtils
{
    // API parameters
    public static readonly string HostNameField = NORCE.Drilling.Trajectory.Service.ServiceConfiguration.FieldHostURL!;
    public static readonly string HostBasePathField = "Field/api/";
    public static readonly HttpClient HttpClientField = APIUtils.SetHttpClient(HostNameField, HostBasePathField);
    public static readonly Client ClientField = new Client(APIUtils.HttpClientField.BaseAddress!.ToString(), APIUtils.HttpClientField);

    public static readonly string HostNameCluster = NORCE.Drilling.Trajectory.Service.ServiceConfiguration.ClusterHostURL!;
    public static readonly string HostBasePathCluster = "Cluster/api/";
    public static readonly HttpClient HttpClientCluster = APIUtils.SetHttpClient(HostNameCluster, HostBasePathCluster);
    public static readonly Client ClientCluster = new Client(APIUtils.HttpClientCluster.BaseAddress!.ToString(), APIUtils.HttpClientCluster);

    public static readonly string HostNameWell = NORCE.Drilling.Trajectory.Service.ServiceConfiguration.WellHostURL!;
    public static readonly string HostBasePathWell = "Well/api/";
    public static readonly HttpClient HttpClientWell = APIUtils.SetHttpClient(HostNameWell, HostBasePathWell);
    public static readonly Client ClientWell = new Client(APIUtils.HttpClientWell.BaseAddress!.ToString(), APIUtils.HttpClientWell);

    public static readonly string HostNameWellBore = NORCE.Drilling.Trajectory.Service.ServiceConfiguration.WellBoreHostURL!;
    public static readonly string HostBasePathWellBore = "WellBore/api/";
    public static readonly HttpClient HttpClientWellBore = APIUtils.SetHttpClient(HostNameWellBore, HostBasePathWellBore);
    public static readonly Client ClientWellBore = new Client(APIUtils.HttpClientWellBore.BaseAddress!.ToString(), APIUtils.HttpClientWellBore);

    public static readonly string HostNameWellBoreArchitecture = NORCE.Drilling.Trajectory.Service.ServiceConfiguration.WellBoreArchitectureHostURL!;
    public static readonly string HostBasePathWellBoreArchitecture = "WellBoreArchitecture/api/";
    public static readonly HttpClient HttpClientWellBoreArchitecture = APIUtils.SetHttpClient(HostNameWellBoreArchitecture, HostBasePathWellBoreArchitecture);
    public static readonly Client ClientWellBoreArchitecture = new Client(APIUtils.HttpClientWellBoreArchitecture.BaseAddress!.ToString(), APIUtils.HttpClientWellBoreArchitecture);

    // API utility methods
    public static HttpClient SetHttpClient(string host, string microServiceUri)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }; // temporary workaround for testing purposes: bypass certificate validation (not recommended for production environments due to security risks)
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri(host + microServiceUri)
        };
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient;
    }

    /// <summary>
    /// Retrieves the cluster, slot, and wellbore hosting the <paramref name="trajectory"/>
    /// <param name="trajectory">the trajectory to retrieve the hosting cluster from</param>
    /// <returns>a tuple formed by:
    /// - the Gaussian geodetic coordinates of the slot hosting the trajectory
    /// - the hosting wellbore
    /// - the ID of the field hosting the cluster
    /// - the info/error message resulting from the interaction with called microservices
    /// </returns>
    /// </summary>
    public static async Task<(SurveyPoint?, WellBore?, string)> GetReferencePointAsync(NORCE.Drilling.Trajectory.Model.Trajectory trajectory)
    {
        string msg;
        try
        {
            WellBore? wellBore = await APIUtils.ClientWellBore.GetWellBoreByIdAsync(trajectory.WellBoreID);
            // We cascade up to the slot it is connected to retrieve the reference point coordinates of the tie-in Gaussian geodetic point
            if (wellBore?.WellID is Guid wellId && wellId != Guid.Empty)
            {
                Well? well = await APIUtils.ClientWell.GetWellByIdAsync(wellId);
                if (well is not null &&
                    well.ClusterID is Guid clusterId && clusterId != Guid.Empty &&
                    well.SlotID is Guid slotId && slotId != Guid.Empty)
                {
                    Cluster? cluster = await APIUtils.ClientCluster.GetClusterByIdAsync(clusterId);
                    if (cluster?.Slots is { } slots &&
                        slots.TryGetValue(slotId.ToString(), out var slot) &&
                        slot is not null &&
                        cluster.FieldID is Guid fieldId && 
                        fieldId != Guid.Empty)
                    {
                        if (slot.Latitude?.GaussianValue?.Mean is { } refLat &&
                            slot.Longitude?.GaussianValue?.Mean is { } refLon &&
                            cluster.ReferenceDepth?.GaussianValue?.Mean is { } refTVD)
                        {
                            msg = "cluster, slot, and wellbore successfully retrieved";
                            SurveyPoint surveyPoint = new SurveyPoint();
                            surveyPoint.Latitude = refLat;
                            surveyPoint.Longitude = refLon;
                            surveyPoint.TVD = refTVD;
                            surveyPoint.Abscissa = refTVD;
                            surveyPoint.Inclination = 0;
                            surveyPoint.Azimuth = 0;
                            return (surveyPoint, wellBore, msg);
                        }
                        else
                        {
                            msg = "coordinates of the hosting slot are not properly set";
                            return (null, null, msg);
                        }
                    }
                    else
                    {
                        msg = "the cluster hosting the well hosting the wellbore hosting the trajectory is null or has no slots or does not contain the expected slot ID";
                        return (null, null, msg);
                    }
                }
                else
                {
                    msg = "the well hosting the wellbore hosting the trajectory is null or has a corrupted cluster ID or slot ID";
                    return (null, null, msg);
                }
            }
            else
            {
                msg = "the wellbore hosting the trajectory has a corrupted well ID";
                return (null, null, msg);
            }
        }
        catch (Exception ex)
        {
            msg = ex.Message + ": an exception was raised while retrieving cluster, slot, and wellbore from trajectory";
            return (null, null, msg);
        }
    }
}
