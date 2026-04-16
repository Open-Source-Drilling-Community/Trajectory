using NORCE.Drilling.Trajectory.ModelShared;

namespace NORCE.Drilling.Trajectory.WebPages;

public static class APIUtils
{
    // API parameters
    public static readonly string HostNameField = WebAppConfiguration.FieldHostURL!;
    public static readonly string HostBasePathField = "Field/api/";
    public static readonly HttpClient HttpClientField = SetHttpClient(HostNameField, HostBasePathField);
    public static readonly Client ClientField = new(HttpClientField.BaseAddress!.ToString(), HttpClientField);

    public static readonly string HostNameCluster = WebAppConfiguration.ClusterHostURL!;
    public static readonly string HostBasePathCluster = "Cluster/api/";
    public static readonly HttpClient HttpClientCluster = SetHttpClient(HostNameCluster, HostBasePathCluster);
    public static readonly Client ClientCluster = new(HttpClientCluster.BaseAddress!.ToString(), HttpClientCluster);

    public static readonly string HostNameRig = WebAppConfiguration.RigHostURL!;
    public static readonly string HostBasePathRig = "Rig/api/";
    public static readonly HttpClient HttpClientRig = SetHttpClient(HostNameRig, HostBasePathRig);
    public static readonly Client ClientRig = new(HttpClientRig.BaseAddress!.ToString(), HttpClientRig);

    public static readonly string HostNameWell = WebAppConfiguration.WellHostURL!;
    public static readonly string HostBasePathWell = "Well/api/";
    public static readonly HttpClient HttpClientWell = SetHttpClient(HostNameWell, HostBasePathWell);
    public static readonly Client ClientWell = new(HttpClientWell.BaseAddress!.ToString(), HttpClientWell);

    public static readonly string HostNameWellBore = WebAppConfiguration.WellBoreHostURL!;
    public static readonly string HostBasePathWellBore = "WellBore/api/";
    public static readonly HttpClient HttpClientWellBore = SetHttpClient(HostNameWellBore, HostBasePathWellBore);
    public static readonly Client ClientWellBore = new(HttpClientWellBore.BaseAddress!.ToString(), HttpClientWellBore);

    public static readonly string HostNameWellBoreArchitecture = WebAppConfiguration.WellBoreArchitectureHostURL!;
    public static readonly string HostBasePathWellBoreArchitecture = "WellBoreArchitecture/api/";
    public static readonly HttpClient HttpClientWellBoreArchitecture = SetHttpClient(HostNameWellBoreArchitecture, HostBasePathWellBoreArchitecture);
    public static readonly Client ClientWellBoreArchitecture = new(HttpClientWellBoreArchitecture.BaseAddress!.ToString(), HttpClientWellBoreArchitecture);

    public static readonly string HostNameTrajectory = WebAppConfiguration.TrajectoryHostURL!;
    public static readonly string HostBasePathTrajectory = "Trajectory/api/";
    public static readonly HttpClient HttpClientTrajectory = SetHttpClient(HostNameTrajectory, HostBasePathTrajectory);
    public static readonly Client ClientTrajectory = new(HttpClientTrajectory.BaseAddress!.ToString(), HttpClientTrajectory);

    public static readonly string HostNameUnitConversion = WebAppConfiguration.UnitConversionHostURL!;
    public static readonly string HostBasePathUnitConversion = "UnitConversion/api/";

    public static HttpClient SetHttpClient(string host, string microServiceUri)
    {
        HttpClientHandler handler = new();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri(host + microServiceUri)
        };
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient;
    }
}
