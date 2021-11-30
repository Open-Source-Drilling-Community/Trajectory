using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Text;
using Newtonsoft.Json;
using NORCE.Drilling.Cluster.ModelClientShared;

namespace NORCE.Drilling.Cluster.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Test1(args);
            Thread.Sleep(10);
        }

        static async void Test1(string[] args)
        {

            //string host = "https://app.DigiWells.no/";
            string host = "https://localhost:44369/";
            if (args != null && args.Length >= 1)
            {
                host = args[0];
            }
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(host + "Cluster/api/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // test #1: read the IDs
            int[] initialClusterIDs = null;
            var a = client.GetAsync("Clusters");
            a.Wait();
            HttpResponseMessage responseGetClusterIDs = a.Result;
            if (responseGetClusterIDs.IsSuccessStatusCode)
            {
                string str = await responseGetClusterIDs.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(str))
                {
                    initialClusterIDs = JsonConvert.DeserializeObject<int[]>(str);
                    if (initialClusterIDs != null)
                    {
                        Console.Write("Test #1: read IDs: success. IDs: ");
                        for (int i = 0; i < initialClusterIDs.Length; i++)
                        {
                            Console.Write(initialClusterIDs[i].ToString() + "\t");
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.Write("Test #1: read IDs: success. but no IDs");
                    }
                }
                else
                {
                    Console.WriteLine("Test #1: read IDs: failure b");
                }
            }
            else
            {
                Console.WriteLine("Test #1: read IDs: failure a");
            }
            
            // test #2: post a new cluster
            ModelClientShared.Cluster cluster = new ModelClientShared.Cluster();
            cluster.Name = "My cluster";
            cluster.Description = "test cluster";
            if (cluster.Slots == null)
            {
                cluster.Slots = new List<Slot>();
            }
            for (int i = 0; i < 10; i++)
            {
                Slot slot = new Slot();
                slot.Name = "Slot " + i.ToString();
                slot.LatitudeWGS84 = i * 1.0;
                slot.LongitudeWGS84 = i * 2.0;
                slot.TVDWGS84 = 3.0;
                cluster.Slots.Add(slot);
            }
           
            // test #2: post a new cluster
            StringContent content = new StringContent(cluster.GetJson(), Encoding.UTF8, "application/json");
            a = client.PostAsync("Clusters", content);
            a.Wait();
            HttpResponseMessage responseTaskPostCluster = a.Result;
            if (responseTaskPostCluster.IsSuccessStatusCode)
            {
                Console.WriteLine("Test #2: post of cluster: success");
            }
            else
            {
                Console.WriteLine("Test #2: post of cluster: failure a");
            }


            // test #3: check that the new ID is present
            int[] newClusterIDs = null;
            a = client.GetAsync("Clusters");
            a.Wait();
            responseGetClusterIDs = a.Result;
            if (responseGetClusterIDs.IsSuccessStatusCode)
            {
                string str = await responseGetClusterIDs.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(str))
                {
                    newClusterIDs = JsonConvert.DeserializeObject<int[]>(str);
                }
                else
                {
                    Console.WriteLine("Test #3: check if new ID is present: failure b");
                }
            }
            else
            {
                Console.WriteLine("Test #3: check if new ID is present: failure a");
            }
            int newClusterID = -1;
            if (newClusterIDs != null && initialClusterIDs != null && newClusterIDs.Length > initialClusterIDs.Length)
            {
                foreach (int id in newClusterIDs)
                {
                    bool found = false;
                    foreach (int oldID in initialClusterIDs)
                    {
                        if (id == oldID)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        newClusterID = id;
                        cluster.ID = newClusterID;
                        break;
                    }
                }
                Console.WriteLine("Test #3 successful");
            }
            else
            {
                Console.WriteLine("Test #3 failed");
            }
            // test #4: read the cluster 
            if (newClusterID > 0) {
                a = client.GetAsync("Clusters/" + newClusterID);
                a.Wait();
                responseGetClusterIDs = a.Result;
                if (responseGetClusterIDs.IsSuccessStatusCode)
                {
                    string str = await responseGetClusterIDs.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(str))
                    {
                        ModelClientShared.Cluster downloadedCluster = JsonConvert.DeserializeObject<ModelClientShared.Cluster>(str);
                        if (downloadedCluster != null && downloadedCluster.Name == cluster.Name && downloadedCluster.Description == cluster.Description) 
                        {
                            Console.WriteLine("test #4: success");
                        }
                        else
                        {
                            Console.WriteLine("Test #4: read the default calibration for the new ID: failure c");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Test #4: read the default calibration for the new ID: failure b");
                    }
                }
                else
                {
                    Console.WriteLine("Test #4: read the default calibration for the new ID: failure a");
                }
            }
            // test #7: put (update) the previous cluster
            cluster.Name = "New name";
            cluster.Description = "new Description";
            content = new StringContent(cluster.GetJson(), Encoding.UTF8, "application/json");
            a = client.PutAsync("Clusters" + "/" + cluster.ID, content);
            a.Wait();
            responseTaskPostCluster = a.Result;
            if (responseTaskPostCluster.IsSuccessStatusCode)
            {
                Console.WriteLine("Test #7: put of cluster: success");
            }
            else
            {
                Console.WriteLine("Test #7: put of cluster: failure a");
            }
            // test #8: read the cluster for the updated ID 
            a = client.GetAsync("Clusters/" + newClusterID);
            a.Wait();
            responseGetClusterIDs = a.Result;
            if (responseGetClusterIDs.IsSuccessStatusCode)
            {
                string str = await responseGetClusterIDs.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(str))
                {
                    ModelClientShared.Cluster downloadedCluster = JsonConvert.DeserializeObject<ModelClientShared.Cluster>(str);
                    if (downloadedCluster != null && downloadedCluster.Name == cluster.Name && downloadedCluster.Description == cluster.Description)
                    {
                        Console.WriteLine("Test #8: success");
                    }
                    else
                    {
                        Console.WriteLine("Test #8: failure c");
                    }
                }
                else
                {
                    Console.WriteLine("Test #8: failure b");
                }
            }
            else
            {
                Console.WriteLine("Test #8: failure a");
            }
            // test #9: delete the cluster 
            a = client.DeleteAsync("Clusters/" + newClusterID);
            a.Wait();
            responseTaskPostCluster = a.Result;
            if (responseTaskPostCluster.IsSuccessStatusCode)
            {
                Console.WriteLine("Test #9: delete of cluster: success");
            }
            else
            {
                Console.WriteLine("Test #9: delete of cluster: failure a");
            }
            // test #10: check that the cluster has been deleted
            List<int> updatedClusterIDs = null;
            a = client.GetAsync("Clusters");
            a.Wait();
            responseGetClusterIDs = a.Result;
            if (responseGetClusterIDs.IsSuccessStatusCode)
            {
                string str = await responseGetClusterIDs.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(str))
                {
                    updatedClusterIDs = JsonConvert.DeserializeObject<List<int>>(str);
                    if (updatedClusterIDs != null && !updatedClusterIDs.Contains(newClusterID))
                    {
                        Console.Write("Test #10: that the cluster has been deleted: success. IDs: ");
                        for (int i = 0; i < updatedClusterIDs.Count; i++)
                        {
                            Console.Write(updatedClusterIDs[i].ToString() + "\t");
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("Test #10: that the cluster has been deleted: failure c");
                    }
                }
                else
                {
                    Console.WriteLine("Test #10: that the cluster has been deleted: failure b");
                }
            }
            else
            {
                Console.WriteLine("Test #10: that the cluster has been deleted: failure a");
            }
        }
     }
}
