using System;
using System.Collections.Generic;
using System.Text;
using NORCE.General.Std;
using NORCE.General.Math;
using NORCE.Drilling.SurveyInstrument.Model;
using NORCE.General.GeoMagneticField;
using NORCE.Drilling.Geodetic.Model;
using System.Text.Json;

namespace NORCE.Drilling.Trajectory
{
    public class ISCWSA_SurveyStationUncertainty : SurveyStationUncertainty
    {
        private double[,] P_ = new double[3, 3];       
        /// <summary>
        /// Convergence [rad]
        /// </summary>
        public double Convergence { get; set; } = 0;
        /// <summary>
        /// Latitude [rad]
        /// </summary>
        public double Latitude { get; set; } = 0;
        /// <summary>
        /// Ineces of error sources
        /// </summary>
        public int[] ErrorIndices { get; set; } = null;
       
        /// <summary>
        /// Used for calculations
        /// </summary>
        public List<ISCWSAErrorData> ISCWSAErrorDataTmp { get; set; } = null;

        private bool continuousMode_ = false;

        private bool initialize_ = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ISCWSA_SurveyStationUncertainty()
        {
            for (int i = 0; i < Covariance.RowCount; i++)
            {
                for (int j = 0; j < Covariance.ColumnCount; j++)
                {
                    Covariance[i, j] = 0;
                }
            }
            for (int i = 0; i < P_.GetLength(0); i++)
            {
                for (int j = 0; j < P_.GetLength(1); j++)
                {
                    P_[i, j] = 0;
                }
            }
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="source"></param>
        public ISCWSA_SurveyStationUncertainty(ISCWSA_SurveyStationUncertainty source)
        {
            if (source != null)
            {
                //SurveyTool = source.SurveyTool;
                Bias = new Vector3D(source.Bias);


                for (int i = 0; i < Covariance.RowCount; i++)
                {
                    for (int j = 0; j < Covariance.ColumnCount; j++)
                    {
                        Covariance[i, j] = source.Covariance[i, j];
                    }
                }
                for (int i = 0; i < P_.GetLength(0); i++)
                {
                    for (int j = 0; j < P_.GetLength(1); j++)
                    {
                        P_[i, j] = source.P_[i, j];
                    }
                }
            }
            else
            {
                for (int i = 0; i < Covariance.RowCount; i++)
                {
                    for (int j = 0; j < Covariance.ColumnCount; j++)
                    {
                        Covariance[i, j] = 0;
                    }
                }
                for (int i = 0; i < P_.GetLength(0); i++)
                {
                    for (int j = 0; j < P_.GetLength(1); j++)
                    {
                        P_[i, j] = 0;
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="surveyTool"></param>
        public ISCWSA_SurveyStationUncertainty(SurveyInstrument.Model.SurveyInstrument surveyTool)
        {
            //SurveyTool = surveyTool;
            for (int i = 0; i < Covariance.RowCount; i++)
            {
                for (int j = 0; j < Covariance.ColumnCount; j++)
                {
                    Covariance[i, j] = 0;
                }
            }
            for (int i = 0; i < P_.GetLength(0); i++)
            {
                for (int j = 0; j < P_.GetLength(1); j++)
                {
                    P_[i, j] = 0;
                }
            }
        }

        /// <summary>
        /// Calculate Covariance matrix for a survey station (ISCWSA MWD Rev 5)
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="previousStation"></param>
        /// <returns></returns>
        public void CalculateCovariance(SurveyStation surveyStation, SurveyStation surveyStationPrev, SurveyStation surveyStationNext, List<ISCWSAErrorData> ISCWSAErrorDataPrev, int c)
        {
            #region Define error sources
            if (surveyStation.SurveyTool.ErrorSources == null || surveyStation.SurveyTool.ErrorSources.Count == 0)
            {
                if(surveyStation.SurveyTool.ErrorSourcesHolder != null && surveyStation.SurveyTool.ErrorSourcesHolder.Count > 0)
                surveyStation.SurveyTool.FillErrorSources();
            }
            #endregion
            #region Calculations from previous survey station are used
            if (ISCWSAErrorDataPrev == null || ISCWSAErrorDataPrev.Count == 0)
            {
                ISCWSAErrorDataPrev = new List<ISCWSAErrorData>();
                for (int i = 0; i < surveyStation.SurveyTool.ErrorSources.Count; i++)
                {
                    ISCWSAErrorDataPrev.Add(new ISCWSAErrorData());
                    ISCWSAErrorDataPrev[i].SigmaErrorRandom = new double[3, 3];
                    for (int j = 0; j < ISCWSAErrorDataPrev[i].SigmaErrorRandom.GetLength(0); j++)
                    {
                        for (int k = 0; k < ISCWSAErrorDataPrev[i].SigmaErrorRandom.GetLength(1); k++)
                        {
                            ISCWSAErrorDataPrev[i].SigmaErrorRandom[j, k] = 0.0;
                        }
                    }
                    ISCWSAErrorDataPrev[i].ErrorSum = new double[3];
                    for (int j = 0; j < 3; j++)
                    {
                        ISCWSAErrorDataPrev[i].ErrorSum[j] = 0.0;
                    }
                }
            }
            ISCWSAErrorDataTmp = ISCWSAErrorDataPrev;
            #endregion
            #region The effect on the borehole positions of changes in the survey measurement vector
            double[,] drdp = new double[3, 3];
            if (c == 0)
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
                drdp = CalculateDisplacementMatrix(surveyStation, surveyStationPrev, c);
            }
            double[,] drdpNext = new double[3, 3];
            drdpNext = CalculateDisplacemenNexttMatrix(surveyStation, surveyStationNext, c);
			#endregion
			#region Covariance calculation
			for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Covariance[j, k] = 0.0;
                }
            }
            
            CalculateAllCovariance(surveyStation, surveyStationPrev, surveyStationNext, drdp, drdpNext, surveyStation.SurveyTool.ErrorSources, null, c);
            #endregion
        }
        
        /// <summary>
        /// Effect of the errors in the survey measurements at station k, on the position vector from survey station k-1 to survey station k
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="previousStation"></param>
        /// <returns></returns>
        public double[,] CalculateDisplacementMatrix(SurveyStation surveyStation, SurveyStation previousStation, int st)
        {
            //ref https://www.iscwsa.net/error-model-documentation/
            double[] drk_dDepth = new double[3];
            double[] drk_dInc = new double[3];
            double[] drk_dAz = new double[3];
            double[,] A = new double[3, 3];
            if (surveyStation != null && surveyStation.SurveyTool != null && previousStation != null && A != null)
            {
                double sinI = System.Math.Sin((double)surveyStation.Incl);
                double cosI = System.Math.Cos((double)surveyStation.Incl);
                double sinA = System.Math.Sin((double)surveyStation.AzWGS84);
                double cosA = System.Math.Cos((double)surveyStation.AzWGS84);
                double sinIp = System.Math.Sin((double)previousStation.Incl);
                double cosIp = System.Math.Cos((double)previousStation.Incl);
                double sinAp = System.Math.Sin((double)previousStation.AzWGS84);
                double cosAp = System.Math.Cos((double)previousStation.AzWGS84);
                double deltaSk = (double)(double)surveyStation.MdWGS84 - (double)previousStation.MdWGS84;
                drk_dDepth[0] = 0.5 * (sinIp * cosAp + sinI * cosA);
                drk_dDepth[1] = 0.5 * (sinIp * sinAp + sinI * sinA);
                drk_dDepth[2] = 0.5 * (cosIp + cosI);
                if (st == 1)
                {
                    //NB Check why multiplied by 2
                    drk_dInc[0] = 0.5 * deltaSk * cosI * cosA * 2;
                    drk_dInc[1] = 0.5 * deltaSk * cosI * sinA * 2;
                    drk_dInc[2] = -0.5 * deltaSk * sinI * 2;
                    drk_dAz[0] = -0.5 * deltaSk * sinI * sinA * 2;
                    drk_dAz[1] = 0.5 * deltaSk * sinI * cosA * 2;
                }
                else
                {
                    drk_dInc[0] = 0.5 * deltaSk * cosI * cosA;
                    drk_dInc[1] = 0.5 * deltaSk * cosI * sinA;
                    drk_dInc[2] = -0.5 * deltaSk * sinI;
                    drk_dAz[0] = -0.5 * deltaSk * sinI * sinA;
                    drk_dAz[1] = 0.5 * deltaSk * sinI * cosA;
                }
                drk_dAz[2] = 0.0;  // =0
                A[0, 0] = drk_dDepth[0];
                A[1, 0] = drk_dDepth[1];
                A[2, 0] = drk_dDepth[2];
                A[0, 1] = drk_dInc[0];
                A[1, 1] = drk_dInc[1];
                A[2, 1] = drk_dInc[2];
                A[0, 2] = drk_dAz[0];
                A[1, 2] = drk_dAz[1];
                A[2, 2] = drk_dAz[2];
            }
            return A;
        }
        /// <summary>
        /// Effect of the errors in the survey measurements at station k, on the position vector from survey station k to survey station k+1
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="nextStation"></param>
        /// <returns></returns>
        public double[,] CalculateDisplacemenNexttMatrix(SurveyStation surveyStation, SurveyStation nextStation, int st)
        {
            //ref https://www.iscwsa.net/error-model-documentation/
            double[] drk_dDepth = new double[3];
            double[] drk_dInc = new double[3];
            double[] drk_dAz = new double[3];
            double[,] A = new double[3, 3];
            if (surveyStation != null && surveyStation.SurveyTool != null && nextStation != null && A != null)
            {
                double sinI = System.Math.Sin((double)surveyStation.Incl);
                double cosI = System.Math.Cos((double)surveyStation.Incl);
                double sinA = System.Math.Sin((double)surveyStation.AzWGS84);
                double cosA = System.Math.Cos((double)surveyStation.AzWGS84);
                double sinIn = System.Math.Sin((double)nextStation.Incl);
                double cosIn = System.Math.Cos((double)nextStation.Incl);
                double sinAn = System.Math.Sin((double)nextStation.AzWGS84);
                double cosAn = System.Math.Cos((double)nextStation.AzWGS84);
                double deltaSk = (double)nextStation.MdWGS84 - (double)(double)surveyStation.MdWGS84;
                drk_dDepth[0] = -0.5 * (sinI * cosA + sinIn * cosAn);
                drk_dDepth[1] = -0.5 * (sinI * sinA + sinIn * sinAn);
                if (st == 0)
                {
                    drk_dDepth[2] = 0.0;
                }
                else
                {
                    drk_dDepth[2] = -0.5 * (cosI + cosIn);  // 
                }
                if (st == 0)
                {
                    drk_dInc[0] = 0.0;
                }
                else
                {
                    drk_dInc[0] = 0.5 * deltaSk * cosI * cosA;
                }
                drk_dInc[1] = 0.5 * deltaSk * cosI * sinA;
                drk_dInc[2] = -0.5 * deltaSk * sinI;
                drk_dAz[0] = -0.5 * deltaSk * sinI * sinA;
                drk_dAz[1] = 0.5 * deltaSk * sinI * cosA;
                drk_dAz[2] = 0.0;  // =0
                A[0, 0] = drk_dDepth[0];
                A[1, 0] = drk_dDepth[1];
                A[2, 0] = drk_dDepth[2];
                A[0, 1] = drk_dInc[0];
                A[1, 1] = drk_dInc[1];
                A[2, 1] = drk_dInc[2];
                A[0, 2] = drk_dAz[0];
                A[1, 2] = drk_dAz[1];
                A[2, 2] = drk_dAz[2];
            }
            return A;
        }

        public static IErrorSource ErrorSourceConverter(object eSource)
        {
            IErrorSource er = null;
            if (eSource is ErrorSourceABXY_TI1S)
            { }
            string s = eSource.ToString();
            if(s.Contains("XYM1"))
			{
                er = JsonSerializer.Deserialize<ErrorSourceXYM1>(s);
            }
            if (s.Contains("XYM2"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceXYM2>(s);
            }
            if (s.Contains("XYM3"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceXYM3>(s);
            }
            if (s.Contains("XYM4"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceXYM4>(s);
            }
            if (s.Contains("SAG"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceSAG>(s);
            }
            if (s.Contains("DRFR"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDRFR>(s);
            }
            if (s.Contains("DRFS"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDRFS>(s);
            }
            if (s.Contains("DSFS"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDSFS>(s);
            }
            if (s.Contains("DSTG"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDSTG>(s);
            }
            if (s.Contains("XYM3E"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceXYM3E>(s);
            }
            if (s.Contains("XYM4E"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceXYM4E>(s);
            }
            if (s.Contains("SAGE"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceSAGE>(s);
            }
            if (s.Contains("XCLH"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceXCLH>(s);
            }
            if (s.Contains("XCLA"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceXCLA>(s);
            }
            if (s.Contains("ABXY_TI1S"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceABXY_TI1S>(s);
            }
            if (s.Contains("ABXY_TI2S"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceABXY_TI2S>(s);
            }
            if (s.Contains("ABZ"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceABZ>(s);
            }
            if (s.Contains("ASXY_TI1S"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceASXY_TI1S>(s);
            }
            if (s.Contains("ASXY_TI2S"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceASXY_TI2S>(s);
            }
            
            if (s.Contains("ASXY_TI3S"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceASXY_TI3S>(s);
            }
            if (s.Contains("ASZ"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceASZ>(s);
            }
            if (s.Contains("MBXY_TI1"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceMBXY_TI1>(s);
            }
            if (s.Contains("MBXY_TI2"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceMBXY_TI2>(s);
            }
            if (s.Contains("MBZ"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceMBZ>(s);
            }
            if (s.Contains("MSXY_TI1"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceMSXY_TI1>(s);
            }
            if (s.Contains("MSXY_TI2"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceMSXY_TI2>(s);
            }
            if (s.Contains("MSXY_TI3"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceMSXY_TI3>(s);
            }
            if (s.Contains("MSZ"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceMSZ>(s);
            }
            if (s.Contains("AMIL"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceAMIL>(s);
            }
            if (s.Contains("DEC_U"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDEC_U>(s);
            }
            if (s.Contains("DEC_OS"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDEC_OS>(s);
            }
            if (s.Contains("DEC_OH"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDEC_OH>(s);
            }
            if (s.Contains("DEC_OI"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDEC_OI>(s);
            }
            if (s.Contains("DECR"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDECR>(s);
            }
            if (s.Contains("DBH_U"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDBH_U>(s);
            }
            if (s.Contains("DBH_OS"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDBH_OS>(s);
            }
            if (s.Contains("DBH_OH"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDBH_OH>(s);
            }
            if (s.Contains("DBH_OI"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDBH_OI>(s);
            }
            if (s.Contains("DBHR"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDBHR>(s);
            }
            if (s.Contains("AXYZ_XYB"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceAXYZ_XYB>(s);
            }
            if (s.Contains("AXYZ_ZB"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceAXYZ_ZB>(s);
            }
            if (s.Contains("AXYZ_SF"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceAXYZ_SF>(s);
            }
            if (s.Contains("AXYZ_MIS"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceAXYZ_MIS>(s);
            }
            if (s.Contains("AXY_B"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceAXY_B>(s);
            }
            if (s.Contains("AXY_SF"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceAXY_SF>(s);
            }
            if (s.Contains("AXY_MS"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceAXY_MS>(s);
            }
            if (s.Contains("AXY_GB"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceAXY_GB>(s);
            }
            if (s.Contains("GXYZ_XYB1"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_XYB1>(s);
            }
            if (s.Contains("GXYZ_XYB2"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_XYB2>(s);
            }
            if (s.Contains("GXYZ_XYRN"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_XYRN>(s);
            }
            if (s.Contains("GXYZ_XYG1"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_XYG1>(s);
            }
            if (s.Contains("GXYZ_XYG2"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_XYG2>(s);
            }
            if (s.Contains("GXYZ_XYG3"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_XYG3>(s);
            }
            if (s.Contains("GXYZ_XYG4"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_XYG4>(s);
            }
            if (s.Contains("GXYZ_ZB"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_ZB>(s);
            }
            if (s.Contains("GXYZ_ZRN"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_ZRN>(s);
            }
            if (s.Contains("GXYZ_ZG1"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_ZG1>(s);
            }
            if (s.Contains("GXYZ_ZG2"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_ZG2>(s);
            }
            if (s.Contains("GXYZ_SF"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_SF>(s);
            }
            if (s.Contains("GXYZ_MIS"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_MIS>(s);
            }
            if (s.Contains("GXY_B1"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_B1>(s);
            }
            if (s.Contains("GXY_B2"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_B2>(s);
            }
            if (s.Contains("GXY_RN"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_RN>(s);
            }
            if (s.Contains("GXY_G1"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_G1>(s);
            }
            if (s.Contains("GXY_G2"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_G2>(s);
            }
            if (s.Contains("GXY_G3"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_G3>(s);
            }
            if (s.Contains("GXY_G4"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_G4>(s);
            }
            if (s.Contains("GXY_SF"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_SF>(s);
            }
            if (s.Contains("GXY_MIS"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_MIS>(s);
            }
            if (s.Contains("EXT_REF"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceEXT_REF>(s);
            }
            if (s.Contains("EXT_TIE"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceEXT_TIE>(s);
            }
            if (s.Contains("EXT_MIS"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceEXT_MIS>(s);
            }
            if (s.Contains("GXYZ_GD"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_GD>(s);
            }
            if (s.Contains("GXYZ_RW"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXYZ_RW>(s);
            }
            if (s.Contains("GXY_GD"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_GD>(s);
            }
            if (s.Contains("GXY_RW"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGXY_RW>(s);
            }
            if (s.Contains("GZ_GD"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGZ_GD>(s);
            }
            if (s.Contains("GZ_RW"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceGZ_RW>(s);
            }
            if (s.Contains("DBHR"))
            {
                er = JsonSerializer.Deserialize<ErrorSourceDBHR>(s);
            }


            //ErrorSourceASXY_TI1S err = JsonSerializer.Deserialize<ErrorSourceASXY_TI1S>(s);
            //IErrorSource errorSource = err;
            return er;
        }
        private List<IErrorSource> ConvertToIErrorSource(List<object> eSources)
		{
            //         List<IErrorSource> errorSources = new List<IErrorSource>();
            //         for(int i=0;i<errorSources.Count;i++)
            //{
            //             double m = eSources.ConvertAll(Converter<object IErrorSources> converter)
            //}

            List<IErrorSource> lp = eSources.ConvertAll(
            new Converter<object, IErrorSource>(ErrorSourceConverter));

            return lp;


        }
        /// <summary>
        /// Calculate Total Covariance matrix for all error sources for a survey station
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="nextStation"></param>
        /// <returns></returns>
        public List<ISCWSAErrorData> CalculateAllCovariance(SurveyStation surveyStation, SurveyStation surveyStationPrev, SurveyStation surveyStationNext, double[,] drdp, double[,] drdpNext, List<IErrorSource> errorSources, List<ISCWSAErrorData> iSCWSAErrorData, int c)
        {
            //ref https://www.iscwsa.net/error-model-documentation/
            double[,] CovarianceSum = new double[3, 3];
            double[,] sigmaerandom = new double[3, 3];

            for (int i = 0; i < sigmaerandom.GetLength(0); i++)
            {
                for (int j = 0; j < sigmaerandom.GetLength(1); j++)
                {
                    sigmaerandom[i, j] = 0.0;
                }
            }
            bool allSystematic = false;
            for (int e = 0; e < ISCWSAErrorDataTmp.Count; e++)
            {
                if (ISCWSAErrorDataTmp[e].IsInitialized)
                {
                    allSystematic = true;
                }
            }
            //List<IErrorSource> errorSources = null;
            //if (eSources[0] is IErrorSource)
            //{
            //    errorSources = new List<IErrorSource>();
            //    for (int i = 0; i < eSources.Count; i++)
            //    {                    
            //        errorSources.Add((IErrorSource)eSources[i]);
            //    }
            //}
            //else
            //{
            //    errorSources = eSources.ConvertAll(new Converter<object, IErrorSource>(ErrorSourceConverter));
            //}
            //List<IErrorSource> errorSources = ConvertToIErrorSource(eSources);
            for (int i = 0; i < errorSources.Count; i++)
            {
                IErrorSource eSource = errorSources[i];
                #region Set GeoMagnetic data
                double latitude = Numeric.UNDEF_DOUBLE;
                double longitude = Numeric.UNDEF_DOUBLE;
                double radius = Numeric.UNDEF_DOUBLE;
                GeodeticDatum geodeticDatum = new GeodeticDatum();
                geodeticDatum.Spheroid = Spheroid.WGS84;
                geodeticDatum.DeltaX = 0.0;
                geodeticDatum.DeltaY = 0.0;
                geodeticDatum.DeltaZ = 0.0;
                geodeticDatum.RotationX = 0.0;
                geodeticDatum.RotationY = 0.0;
                geodeticDatum.RotationZ = 0.0;
                geodeticDatum.ScaleFactor = 1;

                geodeticDatum.ToGeocentric((double)surveyStation.LatitudeWGS84, (double)surveyStation.LongitudeWGS84, (double)surveyStation.TvdWGS84,
                    out latitude, out longitude, out radius);
                DateTime date = DateTime.Now;
                double declination = Numeric.UNDEF_DOUBLE;
                double dip = Numeric.UNDEF_DOUBLE;
                double hStrength = Numeric.UNDEF_DOUBLE;
                double tStrength = Numeric.UNDEF_DOUBLE;
                GeoMagneticFieldModel igrf = GeoMagneticFieldModel.IGRF;
                igrf.GeoMagnetism(date, latitude, longitude, radius,
                                 out declination, out dip,
                                 out hStrength, out tStrength);
                if (Numeric.IsDefined(dip))
                {
                    if(errorSources[i] is NORCE.Drilling.SurveyInstrument.Model.ErrorSourceXYM2)
					{

					}

                    eSource.Dip = dip;
                }
                if (Numeric.IsDefined(declination))
                {
                    eSource.Declination = declination;
                }
                if (Numeric.IsDefined(tStrength))
                {
                    eSource.BField = tStrength;
                }
                #endregion
                bool isInitialized = ISCWSAErrorDataTmp[i].IsInitialized;
                sigmaerandom = ISCWSAErrorDataTmp[i].SigmaErrorRandom;
                bool singular = false;

                double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
                double? depth = 0.0;
                depth = eSource.FunctionDepth((double)(double)surveyStation.MdWGS84, (double)surveyStation.TvdWGS84); //Depth
                if (false && eSource is ErrorSourceDSFS)
                {
                    ErrorSourceDSFS ds = new ErrorSourceDSFS();
                    depth = ds.FunctionDepthGyro((double)(double)surveyStation.MdWGS84, (double)surveyStation.TvdWGS84, (double)surveyStationPrev.MdWGS84, (double)surveyStation.Incl); //Depth
                }
                if (false && eSource is ErrorSourceDSTG)
                {
                    ErrorSourceDSTG ds = new ErrorSourceDSTG();
                    depth = ds.FunctionDepthGyro((double)(double)surveyStation.MdWGS84, (double)surveyStation.TvdWGS84, (double)surveyStationPrev.MdWGS84, (double)surveyStation.Incl); //Depth
                }
                dpde[0] = (double)depth;
                double? inclination = eSource.FunctionInc((double)surveyStation.Incl, (double)surveyStation.AzWGS84); //Inclination
                dpde[1] = (double)inclination;

                double? azimuth = eSource.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84); //Azimuth
                double initializationDepth = ISCWSAErrorDataTmp[i].InitializationDepth;
                double minDistance = 9999;
                if (surveyStation.SurveyTool.GyroMinDist != null)
                {
                    minDistance = (double)surveyStation.SurveyTool.GyroMinDist;// 99999.0; //Minimum distance between initializations. 
                }

                bool reInitialize = ReInitialize((double)surveyStation.Incl, (double)surveyStationPrev.Incl, errorSources, (double)surveyStation.MdWGS84 - ISCWSAErrorDataTmp[i].InitializationDepth, minDistance);
                if (eSource.IsContinuous)
                {
                    double deltaD = (double)(double)surveyStation.MdWGS84 - (double)surveyStationPrev.MdWGS84;
                    double c_gyro = 0.6;
                    if (surveyStation.SurveyTool.GyroRunningSpeed != null)
                    {
                        c_gyro = (double)surveyStation.SurveyTool.GyroRunningSpeed; //Running speed. 
                    }
                    double h = ISCWSAErrorDataTmp[i].GyroH;
                    
                    if ((!isInitialized && surveyStation.Incl >= eSource.StartInclination && surveyStation.Incl <= eSource.EndInclination) || (isInitialized && ((double)surveyStation.MdWGS84 - ISCWSAErrorDataTmp[i].InitializationDepth) > minDistance)) //NB! include initialization inclination code
                    {
                        isInitialized = true;
                        ISCWSAErrorDataTmp[i].GyroH = 0.0;
                        initializationDepth = (double)surveyStation.MdWGS84;
                        h = ISCWSAErrorDataTmp[i].GyroH;
                    }
                    //if ((isInitialized && (surveyStation.Incl < eSource.StartInclination || surveyStation.Incl > eSource.EndInclination))) //NB! include initialization inclination code
                    else if (isInitialized && (surveyStation.Incl < eSource.InitInclination)) //NB! include initialization inclination code
                    {
                        ////isInitialized = false;            //New
                        //ISCWSAErrorDataTmp[i].GyroH = 0.0;
                        //initializationDepth = surveyStation.MD;
                        //h = ISCWSAErrorDataTmp[i].GyroH;
                    }
                    else
                    {


                        h = ISCWSAErrorDataTmp[i].GyroH;
                        if (errorSources[i] is ErrorSourceGXYZ_GD)
                        {
                            ErrorSourceGXYZ_GD da = new ErrorSourceGXYZ_GD();
                            da.StartInclination = eSource.StartInclination;
                            da.EndInclination = eSource.EndInclination;
                            h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84, h, c_gyro, deltaD);
                        }
                        if (errorSources[i] is ErrorSourceGXYZ_RW)
                        {
                            ErrorSourceGXYZ_RW da = new ErrorSourceGXYZ_RW();
                            da.StartInclination = eSource.StartInclination;
                            da.EndInclination = eSource.EndInclination;
                            h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84, h, c_gyro, deltaD);
                        }
                        if (errorSources[i] is ErrorSourceGXY_GD)
                        {
                            ErrorSourceGXY_GD da = new ErrorSourceGXY_GD();
                            da.StartInclination = eSource.StartInclination;
                            da.EndInclination = eSource.EndInclination;
                            h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84, h, c_gyro, deltaD, (double)surveyStationPrev.Incl);
                            if (h == 0 && ISCWSAErrorDataTmp[i].GyroH != 0)
                            {
                                h = ISCWSAErrorDataTmp[i].GyroH;
                            }
                        }
                        if (eSource is ErrorSourceGXY_RW)
                        {
                            ErrorSourceGXY_RW da = new ErrorSourceGXY_RW();
                            da.StartInclination = eSource.StartInclination;
                            da.EndInclination = eSource.EndInclination;
                            h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84, h, c_gyro, deltaD, (double)surveyStationPrev.Incl);
                            if (h == 0 && ISCWSAErrorDataTmp[i].GyroH != 0)
                            {
                                h = ISCWSAErrorDataTmp[i].GyroH;
                            }
                        }
                        if (eSource is ErrorSourceGZ_GD)
                        {
                            ErrorSourceGZ_GD da = new ErrorSourceGZ_GD();
                            da.StartInclination = eSource.StartInclination;
                            da.EndInclination = eSource.EndInclination;
                            h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84, h, c_gyro, deltaD, (double)surveyStationPrev.Incl);
                            if (h == 0 && ISCWSAErrorDataTmp[i].GyroH != 0)
                            {
                                h = ISCWSAErrorDataTmp[i].GyroH;
                            }
                        }
                        if (eSource is ErrorSourceGZ_RW)
                        {
                            ErrorSourceGZ_RW da = new ErrorSourceGZ_RW();
                            da.StartInclination = eSource.StartInclination;
                            da.EndInclination = eSource.EndInclination;
                            h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84, h, c_gyro, deltaD, (double)surveyStationPrev.Incl);
                            if (h == 0 && ISCWSAErrorDataTmp[i].GyroH != 0)
                            {
                                h = ISCWSAErrorDataTmp[i].GyroH;
                            }
                        }
                    }
                    //ISCWSAErrorDataTmp[i].GyroH = h;
                    azimuth = h;
                    ISCWSAErrorDataTmp[i].IsInitialized = isInitialized;
                }
                continuousMode_ = IsContinuousMode(errorSources, (double)surveyStation.Incl);
                if (IsStationary(eSource) && (isInitialized && surveyStation.Incl < eSource.InitInclination)) //NB! include initialization inclination code
                {
                    //isInitialized = false;
                }
                if (IsStationary(eSource) && isInitialized && ((double)surveyStation.MdWGS84 - ISCWSAErrorDataTmp[i].InitializationDepth) > minDistance)
                {
                    azimuth = eSource.FunctionAz((double)eSource.InitInclination, (double)surveyStation.AzWGS84); //Azimuth NB! Unsure
                    ISCWSAErrorDataTmp[i].GyroH = (double)azimuth;
                    initializationDepth = (double)(double)surveyStation.MdWGS84;
                }
                if (IsStationary(eSource) && (isInitialized|| (!isInitialized && surveyStation.Incl > eSource.EndInclination) || continuousMode_))
                {
                    if (false && reInitialize) //New
                    {
                        azimuth = eSource.FunctionAz(eSource.InitInclination, (double)surveyStation.AzWGS84); //Azimuth
                        //azimuth = eSource.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84); //Azimuth
                        initializationDepth = (double)(double)surveyStation.MdWGS84;
                    }
                    //if (false && eSource.InitInclination < 0 && !isInitialized) //New
                    //{
                    //    //double tmp = eSource.EndInclination;
                    //    //eSource.EndInclination = 1.0;
                    //    //azimuth = eSource.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84); //Azimuth
                    //    //eSource.EndInclination = tmp;
                    //}
                    else
                    {
                        azimuth = ISCWSAErrorDataTmp[i].GyroH;
                    }
                    if (!isInitialized && (surveyStation.Incl< eSource.InitInclination || surveyStation.Incl > eSource.EndInclination || eSource.InitInclination < 0))
                    {
                        //azimuth = eSource.FunctionAz((double)surveyStation.Incl, (double)surveyStation.AzWGS84); //Azimuth NB! Unsure
                        //azimuth = ISCWSAErrorDataTmp[i].GyroH + (azimuth- ISCWSAErrorDataTmp[i].GyroH)/2;
                        if (eSource.InitInclination > 0)
                        {
							azimuth = eSource.FunctionAz((double)eSource.InitInclination, (double)surveyStation.AzWGS84); //Azimuth NB! Unsure
						}
                        if (eSource is ErrorSourceGXY_RN || eSource is ErrorSourceGXYZ_XYRN)
                        {
                            double noiseredFactor = (double)surveyStation.SurveyTool.GyroNoiseRed;//1;// 0.5; //NB! Configurable
                            azimuth = noiseredFactor * azimuth;// noiseredFactor * ISCWSAErrorDataTmp[i].GyroH;
                        }
                        initializationDepth = (double)surveyStation.MdWGS84;
                    }
                    isInitialized = true;
                    ISCWSAErrorDataTmp[i].IsInitialized = true;
                }
                ISCWSAErrorDataTmp[i].IsInitialized = isInitialized;//New


                if (azimuth != null)
                {
                    ISCWSAErrorDataTmp[i].GyroH = (double)azimuth;
                    ISCWSAErrorDataTmp[i].InitializationDepth = initializationDepth;
                    dpde[2] = (double)azimuth;
                }
                double magnitude = eSource.Magnitude;
                if (eSource.SingularIssues && (depth == null || inclination == null || azimuth == null))
                {
                    singular = true;
                }
                double[] e = new double[3]; //the error due to the ith error source at the kth survey station in the lth survey leg
                double[] eStar = new double[3]; //the error due to the ith error source at the kth survey stations in the lth survey leg, where k is the last survey of interest
                if (c == 0)
                {
                    e[0] = 0;
                    e[1] = 0;
                    e[2] = 0;
                    eStar[0] = 0;
                    eStar[1] = 0;
                    eStar[2] = 0;
                }
                else
                {
                    if (errorSources[i] is ErrorSourceXCLA)
                    {
                        double azT = (double)surveyStation.AzWGS84 + Convergence;
                        double azTPrev = (double)surveyStationPrev.AzWGS84 + Convergence;
                        double mod = (azT - azTPrev + Math.PI) % (2 * Math.PI);
                        double val1 = mod - Math.PI;
                        double val2 = 0;
                        if (surveyStationPrev.Incl >= 0.0001 * Math.PI / 180.0)
                        {
                            val2 = val1;
                        }
                        double val3 = Math.Abs(Math.Sin((double)surveyStation.Incl) * val2);
                        double defaultTortuosity = 0.000572615; //[rad/m]
                        double val4 = Math.Max(val3, defaultTortuosity * ((double)(double)surveyStation.MdWGS84 - (double)surveyStationPrev.MdWGS84));
                        double val5 = magnitude * ((double)(double)surveyStation.MdWGS84 - (double)surveyStationPrev.MdWGS84) * val4;
                        e[0] = val5 * (-Math.Sin(azT));
                        e[1] = val5 * (Math.Cos(azT));
                        e[2] = 0.0;
                        eStar[0] = e[0];
                        eStar[1] = e[1];
                        eStar[2] = e[2];
                    }
                    else if (errorSources[i] is ErrorSourceXCLH)
                    {
                        double azT = (double)surveyStation.AzWGS84 + Convergence;
                        double azTPrev = (double)surveyStationPrev.AzWGS84 + Convergence;
                        //=Model!$W$37*(Wellpath!$K4-Wellpath!$K3)*MAX(ABS(Wellpath!$L4-Wellpath!$L3);Model!$B$24*(Wellpath!$K4-Wellpath!$K3))*COS(Wellpath!$L4)*COS(Wellpath!$M4)
                        double mod = (azT - azTPrev + Math.PI) % (2 * Math.PI);
                        double val1 = mod - Math.PI;
                        double val2 = 0;
                        if (surveyStationPrev.Incl >= 0.0001 * Math.PI / 180.0)
                        {
                            val2 = val1;
                        }
                        double val3 = Math.Abs((double)surveyStation.Incl - (double)surveyStationPrev.Incl);
                        double defaultTortuosity = 0.000572615; //[rad/m]
                        double val4 = Math.Max(val3, defaultTortuosity * ((double)(double)surveyStation.MdWGS84 - (double)surveyStationPrev.MdWGS84));
                        double val5 = magnitude * ((double)(double)surveyStation.MdWGS84 - (double)surveyStationPrev.MdWGS84) * val4;
                        e[0] = val5 * Math.Cos((double)surveyStation.Incl) * Math.Cos(azT);
                        e[1] = val5 * Math.Cos((double)surveyStation.Incl) * Math.Sin(azT);
                        e[2] = val5 * (-Math.Sin((double)surveyStation.Incl));
                        eStar[0] = e[0];
                        eStar[1] = e[1];
                        eStar[2] = e[2];
                    }
                    else if (eSource.SingularIssues && singular)
                    {
                        if (c == 1)
                        {
                            e[0] = magnitude * ((double)surveyStationNext.MdWGS84 + (double)(double)surveyStation.MdWGS84 - 2 * (double)surveyStationPrev.MdWGS84) / 2 * eSource.FunctionSingularityNorth((double)surveyStation.AzWGS84);
                            e[1] = magnitude * ((double)surveyStationNext.MdWGS84 + (double)(double)surveyStation.MdWGS84 - 2 * (double)surveyStationPrev.MdWGS84) / 2 * eSource.FunctionSingularityEast((double)surveyStation.AzWGS84);
                            e[2] = 0.0;
                            eStar[0] = magnitude * ((double)(double)surveyStation.MdWGS84 - (double)surveyStationPrev.MdWGS84) * eSource.FunctionSingularityNorth((double)surveyStation.AzWGS84);
                            eStar[1] = magnitude * ((double)(double)surveyStation.MdWGS84 - (double)surveyStationPrev.MdWGS84) * eSource.FunctionSingularityEast((double)surveyStation.AzWGS84);
                            eStar[2] = 0.0;
                        }
                        else
                        {
                            e[0] = magnitude * ((double)surveyStationNext.MdWGS84 - (double)surveyStationPrev.MdWGS84) / 2 * eSource.FunctionSingularityNorth((double)surveyStation.AzWGS84);
                            e[1] = magnitude * ((double)surveyStationNext.MdWGS84 - (double)surveyStationPrev.MdWGS84) / 2 * eSource.FunctionSingularityEast((double)surveyStation.AzWGS84);
                            e[2] = 0.0;
                            eStar[0] = magnitude * ((double)(double)surveyStation.MdWGS84 - (double)surveyStationPrev.MdWGS84) / 2 * eSource.FunctionSingularityNorth((double)surveyStation.AzWGS84);
                            eStar[1] = magnitude * ((double)(double)surveyStation.MdWGS84 - (double)surveyStationPrev.MdWGS84) / 2 * eSource.FunctionSingularityEast((double)surveyStation.AzWGS84);
                            eStar[2] = 0.0;
                        }
                    }
                    else
                    {
                        e[0] = magnitude * ((drdp[0, 0] + drdpNext[0, 0]) * dpde[0] + (drdp[0, 1] + drdpNext[0, 1]) * dpde[1] + (drdp[0, 2] + drdpNext[0, 2]) * dpde[2]);
                        e[1] = magnitude * ((drdp[1, 0] + drdpNext[1, 0]) * dpde[0] + (drdp[1, 1] + drdpNext[1, 1]) * dpde[1] + (drdp[1, 2] + drdpNext[1, 2]) * dpde[2]);
                        e[2] = magnitude * ((drdp[2, 0] + drdpNext[2, 0]) * dpde[0] + (drdp[2, 1] + drdpNext[2, 1]) * dpde[1] + (drdp[2, 2] + drdpNext[2, 2]) * dpde[2]);
                        eStar[0] = magnitude * ((drdp[0, 0]) * dpde[0] + (drdp[0, 1]) * dpde[1] + (drdp[0, 2]) * dpde[2]);
                        eStar[1] = magnitude * ((drdp[1, 0]) * dpde[0] + (drdp[1, 1]) * dpde[1] + (drdp[1, 2]) * dpde[2]);
                        eStar[2] = magnitude * ((drdp[2, 0]) * dpde[0] + (drdp[2, 1]) * dpde[1] + (drdp[2, 2]) * dpde[2]);
                    }
                }

                double[,] CovarianceI = new double[3, 3];
                if (eSource.IsRandom && !allSystematic)
                {
                    if (c == 0)
                    {
                        CovarianceI[0, 0] = eStar[0] * eStar[0];
                        CovarianceI[1, 1] = eStar[1] * eStar[1];
                        CovarianceI[2, 2] = eStar[2] * eStar[2];
                        CovarianceI[1, 0] = eStar[0] * eStar[1];
                        CovarianceI[0, 1] = CovarianceI[1, 0];
                        CovarianceI[2, 0] = eStar[0] * eStar[2];
                        CovarianceI[0, 2] = CovarianceI[2, 0];
                        CovarianceI[1, 2] = eStar[1] * eStar[2];
                        CovarianceI[2, 1] = CovarianceI[2, 1];
                    }
                    else
                    {
                        CovarianceI[0, 0] = sigmaerandom[0, 0] + eStar[0] * eStar[0];
                        CovarianceI[1, 1] = sigmaerandom[1, 1] + eStar[1] * eStar[1];
                        CovarianceI[2, 2] = sigmaerandom[2, 2] + eStar[2] * eStar[2];
                        CovarianceI[1, 0] = eStar[0] * eStar[1] + sigmaerandom[1, 0];
                        CovarianceI[0, 1] = CovarianceI[1, 0];
                        CovarianceI[2, 0] = eStar[0] * eStar[2] + sigmaerandom[2, 0];
                        CovarianceI[0, 2] = CovarianceI[2, 0];
                        CovarianceI[1, 2] = eStar[1] * eStar[2] + sigmaerandom[1, 2];
                        CovarianceI[2, 1] = CovarianceI[1, 2];
                    }
                    sigmaerandom[0, 0] = e[0] * e[0] + sigmaerandom[0, 0];
                    sigmaerandom[1, 1] = e[1] * e[1] + sigmaerandom[1, 1];
                    sigmaerandom[2, 2] = e[2] * e[2] + sigmaerandom[2, 2];
                    sigmaerandom[1, 0] = e[0] * e[1] + sigmaerandom[1, 0];
                    sigmaerandom[0, 1] = sigmaerandom[1, 0];
                    sigmaerandom[2, 0] = e[0] * e[2] + sigmaerandom[2, 0];
                    sigmaerandom[0, 2] = sigmaerandom[2, 0];
                    sigmaerandom[1, 2] = e[1] * e[2] + sigmaerandom[1, 2];
                    sigmaerandom[2, 1] = sigmaerandom[1, 2];

                    ISCWSAErrorDataTmp[i].SigmaErrorRandom = sigmaerandom;
                }
                else
                {
                    double[] sigmaesystematic = new double[3];
                    double eNSum = ISCWSAErrorDataTmp[i].ErrorSum[0];
                    double eESum = ISCWSAErrorDataTmp[i].ErrorSum[1];
                    double eVSum = ISCWSAErrorDataTmp[i].ErrorSum[2];
                    sigmaesystematic[0] = eNSum + eStar[0];
                    sigmaesystematic[1] = eESum + eStar[1];
                    sigmaesystematic[2] = eVSum + eStar[2];
                    if (c == 0)
                    {
                        CovarianceI[0, 0] = eStar[0] * eStar[0];
                        CovarianceI[1, 1] = eStar[1] * eStar[1];
                        CovarianceI[2, 2] = eStar[2] * eStar[2];
                        CovarianceI[1, 0] = eStar[0] * eStar[1];
                        CovarianceI[0, 1] = CovarianceI[1, 0];
                        CovarianceI[2, 0] = eStar[0] * eStar[2];
                        CovarianceI[0, 2] = CovarianceI[2, 0];
                        CovarianceI[1, 2] = eStar[1] * eStar[2];
                        CovarianceI[2, 1] = CovarianceI[2, 1];
                    }
                    else
                    {
                        CovarianceI[0, 0] = sigmaesystematic[0] * sigmaesystematic[0];
                        CovarianceI[1, 1] = sigmaesystematic[1] * sigmaesystematic[1];
                        CovarianceI[2, 2] = sigmaesystematic[2] * sigmaesystematic[2];
                        CovarianceI[1, 0] = sigmaesystematic[1] * sigmaesystematic[0];
                        CovarianceI[0, 1] = CovarianceI[1, 0];
                        CovarianceI[2, 0] = sigmaesystematic[2] * sigmaesystematic[0];
                        CovarianceI[0, 2] = CovarianceI[2, 0];
                        CovarianceI[1, 2] = sigmaesystematic[1] * sigmaesystematic[2];
                        CovarianceI[2, 1] = CovarianceI[1, 2];
                    }
                    ISCWSAErrorDataTmp[i].ErrorSum[0] = ISCWSAErrorDataTmp[i].ErrorSum[0] + e[0];
                    ISCWSAErrorDataTmp[i].ErrorSum[1] = ISCWSAErrorDataTmp[i].ErrorSum[1] + e[1];
                    ISCWSAErrorDataTmp[i].ErrorSum[2] = ISCWSAErrorDataTmp[i].ErrorSum[2] + e[2];
                }
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        //surveyStation.Uncertainty.Covariance[j, k] += CovarianceI[j, k];
                        Covariance[j, k] += CovarianceI[j, k];
                        CovarianceSum[j, k] += CovarianceI[j, k];
                    }
                }

                ISCWSAErrorDataTmp[i].Covariance = CovarianceI;
            }
            return ISCWSAErrorDataTmp;
        }
                
        
        private bool IsStationary(IErrorSource errorSource)
		{
            if (errorSource is ErrorSourceGXY_B1 || errorSource is ErrorSourceGXY_B2 || errorSource is ErrorSourceGXY_RN || errorSource is ErrorSourceGXY_G1 || errorSource is ErrorSourceGXY_G2 || errorSource is ErrorSourceGXY_G3 || errorSource is ErrorSourceGXY_G4 || errorSource is ErrorSourceGXY_SF || errorSource is ErrorSourceGXY_MIS ||
                errorSource is ErrorSourceGXYZ_XYB1 || errorSource is ErrorSourceGXYZ_XYB2 || errorSource is ErrorSourceGXYZ_XYRN || errorSource is ErrorSourceGXYZ_XYG1 || errorSource is ErrorSourceGXYZ_XYG2 || errorSource is ErrorSourceGXYZ_XYG3 || errorSource is ErrorSourceGXYZ_XYG4 ||
                errorSource is ErrorSourceGXYZ_ZB || errorSource is ErrorSourceGXYZ_ZRN || errorSource is ErrorSourceGXYZ_ZG1 || errorSource is ErrorSourceGXYZ_ZG2 ||
                errorSource is ErrorSourceGXYZ_SF || errorSource is ErrorSourceGXYZ_MIS)
            {
                return true;
            }
			else { return false; }
		}

        private bool IsContinuousMode(List<IErrorSource> errorSources, double incl)
		{
            bool isContinuous = false;
            for(int i=0;i<ISCWSAErrorDataTmp.Count;i++)
			{
                IErrorSource eSource = (IErrorSource)errorSources[i];
                if (ISCWSAErrorDataTmp[i].IsInitialized && eSource.IsContinuous)
				{
					isContinuous = true;
				}
                if (eSource.IsContinuous && incl >= eSource.StartInclination) //New
                {
					//isContinuous = true;
				}
            }
            return isContinuous;
		}
        private bool ReInitialize(double incl, double inclPrev, List<IErrorSource> errorSources, double dist, double minDist)
        {
            bool reInitialize = false;
            for (int i = 0; i < errorSources.Count; i++)
            {
                IErrorSource eSource = (IErrorSource)errorSources[i];
                if ((IsStationary(eSource) && incl <= eSource.InitInclination && inclPrev > eSource.InitInclination) || dist > minDist)
                {
                    reInitialize = true;
                }
            }
            return reInitialize;
        }       
    }
    /// <summary>
    /// ISCWSAErrorData
    /// </summary>
    public class ISCWSAErrorData
    {
        public double[,] Covariance { get; set; }
        public double[] ErrorSum { get; set; }
        public double[,] SigmaErrorRandom { get; set; }
        public double GyroH { get; set; }
        public bool IsInitialized = false;
        public double InitializationDepth = 0.0;
    }
}
