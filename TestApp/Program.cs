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
            int wellcase = 3;
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
                SurveyStation stPrev = new SurveyStation();
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
                        if (false && surveyList.Count > 0)
                        {
                            double azIncr = (az * Math.PI / 180.0 - (double)stPrev.AzWGS84) / 3;
                            double inclIncr = (incl * Math.PI / 180.0 - (double)stPrev.Incl) / 3;
                            double mdIncr = (md - (double)stPrev.MdWGS84) / 3;
                            double tvdIncr = (tvd - (double)stPrev.TvdWGS84) / 3;
                            for (int i = 0; i < 2; i++)
                            {
                                SurveyStation stInt = new SurveyStation();
                                double azInt = (double)stPrev.AzWGS84 + azIncr * (i + 1);
                                double inclInt = (double)stPrev.Incl + inclIncr * (i + 1);
                                double mdInt = (double)stPrev.MdWGS84 + mdIncr * (i + 1);
                                double tvdInt = (double)stPrev.TvdWGS84 + tvdIncr * (i + 1);
                                stInt.AzWGS84 = azInt;
                                stInt.Incl = inclInt;
                                stInt.MdWGS84 = mdInt;
                                stInt.TvdWGS84 = tvdInt;
                                stInt.NorthOfWellHead  = X;
                                stInt.EastOfWellHead = Y;
                                ISCWSA_SurveyStationUncertainty iscwsat = new ISCWSA_SurveyStationUncertainty();
                                if (wellcase == 1)
                                {
                                    iscwsat.Gravity = 9.80665;
                                    iscwsat.BField = 50000;
                                    iscwsat.Dip = 72 * Math.PI / 180.0;
                                    iscwsat.Declination = -4 * Math.PI / 180.0;
                                    iscwsat.Convergence = 0.0;
                                    iscwsat.Latitude = 60 * Math.PI / 180.0;
                                }
                                else if (wellcase == 2)
                                {
                                    iscwsat.Gravity = 9.80665;
                                    iscwsat.BField = 48000;
                                    iscwsat.Dip = 58 * Math.PI / 180.0;
                                    iscwsat.Declination = 2 * Math.PI / 180.0;
                                    iscwsat.Convergence = 0.0;
                                    iscwsat.Latitude = 28 * Math.PI / 180.0;
                                }
                                else if (wellcase == 3)
                                {
                                    iscwsat.Gravity = 9.80665;
                                    iscwsat.BField = 61000;
                                    iscwsat.Dip = -70 * Math.PI / 180.0;
                                    iscwsat.Declination = 13 * Math.PI / 180.0;
                                    iscwsat.Convergence = 0.0;
                                    iscwsat.Latitude = -40 * Math.PI / 180.0;
                                }
								SurveyInstrument surveyToolt = new SurveyInstrument(SurveyInstrument.ISCWSA_MWD_Rev5_OWSG);
								//SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.ISCWSAGyroExample1);
								//SurveyInstrument surveyToolt = new SurveyInstrument(SurveyInstrument.ISCWSAGyroExample1);
                                //WdWSurveyStationUncertainty wdw = new WdWSurveyStationUncertainty();
                                //                  SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.WdWGoodMag);
                                stInt.SurveyTool = surveyToolt;
                                //st.Uncertainty = wdw;
                                stInt.Uncertainty = iscwsat;
                                surveyList.Add(stInt);
                            }
                        }
                        st.AzWGS84 = az * Math.PI / 180.0;
                        st.Incl = incl * Math.PI / 180.0; ;
                        st.NorthOfWellHead  = X;
                        st.EastOfWellHead = Y;
                        if(wellcase == 2)
						{
                            tvd = tvd * 0.3048;
                            md = md * 0.3048;
                        }
                        st.TvdWGS84 = tvd;
                        st.MdWGS84 = md;
						ISCWSA_SurveyStationUncertainty iscwsa = new ISCWSA_SurveyStationUncertainty();
                        if (wellcase == 1)
                        {
                            iscwsa.Gravity = 9.80665;
                            iscwsa.BField = 50000;
                            iscwsa.Dip = 72 * Math.PI / 180.0;
                            iscwsa.Declination = -4 * Math.PI / 180.0;
                            iscwsa.Convergence = 0.0;
                            iscwsa.Latitude = 60 * Math.PI / 180.0;
                        }
                        else if (wellcase == 2)
                        {
                            iscwsa.Gravity = 9.80665;
                            iscwsa.BField = 48000;
                            iscwsa.Dip = 58 * Math.PI / 180.0;
                            iscwsa.Declination = 2 * Math.PI / 180.0;
                            iscwsa.Convergence = 0.0;
                            iscwsa.Latitude = 28 * Math.PI / 180.0;
                        }
                        else if (wellcase == 3)
                        {
                            iscwsa.Gravity = 9.80665;
                            iscwsa.BField = 61000;
                            iscwsa.Dip = -70 * Math.PI / 180.0;
                            iscwsa.Declination = 13 * Math.PI / 180.0;
                            iscwsa.Convergence = 0.0;
                            iscwsa.Latitude = -40 * Math.PI / 180.0;
                        }
						//SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.ISCWSA_MWD_Rev5_OWSG);
						//SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.ISCWSAGyroExample2);
						//SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.ISCWSAGyroExample3);
                        SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.ISCWSAGyroExample4);
                        //WdWSurveyStationUncertainty wdw = new WdWSurveyStationUncertainty();
                        //                  SurveyInstrument surveyTool = new SurveyInstrument(SurveyInstrument.WdWGoodMag);
                        st.SurveyTool = surveyTool;
                        //st.Uncertainty = wdw;
                        st.Uncertainty = iscwsa;
                        surveyList.Add(st);
                        stPrev = st;
                    }
                }
            }

            if (surveyList != null)
            {                
                surveyList.GetUncertaintyEnvelope(0.95, 1);
                //Print results to file
                string[] lines = new string[surveyList.Count];
                for (int i = 0; i < surveyList.Count; i++)
                {
                    var cov = surveyList[i].Uncertainty.Covariance;
                    lines[i] = surveyList[i].MdWGS84 + ";" + cov[0, 0].ToString() + ";" + cov[1, 1].ToString() + ";" + cov[2, 2].ToString() + ";" + cov[0, 1].ToString()
                        + ";" + cov[0, 2].ToString() + ";" + cov[1, 2].ToString(); 

                }
                File.WriteAllLines("ISCWSACovarianceResults.txt", lines);
            }
        }
	}
}
