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
            string file="";
            int wellcase = 1;
            if (wellcase == 1)
            {
                file = directory + "iscwsa-1.txt";
            }
            else if (wellcase == 2)
            {
                file = directory + "iscwsa-2.txt";
            }
            else if (wellcase == 3)
            {
                file = directory + "iscwsa-3.txt";
            }
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
						ISCWSA_MWDSurveyStationUncertainty iscwsa = new ISCWSA_MWDSurveyStationUncertainty();
                        if (wellcase == 1)
                        {
                            iscwsa.Gravity = 9.80665;
                            iscwsa.BField = 50000;
                            iscwsa.Dip = 72 * Math.PI / 180.0;
                            iscwsa.Declination = -4 * Math.PI / 180.0;
                            iscwsa.Convergence = 0.0;
                        }
                        else if (wellcase == 2)
                        {
                            iscwsa.Gravity = 9.80665;
                            iscwsa.BField = 48000;
                            iscwsa.Dip = 58 * Math.PI / 180.0;
                            iscwsa.Declination = 2 * Math.PI / 180.0;
                            iscwsa.Convergence = 0.0;
                        }
                        else if (wellcase == 3)
                        {
                            iscwsa.Gravity = 9.80665;
                            iscwsa.BField = 61000;
                            iscwsa.Dip = -70 * Math.PI / 180.0;
                            iscwsa.Declination = 13 * Math.PI / 180.0;
                            iscwsa.Convergence = 0.0;
                        }
                        SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.ISCWSA_MWD_Rev5_OWSG);
						//WdWSurveyStationUncertainty wdw = new WdWSurveyStationUncertainty();
      //                  SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.WdWGoodMag);
                        st.SurveyTool = surveyTool;
                        //st.Uncertainty = wdw;
                        st.Uncertainty = iscwsa;
                        surveyList.Add(st);
                    }
                }
            }

            if (surveyList != null)
            {                
                surveyList.GetUncertaintyEnvelope(0.95, 1);
            }
        }
	}
}
