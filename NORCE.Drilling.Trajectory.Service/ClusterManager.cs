using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Service
{
    /// <summary>
    /// A manager for Cluster. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class ClusterManager
    {
        private static ClusterManager instance_ = null;

        public object lock_ = new object();

        /// <summary>
        /// default constructor is private when implementing a singleton pattern
        /// </summary>
        private ClusterManager()
        {
        }

        public static ClusterManager Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new ClusterManager();
                }
                return instance_;

            }
        }

        public async Task<Cluster.ModelClientShared.Cluster> LoadCluster(int id)
        {
            Cluster.ModelClientShared.Cluster cluster = null;
            if (ConfigurationManager.Instance.Configuration != null)
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.HostURL))
                {
                    cluster = await LoadCluster(ConfigurationManager.Instance.Configuration.HostURL, id);
                }
                if (cluster == null && ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberCluster > 0)
                {
                    if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.DockerHostURL))
                    {
                        cluster = await LoadCluster(ConfigurationManager.Instance.Configuration.DockerHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberCluster + "/", id);
                    }
                    if (cluster == null && !string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.LocalHostURL))
                    {
                        cluster = await LoadCluster(ConfigurationManager.Instance.Configuration.LocalHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberCluster + "/", id);
                    }
                }
            }
            return cluster;
        }

        public async Task<Cluster.ModelClientShared.Cluster> LoadCluster(string host, int id)
        {
            HttpClient httpCluster;
            Cluster.ModelClientShared.Cluster cluster = null;
            try
            {
                httpCluster = new HttpClient();
                httpCluster.BaseAddress = new Uri(host + "Cluster/api/");
                httpCluster.DefaultRequestHeaders.Accept.Clear();
                httpCluster.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var a = await httpCluster.GetAsync("Clusters/" + id.ToString());
                if (a.IsSuccessStatusCode && a.Content != null)
                {
                    string str = await a.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(str))
                    {
                        cluster = Newtonsoft.Json.JsonConvert.DeserializeObject<Cluster.ModelClientShared.Cluster>(str);
                    }
                }
            }
            catch (Exception e)
            {
                httpCluster = null;
                cluster = null;
            }
            return cluster;
        }

        public async Task<Dictionary<int, NORCE.Drilling.Cluster.ModelClientShared.Cluster>> LoadClusters()
        {
            Dictionary<int, NORCE.Drilling.Cluster.ModelClientShared.Cluster> clusters = null;
            if (ConfigurationManager.Instance.Configuration != null)
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.HostURL))
                {
                    clusters = await LoadClusters(ConfigurationManager.Instance.Configuration.HostURL);
                }
                if (clusters == null && ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberCluster > 0)
                {
                    if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.DockerHostURL))
                    {
                        clusters = await LoadClusters(ConfigurationManager.Instance.Configuration.DockerHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberCluster + "/");
                    }
                    if (clusters == null && !string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.LocalHostURL))
                    {
                        clusters = await LoadClusters(ConfigurationManager.Instance.Configuration.LocalHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberCluster + "/");
                    }
                }
            }
            return clusters;
        }

        private async Task<Dictionary<int, NORCE.Drilling.Cluster.ModelClientShared.Cluster>> LoadClusters(string host)
        {
            HttpClient httpCluster;
            Dictionary<int, NORCE.Drilling.Cluster.ModelClientShared.Cluster> initialClusters = null;
            try
            {
                httpCluster = new HttpClient();
                httpCluster.BaseAddress = new Uri(host + "Cluster/api/");
                httpCluster.DefaultRequestHeaders.Accept.Clear();
                httpCluster.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var a = await httpCluster.GetAsync("Clusters");
                if (a.IsSuccessStatusCode && a.Content != null)
                {
                    string str = await a.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(str))
                    {
                        int[] initialClusterIDs = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(str);
                        if (initialClusterIDs != null && initialClusterIDs.Length > 0)
                        {
                            initialClusters = new Dictionary<int, NORCE.Drilling.Cluster.ModelClientShared.Cluster>();
                            for (int i = 0; i < initialClusterIDs.Length; i++)
                            {
                                int id = initialClusterIDs[i];
                                a = await httpCluster.GetAsync("Clusters/" + id);
                                if (a.IsSuccessStatusCode && a.Content != null)
                                {
                                    str = await a.Content.ReadAsStringAsync();
                                    if (!string.IsNullOrEmpty(str))
                                    {
                                        NORCE.Drilling.Cluster.ModelClientShared.Cluster downloadedCluster = Newtonsoft.Json.JsonConvert.DeserializeObject<NORCE.Drilling.Cluster.ModelClientShared.Cluster>(str);
                                        if (downloadedCluster != null && !string.IsNullOrEmpty(downloadedCluster.Name))
                                        {
                                            initialClusters.Add(id, downloadedCluster);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                initialClusters = null;
            }
            return initialClusters;
        }

        public async Task<bool> Calculate(Cluster.ModelClientShared.ClusterCoordinate clusterCoordinate, Cluster.ModelClientShared.Cluster cluster, List<Cluster.ModelClientShared.ClusterCoordinate> clusterCoordinates = null)
        {
            bool result = false;
            if (ConfigurationManager.Instance.Configuration != null)
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.HostURL))
                {
                    result = await Calculate(ConfigurationManager.Instance.Configuration.HostURL, clusterCoordinate, cluster, clusterCoordinates);
                }
                if (!result && ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberCluster > 0)
                {
                    if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.DockerHostURL))
                    {
                        result = await Calculate(ConfigurationManager.Instance.Configuration.DockerHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberCluster + "/", clusterCoordinate, cluster, clusterCoordinates);
                    }
                    if (!result && !string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.LocalHostURL))
                    {
                        result = await Calculate(ConfigurationManager.Instance.Configuration.LocalHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberCluster + "/", clusterCoordinate, cluster, clusterCoordinates);
                    }
                }
            }
            return result;
        }

        private async Task<bool> Calculate(string host, Cluster.ModelClientShared.ClusterCoordinate clusterCoordinate, Cluster.ModelClientShared.Cluster cluster, List<Cluster.ModelClientShared.ClusterCoordinate> clusterCoordinates = null)
        {
            HttpClient httpCluster;
            try
            {
                httpCluster = new HttpClient();
                httpCluster.BaseAddress = new Uri(host + "Cluster/api/");
                httpCluster.DefaultRequestHeaders.Accept.Clear();
                httpCluster.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                if (cluster != null && cluster.Field != null)
                {
                    Cluster.ModelClientShared.ClusterCoordinateConversionSet conversionSet = new Cluster.ModelClientShared.ClusterCoordinateConversionSet();
                    conversionSet.ID = Guid.NewGuid().ToString();
                    conversionSet.OctreeBounds = new Cluster.ModelClientShared.Bounds();
                    conversionSet.OctreeBounds.MinX = -Math.PI / 2.0;
                    conversionSet.OctreeBounds.MaxX = Math.PI / 2.0;
                    conversionSet.OctreeBounds.MinY = -Math.PI;
                    conversionSet.OctreeBounds.MaxY = Math.PI;
                    conversionSet.OctreeBounds.MinZ = -6000000.0; // The radius of the earth is around 6000 km.
                    conversionSet.OctreeBounds.MaxZ = 34000000.0; // We want the resolution in z to be of the same order of magnitude as for the other directions in the relevant region (circumference of the earth is ca 40 000 km)
                    if (clusterCoordinates != null)
                    {
                        conversionSet.ClusterCoordinates = clusterCoordinates;
                    }
                    else
                    {
                        conversionSet.ClusterCoordinates = new List<Cluster.ModelClientShared.ClusterCoordinate>();
                        conversionSet.ClusterCoordinates.Add(clusterCoordinate);
                    }
                    conversionSet.Field = cluster.Field;
                    conversionSet.Cluster = cluster;
                    StringContent content = new StringContent(conversionSet.GetJson(), Encoding.UTF8, "application/json");
                    var a = await httpCluster.PutAsync("ClusterCoordinateConversionSets/" + conversionSet.ID, content);
                    if (a.IsSuccessStatusCode)
                    {
                        // retrieve the results
                        a = await httpCluster.GetAsync("ClusterCoordinateConversionSets/" + conversionSet.ID);
                        if (a.IsSuccessStatusCode)
                        {
                            string str = await a.Content.ReadAsStringAsync();
                            if (!string.IsNullOrEmpty(str))
                            {
                                Cluster.ModelClientShared.ClusterCoordinateConversionSet result = Newtonsoft.Json.JsonConvert.DeserializeObject<Cluster.ModelClientShared.ClusterCoordinateConversionSet>(str);
                                if (result != null && result.ClusterCoordinates != null)
                                {
                                    if (result.ClusterCoordinates.Count == 1)
                                    {
                                        foreach (Cluster.ModelClientShared.ClusterCoordinate coord in result.ClusterCoordinates)
                                        {
                                            coord.Copy(clusterCoordinate);
                                        }
                                    }
                                    else if (clusterCoordinates != null)
                                    {
                                        clusterCoordinates = (List<Cluster.ModelClientShared.ClusterCoordinate>)result.ClusterCoordinates;
                                    }
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
            return false;
        }

    }
}
