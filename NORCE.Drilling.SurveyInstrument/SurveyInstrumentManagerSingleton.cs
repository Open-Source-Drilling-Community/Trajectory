using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace NORCE.Drilling.SurveyInstrument
{
    public class SurveyInstrumentManagerSingleton
    {
        public static string Directory = "";
        private static string filename_ = "SurveyInstruments.json";

        private object lock_ = new object();

        private List<SurveyInstrument> instruments_ = new List<SurveyInstrument>();

        private static SurveyInstrumentManagerSingleton instance_ = null;

        private SurveyInstrumentManagerSingleton()
        {

        }

        public static SurveyInstrumentManagerSingleton Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = new SurveyInstrumentManagerSingleton();
                    instance_.Download();
                }
                return instance_;
            }
        }

        public void CreateDefault()
        {
            Drilling.SurveyInstrument.SurveyInstrument u1 = new Drilling.SurveyInstrument.SurveyInstrument("SurveyInstrument1");
            Drilling.SurveyInstrument.SurveyInstrument u2 = new Drilling.SurveyInstrument.SurveyInstrument("SurveyInstrumen2");
            Drilling.SurveyInstrument.SurveyInstrument u3 = new Drilling.SurveyInstrument.SurveyInstrument("SurveyInstrumen3");
            Add(u1);
            Add(u2);
            Add(u3);
        }

        public int Count
        {
            get
            {
                int count = 0;
                lock (lock_)
                {
                    count = instruments_.Count;
                }
                return count;
            }
        }

        public bool Clear()
        {
            bool result = false;
            lock (lock_)
            {
                instruments_.Clear();
                result = Save();
            }
            return result;
        }

        public SurveyInstrument this[int i]
        {
            get
            {
                SurveyInstrument instrument = null;
                lock (lock_)
                {
                    if (i < instruments_.Count)
                    {
                        return instruments_[i];
                    }
                }
                return instrument;
            }
        }

        public SurveyInstrument Get(string instrumentName)
        {
            if (!string.IsNullOrEmpty(instrumentName))
            {
                SurveyInstrument w = null;
                lock (lock_)
                {
                    foreach (SurveyInstrument instrument in instruments_)
                    {
                        if (instrument != null && instrumentName.Equals(instrument.Name))
                        {
                            w = instrument;
                            break;
                        }
                    }
                }
                return w;
            }
            else
            {
                return null;
            }
        }

        public SurveyInstrument Get(int instrumentID)
        {
            if (instrumentID > 0)
            {
                SurveyInstrument w = null;
                lock (lock_)
                {
                    foreach (SurveyInstrument instrument in instruments_)
                    {
                        if (instrument != null && instrumentID == instrument.ID)
                        {
                            w = instrument;
                            break;
                        }
                    }
                }
                return w;
            }
            else
            {
                return null;
            }
        }

        public bool Add(SurveyInstrument instrument)
        {
            bool result = false;
            if (instrument != null && !instrument.IsUndefined())
            {
                lock (lock_)
                {
                    instruments_.Add(instrument);
                    result = Save();
                }
            }
            return result;
        }

        public bool Remove(SurveyInstrument instrument)
        {
            bool result = false;
            if (instruments_.Contains(instrument))
            {
                lock (lock_)
                {
                    instruments_.Remove(instrument);
                    result = Save();
                }
            }
            return result;
        }

        public bool Remove(string instrumentName)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(instrumentName))
            {
                lock (lock_)
                {
                    foreach (SurveyInstrument instrument in instruments_)
                    {
                        if (instrument != null && instrumentName.Equals(instrument.Name))
                        {
                            instruments_.Remove(instrument);
                        }
                    }
                    result = Save();
                }
            }
            return result;
        }

        public bool Remove(int instrumentID)
        {
            bool result = false;
            if (instrumentID > 0)
            {
                lock (lock_)
                {
                    foreach (SurveyInstrument instrument in instruments_)
                    {
                        if (instrument != null && instrument.ID == instrumentID)
                        {
                            instruments_.Remove(instrument);
                            break;
                        }
                    }
                    result = Save();
                }
            }
            return result;
        }

        public bool Update(int instrumentID, SurveyInstrument updatedInstrument)
        {
            bool result = false;
            if (instrumentID > 0 && updatedInstrument != null)
            {
                lock (lock_)
                {
                    SurveyInstrument instrument = Get(instrumentID);
                    if (instrument == null)
                    {
                        result = Add(updatedInstrument);
                    }
                    else
                    {
                        instrument.CopyFrom(updatedInstrument);
                        result = Save();
                    }
                }
            }
            return result;
        }

        private void Download()
        {
            if (File.Exists(Directory + filename_))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(Directory + filename_))
                    {
                        List<SurveyInstrument> instruments = JsonConvert.DeserializeObject<List<SurveyInstrument>>(reader.ReadToEnd());
                        if (instruments != null)
                        {
                            lock (lock_)
                            {
                                instruments_.Clear();
                                foreach (SurveyInstrument instrument in instruments)
                                {
                                    if (instrument != null && !instrument.IsUndefined())
                                    {
                                        instruments_.Add(instrument);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private bool Save()
        {
            bool success = false;
            try
            {
                Random rnd = new Random();
                int itmp = 0;
                do
                {
                    itmp = rnd.Next(999);
                } while (File.Exists(Directory + "Temp" + itmp.ToString("D000") + ".json"));
                string stmp = Directory + "Temp" + itmp.ToString("D000") + ".json";
                if (!File.Exists(stmp))
                {
                    using (StreamWriter writer = new StreamWriter(stmp))
                    {
                        string jsonStr = JsonConvert.SerializeObject(instruments_);
                        if (!string.IsNullOrEmpty(jsonStr))
                        {
                            writer.Write(jsonStr);
                        }
                    }
                }
                if (File.Exists(stmp))
                {
                    if (File.Exists(Directory + filename_))
                    {
                        File.Delete(Directory + filename_);
                    }
                    File.Move(stmp, Directory + filename_);
                    success = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return success;
        }
    }
}
