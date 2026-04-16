using NORCE.Drilling.Trajectory.ModelShared;

namespace NORCE.Drilling.Trajectory.WebPages;

public interface ITrajectoryAPIUtils
{
    string HostNameField { get; }
    string HostBasePathField { get; }
    HttpClient HttpClientField { get; }
    Client ClientField { get; }

    string HostNameCluster { get; }
    string HostBasePathCluster { get; }
    HttpClient HttpClientCluster { get; }
    Client ClientCluster { get; }

    string HostNameRig { get; }
    string HostBasePathRig { get; }
    HttpClient HttpClientRig { get; }
    Client ClientRig { get; }

    string HostNameWell { get; }
    string HostBasePathWell { get; }
    HttpClient HttpClientWell { get; }
    Client ClientWell { get; }

    string HostNameWellBore { get; }
    string HostBasePathWellBore { get; }
    HttpClient HttpClientWellBore { get; }
    Client ClientWellBore { get; }

    string HostNameWellBoreArchitecture { get; }
    string HostBasePathWellBoreArchitecture { get; }
    HttpClient HttpClientWellBoreArchitecture { get; }
    Client ClientWellBoreArchitecture { get; }

    string HostNameTrajectory { get; }
    string HostBasePathTrajectory { get; }
    HttpClient HttpClientTrajectory { get; }
    Client ClientTrajectory { get; }

    string HostNameUnitConversion { get; }
    string HostBasePathUnitConversion { get; }
}
