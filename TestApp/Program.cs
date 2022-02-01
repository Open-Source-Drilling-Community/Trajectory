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
                        //var surveyToolll = LoadSurveyTool(1);



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
                    for (int i = 0; i < surveyList.ListOfSurveys.Count; i++)
                    {
                        //Not able to deserialize matrix yet. Adding values to covatiance matrix
                        if (surveyList.ListOfSurveys[i].Uncertainty != null && surveyList.ListOfSurveys[i].Uncertainty.Covariance != null)
                            surveyList.ListOfSurveys[i].Uncertainty.Covariance[0, 0] = surveyList.ListOfSurveys[i].Uncertainty.C11;
                        surveyList.ListOfSurveys[i].Uncertainty.Covariance[0, 1] = surveyList.ListOfSurveys[i].Uncertainty.C12;
                        surveyList.ListOfSurveys[i].Uncertainty.Covariance[0, 2] = surveyList.ListOfSurveys[i].Uncertainty.C13;
                        surveyList.ListOfSurveys[i].Uncertainty.Covariance[1, 0] = surveyList.ListOfSurveys[i].Uncertainty.C21;
                        surveyList.ListOfSurveys[i].Uncertainty.Covariance[1, 1] = surveyList.ListOfSurveys[i].Uncertainty.C22;
                        surveyList.ListOfSurveys[i].Uncertainty.Covariance[1, 2] = surveyList.ListOfSurveys[i].Uncertainty.C23;
                        surveyList.ListOfSurveys[i].Uncertainty.Covariance[2, 0] = surveyList.ListOfSurveys[i].Uncertainty.C31;
                        surveyList.ListOfSurveys[i].Uncertainty.Covariance[2, 1] = surveyList.ListOfSurveys[i].Uncertainty.C32;
                        surveyList.ListOfSurveys[i].Uncertainty.Covariance[2, 2] = surveyList.ListOfSurveys[i].Uncertainty.C33;

                    }
                    if (surveyList.EllipseVerticesPhi == 0.0)
                    {
                        surveyList.EllipseVerticesPhi = 32;

                    }
                    if (surveyList.IntermediateEllipseNumbers == 0.0)
                    {
                        surveyList.IntermediateEllipseNumbers = 6;

                    }
                    if (surveyList.MaxDistanceCoordinate == 0.0)
                    {
                        surveyList.MaxDistanceCoordinate = 3;

                    }
                    if (surveyList.MaxDistanceEllipse == 0.0)
                    {
                        surveyList.MaxDistanceEllipse = 3;

                    }
                    double[,] A = new double[3, 3];
                    if (surveyList[0].Uncertainty is ISCWSA_MWDSurveyStationUncertainty)
                    {                        
                        for (int i = 0; i < A.GetLength(0); i++)
                        {
                            for (int j = 0; j < A.GetLength(1); j++)
                            {
                                A[i, j] = 0.0;
                            }
                        }
                    }
                    List<double[,]> drdps = new List<double[,]>();
                    List<double[,]> drdpNexts = new List<double[,]>();
                    ISCWSA_MWDSurveyStationUncertainty iscwsaSurveyStatoinUncertainty = (ISCWSA_MWDSurveyStationUncertainty)surveyList[0].Uncertainty;
                    for (int i = 0; i < surveyList.Count; i++)
                    {
                        if (((surveyList[i].Uncertainty is ISCWSA_MWDSurveyStationUncertainty && i > 0) || (surveyList.Count > 1 && surveyList[i].Uncertainty.Covariance[0, 0] == null)))
                        {
                           

                            double[,] drdp = new double[3, 3];
                            if (i == 0)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    for (int k = 0; k < 3; k++)
                                    {
                                        drdp[j, k] = 0.0;
                                    }
                                }
                            }
                            else
                            {
                                drdp = iscwsaSurveyStatoinUncertainty.CalculateDisplacementMatrix(surveyList[i], surveyList[i - 1], i);                                
                            }
                            drdps.Add(drdp);
                            double[,] drdpNext = new double[3, 3];
                            if (i < surveyList.Count - 1)
                            {
                                drdpNext = iscwsaSurveyStatoinUncertainty.CalculateDisplacemenNexttMatrix(surveyList[i], surveyList[i + 1], i);                                
                            }
                            else
                            {
                                SurveyStation surveySt = new SurveyStation();
                                surveySt.X = 0.0;
                                surveySt.Y = 0.0;
                                surveySt.Incl = 0.0;
                                surveySt.Az = 0.0;
                                drdpNext = iscwsaSurveyStatoinUncertainty.CalculateDisplacemenNexttMatrix(surveyList[i], surveySt, i);
                            }
                            drdpNexts.Add(drdpNext);
                            A = iscwsaSurveyStatoinUncertainty.CalculateCovarianceDRFR(drdp, drdpNext, surveyList[i].SurveyTool, A, i);
                            
                        }
                    }
                    iscwsaSurveyStatoinUncertainty.CalculateCovarianceMSZ(surveyList,drdps,drdpNexts, surveyList[0].SurveyTool);
                    ErrorSourceMSZ errorSourceMSZ = new ErrorSourceMSZ();
                    errorSourceMSZ.Dip = 72 * Math.PI / 180.0;
                    errorSourceMSZ.Declination = -4 * Math.PI / 180.0;
                    iscwsaSurveyStatoinUncertainty.CalculateSystematicCovariance(surveyList, drdps, drdpNexts, surveyList[0].SurveyTool, errorSourceMSZ);
                    ErrorSourceMSXY_TI1 errorSourceMSXY_TI1 = new ErrorSourceMSXY_TI1();
                    errorSourceMSXY_TI1.Dip = 72 * Math.PI / 180.0;
                    errorSourceMSXY_TI1.Declination = -4 * Math.PI / 180.0;
                    iscwsaSurveyStatoinUncertainty.CalculateSystematicCovariance(surveyList, drdps, drdpNexts, surveyList[0].SurveyTool, errorSourceMSXY_TI1);
                    //surveyList.GetUncertaintyEnvelope(0.9, 1.0);
                }
            }
        }
	}
}
