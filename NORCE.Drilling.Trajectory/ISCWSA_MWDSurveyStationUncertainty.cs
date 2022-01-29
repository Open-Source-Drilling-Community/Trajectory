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
            double[,] A = new double[3,3];
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
                if(st==1)
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
        /// Error due to the DRDF error source at the kth survey station in the lth survey leg
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="nextStation"></param>
        /// <returns></returns>
        public double[,] CalculateCovarianceDRDF(double[,] drdp, double[,] drdpNext, SurveyInstrument.Model.SurveyInstrument surveyInstrument, double[,] sigmaerandom, int st)
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
           
            
            double[,] CovarianceDRDF = new double[3, 3];
            if (st == 0)
            {
                CovarianceDRDF[0, 0] = eStar[0] * eStar[0];
                CovarianceDRDF[1, 1] = eStar[1] * eStar[1];
                CovarianceDRDF[2, 2] = eStar[2] * eStar[2];
                CovarianceDRDF[1, 0] = eStar[0] * eStar[1];
                CovarianceDRDF[0, 1] = CovarianceDRDF[1, 0];
                CovarianceDRDF[2, 0] = eStar[0] * eStar[2];
                CovarianceDRDF[0, 2] = CovarianceDRDF[2, 0];
                CovarianceDRDF[1, 2] = eStar[1] * eStar[2];
                CovarianceDRDF[2, 1] = CovarianceDRDF[2, 1];
            }
			else 
            {
                CovarianceDRDF[0, 0] = sigmaerandom[0, 0] + eStar[0] * eStar[0];
                CovarianceDRDF[1, 1] = sigmaerandom[1, 1] + eStar[1] * eStar[1];
                CovarianceDRDF[2, 2] = sigmaerandom[2, 2] + eStar[2] * eStar[2];
                CovarianceDRDF[1, 0] = eStar[0] * eStar[1] + sigmaerandom[1, 0];
                CovarianceDRDF[0, 1] = CovarianceDRDF[1, 0];
                CovarianceDRDF[2, 0] = eStar[0] * eStar[2] + sigmaerandom[2, 0];
                CovarianceDRDF[0, 2] = CovarianceDRDF[2, 0];
                CovarianceDRDF[1, 2] = eStar[1] * eStar[2] + sigmaerandom[1, 2];
                CovarianceDRDF[2, 1] = CovarianceDRDF[1, 2];
            }
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Covariance[j, k] = CovarianceDRDF[j, k];
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
    }
}
