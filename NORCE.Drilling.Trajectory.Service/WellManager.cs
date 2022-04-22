using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Service
{
    /// <summary>
    /// A manager for Well. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class WellManager
    {
        private static WellManager instance_ = null;

        public object lock_ = new object();

        /// <summary>
        /// default constructor is private when implementing a singleton pattern
        /// </summary>
        private WellManager()
        {
        }

        public static WellManager Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new WellManager();
                }
                return instance_;

            }
        }

        public async Task<Well.ModelClientShared.Well> LoadWell(int id)
        {
            Well.ModelClientShared.Well well = null;
            if (ConfigurationManager.Instance.Configuration != null)
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.HostURL))
                {
                    well = await LoadWell(ConfigurationManager.Instance.Configuration.HostURL, id);
                }
                if (well == null && ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWell > 0)
                {
                    if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.DockerHostURL))
                    {
                        well = await LoadWell(ConfigurationManager.Instance.Configuration.DockerHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWell + "/", id);
                    }
                    if (well == null && !string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.LocalHostURL))
                    {
                        well = await LoadWell(ConfigurationManager.Instance.Configuration.LocalHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWell + "/", id);
                    }
                }
            }
            return well;
        }

        public async Task<Dictionary<int, NORCE.Drilling.Well.ModelClientShared.Well>> LoadWells()
        {
            Dictionary<int, NORCE.Drilling.Well.ModelClientShared.Well> wells = null;
            if (ConfigurationManager.Instance.Configuration != null)
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.HostURL))
                {
                    wells = await LoadWells(ConfigurationManager.Instance.Configuration.HostURL);
                }
                if (wells == null && ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWell > 0)
                {
                    if (!string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.DockerHostURL))
                    {
                        wells = await LoadWells(ConfigurationManager.Instance.Configuration.DockerHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWell + "/");
                    }
                    if (wells == null && !string.IsNullOrEmpty(ConfigurationManager.Instance.Configuration.LocalHostURL))
                    {
                        wells = await LoadWells(ConfigurationManager.Instance.Configuration.LocalHostURL + ConfigurationManager.Instance.Configuration.InternalHTTPPortNumberWell + "/");
                    }
                }
            }
            return wells;
        }

        private async Task<Dictionary<int, NORCE.Drilling.Well.ModelClientShared.Well>> LoadWells(string host)
        {
            HttpClient httpWell;
            Dictionary<int, NORCE.Drilling.Well.ModelClientShared.Well> initialWells = null;
            try
            {
                httpWell = new HttpClient();
                httpWell.BaseAddress = new Uri(host + "Well/api/");
                httpWell.DefaultRequestHeaders.Accept.Clear();
                httpWell.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var a = await httpWell.GetAsync("Wells");
                if (a.IsSuccessStatusCode && a.Content != null)
                {
                    string str = await a.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(str))
                    {
                        int[] initialWellIDs = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(str);
                        if (initialWellIDs != null && initialWellIDs.Length > 0)
                        {
                            initialWells = new Dictionary<int, NORCE.Drilling.Well.ModelClientShared.Well>();
                            for (int i = 0; i < initialWellIDs.Length; i++)
                            {
                                int id = initialWellIDs[i];
                                a = await httpWell.GetAsync("Wells/" + id);
                                if (a.IsSuccessStatusCode && a.Content != null)
                                {
                                    str = await a.Content.ReadAsStringAsync();
                                    if (!string.IsNullOrEmpty(str))
                                    {
                                        NORCE.Drilling.Well.ModelClientShared.Well downloadedWell = Newtonsoft.Json.JsonConvert.DeserializeObject<NORCE.Drilling.Well.ModelClientShared.Well>(str);
                                        if (downloadedWell != null && !string.IsNullOrEmpty(downloadedWell.Name))
                                        {
                                            initialWells.Add(id, downloadedWell);
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
                initialWells = null;
            }
            return initialWells;
        }

        private async Task<Well.ModelClientShared.Well> LoadWell(string host, int id)
        {
            HttpClient httpWell;
            try
            {
                httpWell = new HttpClient();
                httpWell.BaseAddress = new Uri(host + "Well/api/");
                httpWell.DefaultRequestHeaders.Accept.Clear();
                httpWell.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var a = await httpWell.GetAsync("Wells/" + id);
                if (a.IsSuccessStatusCode && a.Content != null)
                {
                    string str = await a.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(str))
                    {
                        NORCE.Drilling.Well.ModelClientShared.Well downloadedWell = Newtonsoft.Json.JsonConvert.DeserializeObject<NORCE.Drilling.Well.ModelClientShared.Well>(str);
                        if (downloadedWell != null && !string.IsNullOrEmpty(downloadedWell.Name))
                        {
                            return downloadedWell;
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
