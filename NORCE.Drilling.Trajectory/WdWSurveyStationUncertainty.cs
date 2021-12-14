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
        private SurveyInstrument.Model.SurveyInstrument surveyTool_ = null;
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
                surveyTool_ = source.surveyTool_;
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
            surveyTool_ = surveyTool;
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
        public SurveyInstrument.Model.SurveyInstrument SurveyTool
        {
            get { return surveyTool_; }
            set { surveyTool_ = value; }
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
            if (surveyStation != null && surveyTool_ != null && previousStation != null && A != null)
            {
                double sinI = System.Math.Sin((double)surveyStation.Incl);
                double cosI = System.Math.Cos((double)surveyStation.Incl);
                double sinA = System.Math.Sin((double)surveyStation.Az);
                double cosA = System.Math.Cos((double)surveyStation.Az);
                double deltaSk = surveyStation.MD - previousStation.MD;
                //double Vk = (double)surveyStation.Z;
                //double Nk = (double)surveyStation.X;
                //double Ek = (double)surveyStation.Y;
                double deltaC10 = surveyTool_.ReferenceError!=null ? (double)surveyTool_.ReferenceError : 0;
                double deltaC20 = surveyTool_.DrillStringMag != null ? (double)surveyTool_.DrillStringMag : 0;
                double deltaC30 = surveyTool_.GyroCompassError != null ? (double)surveyTool_.GyroCompassError : 0;
                double deltaIt0 = surveyTool_.TrueInclination != null ? (double)surveyTool_.TrueInclination : 0;
                double deltaIm = surveyTool_.Misalignment != null ? (double)surveyTool_.Misalignment : 0;
                double epsilon = surveyTool_.RelDepthError != null ? (double)surveyTool_.RelDepthError : 0;
                double deltaZ = (double)surveyStation.Z - (double)previousStation.Z;
                double deltaX = (double)surveyStation.X - (double)previousStation.X;
                double deltaY = (double)surveyStation.Y - (double)previousStation.Y;

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
