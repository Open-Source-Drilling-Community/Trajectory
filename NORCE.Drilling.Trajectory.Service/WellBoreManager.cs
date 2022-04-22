using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Service
{
    /// <summary>
    /// A manager for WellBore. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class WellBoreManager
    {
        private static WellBoreManager instance_ = null;

        public object lock_ = new object();

        /// <summary>
        /// default constructor is private when implementing a singleton pattern
        /// </summary>
        private WellBoreManager()
        {
        }

        public static WellBoreManager Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new WellBoreManager();
                }
                return instance_;

            }
        }

        public async Task<NORCE.Drilling.WellBore.ModelClientShared.WellBore> LoadWellBore(int id)
        {
            NORCE.Drilling.WellBore.ModelClientShared.WellBore wellBore = null;
            if (ConfigurationManager.Instance.Configuration != null)
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.HostURL))
                {
                    wellBore = await LoadWellBore(ConfigurationManager.Instance.Configuration.HostURL, id);
                }
                if (wellBore == null && ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWellBore > 0)
                {
                    if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.DockerHostURL))
                    {
                        wellBore = await LoadWellBore(ConfigurationManager.Instance.Configuration.DockerHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWellBore + "/", id);
                    }
                    if (wellBore == null && !string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.LocalHostURL))
                    {
                        wellBore = await LoadWellBore(ConfigurationManager.Instance.Configuration.LocalHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWellBore + "/", id);
                    }
                }
            }
            return wellBore;
        }

        public async Task<Dictionary<int, NORCE.Drilling.WellBore.ModelClientShared.WellBore>> LoadWellBores()
        {
            Dictionary<int, NORCE.Drilling.WellBore.ModelClientShared.WellBore> wellBores = null;
            if (ConfigurationManager.Instance.Configuration != null)
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.HostURL))
                {
                    wellBores = await LoadWellBores(ConfigurationManager.Instance.Configuration.HostURL);
                }
                if (wellBores == null && ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWellBore > 0)
                {
                    if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.DockerHostURL))
                    {
                        wellBores = await LoadWellBores(ConfigurationManager.Instance.Configuration.DockerHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWellBore + "/");
                    }
                    if (wellBores == null && !string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.LocalHostURL))
                    {
                        wellBores = await LoadWellBores(ConfigurationManager.Instance.Configuration.LocalHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWellBore + "/");
                    }
                }
            }
            return wellBores;
        }

        private async Task<Dictionary<int, NORCE.Drilling.WellBore.ModelClientShared.WellBore>> LoadWellBores(string host)
        {
            HttpClient httpWellBore;
            Dictionary<int, NORCE.Drilling.WellBore.ModelClientShared.WellBore> initialWellBores = null;
            try
            {
                httpWellBore = new HttpClient();
                httpWellBore.BaseAddress = new Uri(host + "WellBore/api/");
                httpWellBore.DefaultRequestHeaders.Accept.Clear();
                httpWellBore.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var a = await httpWellBore.GetAsync("WellBores");
                if (a.IsSuccessStatusCode && a.Content != null)
                {
                    string str = await a.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(str))
                    {
                        int[] initialWellBoreIDs = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(str);
                        if (initialWellBoreIDs != null && initialWellBoreIDs.Length > 0)
                        {
                            initialWellBores = new Dictionary<int, NORCE.Drilling.WellBore.ModelClientShared.WellBore>();
                            for (int i = 0; i < initialWellBoreIDs.Length; i++)
                            {
                                int id = initialWellBoreIDs[i];
                                var downloadedWellBore = await LoadWellBore(host, id);
                                if (a.IsSuccessStatusCode && a.Content != null)
                                {
                                    if (downloadedWellBore != null && !string.IsNullOrEmpty(downloadedWellBore.Name))
                                    {
                                        initialWellBores.Add(id, downloadedWellBore);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                initialWellBores = null;
            }
            return initialWellBores;
        }

        private async Task<NORCE.Drilling.WellBore.ModelClientShared.WellBore> LoadWellBore(string host, int id)
        {
            HttpClient httpWellBore;
            try
            {
                httpWellBore = new HttpClient();
                httpWellBore.BaseAddress = new Uri(host + "WellBore/api/");
                httpWellBore.DefaultRequestHeaders.Accept.Clear();
                httpWellBore.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var a = await httpWellBore.GetAsync("WellBores/" + id);
                if (a.IsSuccessStatusCode && a.Content != null)
                {
                    string str = await a.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(str))
                    {
                        NORCE.Drilling.WellBore.ModelClientShared.WellBore downloadedWellBore = Newtonsoft.Json.JsonConvert.DeserializeObject<NORCE.Drilling.WellBore.ModelClientShared.WellBore>(str);
                        if (downloadedWellBore != null && !string.IsNullOrEmpty(downloadedWellBore.Name))
                        {
                            return downloadedWellBore;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }
            return null;
        }

    }
}
