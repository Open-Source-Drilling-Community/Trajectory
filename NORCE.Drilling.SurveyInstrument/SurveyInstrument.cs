using System;
using Newtonsoft.Json;
using NORCE.General.Std;

namespace NORCE.Drilling.SurveyInstrument
{
    public class SurveyInstrument : INameable, IIdentifiable, IUndefinable
    {
        protected static Random rnd_ = null;
        public string Name { get; set; }
        public int ID { get; set; }

        public SurveyInstrument()
        {
            if (rnd_ == null)
            {
                InitializeRandomGenerator();
            }
        }
        public SurveyInstrument(string name)
        {
            if (rnd_ == null)
            {
                InitializeRandomGenerator();
            }
            Name = name;
            ID = rnd_.Next();
        }

        public SurveyInstrument(string name, int id)
        {
            Name = name;
            ID = id;
        }

        public SurveyInstrument(SurveyInstrument wb)
        {
            if (wb != null)
            {
                Name = wb.Name;
                ID = wb.ID;
            }
        }

        public void CopyFrom(SurveyInstrument source)
        {
            if (source != null)
            {
                Name = source.Name;
            }
        }

        public bool IsUndefined()
        {
            return string.IsNullOrEmpty(Name) || ID <= 0;
        }

        public void SetUndefined()
        {
            ID = 0;
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static SurveyInstrument FromJson(string str)
        {
            SurveyInstrument wb = null;
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    wb = JsonConvert.DeserializeObject<SurveyInstrument>(str);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            return wb;
        }

        private void InitializeRandomGenerator()
        {
            Guid guid = Guid.NewGuid();
            byte[] bytes = guid.ToByteArray();
            int sum = 0;
            foreach (byte b in bytes)
            {
                if (sum < int.MaxValue - 256)
                {
                    sum += (int)b;
                }
            }
            rnd_ = new Random(sum);
        }
    }
}
