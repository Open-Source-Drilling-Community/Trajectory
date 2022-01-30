using System;
using System.Collections.Generic;
using System.Text;
using NORCE.General.Std;
using NORCE.General.Math;
using NORCE.Drilling.SurveyInstrument.Model;

namespace NORCE.Drilling.Trajectory
{
    public class ISCWSA_MWDSurveyStationUncertainty : SurveyStationUncertainty
    {
        private double[,] P_ = new double[3, 3];
        /// <summary>
        /// Dip angle [rad]
        /// </summary>
        public double Dip { get; set; } = 72 * Math.PI / 180.0;
        /// <summary>
        /// Declination angle [rad]
        /// </summary>
        public double Declination { get; set; } = -4 * Math.PI / 180.0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ISCWSA_MWDSurveyStationUncertainty()
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
        public ISCWSA_MWDSurveyStationUncertainty(ISCWSA_MWDSurveyStationUncertainty source)
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
        public ISCWSA_MWDSurveyStationUncertainty(SurveyInstrument.Model.SurveyInstrument surveyTool)
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
                double sinA = System.Math.Sin((double)surveyStation.Az);
                double cosA = System.Math.Cos((double)surveyStation.Az);
                double sinIp = System.Math.Sin((double)previousStation.Incl);
                double cosIp = System.Math.Cos((double)previousStation.Incl);
                double sinAp = System.Math.Sin((double)previousStation.Az);
                double cosAp = System.Math.Cos((double)previousStation.Az);
                double deltaSk = surveyStation.MD - previousStation.MD;
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
                double sinA = System.Math.Sin((double)surveyStation.Az);
                double cosA = System.Math.Cos((double)surveyStation.Az);
                double sinIn = System.Math.Sin((double)nextStation.Incl);
                double cosIn = System.Math.Cos((double)nextStation.Incl);
                double sinAn = System.Math.Sin((double)nextStation.Az);
                double cosAn = System.Math.Cos((double)nextStation.Az);
                double deltaSk = nextStation.MD - surveyStation.MD;
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
        /// <summary>
        /// Error due to the DRFR error source at the kth survey station in the lth survey leg
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="nextStation"></param>
        /// <returns></returns>
        public double[,] CalculateCovarianceDRFR(double[,] drdp, double[,] drdpNext, SurveyInstrument.Model.SurveyInstrument surveyInstrument, double[,] sigmaerandom, int st)
        {
            //ref https://www.iscwsa.net/error-model-documentation/
            double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
            dpde[0] = 1; //Depth
            dpde[1] = 0; //Inclination
            dpde[2] = 0; //Azimuth
            double magnitude = (double)surveyInstrument.DRFR;
            double[] e = new double[3]; //the error due to the ith error source at the kth survey station in the lth survey leg
            double[] eStar = new double[3]; //the error due to the ith error source at the kth survey stations in the lth survey leg, where k is the last survey of interest
            if (st == 0)
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
                e[0] = magnitude * ((drdp[0, 0] + drdpNext[0, 0]) * dpde[0] + (drdp[0, 1] + drdpNext[0, 1]) * dpde[1] + (drdp[0, 2] + drdpNext[0, 2]) * dpde[2]);
                e[1] = magnitude * ((drdp[1, 0] + drdpNext[1, 0]) * dpde[0] + (drdp[1, 1] + drdpNext[1, 1]) * dpde[1] + (drdp[1, 2] + drdpNext[1, 2]) * dpde[2]);
                e[2] = magnitude * ((drdp[2, 0] + drdpNext[2, 0]) * dpde[0] + (drdp[2, 1] + drdpNext[2, 1]) * dpde[1] + (drdp[2, 2] + drdpNext[2, 2]) * dpde[2]);
                eStar[0] = magnitude * ((drdp[0, 0]) * dpde[0] + (drdp[0, 1]) * dpde[1] + (drdp[0, 2]) * dpde[2]);
                eStar[1] = magnitude * ((drdp[1, 0]) * dpde[0] + (drdp[1, 1]) * dpde[1] + (drdp[1, 2]) * dpde[2]);
                eStar[2] = magnitude * ((drdp[2, 0]) * dpde[0] + (drdp[2, 1]) * dpde[1] + (drdp[2, 2]) * dpde[2]);
            }


            double[,] CovarianceDRFR = new double[3, 3];
            if (st == 0)
            {
                CovarianceDRFR[0, 0] = eStar[0] * eStar[0];
                CovarianceDRFR[1, 1] = eStar[1] * eStar[1];
                CovarianceDRFR[2, 2] = eStar[2] * eStar[2];
                CovarianceDRFR[1, 0] = eStar[0] * eStar[1];
                CovarianceDRFR[0, 1] = CovarianceDRFR[1, 0];
                CovarianceDRFR[2, 0] = eStar[0] * eStar[2];
                CovarianceDRFR[0, 2] = CovarianceDRFR[2, 0];
                CovarianceDRFR[1, 2] = eStar[1] * eStar[2];
                CovarianceDRFR[2, 1] = CovarianceDRFR[2, 1];
            }
            else
            {
                CovarianceDRFR[0, 0] = sigmaerandom[0, 0] + eStar[0] * eStar[0];
                CovarianceDRFR[1, 1] = sigmaerandom[1, 1] + eStar[1] * eStar[1];
                CovarianceDRFR[2, 2] = sigmaerandom[2, 2] + eStar[2] * eStar[2];
                CovarianceDRFR[1, 0] = eStar[0] * eStar[1] + sigmaerandom[1, 0];
                CovarianceDRFR[0, 1] = CovarianceDRFR[1, 0];
                CovarianceDRFR[2, 0] = eStar[0] * eStar[2] + sigmaerandom[2, 0];
                CovarianceDRFR[0, 2] = CovarianceDRFR[2, 0];
                CovarianceDRFR[1, 2] = eStar[1] * eStar[2] + sigmaerandom[1, 2];
                CovarianceDRFR[2, 1] = CovarianceDRFR[1, 2];
            }
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Covariance[j, k] += CovarianceDRFR[j, k];
                }
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

            return sigmaerandom;
        }
        /// <summary>
        /// Error due to the Systematic error source at the kth survey station in the lth survey leg
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="nextStation"></param>
        /// <returns></returns>
        public double[,] CalculateSystematicCovariance(SurveyList surveyStations, List<double[,]> drdps, List<double[,]> drdpNexts, SurveyInstrument.Model.SurveyInstrument surveyInstrument, IErrorSource errorSource)
        {
            //ref https://www.iscwsa.net/error-model-documentation/
            List<double[]> eAll = new List<double[]>();
            List<double[]> eStarAll = new List<double[]>();
            double eNSum = 0.0;
            double eESum = 0.0;
            double eVSum = 0.0;
            for (int i = 0; i < surveyStations.Count; i++)
            {
                double[] sigmaesystematic = new double[3];
                SurveyStation surveyStation = surveyStations[i];
                double[,] drdp = drdps[i];
                double[,] drdpNext = drdpNexts[i];
                double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
                dpde[0] = errorSource.FunctionDepth(surveyStation.MD, (double)surveyStation.Z); //Depth
                dpde[1] = errorSource.FunctionInc((double)surveyStation.Incl, (double)surveyStation.Az); //Inclination
                dpde[2] = errorSource.FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az); //Azimuth
                double magnitude = (double)surveyInstrument.MSZ;
                double[] e = new double[3]; //the error due to the ith error source at the kth survey station in the lth survey leg
                double[] eStar = new double[3]; //the error due to the ith error source at the kth survey stations in the lth survey leg, where k is the last survey of interest
                if (i == 0)
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
                    e[0] = magnitude * ((drdp[0, 0] + drdpNext[0, 0]) * dpde[0] + (drdp[0, 1] + drdpNext[0, 1]) * dpde[1] + (drdp[0, 2] + drdpNext[0, 2]) * dpde[2]);
                    e[1] = magnitude * ((drdp[1, 0] + drdpNext[1, 0]) * dpde[0] + (drdp[1, 1] + drdpNext[1, 1]) * dpde[1] + (drdp[1, 2] + drdpNext[1, 2]) * dpde[2]);
                    e[2] = magnitude * ((drdp[2, 0] + drdpNext[2, 0]) * dpde[0] + (drdp[2, 1] + drdpNext[2, 1]) * dpde[1] + (drdp[2, 2] + drdpNext[2, 2]) * dpde[2]);
                    eStar[0] = magnitude * ((drdp[0, 0]) * dpde[0] + (drdp[0, 1]) * dpde[1] + (drdp[0, 2]) * dpde[2]);
                    eStar[1] = magnitude * ((drdp[1, 0]) * dpde[0] + (drdp[1, 1]) * dpde[1] + (drdp[1, 2]) * dpde[2]);
                    eStar[2] = magnitude * ((drdp[2, 0]) * dpde[0] + (drdp[2, 1]) * dpde[1] + (drdp[2, 2]) * dpde[2]);
                }

                sigmaesystematic[0] = eNSum + eStar[0];
                sigmaesystematic[1] = eESum + eStar[1];
                sigmaesystematic[2] = eVSum + eStar[2];

                double[,] CovarianceSys = new double[3, 3];
                if (i == 0)
                {
                    CovarianceSys[0, 0] = eStar[0] * eStar[0];
                    CovarianceSys[1, 1] = eStar[1] * eStar[1];
                    CovarianceSys[2, 2] = eStar[2] * eStar[2];
                    CovarianceSys[1, 0] = eStar[0] * eStar[1];
                    CovarianceSys[0, 1] = CovarianceSys[1, 0];
                    CovarianceSys[2, 0] = eStar[0] * eStar[2];
                    CovarianceSys[0, 2] = CovarianceSys[2, 0];
                    CovarianceSys[1, 2] = eStar[1] * eStar[2];
                    CovarianceSys[2, 1] = CovarianceSys[2, 1];
                }
                else
                {
                    CovarianceSys[0, 0] = sigmaesystematic[0] * sigmaesystematic[0];
                    CovarianceSys[1, 1] = sigmaesystematic[1] * sigmaesystematic[1];
                    CovarianceSys[2, 2] = sigmaesystematic[2] * sigmaesystematic[2];
                    CovarianceSys[1, 0] = sigmaesystematic[1] * sigmaesystematic[0];
                    CovarianceSys[0, 1] = CovarianceSys[1, 0];
                    CovarianceSys[2, 0] = sigmaesystematic[2] * sigmaesystematic[0];
                    CovarianceSys[0, 2] = CovarianceSys[2, 0];
                    CovarianceSys[1, 2] = sigmaesystematic[1] * sigmaesystematic[2];
                    CovarianceSys[2, 1] = CovarianceSys[1, 2];
                }
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        Covariance[j, k] += CovarianceSys[j, k];
                    }
                }
                eAll.Add(e);
                eStarAll.Add(eStar);
                eNSum += e[0];
                eESum += e[1];
                eVSum += e[2];
            }
            return null;
        }

        /// <summary>
        /// Error due to the MWD: Z-Magnetometer Scale Factor error source at the kth survey station in the lth survey leg
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="nextStation"></param>
        /// <returns></returns>
        public double[,] CalculateCovarianceMSZ(SurveyList surveyStations, List<double[,]> drdps, List<double[,]> drdpNexts, SurveyInstrument.Model.SurveyInstrument surveyInstrument)
        {
            //ref https://www.iscwsa.net/error-model-documentation/
            List<double[]> eAll = new List<double[]>();
            List<double[]> eStarAll = new List<double[]>();
            double eNSum = 0.0;
            double eESum = 0.0;
            double eVSum = 0.0;
            for (int i = 0; i < surveyStations.Count; i++)
            {
                double[] sigmaesystematic = new double[3];
                SurveyStation surveyStation = surveyStations[i];
                double[,] drdp = drdps[i];
                double[,] drdpNext = drdpNexts[i];
                double sinI = System.Math.Sin((double)surveyStation.Incl);
                double cosI = System.Math.Cos((double)surveyStation.Incl);
                double sinAm = System.Math.Sin((double)surveyStation.Az - Declination);
                double cosAm = System.Math.Cos((double)surveyStation.Az - Declination);
                double tanDip = System.Math.Tan(Dip);
                double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
                dpde[0] = 0; //Depth
                dpde[1] = 0; //Inclination
                dpde[2] = -(sinI * cosAm + tanDip * cosI) * sinI * sinAm; //Azimuth
                double magnitude = (double)surveyInstrument.MSZ;
                double[] e = new double[3]; //the error due to the ith error source at the kth survey station in the lth survey leg
                double[] eStar = new double[3]; //the error due to the ith error source at the kth survey stations in the lth survey leg, where k is the last survey of interest
                if (i == 0)
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
                    e[0] = magnitude * ((drdp[0, 0] + drdpNext[0, 0]) * dpde[0] + (drdp[0, 1] + drdpNext[0, 1]) * dpde[1] + (drdp[0, 2] + drdpNext[0, 2]) * dpde[2]);
                    e[1] = magnitude * ((drdp[1, 0] + drdpNext[1, 0]) * dpde[0] + (drdp[1, 1] + drdpNext[1, 1]) * dpde[1] + (drdp[1, 2] + drdpNext[1, 2]) * dpde[2]);
                    e[2] = magnitude * ((drdp[2, 0] + drdpNext[2, 0]) * dpde[0] + (drdp[2, 1] + drdpNext[2, 1]) * dpde[1] + (drdp[2, 2] + drdpNext[2, 2]) * dpde[2]);
                    eStar[0] = magnitude * ((drdp[0, 0]) * dpde[0] + (drdp[0, 1]) * dpde[1] + (drdp[0, 2]) * dpde[2]);
                    eStar[1] = magnitude * ((drdp[1, 0]) * dpde[0] + (drdp[1, 1]) * dpde[1] + (drdp[1, 2]) * dpde[2]);
                    eStar[2] = magnitude * ((drdp[2, 0]) * dpde[0] + (drdp[2, 1]) * dpde[1] + (drdp[2, 2]) * dpde[2]);
                }

                sigmaesystematic[0] = eNSum + eStar[0];
                sigmaesystematic[1] = eESum + eStar[1];
                sigmaesystematic[2] = eVSum + eStar[2];

                double[,] CovarianceMSZ = new double[3, 3];
                if (i == 0)
                {
                    CovarianceMSZ[0, 0] = eStar[0] * eStar[0];
                    CovarianceMSZ[1, 1] = eStar[1] * eStar[1];
                    CovarianceMSZ[2, 2] = eStar[2] * eStar[2];
                    CovarianceMSZ[1, 0] = eStar[0] * eStar[1];
                    CovarianceMSZ[0, 1] = CovarianceMSZ[1, 0];
                    CovarianceMSZ[2, 0] = eStar[0] * eStar[2];
                    CovarianceMSZ[0, 2] = CovarianceMSZ[2, 0];
                    CovarianceMSZ[1, 2] = eStar[1] * eStar[2];
                    CovarianceMSZ[2, 1] = CovarianceMSZ[2, 1];
                }
                else
                {
                    CovarianceMSZ[0, 0] = sigmaesystematic[0] * sigmaesystematic[0];
                    CovarianceMSZ[1, 1] = sigmaesystematic[1] * sigmaesystematic[1];
                    CovarianceMSZ[2, 2] = sigmaesystematic[2] * sigmaesystematic[2];
                    CovarianceMSZ[1, 0] = sigmaesystematic[1] * sigmaesystematic[0];
                    CovarianceMSZ[0, 1] = CovarianceMSZ[1, 0];
                    CovarianceMSZ[2, 0] = sigmaesystematic[2] * sigmaesystematic[0];
                    CovarianceMSZ[0, 2] = CovarianceMSZ[2, 0];
                    CovarianceMSZ[1, 2] = sigmaesystematic[1] * sigmaesystematic[2];
                    CovarianceMSZ[2, 1] = CovarianceMSZ[1, 2];
                }
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        Covariance[j, k] += CovarianceMSZ[j, k];
                    }
                }
                eAll.Add(e);
                eStarAll.Add(eStar);
                eNSum += e[0];
                eESum += e[1];
                eVSum += e[2];
            }

            return null;
        }
    }

    public interface IErrorSource
	{
        string ErrorCode
        {
            get;
        }
        bool IsSystematic { get; }
        bool IsRandom { get; }
        bool IsGlobal { get; }
        double FunctionDepth(double md, double tvd);
        double FunctionInc(double incl, double az);
        double FunctionAz(double incl, double az);
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Magnetometer Scale Factor error source
    /// </summary>
    public class ErrorSourceMSXY_TI1 : IErrorSource
    {
        public ErrorSourceMSXY_TI1()
        {

        }
        public string ErrorCode
        {
            get { return "MSXY_TI1"; }
        }
        public bool IsSystematic
        {
            get { return true; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gavitude { get; set; }
        public double Btotal { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return sinI * sinAm * (tanDip * cosI + sinI*cosAm) / Math.Sqrt(2); //Azimuth
        }
    }
    /// <summary>
    /// Error due to the MWD: Z-Magnetometer Scale Factor error source
    /// </summary>
    public class ErrorSourceMSZ : IErrorSource
	{
        public ErrorSourceMSZ()
		{

		}
        public string ErrorCode
        {
            get{return "MSZ";}
        }
        public bool IsSystematic
        {
            get {return true;}
        }
        public bool IsRandom
        {
            get{return false;}
        }
        public bool IsGlobal
        {
            get{return false;}
        }
        public double Latitude { get; set; }
        public double Gavitude { get; set; }
        public double Btotal { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double FunctionDepth(double md, double tvd)
		{
            return 0.0;
		}
        public double FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return -(sinI * cosAm + tanDip * cosI) * sinI * sinAm; //Azimuth
        }
    }
}
