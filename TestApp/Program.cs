using System;
using NORCE.Drilling.Trajectory;
using NORCE.Drilling.SurveyInstrument.Model;
using System.IO;
using System.Collections.Generic;



namespace TestApp
{
	class Program
	{
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            SurveyList surveyList = new SurveyList();
            string homeDirectory = ".." + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar+ ".." + Path.DirectorySeparatorChar;
            string directory = @homeDirectory ;
            string file = directory + "iscwsa-1.txt";
            using (StreamReader r = new StreamReader(file))
            {
                while (!r.EndOfStream)
                {
                    char[] sep = { '\t' };
                    string[] words = r.ReadLine().Split(sep);
                    if (words.Length > 1)
                    {
                        SurveyStation st = new SurveyStation();
                        double md = 0.0;
                        bool ok = NORCE.General.Std.Numeric.TryParse(words[0], out md);
                        double incl = 0.0;
                        ok = NORCE.General.Std.Numeric.TryParse(words[1], out incl);
                        double az = 0.0;
                        ok = NORCE.General.Std.Numeric.TryParse(words[2], out az);
                        double tvd = 0.0;
                        ok = NORCE.General.Std.Numeric.TryParse(words[3], out tvd);
                        double X = 0.0;
                        //ok = NORCE.General.Std.Numeric.TryParse(words[4], out X);
                        double Y = 0.0;
                        //ok = NORCE.General.Std.Numeric.TryParse(words[5], out Y);
                        st.Az = az * Math.PI / 180.0;
                        st.Incl = incl * Math.PI / 180.0; ;
                        st.X = X;
                        st.Y = Y;
                        st.Z = tvd;
                        st.MD = md;
                        ISCWSA_MWDSurveyStationUncertainty wdwun = new ISCWSA_MWDSurveyStationUncertainty();
                        SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.ISCWSA_MWD_Rev5_OWSG);
                        st.SurveyTool = surveyTool;
                        st.Uncertainty = wdwun;
                        surveyList.Add(st);
                    }
                }
            }

            if (surveyList != null)
            {
                if (surveyList.ListOfSurveys != null)
                {               
                    List<ISCWSAErrorData> ISCWSAErrorDataTmp = new List<ISCWSAErrorData>();
                    for (int i = 0; i < surveyList.Count; i++)
                    {
                        ISCWSA_MWDSurveyStationUncertainty iscwsaSurveyStatoinUncertainty = (ISCWSA_MWDSurveyStationUncertainty)surveyList[i].Uncertainty;
                        if (((surveyList[i].Uncertainty is ISCWSA_MWDSurveyStationUncertainty && i > 0) || (surveyList.Count > 1 && surveyList[i].Uncertainty.Covariance[0, 0] == null)))
                        {
                            SurveyStation surveyStation = surveyList[i];
                            SurveyStation surveyStationPrev = new SurveyStation();
                            SurveyStation surveyStationNext = new SurveyStation();                           
                            if (i == 0)
                            {                                
                                surveyStationPrev.X = 0.0;
                                surveyStationPrev.Y = 0.0;
                                surveyStationPrev.Incl = 0.0;
                                surveyStationPrev.Az = 0.0;
                                surveyStationPrev.MD = 0.0;
                            }
                            else
                            {
                                surveyStationPrev = surveyList[i - 1];                                                       
                            }                            
                            if (i < surveyList.Count - 1)
                            {
                                surveyStationNext = surveyList[i + 1];
                            }
                            else
                            {
                                surveyStationNext.X = 0.0;
                                surveyStationNext.Y = 0.0;
                                surveyStationNext.Incl = 0.0;
                                surveyStationNext.Az = 0.0;
                                surveyStationNext.MD = 0.0;
                            }
                            iscwsaSurveyStatoinUncertainty.CalculateCovariance(surveyStation, surveyStationPrev, surveyStationNext, ISCWSAErrorDataTmp, i);
                            ISCWSAErrorDataTmp = iscwsaSurveyStatoinUncertainty.ISCWSAErrorDataTmp;                  
                        }
                    }                    
                }
            }
        }
	}
}
