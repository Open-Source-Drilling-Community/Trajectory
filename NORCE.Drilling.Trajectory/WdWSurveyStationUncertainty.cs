using System;
using System.Collections.Generic;
using System.Text;
using NORCE.General.Std;
using NORCE.General.Math;
using NORCE.Drilling.SurveyInstrument.Model;

namespace NORCE.Drilling.Trajectory
{
    public class WdWSurveyStationUncertainty : SurveyStationUncertainty
    {       
        private double[,] P_ = new double[3, 3];

        /// <summary>
        /// Default constructor
        /// </summary>
        public WdWSurveyStationUncertainty()
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
        public WdWSurveyStationUncertainty(WdWSurveyStationUncertainty source)
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
        public WdWSurveyStationUncertainty(SurveyInstrument.Model.SurveyInstrument surveyTool)
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
        /// 
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="previousStation"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        public double[,] CalculateCovariances(SurveyStation surveyStation, SurveyStation previousStation, double[,] A)
        {
            if (surveyStation != null && surveyStation.SurveyTool != null && previousStation != null && A != null)
            {
                double sinI = System.Math.Sin((double)surveyStation.Incl);
                double cosI = System.Math.Cos((double)surveyStation.Incl);
                double sinA = System.Math.Sin((double)surveyStation.AzWGS84);
                double cosA = System.Math.Cos((double)surveyStation.AzWGS84);
                double deltaSk = (double)surveyStation.MdWGS84 - (double)previousStation.MdWGS84;
                //double Vk = (double)surveyStation.TvdWGS84;
                //double Nk = (double)surveyStation.NorthOfWellHead ;
                //double Ek = (double)surveyStation.EastOfWellHead;
                double deltaC10 = surveyStation.SurveyTool.ReferenceError!=null ? (double)surveyStation.SurveyTool.ReferenceError : 0;
                double deltaC20 = surveyStation.SurveyTool.DrillStringMag != null ? (double)surveyStation.SurveyTool.DrillStringMag : 0;
                double deltaC30 = surveyStation.SurveyTool.GyroCompassError != null ? (double)surveyStation.SurveyTool.GyroCompassError : 0;
                double deltaIt0 = surveyStation.SurveyTool.TrueInclination != null ? (double)surveyStation.SurveyTool.TrueInclination : 0;
                double deltaIm = surveyStation.SurveyTool.Misalignment != null ? (double)surveyStation.SurveyTool.Misalignment : 0;
                double epsilon = surveyStation.SurveyTool.RelDepthError != null ? (double)surveyStation.SurveyTool.RelDepthError : 0;
                double deltaZ = (double)surveyStation.TvdWGS84 - (double)previousStation.TvdWGS84;
                double deltaX = (double)surveyStation.NorthOfWellHead  - (double)previousStation.NorthOfWellHead ;
                double deltaY = (double)surveyStation.EastOfWellHead - (double)previousStation.EastOfWellHead;

                if ((Numeric.EQ(cosI, 0.0) && Numeric.IsDefined(deltaC30) && !Numeric.EQ(deltaC30, 0.0)) || Numeric.IsUndefined(A[0, 0]))
                {
                    for (int i = 0; i < A.GetLength(0); i++)
                    {
                        for (int j = 0; j < A.GetLength(1); j++)
                        {
                            A[i, j] = Numeric.UNDEF_DOUBLE;
                        }
                    }
                }
                else
                {
                    // calculate Transfer vectors
                    double tmp = deltaC10 * sinI * deltaSk;
                    A[0, 0] = A[0, 0] - tmp * sinA;
                    A[0, 1] = A[0, 1] + tmp * cosA;
                    tmp = deltaC20 * sinI * sinI * sinA * deltaSk;
                    A[1, 0] = A[1, 0] - tmp * sinA;
                    A[1, 1] = A[1, 1] + tmp * cosA;
                    if (!Numeric.EQ(cosI, 0.0))
                    {
                        tmp = deltaC30 * deltaSk * sinI / cosI;
                        A[2, 0] = A[2, 0] - tmp * sinA;
                        A[2, 1] = A[2, 1] + tmp * cosA;
                    }
                    else
                    {
                        A[2, 0] = 0;
                        A[2, 1] = 0;
                    }
                    tmp = deltaIt0 * sinI * deltaSk;
                    A[3, 0] = A[3, 0] + tmp * cosI * cosA;
                    A[3, 1] = A[3, 1] + tmp * cosI * sinA;
                    A[3, 2] = A[3, 2] - tmp * sinI;
                    tmp = deltaIm;
                    A[4, 0] = A[4, 0] + tmp * deltaX;
                    A[4, 1] = A[4, 1] + tmp * deltaY;
                    A[4, 2] = A[4, 2] + tmp * deltaZ;
                    tmp = epsilon;
                    A[5, 0] = A[5, 0] + tmp * deltaX;
                    A[5, 1] = A[5, 1] + tmp * deltaY;
                    A[5, 2] = A[5, 2] + tmp * deltaZ;

                    //calculate covariance matrix
                    tmp = A[4, 0] * A[4, 0] + A[4, 1] * A[4, 1] + A[4, 2] * A[4, 2];
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            Covariance[i, j] = A[0, i] * A[0, j] + A[1, i] * A[1, j] + A[2, i] * A[2, j] + A[3, i] * A[3, j] + A[4, i] * A[4, j] + A[5, i] * A[5, j] + ((i == j) ? tmp : 0.0) - A[4, i] * A[4, j];
                        }
                    }

                    // apply horizontal magnetic deviations
                    Bias = new Vector3D(A[1, 1], A[1, 0], 0.0);
                }
                return A;
            }
            else
            {
                return null;
            }
        }
    }
}
