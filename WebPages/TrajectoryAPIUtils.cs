using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.Trajectory.WebPages;

public class TrajectoryAPIUtils : APIUtils, ITrajectoryAPIUtils
{
    public TrajectoryAPIUtils(ITrajectoryWebPagesConfiguration configuration)
    {
        HostNameField = Require(configuration.FieldHostURL, nameof(configuration.FieldHostURL));
        HttpClientField = SetHttpClient(HostNameField, HostBasePathField);
        ClientField = new Client(HttpClientField.BaseAddress!.ToString(), HttpClientField);

        HostNameCluster = Require(configuration.ClusterHostURL, nameof(configuration.ClusterHostURL));
        HttpClientCluster = SetHttpClient(HostNameCluster, HostBasePathCluster);
        ClientCluster = new Client(HttpClientCluster.BaseAddress!.ToString(), HttpClientCluster);

        HostNameRig = Require(configuration.RigHostURL, nameof(configuration.RigHostURL));
        HttpClientRig = SetHttpClient(HostNameRig, HostBasePathRig);
        ClientRig = new Client(HttpClientRig.BaseAddress!.ToString(), HttpClientRig);

        HostNameWell = Require(configuration.WellHostURL, nameof(configuration.WellHostURL));
        HttpClientWell = SetHttpClient(HostNameWell, HostBasePathWell);
        ClientWell = new Client(HttpClientWell.BaseAddress!.ToString(), HttpClientWell);

        HostNameWellBore = Require(configuration.WellBoreHostURL, nameof(configuration.WellBoreHostURL));
        HttpClientWellBore = SetHttpClient(HostNameWellBore, HostBasePathWellBore);
        ClientWellBore = new Client(HttpClientWellBore.BaseAddress!.ToString(), HttpClientWellBore);

        HostNameWellBoreArchitecture = Require(configuration.WellBoreArchitectureHostURL, nameof(configuration.WellBoreArchitectureHostURL));
        HttpClientWellBoreArchitecture = SetHttpClient(HostNameWellBoreArchitecture, HostBasePathWellBoreArchitecture);
        ClientWellBoreArchitecture = new Client(HttpClientWellBoreArchitecture.BaseAddress!.ToString(), HttpClientWellBoreArchitecture);

        HostNameTrajectory = Require(configuration.TrajectoryHostURL, nameof(configuration.TrajectoryHostURL));
        HttpClientTrajectory = SetHttpClient(HostNameTrajectory, HostBasePathTrajectory);
        ClientTrajectory = new Client(HttpClientTrajectory.BaseAddress!.ToString(), HttpClientTrajectory);

        HostNameUnitConversion = Require(configuration.UnitConversionHostURL, nameof(configuration.UnitConversionHostURL));
    }

    private static string Require(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{propertyName}' must be assigned before WebPages is used.");
        }

        return value;
    }

    public string HostNameField { get; }
    public string HostBasePathField { get; } = "Field/api/";
    public HttpClient HttpClientField { get; }
    public Client ClientField { get; }

    public string HostNameCluster { get; }
    public string HostBasePathCluster { get; } = "Cluster/api/";
    public HttpClient HttpClientCluster { get; }
    public Client ClientCluster { get; }

    public string HostNameRig { get; }
    public string HostBasePathRig { get; } = "Rig/api/";
    public HttpClient HttpClientRig { get; }
    public Client ClientRig { get; }

    public string HostNameWell { get; }
    public string HostBasePathWell { get; } = "Well/api/";
    public HttpClient HttpClientWell { get; }
    public Client ClientWell { get; }

    public string HostNameWellBore { get; }
    public string HostBasePathWellBore { get; } = "WellBore/api/";
    public HttpClient HttpClientWellBore { get; }
    public Client ClientWellBore { get; }

    public string HostNameWellBoreArchitecture { get; }
    public string HostBasePathWellBoreArchitecture { get; } = "WellBoreArchitecture/api/";
    public HttpClient HttpClientWellBoreArchitecture { get; }
    public Client ClientWellBoreArchitecture { get; }

    public string HostNameTrajectory { get; }
    public string HostBasePathTrajectory { get; } = "Trajectory/api/";
    public HttpClient HttpClientTrajectory { get; }
    public Client ClientTrajectory { get; }

    public string HostNameUnitConversion { get; }
    public string HostBasePathUnitConversion { get; } = "UnitConversion/api/";
}
