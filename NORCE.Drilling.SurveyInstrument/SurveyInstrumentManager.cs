using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using NORCE.General.Std;

namespace NORCE.Drilling.SurveyInstrument
{
    public class SurveyInstrumentManager : IManager<SurveyInstrument>
    {
        public SurveyInstrumentManager()
        {
        }

        public int Count
        {
            get
            {
                return SurveyInstrumentManagerSingleton.Instance.Count;
            }
        }

        public bool Clear()
        {
            return SurveyInstrumentManagerSingleton.Instance.Clear();
        }

        public SurveyInstrument this[int i]
        {
            get
            {
                return SurveyInstrumentManagerSingleton.Instance[i];
            }
        }

        public SurveyInstrument Get(string instrumentName)
        {
            return SurveyInstrumentManagerSingleton.Instance.Get(instrumentName);
        }

        public SurveyInstrument Get(int instrumentID)
        {
            return SurveyInstrumentManagerSingleton.Instance.Get(instrumentID);
        }

        public bool Add(SurveyInstrument instrument)
        {
            return SurveyInstrumentManagerSingleton.Instance.Add(instrument);
        }

        public bool Remove(SurveyInstrument instrument)
        {
            return SurveyInstrumentManagerSingleton.Instance.Remove(instrument);
        }

        public bool Remove(string instrumentName)
        {
            return SurveyInstrumentManagerSingleton.Instance.Remove(instrumentName);
        }

        public bool Remove(int instrumentID)
        {
            return SurveyInstrumentManagerSingleton.Instance.Remove(instrumentID);
        }

        public bool Update(int instrumentID, SurveyInstrument updatedInstrument)
        {
            return SurveyInstrumentManagerSingleton.Instance.Update(instrumentID, updatedInstrument);
        }
    }
}
