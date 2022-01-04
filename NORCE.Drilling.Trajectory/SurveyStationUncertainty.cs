using System;
using System.Collections.Generic;
using System.Text;
using NORCE.General.Math;
using NORCE.General.Math;
using NORCE.General.Std;


namespace NORCE.Drilling.Trajectory
{
    /// <summary>
    /// class representing a survey station uncertainty
    /// </summary>
    public class SurveyStationUncertainty
    {
        protected static double[,] chiSquare3D_ = new double[,] { { 0.05, 0.10, 0.2, 0.3, 0.5, 0.7, 0.8, 0.9, 0.95, 0.99, 0.999 }, { 0.35, 0.58, 1.01, 1.42, 2.37, 3.66, 4.64, 6.25, 7.82, 11.34, 16.27 } };

        /// <summary>
        /// covariance matrix associated with the survey station
        /// </summary>
        public SymmetricMatrix3x3 Covariance { get; set; }

        public double? C11 { get; set; }
        public double? C12 { get; set; }
        public double? C13 { get; set; }
        public double? C21 { get; set; }
        public double? C22 { get; set; }
        public double? C23 { get; set; }
        public double? C31 { get; set; }
        public double? C32 { get; set; }
        public double? C33 { get; set; }

        /// <summary>
        /// bias vector associated with survey station
        /// </summary>
        public Vector3D Bias { get; set; }

        /// <summary>
        /// 
        /// </summary>
        //public SurveyInstrument.Model.SurveyInstrument SurveyTool { get; set; }
        /// <summary>
        /// Eigenvector matrix associated with survey station Covariance
        /// </summary>
        public Matrix EigenVectors { get; set; }


        /// <summary>
        /// Radius of error ellipsoid associated with survey station
        /// </summary>
        public Vector3D EllipsoidRadius { get; set; }

        /// <summary>
        /// Radius of error ellipse associated with survey station
        /// </summary>
        public Vector2D EllipseRadius { get; set; }
        public double PerpendicularDirection { get; set; }
        public List<UncertaintyEnvelopeEllipse> UncertaintyCylinder { get; set; }
        /// <summary>
        /// default constructor
        /// </summary>
        public SurveyStationUncertainty()
        {
            Covariance = new SymmetricMatrix3x3();
            Bias = new Vector3D();
            EigenVectors = new Matrix(3, 3);
            EllipsoidRadius = new Vector3D();
            EllipseRadius = new Vector2D();
        }

        /// <summary>
        /// Calculate error ellipsoid and error ellipse radiuses
        /// </summary>
        public void Calculate(SurveyStation surveyStation, double confidenceFactor, double scalingFactor = 1.0, double boreholeRadius = 0.0)
        {
            // calculate the parameters of the ellipsoid
            double chiSquare = GetChiSquare3D(confidenceFactor);

            //Calculate eigenvectors and eigenvalues
            double[,] z = new double[3, 3];
            z[0, 0] = (double)Covariance[0, 0];
            z[0, 1] = (double)Covariance[0, 1];
            z[0, 2] = (double)Covariance[0, 2];
            z[1, 0] = (double)Covariance[1, 0];
            z[1, 1] = (double)Covariance[1, 1];
            z[1, 2] = (double)Covariance[1, 2];
            z[2, 0] = (double)Covariance[2, 0];
            z[2, 1] = (double)Covariance[2, 1];
            z[2, 2] = (double)Covariance[2, 2];
            double[] d = new double[4];
            double[] e = new double[3];
            double[,] zCov = new double[3, 3];
            zCov[0, 0] = (double)Covariance[0, 0];
            zCov[0, 1] = (double)Covariance[0, 1];
            zCov[0, 2] = (double)Covariance[0, 2];
            zCov[1, 0] = (double)Covariance[1, 0];
            zCov[1, 1] = (double)Covariance[1, 1];
            zCov[1, 2] = (double)Covariance[1, 2];
            zCov[2, 0] = (double)Covariance[2, 0];
            zCov[2, 1] = (double)Covariance[2, 1];
            zCov[2, 2] = (double)Covariance[2, 2];
            // tranform a symmetric matrix into a tridiagonal matrix
            tred2(3, z, d, e);
            // QL decomposition
            tql2(3, z, d, e);
            //transfer eigenvectors
            Matrix P = new Matrix(3, 3);
            P[0, 0] = z[0, 0];
            P[0, 1] = z[0, 1];
            P[0, 2] = z[0, 2];
            P[1, 0] = z[1, 0];
            P[1, 1] = z[1, 1];
            P[1, 2] = z[1, 2];
            P[2, 0] = z[2, 0];
            P[2, 1] = z[2, 1];
            P[2, 2] = z[2, 2];
            EigenVectors = P;
            // transfer eigen values
            double a;
            double b;
            double c;
            a = d[0];
            b = d[1];
            c = d[2];

            //Verify
            if (true)
            {
                double[,] PInverse = new double[3, 3];
                // useful variables
                double p11 = (double)P[0, 0];
                double p12 = (double)P[0, 1];
                double p13 = (double)P[0, 2];
                double p21 = (double)P[1, 0];
                double p22 = (double)P[1, 1];
                double p23 = (double)P[1, 2];
                double p31 = (double)P[2, 0];
                double p32 = (double)P[2, 1];
                double p33 = (double)P[2, 2];

                //test (A-LambdaI)v=0
                double[,] zCovL = new double[3, 3];
                zCovL[0, 0] = zCov[0, 0] - a;
                zCovL[0, 1] = zCov[0, 1];
                zCovL[0, 2] = zCov[0, 2];
                zCovL[1, 0] = zCov[1, 0];
                zCovL[1, 1] = zCov[1, 1] - a;
                zCovL[1, 2] = zCov[1, 2];
                zCovL[2, 0] = zCov[2, 0];
                zCovL[2, 1] = zCov[2, 1];
                zCovL[2, 2] = zCov[2, 2] - a;
                double test1 = zCovL[0, 0] * p11 + zCovL[0, 1] * p21 + zCovL[0, 2] * p31;
                double test2 = zCovL[1, 0] * p11 + zCovL[1, 1] * p21 + zCovL[1, 2] * p31;
                double test3 = zCovL[2, 0] * p11 + zCovL[2, 1] * p21 + zCovL[2, 2] * p31;

                // calculate the inverse of the eigenvectors
                //double[,] PInverse = new double[3, 3];
                //Check if this is correct. Maybe a transpose is forgotten
                double determinant = (p11 * p22 - p12 * p21) * p33 + (p13 * p21 - p11 * p23) * p32 + (p12 * p23 - p13 * p22) * p31;
                PInverse[0, 0] = (p22 * p33 - p23 * p32) / determinant;
                PInverse[1, 0] = -(p12 * p33 - p13 * p32) / determinant;
                PInverse[2, 0] = (p12 * p23 - p13 * p22) / determinant;
                PInverse[0, 1] = -(p21 * p33 - p23 * p31) / determinant;
                PInverse[1, 1] = (p11 * p33 - p13 * p31) / determinant;
                PInverse[2, 1] = -(p11 * p23 - p13 * p21) / determinant;
                PInverse[0, 2] = (p21 * p32 - p22 * p31) / determinant;
                PInverse[1, 2] = -(p11 * p32 - p12 * p31) / determinant;
                PInverse[2, 2] = (p11 * p22 - p12 * p21) / determinant;

                // verify that P is orthogonal, i.e. P^-1 = Pt
                bool ok = true;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        ok &= Numeric.EQ(PInverse[i, j], P[j, i], 1e-6);
                    }
                }
                if (!ok)
                {
                    Console.WriteLine("problem");
                }

                // verification H = P*L*Pt
                double[,] L = new double[3, 3];
                L[0, 0] = a;
                L[0, 1] = 0.0;
                L[0, 2] = 0.0;
                L[1, 0] = 0.0;
                L[1, 1] = b;
                L[1, 2] = 0.0;
                L[2, 0] = 0.0;
                L[2, 1] = 0.0;
                L[2, 2] = c;

                // first calculate L * P^-1
                double[,] LP1 = new double[3, 3];
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        double tt = 0;
                        for (int k = 0; k < 3; k++)
                        {
                            tt += L[i, k] * (double)P[j, k];
                        }
                        LP1[i, j] = tt;
                    }
                }

                // second calculate P*PL1
                double[,] PLP1 = new double[3, 3];
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        double tt = 0;
                        for (int k = 0; k < 3; k++)
                        {
                            tt += (double)P[i, k] * LP1[k, j];
                        }
                        PLP1[i, j] = tt;
                    }
                }

                // finally compare with H
                ok = true;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (!Numeric.EQ(Covariance[i, j], PLP1[i, j], 1e-6))
                        {
                            Console.WriteLine(i + "\t" + j + "\t" + Covariance[i, j] + "\t" + PLP1[i, j]);
                        }
                        ok &= Numeric.EQ(Covariance[i, j], PLP1[i, j], 1e-6);
                    }
                }
                if (!ok)
                {
                    Console.WriteLine("problem H");
                }
            }

            //Calculate error ellipsoid                
            double[,] A = new double[3, 3];
            double sinI = System.Math.Sin((double)surveyStation.Incl);
            double cosI = System.Math.Cos((double)surveyStation.Incl);
            double sinA = System.Math.Sin((double)surveyStation.Az);
            double cosA = System.Math.Cos((double)surveyStation.Az);

            double kScale = 1; //Scaling factor of error ellipsoid
            kScale = System.Math.Sqrt(chiSquare);
            double[] lambda = new double[3];
            lambda[0] = a;
            lambda[1] = b;
            lambda[2] = c;
            double[] R = new double[3];
            for (int i = 0; i < 3; i++)
            {
                R[i] = kScale * System.Math.Sqrt(lambda[i]);
            }
            EllipsoidRadius[0] = (R[0] + boreholeRadius) * scalingFactor;
            EllipsoidRadius[1] = (R[1] + boreholeRadius) * scalingFactor;
            EllipsoidRadius[2] = (R[2] + boreholeRadius) * scalingFactor;

            //Calculate error ellipse
            double[,] T = new double[3, 3];
            T[0, 0] = cosI * cosA;
            T[1, 0] = -sinA;
            T[2, 0] = sinI * cosA;
            T[0, 1] = cosI * sinA;
            T[1, 1] = cosA;
            T[2, 1] = sinI * sinA;
            T[0, 2] = -sinI;
            T[1, 2] = 0;
            T[2, 2] = cosI;

            double[,] Tt = new double[3, 3];
            Tt[0, 0] = T[0, 0];
            Tt[1, 0] = T[0, 1];
            Tt[2, 0] = T[0, 2];
            Tt[0, 1] = T[1, 0];
            Tt[1, 1] = T[1, 1];
            Tt[2, 1] = T[1, 2];
            Tt[0, 2] = T[2, 0];
            Tt[1, 2] = T[2, 1];
            Tt[2, 2] = T[2, 2];

            double[,] C = new double[3, 3];
            C[0, 0] = (double)Covariance[0, 0];
            C[0, 1] = (double)Covariance[0, 1];
            C[0, 2] = (double)Covariance[0, 2];
            C[1, 0] = (double)Covariance[1, 0];
            C[1, 1] = (double)Covariance[1, 1];
            C[1, 2] = (double)Covariance[1, 2];
            C[2, 0] = (double)Covariance[2, 0];
            C[2, 1] = (double)Covariance[2, 1];
            C[2, 2] = (double)Covariance[2, 2];

            double[,] Cxyz1 = new double[3, 3];
            double[,] Cxyz = new double[3, 3];
            Cxyz1 = MatrixMuliprication(C, Tt);
            Cxyz = MatrixMuliprication(T, Cxyz1);
            double tan2Thetaxy = 2 * Cxyz[0, 1] / (Cxyz[0, 0] * Cxyz[0, 0] - Cxyz[1, 1] * Cxyz[1, 1]);
            double tanInv2Thetaxy = System.Math.Atan(tan2Thetaxy);
            double thetaxy = tanInv2Thetaxy / 2;
            if (Numeric.EQ(Cxyz[0, 0], Cxyz[1, 1]))
            {
                thetaxy = 0.25 * Numeric.PI * ((Cxyz[0, 1] >= 0.0) ? 1.0 : -1.0);
            }

            double[] Rxy = new double[2];
            double[] lambdaxy = new double[2];
            double temp = Cxyz[0, 1] * System.Math.Sin(2 * thetaxy);
            double tmp = 2.0 * System.Math.Cos(thetaxy) * System.Math.Sin(thetaxy) * Cxyz[0, 1];
            lambdaxy[0] = Cxyz[0, 0] * System.Math.Cos(thetaxy) * System.Math.Cos(thetaxy) + tmp + Cxyz[1, 1] * System.Math.Sin(thetaxy) * System.Math.Sin(thetaxy);
            lambdaxy[1] = Cxyz[0, 0] * System.Math.Sin(thetaxy) * System.Math.Sin(thetaxy) - tmp + Cxyz[1, 1] * System.Math.Cos(thetaxy) * System.Math.Cos(thetaxy);
            Rxy[0] = kScale * System.Math.Sqrt(lambdaxy[0]);
            Rxy[1] = kScale * System.Math.Sqrt(lambdaxy[1]);
            double?[] errorCylinderEllipseRNor = new double?[2];
            EllipseRadius[0] = ( Rxy[0] + boreholeRadius) * scalingFactor;
            EllipseRadius[1] = (Rxy[1] + boreholeRadius) * scalingFactor;


            //From "old" code
            //perpendicular projection
            double K22 = (double)(cosA * cosI * (Covariance[0, 0] * cosA * cosI + Covariance[0, 1] * cosI * sinA - Covariance[0, 2] * sinI) + cosI * sinA * (Covariance[0, 1] * cosA * cosI + Covariance[1, 1] * cosI * sinA - Covariance[1, 2] * sinI) - sinI * (Covariance[0, 2] * cosA * cosI + Covariance[1, 2] * cosI * sinA - Covariance[2, 2] * sinI));
            double K23 = (double)(cosA * (Covariance[0, 1] * cosA * cosI + Covariance[1, 1] * cosI * sinA - Covariance[1, 2] * sinI) - sinA * (Covariance[0, 0] * cosA * cosI + Covariance[0, 1] * cosI * sinA - Covariance[0, 2] * sinI));
            double K33 = (double)(cosA * (Covariance[1, 1] * cosA - Covariance[0, 1] * sinA) - sinA * (Covariance[0, 1] * cosA - Covariance[0, 0] * sinA));
            double phi = 0.0;
            double phi2 = 0.0;
            if (Numeric.EQ(K22, K33))
            {
                phi = 0.25 * Numeric.PI * ((K23 >= 0.0) ? 1.0 : -1.0);
            }
            else
            {
                phi = 0.5 * System.Math.Atan((2.0 * K23) / (K22 - K33));
                //phi2 = 0.5 * System.Math.Atan((2.0 * K23) / (K22* K22 - K33 * K33));
                //phi = phi2;
            }
            // to transform compare to vertical instead of horizontal
            double sinP = System.Math.Sin(phi);
            double cosP = System.Math.Cos(phi);
            tmp = 2.0 * sinP * cosP * K23;
            double semiMinorPerpendicularAxis_ = Numeric.SqrtEqual(chiSquare * (K22 * cosP * cosP + K33 * sinP * sinP + tmp));
            double semiMajorPerpendicularAxis_ = Numeric.SqrtEqual(chiSquare * (K22 * sinP * sinP + K33 * cosP * cosP - tmp));
            //if (semiMajorPerpendicularAxis_ < semiMinorPerpendicularAxis_)
            //{
            //    tmp = semiMinorPerpendicularAxis_;
            //    semiMinorPerpendicularAxis_ = semiMajorPerpendicularAxis_;
            //    semiMajorPerpendicularAxis_ = tmp;
            //}
            //else
            //{
            //    phi += 0.5 * Numeric.PI;
            //}
            //phi += 0.5 * Numeric.PI;
            //if (phi > 2.0 * Numeric.PI)
            //{
            //    phi -= 2.0 * Numeric.PI;
            //}
            //if (phi < 0.0)
            //{
            //    phi += Numeric.PI;
            //}
            double perpendicularDirection_ = phi;
            PerpendicularDirection = perpendicularDirection_;
            EllipseRadius[0] = (semiMinorPerpendicularAxis_ + boreholeRadius) * scalingFactor;
            EllipseRadius[1] = (semiMajorPerpendicularAxis_ + boreholeRadius) * scalingFactor;

            C11 = (double)Covariance[0, 0];
            C12 = (double)Covariance[0, 1];
            C13 = (double)Covariance[0, 2];
            C21 = (double)Covariance[1, 0];
            C22 = (double)Covariance[1, 1];
            C23 = (double)Covariance[1, 2];
            C31 = (double)Covariance[2, 0];
            C32 = (double)Covariance[2, 1];
            C33 = (double)Covariance[2, 2];

        }

        /// <summary>
        /// Calculate Covariance matrix using WdW
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="previousStation"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        public double[,] CalculateWdWCovariancesOld(SurveyStation surveyStation, SurveyStation previousStation, double[,] A)
        {
            
            if (surveyStation != null &&  previousStation != null && A != null)
            {
                double RelativeDepthError = 0.002;
                double Misalignment = 0.3 * Numeric.PI / 180.0;
                double TrueInclination = 1.0 * Numeric.PI / 180.0;
                double ReferenceError = 1.5 * Numeric.PI / 180.0;
                double DrillStringMagnetisation = 5.0 * Numeric.PI / 180.0;
          double GyroCompassError = Numeric.UNDEF_DOUBLE;
                double sinI = System.Math.Sin((double)surveyStation.Incl);
                double cosI = System.Math.Cos((double)surveyStation.Incl);
                double sinA = System.Math.Sin((double)surveyStation.Az);
                double cosA = System.Math.Cos((double)surveyStation.Az);
                double deltaSk = surveyStation.MD - previousStation.MD;
                double Vk = (double)surveyStation.Z;
                double Nk = (double)surveyStation.X;
                double Ek = (double)surveyStation.Y;
                double deltaC10 = (Numeric.IsDefined(ReferenceError)) ? ReferenceError : 0;
                double deltaC20 = (Numeric.IsDefined(DrillStringMagnetisation)) ? DrillStringMagnetisation : 0;
                double deltaC30 = (Numeric.IsDefined(GyroCompassError)) ? GyroCompassError : 0;
                double deltaIt0 = (Numeric.IsDefined(TrueInclination)) ? TrueInclination : 0;
                double deltaIm = (Numeric.IsDefined(Misalignment)) ?Misalignment : 0;
                double epsilon = (Numeric.IsDefined(RelativeDepthError)) ? RelativeDepthError : 0;
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
                    double[,] testt = new double[3, 3];
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            testt[i, j] = A[0, i] * A[0, j] + A[1, i] * A[1, j] + A[2, i] * A[2, j] + A[3, i] * A[3, j] + A[4, i] * A[4, j] + A[5, i] * A[5, j] + ((i == j) ? tmp : 0.0) - A[4, i] * A[4, j];
                            Covariance[i, j] = A[0, i] * A[0, j] + A[1, i] * A[1, j] + A[2, i] * A[2, j] + A[3, i] * A[3, j] + A[4, i] * A[4, j] + A[5, i] * A[5, j] + ((i == j) ? tmp : 0.0) - A[4, i] * A[4, j];
                        }
                    }


                    // apply horizontal magnetic deviations
                    //Bias.Set(A[1, 1], A[1, 0], 0.0);
                }
                return A;
            }
            else
            {
                return null;
            }
        }

        protected static double GetChiSquare3D(double p)
        {
            if (Numeric.IsUndefined(p))
            {
                return Numeric.UNDEF_DOUBLE;
            }
            else
            {
                int last = chiSquare3D_.GetLength(1) - 1;
                if (p < chiSquare3D_[0, 0])
                {
                    double factor = (p - chiSquare3D_[0, 0]) / (chiSquare3D_[0, 1] - chiSquare3D_[0, 0]);
                    return chiSquare3D_[1, 0] + factor * (chiSquare3D_[1, 1] - chiSquare3D_[1, 0]);
                }
                else if (p >= chiSquare3D_[0, last])
                {
                    double factor = (p - chiSquare3D_[0, last - 1]) / (chiSquare3D_[0, last] - chiSquare3D_[0, last - 1]);
                    return chiSquare3D_[1, last - 1] + factor * (chiSquare3D_[1, last] - chiSquare3D_[1, last - 1]);
                }
                else
                {
                    for (int i = 0; i < last; i++)
                    {
                        if (p >= chiSquare3D_[0, i] && p < chiSquare3D_[0, i + 1])
                        {
                            double factor = (p - chiSquare3D_[0, i]) / (chiSquare3D_[0, i + 1] - chiSquare3D_[0, i]);
                            return chiSquare3D_[1, i] + factor * (chiSquare3D_[1, i + 1] - chiSquare3D_[1, i]);
                        }
                    }
                    return Numeric.UNDEF_DOUBLE;
                }
            }
        }

        protected double[,] MatrixMuliprication(double[,] A, double[,] B)
        {

            int rA = A.GetLength(0);
            int cA = A.GetLength(1);
            int rB = B.GetLength(0);
            int cB = B.GetLength(1);
            double temp = 0;
            double[,] M = new double[rA, cB];
            if (cA == rB)
            {
                for (int i = 0; i < rA; i++)
                {
                    for (int j = 0; j < cB; j++)
                    {
                        temp = 0;
                        for (int k = 0; k < cA; k++)
                        {
                            temp += A[i, k] * B[k, j];
                        }
                        M[i, j] = temp;
                    }
                }

            }
            return M;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n"></param>
        /// <param name="V"></param>
        /// <param name="d"></param>
        /// <param name="e"></param>
        protected void tred2(int n, double[,] V, double[] d, double[] e)
        {

            //  This is derived from the Algol procedures tred2 by
            //  Bowdler, Martin, Reinsch, and Wilkinson, Handbook for
            //  Auto. Comp., Vol.ii-Linear Algebra, and the corresponding
            //  Fortran subroutine in EISPACK.

            for (int j = 0; j < n; j++)
            {
                d[j] = V[n - 1, j];
            }

            // Householder reduction to tridiagonal form.

            for (int i = n - 1; i > 0; i--)
            {

                // Scale to avoid under/overflow.

                double scale = 0.0;
                double h = 0.0;
                for (int k = 0; k < i; k++)
                {
                    scale = scale + System.Math.Abs(d[k]);
                }
                if (scale == 0.0)
                {
                    e[i] = d[i - 1];
                    for (int j = 0; j < i; j++)
                    {
                        d[j] = V[i - 1, j];
                        V[i, j] = 0.0;
                        V[j, i] = 0.0;
                    }
                }
                else
                {

                    // Generate Householder vector.

                    for (int k = 0; k < i; k++)
                    {
                        d[k] /= scale;
                        h += d[k] * d[k];
                    }
                    double f = d[i - 1];
                    double g = System.Math.Sqrt(h);
                    if (f > 0)
                    {
                        g = -g;
                    }
                    e[i] = scale * g;
                    h = h - f * g;
                    d[i - 1] = f - g;
                    for (int j = 0; j < i; j++)
                    {
                        e[j] = 0.0;
                    }

                    // Apply similarity transformation to remaining columns.

                    for (int j = 0; j < i; j++)
                    {
                        f = d[j];
                        V[j, i] = f;
                        g = e[j] + V[j, j] * f;
                        for (int k = j + 1; k <= i - 1; k++)
                        {
                            g += V[k, j] * d[k];
                            e[k] += V[k, j] * f;
                        }
                        e[j] = g;
                    }
                    f = 0.0;
                    for (int j = 0; j < i; j++)
                    {
                        e[j] /= h;
                        f += e[j] * d[j];
                    }
                    double hh = f / (h + h);
                    for (int j = 0; j < i; j++)
                    {
                        e[j] -= hh * d[j];
                    }
                    for (int j = 0; j < i; j++)
                    {
                        f = d[j];
                        g = e[j];
                        for (int k = j; k <= i - 1; k++)
                        {
                            V[k, j] -= (f * e[k] + g * d[k]);
                        }
                        d[j] = V[i - 1, j];
                        V[i, j] = 0.0;
                    }
                }
                d[i] = h;
            }

            // Accumulate transformations.

            for (int i = 0; i < n - 1; i++)
            {
                V[n - 1, i] = V[i, i];
                V[i, i] = 1.0;
                double h = d[i + 1];
                if (h != 0.0)
                {
                    for (int k = 0; k <= i; k++)
                    {
                        d[k] = V[k, i + 1] / h;
                    }
                    for (int j = 0; j <= i; j++)
                    {
                        double g = 0.0;
                        for (int k = 0; k <= i; k++)
                        {
                            g += V[k, i + 1] * V[k, j];
                        }
                        for (int k = 0; k <= i; k++)
                        {
                            V[k, j] -= g * d[k];
                        }
                    }
                }
                for (int k = 0; k <= i; k++)
                {
                    V[k, i + 1] = 0.0;
                }
            }
            for (int j = 0; j < n; j++)
            {
                d[j] = V[n - 1, j];
                V[n - 1, j] = 0.0;
            }
            V[n - 1, n - 1] = 1.0;
            e[0] = 0.0;
        }


        // Symmetric tridiagonal QL algorithm.

        protected void tql2(int n, double[,] V, double[] d, double[] e)
        {

            //  This is derived from the Algol procedures tql2, by
            //  Bowdler, Martin, Reinsch, and Wilkinson, Handbook for
            //  Auto. Comp., Vol.ii-Linear Algebra, and the corresponding
            //  Fortran subroutine in EISPACK.

            for (int i = 1; i < n; i++)
            {
                e[i - 1] = e[i];
            }
            e[n - 1] = 0.0;

            double f = 0.0;
            double tst1 = 0.0;
            double eps = System.Math.Pow(2.0, -52.0);
            for (int l = 0; l < n; l++)
            {

                // Find small subdiagonal element

                tst1 = System.Math.Max(tst1, System.Math.Abs(d[l]) + System.Math.Abs(e[l]));
                int m = l;
                while (m < n)
                {
                    if (System.Math.Abs(e[m]) <= eps * tst1)
                    {
                        break;
                    }
                    m++;
                }

                // If m == l, d[l] is an eigenvalue,
                // otherwise, iterate.

                if (m > l)
                {
                    int iter = 0;
                    do
                    {
                        iter = iter + 1;  // (Could check iteration count here.)

                        // Compute implicit shift

                        double g = d[l];
                        double p = (d[l + 1] - g) / (2.0 * e[l]);
                        double r = Pythag(p, 1.0);
                        if (p < 0)
                        {
                            r = -r;
                        }
                        d[l] = e[l] / (p + r);
                        d[l + 1] = e[l] * (p + r);
                        double dl1 = d[l + 1];
                        double h = g - d[l];
                        for (int i = l + 2; i < n; i++)
                        {
                            d[i] -= h;
                        }
                        f = f + h;

                        // Implicit QL transformation.

                        p = d[m];
                        double c = 1.0;
                        double c2 = c;
                        double c3 = c;
                        double el1 = e[l + 1];
                        double s = 0.0;
                        double s2 = 0.0;
                        for (int i = m - 1; i >= l; i--)
                        {
                            c3 = c2;
                            c2 = c;
                            s2 = s;
                            g = c * e[i];
                            h = c * p;
                            r = Pythag(p, e[i]);
                            e[i + 1] = s * r;
                            s = e[i] / r;
                            c = p / r;
                            p = c * d[i] - s * g;
                            d[i + 1] = h + s * (c * g + s * d[i]);

                            // Accumulate transformation.

                            for (int k = 0; k < n; k++)
                            {
                                h = V[k, i + 1];
                                V[k, i + 1] = s * V[k, i] + c * h;
                                V[k, i] = c * V[k, i] - s * h;
                            }
                        }
                        p = -s * s2 * c3 * el1 * e[l] / dl1;
                        e[l] = s * p;
                        d[l] = c * p;

                        // Check for convergence.

                    } while (System.Math.Abs(e[l]) > eps * tst1);
                }
                d[l] = d[l] + f;
                e[l] = 0.0;
            }

            // Sort eigenvalues and corresponding vectors.

            for (int i = 0; i < n - 1; i++)
            {
                int k = i;
                double p = d[i];
                for (int j = i + 1; j < n; j++)
                {
                    if (d[j] < p)
                    {
                        k = j;
                        p = d[j];
                    }
                }
                if (k != i)
                {
                    d[k] = d[i];
                    d[i] = p;
                    for (int j = 0; j < n; j++)
                    {
                        p = V[j, i];
                        V[j, i] = V[j, k];
                        V[j, k] = p;
                    }
                }
            }
        }

        // <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected double Pythag(double a, double b)
        {
            double absa = System.Math.Abs(a);
            double absb = System.Math.Abs(b);
            if (absa > absb)
            {
                return absa * System.Math.Sqrt(1 + absb * absb / (absa * absa));
            }
            else
            {
                if (Numeric.EQ(absb, 0))
                {
                    return 0.0;
                }
                else
                {
                    return absb * System.Math.Sqrt(1.0 + absa * absa / (absb * absb));
                }
            }
        }
    }
}
