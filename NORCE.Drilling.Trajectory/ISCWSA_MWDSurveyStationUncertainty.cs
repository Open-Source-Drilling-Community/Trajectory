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
        /// Magnetic Dip angle [rad]
        /// </summary>
        public double Dip { get; set; } = 72 * Math.PI / 180.0;
        /// <summary>
        /// Declination angle [rad]
        /// </summary>
        public double Declination { get; set; } = -4 * Math.PI / 180.0;
        /// <summary>
        /// Earth's Gravity [m/s2]
        /// </summary>
        public double Gravity { get; set; } = 9.80665;
        /// <summary>
        /// Magnetic Total Field [nT]
        /// </summary>
        public double BField { get; set; } = 50000;
        /// <summary>
        /// Convergence [rad]
        /// </summary>
        public double Convergence { get; set; } = 0;
        /// <summary>
        /// Ineces of error sources
        /// </summary>
        public int[] ErrorIndices { get; set; } = null;
        // <summary>
        /// List of error sources
        /// </summary>
        public List<IErrorSource> ErrorSources { get; set; } = null;
        /// <summary>
        /// Used for calculations
        /// </summary>
        public List<ISCWSAErrorData> ISCWSAErrorDataTmp { get; set; } = null;

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
        /// Calculate Covariance matrix for a survey station (ISCWSA MWD Rev 5)
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="previousStation"></param>
        /// <returns></returns>
        public void CalculateCovariance(SurveyStation surveyStation, SurveyStation surveyStationPrev, SurveyStation surveyStationNext, List<ISCWSAErrorData> ISCWSAErrorDataPrev, int c)
        {
			#region Define error sources
			if (ErrorSources == null)
            {
                //Use all error sources
                ErrorSources = new List<IErrorSource>();
                if (ErrorIndices == null)
                {
                    ErrorSourceDRFR errorSourceDRFR = new ErrorSourceDRFR();
                    ErrorSources.Add(errorSourceDRFR);
                    ErrorSourceDSFS errorSourceDSFS = new ErrorSourceDSFS();
                    ErrorSources.Add(errorSourceDSFS);
                    ErrorSourceDSTG errorSourceDSTG = new ErrorSourceDSTG();
                    ErrorSources.Add(errorSourceDSTG);
                    ErrorSourceABXY_TI1S errorSourceABXY_TI1S = new ErrorSourceABXY_TI1S();
                    errorSourceABXY_TI1S.Dip = Dip;
                    errorSourceABXY_TI1S.Gravity = Gravity;
                    errorSourceABXY_TI1S.Declination = Declination;
                    ErrorSources.Add(errorSourceABXY_TI1S);
                    ErrorSourceABXY_TI2S errorSourceABXY_TI2S = new ErrorSourceABXY_TI2S();
                    errorSourceABXY_TI2S.Dip = Dip;
                    errorSourceABXY_TI2S.Gravity = Gravity;
                    errorSourceABXY_TI2S.Declination = Declination;
                    ErrorSources.Add(errorSourceABXY_TI2S);
                    ErrorSourceABZ errorSourceABZ = new ErrorSourceABZ();
                    errorSourceABZ.Dip = Dip;
                    errorSourceABZ.Gravity = Gravity;
                    errorSourceABZ.Declination = Declination;
                    ErrorSources.Add(errorSourceABZ);
                    ErrorSourceASXY_TI1S errorSourceASXY_TI1S = new ErrorSourceASXY_TI1S();
                    errorSourceASXY_TI1S.Dip = Dip;
                    errorSourceASXY_TI1S.Declination = Declination;
                    ErrorSources.Add(errorSourceASXY_TI1S);
                    ErrorSourceASXY_TI2S errorSourceASXY_TI2S = new ErrorSourceASXY_TI2S();
                    errorSourceASXY_TI2S.Dip = Dip;
                    errorSourceASXY_TI2S.Declination = Declination;
                    ErrorSources.Add(errorSourceASXY_TI2S);
                    ErrorSourceASXY_TI3S errorSourceASXY_TI3S = new ErrorSourceASXY_TI3S();
                    errorSourceASXY_TI3S.Dip = Dip;
                    errorSourceASXY_TI3S.Declination = Declination;
                    ErrorSources.Add(errorSourceASXY_TI3S);
                    ErrorSourceASZ errorSourceASZ = new ErrorSourceASZ();
                    errorSourceASZ.Dip = Dip;
                    errorSourceASZ.Declination = Declination;
                    ErrorSources.Add(errorSourceASZ);
                    ErrorSourceMBXY_TI1 errorSourceMBXY_TI1 = new ErrorSourceMBXY_TI1();
                    errorSourceMBXY_TI1.Dip = Dip;
                    errorSourceMBXY_TI1.Declination = Declination;
                    errorSourceMBXY_TI1.BField = BField;
                    ErrorSources.Add(errorSourceMBXY_TI1);
                    ErrorSourceMBXY_TI2 ErrorSourceMBXY_TI2 = new ErrorSourceMBXY_TI2();
                    ErrorSourceMBXY_TI2.Dip = Dip;
                    ErrorSourceMBXY_TI2.Declination = Declination;
                    ErrorSourceMBXY_TI2.BField = BField;
                    ErrorSources.Add(ErrorSourceMBXY_TI2);
                    ErrorSourceMBZ errorSourceMBZ = new ErrorSourceMBZ();
                    errorSourceMBZ.Dip = Dip;
                    errorSourceMBZ.Declination = Declination;
                    errorSourceMBZ.BField = BField;
                    ErrorSources.Add(errorSourceMBZ);
                    ErrorSourceMSXY_TI1 errorSourceMSXY_TI1 = new ErrorSourceMSXY_TI1();
                    errorSourceMSXY_TI1.Dip = Dip;
                    errorSourceMSXY_TI1.Declination = Declination;
                    ErrorSources.Add(errorSourceMSXY_TI1);
                    ErrorSourceMSXY_TI2 errorSourceMSXY_TI2 = new ErrorSourceMSXY_TI2();
                    errorSourceMSXY_TI2.Dip = Dip;
                    errorSourceMSXY_TI2.Declination = Declination;
                    ErrorSources.Add(errorSourceMSXY_TI2);
                    ErrorSourceMSXY_TI3 errorSourceMSXY_TI3 = new ErrorSourceMSXY_TI3();
                    errorSourceMSXY_TI3.Dip = Dip;
                    errorSourceMSXY_TI3.Declination = Declination;
                    ErrorSources.Add(errorSourceMSXY_TI3);
                    ErrorSourceMSZ errorSourceMSZ = new ErrorSourceMSZ();
                    errorSourceMSZ.Dip = Dip;
                    errorSourceMSZ.Declination = Declination;
                    ErrorSources.Add(errorSourceMSZ);
                    ErrorSourceDEC_U errorSourceDEC_U = new ErrorSourceDEC_U();
                    ErrorSources.Add(errorSourceDEC_U);
                    ErrorSourceDEC_OS errorSourceDEC_OS = new ErrorSourceDEC_OS();
                    ErrorSources.Add(errorSourceDEC_OS);
                    ErrorSourceDEC_OH errorSourceDEC_OH = new ErrorSourceDEC_OH();
                    ErrorSources.Add(errorSourceDEC_OH);
                    ErrorSourceDEC_OI errorSourceDEC_OI = new ErrorSourceDEC_OI();
                    ErrorSources.Add(errorSourceDEC_OI);
                    ErrorSourceDECR errorSourceDECR = new ErrorSourceDECR();
                    ErrorSources.Add(errorSourceDECR);
                    ErrorSourceDBH_U errorSourceDBH_U = new ErrorSourceDBH_U();
                    errorSourceDBH_U.Dip = Dip;
                    errorSourceDBH_U.BField = BField;
                    ErrorSources.Add(errorSourceDBH_U);
                    ErrorSourceDBH_OS errorSourceDBH_OS = new ErrorSourceDBH_OS();
                    errorSourceDBH_OS.Dip = Dip;
                    errorSourceDBH_OS.BField = BField;
                    ErrorSources.Add(errorSourceDBH_OS);
                    ErrorSourceDBH_OH errorSourceDBH_OH = new ErrorSourceDBH_OH();
                    errorSourceDBH_OH.Dip = Dip;
                    errorSourceDBH_OH.BField = BField;
                    ErrorSources.Add(errorSourceDBH_OH);
                    ErrorSourceDBH_OI errorSourceDBH_OI = new ErrorSourceDBH_OI();
                    errorSourceDBH_OI.Dip = Dip;
                    errorSourceDBH_OI.BField = BField;
                    ErrorSources.Add(errorSourceDBH_OI);
                    ErrorSourceDBHR errorSourceDBHR = new ErrorSourceDBHR();
                    errorSourceDBHR.Dip = Dip;
                    errorSourceDBHR.BField = BField;
                    ErrorSources.Add(errorSourceDBHR);
                    ErrorSourceAMIL errorSourceAMIL = new ErrorSourceAMIL();
                    errorSourceAMIL.Dip = Dip;
                    errorSourceAMIL.BField = BField;
                    errorSourceAMIL.Declination = Declination;
                    ErrorSources.Add(errorSourceAMIL);
                    ErrorSourceSAGE errorSourceSAGE = new ErrorSourceSAGE();
                    ErrorSources.Add(errorSourceSAGE);
                    ErrorSourceXYM1 errorSourceXYM1 = new ErrorSourceXYM1();
                    ErrorSources.Add(errorSourceXYM1);
                    ErrorSourceXYM2 errorSourceXYM2 = new ErrorSourceXYM2();
                    ErrorSources.Add(errorSourceXYM2);
                    ErrorSourceXYM3E errorSourceXYM3 = new ErrorSourceXYM3E();
                    errorSourceXYM3.Convergence = Convergence;
                    ErrorSources.Add(errorSourceXYM3);
                    ErrorSourceXYM4E errorSourceXYM4 = new ErrorSourceXYM4E();
                    errorSourceXYM4.Convergence = Convergence;
                    ErrorSources.Add(errorSourceXYM4);
					ErrorSourceXCLA errorSourceXCLA = new ErrorSourceXCLA();
					ErrorSources.Add(errorSourceXCLA);
					ErrorSourceXCLH errorSourceXCLH = new ErrorSourceXCLH();
					ErrorSources.Add(errorSourceXCLH);
				}
                else
                {
                    for (int i = 0; i > ErrorIndices.Length; i++)
                    {
                        if (ErrorIndices[i] == 1)
                        {
                            ErrorSourceDRFR errorSourceDRFR = new ErrorSourceDRFR();
                            ErrorSources.Add(errorSourceDRFR);
                        }
                        if (ErrorIndices[i] == 2)
                        {
                            ErrorSourceDSFS errorSourceDSFS = new ErrorSourceDSFS();
                            ErrorSources.Add(errorSourceDSFS);
                        }
                        if (ErrorIndices[i] == 3)
                        {
                            ErrorSourceDSTG errorSourceDSTG = new ErrorSourceDSTG();
                            ErrorSources.Add(errorSourceDSTG);
                        }
                        if (ErrorIndices[i] == 4)
                        {
                            ErrorSourceABXY_TI1S errorSourceABXY_TI1S = new ErrorSourceABXY_TI1S();
                            errorSourceABXY_TI1S.Dip = Dip;
                            errorSourceABXY_TI1S.Gravity = Gravity;
                            errorSourceABXY_TI1S.Declination = Declination;
                            ErrorSources.Add(errorSourceABXY_TI1S);
                        }
                        if (ErrorIndices[i] == 5)
                        {
                            ErrorSourceABXY_TI2S errorSourceABXY_TI2S = new ErrorSourceABXY_TI2S();
                            errorSourceABXY_TI2S.Dip = Dip;
                            errorSourceABXY_TI2S.Gravity = Gravity;
                            errorSourceABXY_TI2S.Declination = Declination;
                            ErrorSources.Add(errorSourceABXY_TI2S);
                        }
                        if (ErrorIndices[i] == 6)
                        {
                            ErrorSourceABZ errorSourceABZ = new ErrorSourceABZ();
                            errorSourceABZ.Dip = Dip;
                            errorSourceABZ.Gravity = Gravity;
                            errorSourceABZ.Declination = Declination;
                            ErrorSources.Add(errorSourceABZ);
                        }
                        if (ErrorIndices[i] == 7)
                        {
                            ErrorSourceASXY_TI1S errorSourceASXY_TI1S = new ErrorSourceASXY_TI1S();
                            errorSourceASXY_TI1S.Dip = Dip;
                            errorSourceASXY_TI1S.Declination = Declination;
                            ErrorSources.Add(errorSourceASXY_TI1S);
                        }
                        if (ErrorIndices[i] == 8)
                        {
                            ErrorSourceASXY_TI2S errorSourceASXY_TI2S = new ErrorSourceASXY_TI2S();
                            errorSourceASXY_TI2S.Dip = Dip;
                            errorSourceASXY_TI2S.Declination = Declination;
                            ErrorSources.Add(errorSourceASXY_TI2S);
                        }
                        if (ErrorIndices[i] == 9)
                        {
                            ErrorSourceASXY_TI3S errorSourceASXY_TI3S = new ErrorSourceASXY_TI3S();
                            errorSourceASXY_TI3S.Dip = Dip;
                            errorSourceASXY_TI3S.Declination = Declination;
                            ErrorSources.Add(errorSourceASXY_TI3S);
                        }
                        if (ErrorIndices[i] == 10)
                        {
                            ErrorSourceASZ errorSourceASZ = new ErrorSourceASZ();
                            errorSourceASZ.Dip = Dip;
                            errorSourceASZ.Declination = Declination;
                            ErrorSources.Add(errorSourceASZ);
                        }
                        if (ErrorIndices[i] == 11)
                        {
                            ErrorSourceMBXY_TI1 errorSourceMBXY_TI1 = new ErrorSourceMBXY_TI1();
                            errorSourceMBXY_TI1.Dip = Dip;
                            errorSourceMBXY_TI1.Declination = Declination;
                            errorSourceMBXY_TI1.BField = BField;
                            ErrorSources.Add(errorSourceMBXY_TI1);
                        }
                        if (ErrorIndices[i] == 12)
                        {
                            ErrorSourceMBXY_TI2 ErrorSourceMBXY_TI2 = new ErrorSourceMBXY_TI2();
                            ErrorSourceMBXY_TI2.Dip = Dip;
                            ErrorSourceMBXY_TI2.Declination = Declination;
                            ErrorSourceMBXY_TI2.BField = BField;
                            ErrorSources.Add(ErrorSourceMBXY_TI2);
                        }
                        if (ErrorIndices[i] == 13)
                        {
                            ErrorSourceMBZ errorSourceMBZ = new ErrorSourceMBZ();
                            errorSourceMBZ.Dip = Dip;
                            errorSourceMBZ.Declination = Declination;
                            errorSourceMBZ.BField = BField;
                            ErrorSources.Add(errorSourceMBZ);
                        }
                        if (ErrorIndices[i] == 14)
                        {
                            ErrorSourceMSXY_TI1 errorSourceMSXY_TI1 = new ErrorSourceMSXY_TI1();
                            errorSourceMSXY_TI1.Dip = Dip;
                            errorSourceMSXY_TI1.Declination = Declination;
                            ErrorSources.Add(errorSourceMSXY_TI1);
                        }
                        if (ErrorIndices[i] == 15)
                        {
                            ErrorSourceMSXY_TI2 errorSourceMSXY_TI2 = new ErrorSourceMSXY_TI2();
                            errorSourceMSXY_TI2.Dip = Dip;
                            errorSourceMSXY_TI2.Declination = Declination;
                            ErrorSources.Add(errorSourceMSXY_TI2);
                        }
                        if (ErrorIndices[i] == 16)
                        {
                            ErrorSourceMSXY_TI3 errorSourceMSXY_TI3 = new ErrorSourceMSXY_TI3();
                            errorSourceMSXY_TI3.Dip = Dip;
                            errorSourceMSXY_TI3.Declination = Declination;
                            ErrorSources.Add(errorSourceMSXY_TI3);
                        }
                        if (ErrorIndices[i] == 17)
                        {
                            ErrorSourceMSZ errorSourceMSZ = new ErrorSourceMSZ();
                            errorSourceMSZ.Dip = Dip;
                            errorSourceMSZ.Declination = Declination;
                            ErrorSources.Add(errorSourceMSZ);
                        }
                        if (ErrorIndices[i] == 18)
                        {
                            ErrorSourceDEC_U errorSourceDEC_U = new ErrorSourceDEC_U();
                            ErrorSources.Add(errorSourceDEC_U);
                        }
                        if (ErrorIndices[i] == 19)
                        {
                            ErrorSourceDEC_OS errorSourceDEC_OS = new ErrorSourceDEC_OS();
                            ErrorSources.Add(errorSourceDEC_OS);
                        }
                        if (ErrorIndices[i] == 20)
                        {
                            ErrorSourceDEC_OH errorSourceDEC_OH = new ErrorSourceDEC_OH();
                            ErrorSources.Add(errorSourceDEC_OH);
                        }
                        if (ErrorIndices[i] == 21)
                        {
                            ErrorSourceDEC_OI errorSourceDEC_OI = new ErrorSourceDEC_OI();
                            ErrorSources.Add(errorSourceDEC_OI);
                        }
                        if (ErrorIndices[i] == 22)
                        {
                            ErrorSourceDECR errorSourceDECR = new ErrorSourceDECR();
                            ErrorSources.Add(errorSourceDECR);
                        }
                        if (ErrorIndices[i] == 23)
                        {
                            ErrorSourceDBH_U errorSourceDBH_U = new ErrorSourceDBH_U();
                            errorSourceDBH_U.Dip = Dip;
                            errorSourceDBH_U.BField = BField;
                            ErrorSources.Add(errorSourceDBH_U);
                        }
                        if (ErrorIndices[i] == 24)
                        {
                            ErrorSourceDBH_OS errorSourceDBH_OS = new ErrorSourceDBH_OS();
                            errorSourceDBH_OS.Dip = Dip;
                            errorSourceDBH_OS.BField = BField;
                            ErrorSources.Add(errorSourceDBH_OS);
                        }
                        if (ErrorIndices[i] == 25)
                        {
                            ErrorSourceDBH_OH errorSourceDBH_OH = new ErrorSourceDBH_OH();
                            errorSourceDBH_OH.Dip = Dip;
                            errorSourceDBH_OH.BField = BField;
                            ErrorSources.Add(errorSourceDBH_OH);
                        }
                        if (ErrorIndices[i] == 26)
                        {
                            ErrorSourceDBH_OI errorSourceDBH_OI = new ErrorSourceDBH_OI();
                            errorSourceDBH_OI.Dip = Dip;
                            errorSourceDBH_OI.BField = BField;
                            ErrorSources.Add(errorSourceDBH_OI);
                        }
                        if (ErrorIndices[i] == 27)
                        {
                            ErrorSourceDBHR errorSourceDBHR = new ErrorSourceDBHR();
                            errorSourceDBHR.Dip = Dip;
                            errorSourceDBHR.BField = BField;
                            ErrorSources.Add(errorSourceDBHR);
                        }
                        if (ErrorIndices[i] == 28)
                        {
                            ErrorSourceAMIL errorSourceAMIL = new ErrorSourceAMIL();
                            errorSourceAMIL.Dip = Dip;
                            errorSourceAMIL.BField = BField;
                            errorSourceAMIL.Declination = Declination;
                            ErrorSources.Add(errorSourceAMIL);
                        }
                        if (ErrorIndices[i] == 29)
                        {
                            ErrorSourceSAGE errorSourceSAGE = new ErrorSourceSAGE();
                            ErrorSources.Add(errorSourceSAGE);
                        }
                        if (ErrorIndices[i] == 30)
                        {
                            ErrorSourceXYM1 errorSourceXYM1 = new ErrorSourceXYM1();
                            ErrorSources.Add(errorSourceXYM1);
                        }
                        if (ErrorIndices[i] == 31)
                        {
                            ErrorSourceXYM2 errorSourceXYM2 = new ErrorSourceXYM2();
                            ErrorSources.Add(errorSourceXYM2);
                        }
                        if (ErrorIndices[i] == 32)
                        {
                            ErrorSourceXYM3E errorSourceXYM3 = new ErrorSourceXYM3E();
                            errorSourceXYM3.Convergence = Convergence;
                            ErrorSources.Add(errorSourceXYM3);
                        }
                        if (ErrorIndices[i] == 33)
                        {
                            ErrorSourceXYM4E errorSourceXYM4 = new ErrorSourceXYM4E();
                            errorSourceXYM4.Convergence = Convergence;
                            ErrorSources.Add(errorSourceXYM4);
                        }
                        if (ErrorIndices[i] == 34)
                        {
                            ErrorSourceXCLA errorSourceXCLA = new ErrorSourceXCLA();
                            ErrorSources.Add(errorSourceXCLA);
                        }
                        if (ErrorIndices[i] == 35)
                        {
                            ErrorSourceXCLH errorSourceXCLH = new ErrorSourceXCLH();
                            ErrorSources.Add(errorSourceXCLH);
                        }

                    }
                }
            }
            #endregion
            #region Calculations from previous survey station are used
            if (ISCWSAErrorDataPrev == null || ISCWSAErrorDataPrev.Count == 0)
            {
                ISCWSAErrorDataPrev = new List<ISCWSAErrorData>();
                for (int i = 0; i < ErrorSources.Count; i++)
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
            CalculateAllCovariance(surveyStation, surveyStationPrev, surveyStationNext, drdp, drdpNext, ErrorSources, null, c);
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
        /// Calculate Total Covariance matrix for all error sources for a survey station
        /// </summary>
        /// <param name="surveyStation"></param>
        /// <param name="nextStation"></param>
        /// <returns></returns>
        public List<ISCWSAErrorData> CalculateAllCovariance(SurveyStation surveyStation, SurveyStation surveyStationPrev, SurveyStation surveyStationNext, double[,]drdp, double[,]drdpNext, List<IErrorSource> errorSources, List<ISCWSAErrorData> iSCWSAErrorData, int c)
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

            for (int i = 0; i < errorSources.Count; i++)
            {
                sigmaerandom = ISCWSAErrorDataTmp[i].SigmaErrorRandom;
                bool singular = false;                
               
                double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
                double? depth = errorSources[i].FunctionDepth(surveyStation.MD, (double)surveyStation.Z); //Depth
                dpde[0] = (double)depth;
                double? inclination = errorSources[i].FunctionInc((double)surveyStation.Incl, (double)surveyStation.Az); //Inclination
                dpde[1] = (double)inclination;
                double? azimuth = errorSources[i].FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az); //Azimuth
                if (azimuth != null)
                {
                    dpde[2] = (double)azimuth;
                }
                double magnitude = errorSources[i].Magnitude;
                if (errorSources[i].SingularIssues && (depth == null || inclination == null || azimuth == null))
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
                    if(errorSources[i] is ErrorSourceXCLA)
					{
                        double azT = (double)surveyStation.Az + Convergence;
                        double azTPrev = (double)surveyStationPrev.Az + Convergence;
                        double mod = (azT - azTPrev + Math.PI) % (2 * Math.PI);
                        double val1 = mod - Math.PI;
                        double val2 = 0;
                        if(surveyStationPrev.Incl >= 0.0001 * Math.PI / 180.0)
						{
                            val2 = val1;
						}
                        double val3 = Math.Abs(Math.Sin((double)surveyStation.Incl)*val2);
                        double defaultTortuosity = 0.000572615; //[rad/m]
                        double val4 = Math.Max(val3, defaultTortuosity * (surveyStation.MD - surveyStationPrev.MD));
                        double val5 = magnitude * (surveyStation.MD - surveyStationPrev.MD) * val4;
                        e[0] = val5 * (-Math.Sin(azT)); 
                        e[1] = val5 * (Math.Cos(azT));
                        e[2] = 0.0;
                        eStar[0] = e[0];
                        eStar[1] = e[1];
                        eStar[2] = e[2];
                    }
                    else if (errorSources[i] is ErrorSourceXCLH)
                    {
                        double azT = (double)surveyStation.Az + Convergence;
                        double azTPrev = (double)surveyStationPrev.Az + Convergence;
                        //=Model!$W$37*(Wellpath!$K4-Wellpath!$K3)*MAX(ABS(Wellpath!$L4-Wellpath!$L3);Model!$B$24*(Wellpath!$K4-Wellpath!$K3))*COS(Wellpath!$L4)*COS(Wellpath!$M4)
                        double mod = (azT - azTPrev + Math.PI) % (2 * Math.PI);
                        double val1 = mod - Math.PI;
                        double val2 = 0;
                        if (surveyStationPrev.Incl >= 0.0001 * Math.PI / 180.0)
                        {
                            val2 = val1;
                        }
                        double val3 = Math.Abs((double)surveyStation.Incl- (double)surveyStationPrev.Incl);
                        double defaultTortuosity = 0.000572615; //[rad/m]
                        double val4 = Math.Max(val3, defaultTortuosity * (surveyStation.MD - surveyStationPrev.MD));
                        double val5 = magnitude * (surveyStation.MD - surveyStationPrev.MD) * val4;
                        e[0] = val5 * Math.Cos((double)surveyStation.Incl) * Math.Cos(azT);
                        e[1] = val5 * Math.Cos((double)surveyStation.Incl) * Math.Sin(azT);
                        e[2] = val5 * (-Math.Sin((double)surveyStation.Incl));
                        eStar[0] = e[0];
                        eStar[1] = e[1];
                        eStar[2] = e[2];
                    }
                    else if (errorSources[i].SingularIssues && singular)
                    {
                        if (c == 1)
                        {
                            e[0] = magnitude * (surveyStationNext.MD + surveyStation.MD - 2 * surveyStationPrev.MD) / 2 * errorSources[i].FunctionSingularityNorth((double)surveyStation.Az);
                            e[1] = magnitude * (surveyStationNext.MD + surveyStation.MD - 2 * surveyStationPrev.MD) / 2 * errorSources[i].FunctionSingularityEast((double)surveyStation.Az);
                            e[2] = 0.0;
                            eStar[0] = magnitude * (surveyStation.MD - surveyStationPrev.MD) * errorSources[i].FunctionSingularityNorth((double)surveyStation.Az);
                            eStar[1] = magnitude * (surveyStation.MD - surveyStationPrev.MD) * errorSources[i].FunctionSingularityEast((double)surveyStation.Az);
                            eStar[2] = 0.0;
                        }
                        else
                        {
                            e[0] = magnitude * (surveyStationNext.MD - surveyStationPrev.MD) / 2 * errorSources[i].FunctionSingularityNorth((double)surveyStation.Az);
                            e[1] = magnitude * (surveyStationNext.MD - surveyStationPrev.MD) / 2 * errorSources[i].FunctionSingularityEast((double)surveyStation.Az);
                            e[2] = 0.0;
                            eStar[0] = magnitude * (surveyStation.MD - surveyStationPrev.MD) / 2 * errorSources[i].FunctionSingularityNorth((double)surveyStation.Az);
                            eStar[1] = magnitude * (surveyStation.MD - surveyStationPrev.MD) / 2 * errorSources[i].FunctionSingularityEast((double)surveyStation.Az);
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
                if (errorSources[i].IsRandom)
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

        #region Not in use
        ///// <summary>
        ///// Calculate Covariance matrix
        ///// </summary>
        ///// <param name="surveyStation"></param>
        ///// <param name="previousStation"></param>
        ///// <returns></returns>
        //public void CalculateCovariances(SurveyList surveyList)
        //{
        //    List<double[,]> drdps = new List<double[,]>();
        //    List<double[,]> drdpNexts = new List<double[,]>();
        //    ISCWSA_MWDSurveyStationUncertainty iscwsaSurveyStatoinUncertainty = (ISCWSA_MWDSurveyStationUncertainty)surveyList[0].Uncertainty;
        //    if (surveyList.Count > 1)
        //    {
        //        for (int i = 0; i < surveyList.Count; i++)
        //        {



        //            double[,] drdp = new double[3, 3];
        //            if (i == 0)
        //            {
        //                for (int j = 0; j < 3; j++)
        //                {
        //                    for (int k = 0; k < 3; k++)
        //                    {
        //                        drdp[j, k] = 0.0;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                drdp = CalculateDisplacementMatrix(surveyList[i], surveyList[i - 1], i);
        //            }
        //            drdps.Add(drdp);
        //            double[,] drdpNext = new double[3, 3];
        //            if (i < surveyList.Count - 1)
        //            {
        //                drdpNext = CalculateDisplacemenNexttMatrix(surveyList[i], surveyList[i + 1], i);
        //            }
        //            else
        //            {
        //                SurveyStation surveySt = new SurveyStation();
        //                surveySt.X = 0.0;
        //                surveySt.Y = 0.0;
        //                surveySt.Incl = 0.0;
        //                surveySt.Az = 0.0;
        //                drdpNext = CalculateDisplacemenNexttMatrix(surveyList[i], surveySt, i);
        //            }
        //            drdpNexts.Add(drdpNext);
        //        }
        //    }
        //    for (int i = 0; i < surveyList.Count; i++)
        //    {
        //        for (int j = 0; j < 3; j++)
        //        {
        //            for (int k = 0; k < 3; k++)
        //            {
        //                surveyList[i].Uncertainty.Covariance[j, k] = 0.0;
        //            }
        //        }
        //    }

        //    if (ErrorIndices != null)
        //    {

        //    }
        //    else
        //    {
        //        ErrorSourceDRFR errorSourceDRFR = new ErrorSourceDRFR();
        //        CalculateRandomCovariance(surveyList, drdps, drdpNexts, errorSourceDRFR);
        //        ErrorSourceDSFS errorSourceDSFS = new ErrorSourceDSFS();
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDSFS);
        //        ErrorSourceDSTG errorSourceDSTG = new ErrorSourceDSTG();
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDSTG);
        //        ErrorSourceABXY_TI1S errorSourceABXY_TI1S = new ErrorSourceABXY_TI1S();
        //        errorSourceABXY_TI1S.Dip = Dip;
        //        errorSourceABXY_TI1S.Gravity = Gravity;
        //        errorSourceABXY_TI1S.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceABXY_TI1S);
        //        ErrorSourceABXY_TI2S errorSourceABXY_TI2S = new ErrorSourceABXY_TI2S();
        //        errorSourceABXY_TI2S.Dip = Dip;
        //        errorSourceABXY_TI2S.Gravity = Gravity;
        //        errorSourceABXY_TI2S.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceABXY_TI2S);
        //        ErrorSourceABZ errorSourceABZ = new ErrorSourceABZ();
        //        errorSourceABZ.Dip = Dip;
        //        errorSourceABZ.Gravity = Gravity;
        //        errorSourceABZ.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceABZ);
        //        ErrorSourceASXY_TI1S errorSourceASXY_TI1S = new ErrorSourceASXY_TI1S();
        //        errorSourceASXY_TI1S.Dip = Dip;
        //        errorSourceASXY_TI1S.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceASXY_TI1S);
        //        ErrorSourceASXY_TI2S errorSourceASXY_TI2S = new ErrorSourceASXY_TI2S();
        //        errorSourceASXY_TI2S.Dip = Dip;
        //        errorSourceASXY_TI2S.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceASXY_TI2S);
        //        ErrorSourceASXY_TI3S errorSourceASXY_TI3S = new ErrorSourceASXY_TI3S();
        //        errorSourceASXY_TI3S.Dip = Dip;
        //        errorSourceASXY_TI3S.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceASXY_TI3S);
        //        ErrorSourceASZ errorSourceASZ = new ErrorSourceASZ();
        //        errorSourceASZ.Dip = Dip;
        //        errorSourceASZ.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceASZ);
        //        ErrorSourceMBXY_TI1 errorSourceMBXY_TI1 = new ErrorSourceMBXY_TI1();
        //        errorSourceMBXY_TI1.Dip = Dip;
        //        errorSourceMBXY_TI1.Declination = Declination;
        //        errorSourceMBXY_TI1.BField = BField;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceMBXY_TI1);
        //        ErrorSourceMBXY_TI2 ErrorSourceMBXY_TI2 = new ErrorSourceMBXY_TI2();
        //        ErrorSourceMBXY_TI2.Dip = Dip;
        //        ErrorSourceMBXY_TI2.Declination = Declination;
        //        ErrorSourceMBXY_TI2.BField = BField;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, ErrorSourceMBXY_TI2);
        //        ErrorSourceMBZ errorSourceMBZ = new ErrorSourceMBZ();
        //        errorSourceMBZ.Dip = Dip;
        //        errorSourceMBZ.Declination = Declination;
        //        errorSourceMBZ.BField = BField;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceMBZ);
        //        ErrorSourceMSXY_TI1 errorSourceMSXY_TI1 = new ErrorSourceMSXY_TI1();
        //        errorSourceMSXY_TI1.Dip = Dip;
        //        errorSourceMSXY_TI1.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceMSXY_TI1);
        //        ErrorSourceMSXY_TI2 errorSourceMSXY_TI2 = new ErrorSourceMSXY_TI2();
        //        errorSourceMSXY_TI2.Dip = Dip;
        //        errorSourceMSXY_TI2.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceMSXY_TI2);
        //        ErrorSourceMSXY_TI3 errorSourceMSXY_TI3 = new ErrorSourceMSXY_TI3();
        //        errorSourceMSXY_TI3.Dip = Dip;
        //        errorSourceMSXY_TI3.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceMSXY_TI3);
        //        ErrorSourceMSZ errorSourceMSZ = new ErrorSourceMSZ();
        //        errorSourceMSZ.Dip = Dip;
        //        errorSourceMSZ.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceMSZ);
        //        ErrorSourceDEC_U errorSourceDEC_U = new ErrorSourceDEC_U();
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDEC_U);
        //        ErrorSourceDEC_OS errorSourceDEC_OS = new ErrorSourceDEC_OS();
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDEC_OS);
        //        ErrorSourceDEC_OH errorSourceDEC_OH = new ErrorSourceDEC_OH();
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDEC_OH);
        //        ErrorSourceDEC_OI errorSourceDEC_OI = new ErrorSourceDEC_OI();
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDEC_OI);
        //        ErrorSourceDECR errorSourceDECR = new ErrorSourceDECR();
        //        CalculateRandomCovariance(surveyList, drdps, drdpNexts, errorSourceDECR);
        //        ErrorSourceDBH_U errorSourceDBH_U = new ErrorSourceDBH_U();
        //        errorSourceDBH_U.Dip = Dip;
        //        errorSourceDBH_U.BField = BField;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDBH_U);
        //        ErrorSourceDBH_OS errorSourceDBH_OS = new ErrorSourceDBH_OS();
        //        errorSourceDBH_OS.Dip = Dip;
        //        errorSourceDBH_OS.BField = BField;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDBH_OS);
        //        ErrorSourceDBH_OH errorSourceDBH_OH = new ErrorSourceDBH_OH();
        //        errorSourceDBH_OH.Dip = Dip;
        //        errorSourceDBH_OH.BField = BField;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDBH_OH);
        //        ErrorSourceDBH_OI errorSourceDBH_OI = new ErrorSourceDBH_OI();
        //        errorSourceDBH_OI.Dip = Dip;
        //        errorSourceDBH_OI.BField = BField;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceDBH_OI);
        //        ErrorSourceDBHR errorSourceDBHR = new ErrorSourceDBHR();
        //        errorSourceDBHR.Dip = Dip;
        //        errorSourceDBHR.BField = BField;
        //        CalculateRandomCovariance(surveyList, drdps, drdpNexts, errorSourceDBHR);
        //        ErrorSourceAMIL errorSourceAMIL = new ErrorSourceAMIL();
        //        errorSourceAMIL.Dip = Dip;
        //        errorSourceAMIL.BField = BField;
        //        errorSourceAMIL.Declination = Declination;
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceAMIL);
        //        ErrorSourceSAGE errorSourceSAGE = new ErrorSourceSAGE();
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceSAGE);
        //        ErrorSourceXYM1 errorSourceXYM1 = new ErrorSourceXYM1();
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceXYM1);
        //        ErrorSourceXYM2 errorSourceXYM2 = new ErrorSourceXYM2();
        //        CalculateSystematicCovariance(surveyList, drdps, drdpNexts, errorSourceXYM2);
        //        ErrorSourceXYM3E errorSourceXYM3 = new ErrorSourceXYM3E();
        //        errorSourceXYM3.Convergence = Convergence;
        //        CalculateRandomCovariance(surveyList, drdps, drdpNexts, errorSourceXYM3);
        //        ErrorSourceXYM4E errorSourceXYM4 = new ErrorSourceXYM4E();
        //        errorSourceXYM4.Convergence = Convergence;
        //        CalculateRandomCovariance(surveyList, drdps, drdpNexts, errorSourceXYM4);
        //        //ErrorSourceXCL errorSourceXCL = new ErrorSourceXCL();
        //        //CalculateRandomCovariance(surveyList, drdps, drdpNexts, errorSourceXCL);
        //    }

        //}
        ///// <summary>
        ///// Error due to the DRFR error source at the kth survey station in the lth survey leg
        ///// </summary>
        ///// <param name="surveyStation"></param>
        ///// <param name="nextStation"></param>
        ///// <returns></returns>
        //public double[,] CalculateRandomCovariance(SurveyList surveyStations, List<double[,]> drdps, List<double[,]> drdpNexts, IErrorSource errorSource)
        //{
        //    //ref https://www.iscwsa.net/error-model-documentation/
        //    double[,] sigmaerandom = new double[3, 3];

        //    for (int i = 0; i < sigmaerandom.GetLength(0); i++)
        //    {
        //        for (int j = 0; j < sigmaerandom.GetLength(1); j++)
        //        {
        //            sigmaerandom[i, j] = 0.0;
        //        }
        //    }

        //    for (int i = 0; i < surveyStations.Count; i++)
        //    {
        //        bool singular = false;
        //        SurveyStation surveyStation = surveyStations[i];
        //        double[,] drdp = drdps[i];
        //        double[,] drdpNext = drdpNexts[i];
        //        double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
        //        double? depth = errorSource.FunctionDepth(surveyStation.MD, (double)surveyStation.Z); //Depth
        //        dpde[0] = (double)depth;
        //        double? inclination = errorSource.FunctionInc((double)surveyStation.Incl, (double)surveyStation.Az); //Inclination
        //        dpde[1] = (double)inclination;
        //        double? azimuth = errorSource.FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az); //Azimuth
        //        if (azimuth != null)
        //        {
        //            dpde[2] = (double)azimuth;
        //        }
        //        double magnitude = errorSource.Magnitude;
        //        if (errorSource.SingularIssues && (depth == null || inclination == null || azimuth == null))
        //        {
        //            singular = true;
        //        }
        //        double[] e = new double[3]; //the error due to the ith error source at the kth survey station in the lth survey leg
        //        double[] eStar = new double[3]; //the error due to the ith error source at the kth survey stations in the lth survey leg, where k is the last survey of interest
        //        if (i == 0)
        //        {
        //            e[0] = 0;
        //            e[1] = 0;
        //            e[2] = 0;
        //            eStar[0] = 0;
        //            eStar[1] = 0;
        //            eStar[2] = 0;
        //        }
        //        else
        //        {
        //            if (errorSource.SingularIssues && singular)
        //            {
        //                if (i == 1)
        //                {
        //                    e[0] = magnitude * (surveyStations[i + 1].MD + surveyStations[i].MD - 2 * surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityNorth((double)surveyStation.Az);
        //                    e[1] = magnitude * (surveyStations[i + 1].MD + surveyStations[i].MD - 2 * surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityEast((double)surveyStation.Az);
        //                    e[2] = 0.0;
        //                    eStar[0] = magnitude * (surveyStations[i].MD - surveyStations[i - 1].MD) * errorSource.FunctionSingularityNorth((double)surveyStation.Az);
        //                    eStar[1] = magnitude * (surveyStations[i].MD - surveyStations[i - 1].MD) * errorSource.FunctionSingularityEast((double)surveyStation.Az);
        //                    eStar[2] = 0.0;
        //                }
        //                else
        //                {
        //                    e[0] = magnitude * (surveyStations[i + 1].MD - surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityNorth((double)surveyStation.Az);
        //                    e[1] = magnitude * (surveyStations[i + 1].MD - surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityEast((double)surveyStation.Az);
        //                    e[2] = 0.0;
        //                    eStar[0] = magnitude * (surveyStations[i].MD - surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityNorth((double)surveyStation.Az);
        //                    eStar[1] = magnitude * (surveyStations[i].MD - surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityEast((double)surveyStation.Az);
        //                    eStar[2] = 0.0;
        //                }
        //            }
        //            else
        //            {
        //                e[0] = magnitude * ((drdp[0, 0] + drdpNext[0, 0]) * dpde[0] + (drdp[0, 1] + drdpNext[0, 1]) * dpde[1] + (drdp[0, 2] + drdpNext[0, 2]) * dpde[2]);
        //                e[1] = magnitude * ((drdp[1, 0] + drdpNext[1, 0]) * dpde[0] + (drdp[1, 1] + drdpNext[1, 1]) * dpde[1] + (drdp[1, 2] + drdpNext[1, 2]) * dpde[2]);
        //                e[2] = magnitude * ((drdp[2, 0] + drdpNext[2, 0]) * dpde[0] + (drdp[2, 1] + drdpNext[2, 1]) * dpde[1] + (drdp[2, 2] + drdpNext[2, 2]) * dpde[2]);
        //                eStar[0] = magnitude * ((drdp[0, 0]) * dpde[0] + (drdp[0, 1]) * dpde[1] + (drdp[0, 2]) * dpde[2]);
        //                eStar[1] = magnitude * ((drdp[1, 0]) * dpde[0] + (drdp[1, 1]) * dpde[1] + (drdp[1, 2]) * dpde[2]);
        //                eStar[2] = magnitude * ((drdp[2, 0]) * dpde[0] + (drdp[2, 1]) * dpde[1] + (drdp[2, 2]) * dpde[2]);
        //            }
        //        }

        //        double[,] CovarianceRan = new double[3, 3];
        //        if (i == 0)
        //        {
        //            CovarianceRan[0, 0] = eStar[0] * eStar[0];
        //            CovarianceRan[1, 1] = eStar[1] * eStar[1];
        //            CovarianceRan[2, 2] = eStar[2] * eStar[2];
        //            CovarianceRan[1, 0] = eStar[0] * eStar[1];
        //            CovarianceRan[0, 1] = CovarianceRan[1, 0];
        //            CovarianceRan[2, 0] = eStar[0] * eStar[2];
        //            CovarianceRan[0, 2] = CovarianceRan[2, 0];
        //            CovarianceRan[1, 2] = eStar[1] * eStar[2];
        //            CovarianceRan[2, 1] = CovarianceRan[2, 1];
        //        }
        //        else
        //        {
        //            CovarianceRan[0, 0] = sigmaerandom[0, 0] + eStar[0] * eStar[0];
        //            CovarianceRan[1, 1] = sigmaerandom[1, 1] + eStar[1] * eStar[1];
        //            CovarianceRan[2, 2] = sigmaerandom[2, 2] + eStar[2] * eStar[2];
        //            CovarianceRan[1, 0] = eStar[0] * eStar[1] + sigmaerandom[1, 0];
        //            CovarianceRan[0, 1] = CovarianceRan[1, 0];
        //            CovarianceRan[2, 0] = eStar[0] * eStar[2] + sigmaerandom[2, 0];
        //            CovarianceRan[0, 2] = CovarianceRan[2, 0];
        //            CovarianceRan[1, 2] = eStar[1] * eStar[2] + sigmaerandom[1, 2];
        //            CovarianceRan[2, 1] = CovarianceRan[1, 2];
        //        }
        //        for (int j = 0; j < 3; j++)
        //        {
        //            for (int k = 0; k < 3; k++)
        //            {
        //                surveyStations[i].Uncertainty.Covariance[j, k] += CovarianceRan[j, k];
        //            }
        //        }
        //        sigmaerandom[0, 0] = e[0] * e[0] + sigmaerandom[0, 0];
        //        sigmaerandom[1, 1] = e[1] * e[1] + sigmaerandom[1, 1];
        //        sigmaerandom[2, 2] = e[2] * e[2] + sigmaerandom[2, 2];
        //        sigmaerandom[1, 0] = e[0] * e[1] + sigmaerandom[1, 0];
        //        sigmaerandom[0, 1] = sigmaerandom[1, 0];
        //        sigmaerandom[2, 0] = e[0] * e[2] + sigmaerandom[2, 0];
        //        sigmaerandom[0, 2] = sigmaerandom[2, 0];
        //        sigmaerandom[1, 2] = e[1] * e[2] + sigmaerandom[1, 2];
        //        sigmaerandom[2, 1] = sigmaerandom[1, 2];

        //    }
        //    return null;
        //}

        ///// <summary>
        ///// Error due to the Systematic error source at the kth survey station in the lth survey leg
        ///// </summary>
        ///// <param name="surveyStation"></param>
        ///// <param name="nextStation"></param>
        ///// <returns></returns>
        //public double[,] CalculateSystematicCovariance(SurveyList surveyStations, List<double[,]> drdps, List<double[,]> drdpNexts, IErrorSource errorSource)
        //{
        //    //ref https://www.iscwsa.net/error-model-documentation/
        //    List<double[]> eAll = new List<double[]>();
        //    List<double[]> eStarAll = new List<double[]>();
        //    double eNSum = 0.0;
        //    double eESum = 0.0;
        //    double eVSum = 0.0;
        //    for (int i = 0; i < surveyStations.Count; i++)
        //    {
        //        bool singular = false;
        //        double[] sigmaesystematic = new double[3];
        //        SurveyStation surveyStation = surveyStations[i];
        //        double[,] drdp = drdps[i];
        //        double[,] drdpNext = drdpNexts[i];
        //        double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
        //        double? depth = errorSource.FunctionDepth(surveyStation.MD, (double)surveyStation.Z); //Depth
        //        dpde[0] = (double)depth;
        //        double? inclination = errorSource.FunctionInc((double)surveyStation.Incl, (double)surveyStation.Az); //Inclination
        //        dpde[1] = (double)inclination;
        //        double? azimuth = errorSource.FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az); //Azimuth
        //        if (azimuth != null)
        //        {
        //            dpde[2] = (double)azimuth;
        //        }
        //        double magnitude = errorSource.Magnitude;// (double)surveyInstrument.MSZ;
        //        if (errorSource.SingularIssues && (depth == null || inclination == null || azimuth == null))
        //        {
        //            singular = true;
        //        }
        //        double[] e = new double[3]; //the error due to the ith error source at the kth survey station in the lth survey leg
        //        double[] eStar = new double[3]; //the error due to the ith error source at the kth survey stations in the lth survey leg, where k is the last survey of interest
        //        if (i == 0)
        //        {
        //            e[0] = 0;
        //            e[1] = 0;
        //            e[2] = 0;
        //            eStar[0] = 0;
        //            eStar[1] = 0;
        //            eStar[2] = 0;
        //        }
        //        else
        //        {
        //            if (errorSource.SingularIssues && singular)
        //            {
        //                if (i == 1)
        //                {
        //                    e[0] = magnitude * (surveyStations[i + 1].MD + surveyStations[i].MD - 2 * surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityNorth((double)surveyStation.Az);
        //                    e[1] = magnitude * (surveyStations[i + 1].MD + surveyStations[i].MD - 2 * surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityEast((double)surveyStation.Az);
        //                    e[2] = 0.0;
        //                    eStar[0] = magnitude * (surveyStations[i].MD - surveyStations[i - 1].MD) * errorSource.FunctionSingularityNorth((double)surveyStation.Az);
        //                    eStar[1] = magnitude * (surveyStations[i].MD - surveyStations[i - 1].MD) * errorSource.FunctionSingularityEast((double)surveyStation.Az);
        //                    eStar[2] = 0.0;
        //                }
        //                else
        //                {
        //                    e[0] = magnitude * (surveyStations[i + 1].MD - surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityNorth((double)surveyStation.Az);
        //                    e[1] = magnitude * (surveyStations[i + 1].MD - surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityEast((double)surveyStation.Az);
        //                    e[2] = 0.0;
        //                    eStar[0] = magnitude * (surveyStations[i].MD - surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityNorth((double)surveyStation.Az);
        //                    eStar[1] = magnitude * (surveyStations[i].MD - surveyStations[i - 1].MD) / 2 * errorSource.FunctionSingularityEast((double)surveyStation.Az);
        //                    eStar[2] = 0.0;
        //                }
        //            }
        //            else
        //            {
        //                e[0] = magnitude * ((drdp[0, 0] + drdpNext[0, 0]) * dpde[0] + (drdp[0, 1] + drdpNext[0, 1]) * dpde[1] + (drdp[0, 2] + drdpNext[0, 2]) * dpde[2]);
        //                e[1] = magnitude * ((drdp[1, 0] + drdpNext[1, 0]) * dpde[0] + (drdp[1, 1] + drdpNext[1, 1]) * dpde[1] + (drdp[1, 2] + drdpNext[1, 2]) * dpde[2]);
        //                e[2] = magnitude * ((drdp[2, 0] + drdpNext[2, 0]) * dpde[0] + (drdp[2, 1] + drdpNext[2, 1]) * dpde[1] + (drdp[2, 2] + drdpNext[2, 2]) * dpde[2]);
        //                eStar[0] = magnitude * ((drdp[0, 0]) * dpde[0] + (drdp[0, 1]) * dpde[1] + (drdp[0, 2]) * dpde[2]);
        //                eStar[1] = magnitude * ((drdp[1, 0]) * dpde[0] + (drdp[1, 1]) * dpde[1] + (drdp[1, 2]) * dpde[2]);
        //                eStar[2] = magnitude * ((drdp[2, 0]) * dpde[0] + (drdp[2, 1]) * dpde[1] + (drdp[2, 2]) * dpde[2]);
        //            }

        //        }

        //        sigmaesystematic[0] = eNSum + eStar[0];
        //        sigmaesystematic[1] = eESum + eStar[1];
        //        sigmaesystematic[2] = eVSum + eStar[2];

        //        double[,] CovarianceSys = new double[3, 3];
        //        if (i == 0)
        //        {
        //            CovarianceSys[0, 0] = eStar[0] * eStar[0];
        //            CovarianceSys[1, 1] = eStar[1] * eStar[1];
        //            CovarianceSys[2, 2] = eStar[2] * eStar[2];
        //            CovarianceSys[1, 0] = eStar[0] * eStar[1];
        //            CovarianceSys[0, 1] = CovarianceSys[1, 0];
        //            CovarianceSys[2, 0] = eStar[0] * eStar[2];
        //            CovarianceSys[0, 2] = CovarianceSys[2, 0];
        //            CovarianceSys[1, 2] = eStar[1] * eStar[2];
        //            CovarianceSys[2, 1] = CovarianceSys[2, 1];
        //        }
        //        else
        //        {
        //            CovarianceSys[0, 0] = sigmaesystematic[0] * sigmaesystematic[0];
        //            CovarianceSys[1, 1] = sigmaesystematic[1] * sigmaesystematic[1];
        //            CovarianceSys[2, 2] = sigmaesystematic[2] * sigmaesystematic[2];
        //            CovarianceSys[1, 0] = sigmaesystematic[1] * sigmaesystematic[0];
        //            CovarianceSys[0, 1] = CovarianceSys[1, 0];
        //            CovarianceSys[2, 0] = sigmaesystematic[2] * sigmaesystematic[0];
        //            CovarianceSys[0, 2] = CovarianceSys[2, 0];
        //            CovarianceSys[1, 2] = sigmaesystematic[1] * sigmaesystematic[2];
        //            CovarianceSys[2, 1] = CovarianceSys[1, 2];
        //        }
        //        for (int j = 0; j < 3; j++)
        //        {
        //            for (int k = 0; k < 3; k++)
        //            {
        //                Covariance[j, k] += CovarianceSys[j, k];
        //            }
        //        }
        //        eAll.Add(e);
        //        eStarAll.Add(eStar);
        //        eNSum += e[0];
        //        eESum += e[1];
        //        eVSum += e[2];
        //    }
        //    return null;
        //}
        ///// <summary>
        ///// Error due to the DRFR error source at the kth survey station in the lth survey leg
        ///// </summary>
        ///// <param name="surveyStation"></param>
        ///// <param name="nextStation"></param>
        ///// <returns></returns>
        //public double[,] CalculateCovarianceDRFR(double[,] drdp, double[,] drdpNext, SurveyInstrument.Model.SurveyInstrument surveyInstrument, double[,] sigmaerandom, int st)
        //{
        //    //ref https://www.iscwsa.net/error-model-documentation/
        //    double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
        //    dpde[0] = 1; //Depth
        //    dpde[1] = 0; //Inclination
        //    dpde[2] = 0; //Azimuth
        //    double magnitude = (double)surveyInstrument.DRFR;
        //    double[] e = new double[3]; //the error due to the ith error source at the kth survey station in the lth survey leg
        //    double[] eStar = new double[3]; //the error due to the ith error source at the kth survey stations in the lth survey leg, where k is the last survey of interest
        //    if (st == 0)
        //    {
        //        e[0] = 0;
        //        e[1] = 0;
        //        e[2] = 0;
        //        eStar[0] = 0;
        //        eStar[1] = 0;
        //        eStar[2] = 0;
        //    }
        //    else
        //    {
        //        e[0] = magnitude * ((drdp[0, 0] + drdpNext[0, 0]) * dpde[0] + (drdp[0, 1] + drdpNext[0, 1]) * dpde[1] + (drdp[0, 2] + drdpNext[0, 2]) * dpde[2]);
        //        e[1] = magnitude * ((drdp[1, 0] + drdpNext[1, 0]) * dpde[0] + (drdp[1, 1] + drdpNext[1, 1]) * dpde[1] + (drdp[1, 2] + drdpNext[1, 2]) * dpde[2]);
        //        e[2] = magnitude * ((drdp[2, 0] + drdpNext[2, 0]) * dpde[0] + (drdp[2, 1] + drdpNext[2, 1]) * dpde[1] + (drdp[2, 2] + drdpNext[2, 2]) * dpde[2]);
        //        eStar[0] = magnitude * ((drdp[0, 0]) * dpde[0] + (drdp[0, 1]) * dpde[1] + (drdp[0, 2]) * dpde[2]);
        //        eStar[1] = magnitude * ((drdp[1, 0]) * dpde[0] + (drdp[1, 1]) * dpde[1] + (drdp[1, 2]) * dpde[2]);
        //        eStar[2] = magnitude * ((drdp[2, 0]) * dpde[0] + (drdp[2, 1]) * dpde[1] + (drdp[2, 2]) * dpde[2]);
        //    }


        //    double[,] CovarianceDRFR = new double[3, 3];
        //    if (st == 0)
        //    {
        //        CovarianceDRFR[0, 0] = eStar[0] * eStar[0];
        //        CovarianceDRFR[1, 1] = eStar[1] * eStar[1];
        //        CovarianceDRFR[2, 2] = eStar[2] * eStar[2];
        //        CovarianceDRFR[1, 0] = eStar[0] * eStar[1];
        //        CovarianceDRFR[0, 1] = CovarianceDRFR[1, 0];
        //        CovarianceDRFR[2, 0] = eStar[0] * eStar[2];
        //        CovarianceDRFR[0, 2] = CovarianceDRFR[2, 0];
        //        CovarianceDRFR[1, 2] = eStar[1] * eStar[2];
        //        CovarianceDRFR[2, 1] = CovarianceDRFR[2, 1];
        //    }
        //    else
        //    {
        //        CovarianceDRFR[0, 0] = sigmaerandom[0, 0] + eStar[0] * eStar[0];
        //        CovarianceDRFR[1, 1] = sigmaerandom[1, 1] + eStar[1] * eStar[1];
        //        CovarianceDRFR[2, 2] = sigmaerandom[2, 2] + eStar[2] * eStar[2];
        //        CovarianceDRFR[1, 0] = eStar[0] * eStar[1] + sigmaerandom[1, 0];
        //        CovarianceDRFR[0, 1] = CovarianceDRFR[1, 0];
        //        CovarianceDRFR[2, 0] = eStar[0] * eStar[2] + sigmaerandom[2, 0];
        //        CovarianceDRFR[0, 2] = CovarianceDRFR[2, 0];
        //        CovarianceDRFR[1, 2] = eStar[1] * eStar[2] + sigmaerandom[1, 2];
        //        CovarianceDRFR[2, 1] = CovarianceDRFR[1, 2];
        //    }
        //    for (int j = 0; j < 3; j++)
        //    {
        //        for (int k = 0; k < 3; k++)
        //        {
        //            Covariance[j, k] += CovarianceDRFR[j, k];
        //        }
        //    }
        //    sigmaerandom[0, 0] = e[0] * e[0] + sigmaerandom[0, 0];
        //    sigmaerandom[1, 1] = e[1] * e[1] + sigmaerandom[1, 1];
        //    sigmaerandom[2, 2] = e[2] * e[2] + sigmaerandom[2, 2];
        //    sigmaerandom[1, 0] = e[0] * e[1] + sigmaerandom[1, 0];
        //    sigmaerandom[0, 1] = sigmaerandom[1, 0];
        //    sigmaerandom[2, 0] = e[0] * e[2] + sigmaerandom[2, 0];
        //    sigmaerandom[0, 2] = sigmaerandom[2, 0];
        //    sigmaerandom[1, 2] = e[1] * e[2] + sigmaerandom[1, 2];
        //    sigmaerandom[2, 1] = sigmaerandom[1, 2];

        //    return sigmaerandom;
        //}
        ///// <summary>
        ///// Error due to the MWD: Z-Magnetometer Scale Factor error source at the kth survey station in the lth survey leg
        ///// </summary>
        ///// <param name="surveyStation"></param>
        ///// <param name="nextStation"></param>
        ///// <returns></returns>
        //public double[,] CalculateCovarianceMSZ(SurveyList surveyStations, List<double[,]> drdps, List<double[,]> drdpNexts, SurveyInstrument.Model.SurveyInstrument surveyInstrument)
        //{
        //    //ref https://www.iscwsa.net/error-model-documentation/
        //    List<double[]> eAll = new List<double[]>();
        //    List<double[]> eStarAll = new List<double[]>();
        //    double eNSum = 0.0;
        //    double eESum = 0.0;
        //    double eVSum = 0.0;
        //    for (int i = 0; i < surveyStations.Count; i++)
        //    {
        //        double[] sigmaesystematic = new double[3];
        //        SurveyStation surveyStation = surveyStations[i];
        //        double[,] drdp = drdps[i];
        //        double[,] drdpNext = drdpNexts[i];
        //        double sinI = System.Math.Sin((double)surveyStation.Incl);
        //        double cosI = System.Math.Cos((double)surveyStation.Incl);
        //        double sinAm = System.Math.Sin((double)surveyStation.Az - Declination);
        //        double cosAm = System.Math.Cos((double)surveyStation.Az - Declination);
        //        double tanDip = System.Math.Tan(Dip);
        //        double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
        //        dpde[0] = 0; //Depth
        //        dpde[1] = 0; //Inclination
        //        dpde[2] = -(sinI * cosAm + tanDip * cosI) * sinI * sinAm; //Azimuth
        //        double magnitude = (double)surveyInstrument.MSZ;
        //        double[] e = new double[3]; //the error due to the ith error source at the kth survey station in the lth survey leg
        //        double[] eStar = new double[3]; //the error due to the ith error source at the kth survey stations in the lth survey leg, where k is the last survey of interest
        //        if (i == 0)
        //        {
        //            e[0] = 0;
        //            e[1] = 0;
        //            e[2] = 0;
        //            eStar[0] = 0;
        //            eStar[1] = 0;
        //            eStar[2] = 0;
        //        }
        //        else
        //        {
        //            e[0] = magnitude * ((drdp[0, 0] + drdpNext[0, 0]) * dpde[0] + (drdp[0, 1] + drdpNext[0, 1]) * dpde[1] + (drdp[0, 2] + drdpNext[0, 2]) * dpde[2]);
        //            e[1] = magnitude * ((drdp[1, 0] + drdpNext[1, 0]) * dpde[0] + (drdp[1, 1] + drdpNext[1, 1]) * dpde[1] + (drdp[1, 2] + drdpNext[1, 2]) * dpde[2]);
        //            e[2] = magnitude * ((drdp[2, 0] + drdpNext[2, 0]) * dpde[0] + (drdp[2, 1] + drdpNext[2, 1]) * dpde[1] + (drdp[2, 2] + drdpNext[2, 2]) * dpde[2]);
        //            eStar[0] = magnitude * ((drdp[0, 0]) * dpde[0] + (drdp[0, 1]) * dpde[1] + (drdp[0, 2]) * dpde[2]);
        //            eStar[1] = magnitude * ((drdp[1, 0]) * dpde[0] + (drdp[1, 1]) * dpde[1] + (drdp[1, 2]) * dpde[2]);
        //            eStar[2] = magnitude * ((drdp[2, 0]) * dpde[0] + (drdp[2, 1]) * dpde[1] + (drdp[2, 2]) * dpde[2]);
        //        }

        //        sigmaesystematic[0] = eNSum + eStar[0];
        //        sigmaesystematic[1] = eESum + eStar[1];
        //        sigmaesystematic[2] = eVSum + eStar[2];

        //        double[,] CovarianceMSZ = new double[3, 3];
        //        if (i == 0)
        //        {
        //            CovarianceMSZ[0, 0] = eStar[0] * eStar[0];
        //            CovarianceMSZ[1, 1] = eStar[1] * eStar[1];
        //            CovarianceMSZ[2, 2] = eStar[2] * eStar[2];
        //            CovarianceMSZ[1, 0] = eStar[0] * eStar[1];
        //            CovarianceMSZ[0, 1] = CovarianceMSZ[1, 0];
        //            CovarianceMSZ[2, 0] = eStar[0] * eStar[2];
        //            CovarianceMSZ[0, 2] = CovarianceMSZ[2, 0];
        //            CovarianceMSZ[1, 2] = eStar[1] * eStar[2];
        //            CovarianceMSZ[2, 1] = CovarianceMSZ[2, 1];
        //        }
        //        else
        //        {
        //            CovarianceMSZ[0, 0] = sigmaesystematic[0] * sigmaesystematic[0];
        //            CovarianceMSZ[1, 1] = sigmaesystematic[1] * sigmaesystematic[1];
        //            CovarianceMSZ[2, 2] = sigmaesystematic[2] * sigmaesystematic[2];
        //            CovarianceMSZ[1, 0] = sigmaesystematic[1] * sigmaesystematic[0];
        //            CovarianceMSZ[0, 1] = CovarianceMSZ[1, 0];
        //            CovarianceMSZ[2, 0] = sigmaesystematic[2] * sigmaesystematic[0];
        //            CovarianceMSZ[0, 2] = CovarianceMSZ[2, 0];
        //            CovarianceMSZ[1, 2] = sigmaesystematic[1] * sigmaesystematic[2];
        //            CovarianceMSZ[2, 1] = CovarianceMSZ[1, 2];
        //        }
        //        for (int j = 0; j < 3; j++)
        //        {
        //            for (int k = 0; k < 3; k++)
        //            {
        //                Covariance[j, k] += CovarianceMSZ[j, k];
        //            }
        //        }
        //        eAll.Add(e);
        //        eStarAll.Add(eStar);
        //        eNSum += e[0];
        //        eESum += e[1];
        //        eVSum += e[2];
        //    }

        //    return null;
        //}
        #endregion
    }

    public interface IErrorSource
    {
        string ErrorCode
        {
            get;
        }
        int Index { get; }
        bool IsSystematic { get; }
        bool IsRandom { get; }
        bool IsGlobal { get; }
        bool SingularIssues { get; }
        double Magnitude { get; set; }
        double? FunctionDepth(double md, double tvd);
        double? FunctionInc(double incl, double az);
        double? FunctionAz(double incl, double az);
        double FunctionSingularityNorth(double az);
        double FunctionSingularityEast(double az);
        double FunctionSingularityVert();
    }
    /// <summary>
    /// Error due to the Depth: Depth Reference - Random error source
    /// </summary>
    public class ErrorSourceDRFR : IErrorSource
    {
        public ErrorSourceDRFR()
        {

        }
        public string ErrorCode
        {
            get { return "DRFR"; }
        }
        public int Index
        {
            get { return 1; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public bool SingularIssues { get; } = false;
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.35;
        public double? FunctionDepth(double md, double tvd)
        {
            return 1.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0.0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Depth: Depth: Depth Scale Factor - Systematic error source
    /// </summary>
    public class ErrorSourceDSFS : IErrorSource
    {
        public ErrorSourceDSFS()
        {

        }
        public string ErrorCode
        {
            get { return "DSFS"; }
        }
        public int Index
        {
            get { return 2; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.00056;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return md;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0.0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Depth: Depth Stretch - Global error source
    /// </summary>
    public class ErrorSourceDSTG : IErrorSource
    {
        public ErrorSourceDSTG()
        {

        }
        public string ErrorCode
        {
            get { return "DSTG"; }
        }
        public int Index
        {
            get { return 3; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.00000025;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return md * tvd;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0.0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Accelerometer Bias error source
    /// </summary>
    public class ErrorSourceABXY_TI1S : IErrorSource
    {
        public ErrorSourceABXY_TI1S()
        {

        }
        public string ErrorCode
        {
            get { return "ABXY_TI1S"; }
        }
        public int Index
        {
            get { return 4; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.004;
        public bool SingularIssues { get; } = false;
        public double Gfield { get; set; } = 9.80665;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return -System.Math.Cos(incl) / Gfield;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return (tanDip * cosI * sinAm) / Gfield;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Accelerometer Bias error source
    /// </summary>
    public class ErrorSourceABXY_TI2S : IErrorSource
    {
        // NB Singularity when vertical
        public ErrorSourceABXY_TI2S()
        {

        }
        public string ErrorCode
        {
            get { return "ABXY_TI2S"; }
        }
        public int Index
        {
            get { return 5; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.004;

        public bool SingularIssues { get; } = true;
        public double Gfield { get; set; } = 9.80665;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            if (incl < 0.0001 * Math.PI / 180.0)
            {
                return null;
            }
            else
            {
                return (System.Math.Tan(Math.PI / 2 - incl) - tanDip * cosAm) / Gfield;
            }
        }
        public double FunctionSingularityNorth(double az) { return -Math.Sin(az) / Gfield; }
        public double FunctionSingularityEast(double az) { return Math.Cos(az) / Gfield; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Z-Accelerometer Bias error source
    /// </summary>
    public class ErrorSourceABZ : IErrorSource
    {
        public ErrorSourceABZ()
        {

        }
        public string ErrorCode
        {
            get { return "ABZ"; }
        }
        public int Index
        {
            get { return 6; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.004;

        public bool SingularIssues { get; } = false;
        public double Gfield { get; set; } = 9.80665;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return -System.Math.Sin(incl) / Gfield;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return tanDip * sinI * sinAm / Gfield;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Accelerometer Scale Factor error source
    /// </summary>
    public class ErrorSourceASXY_TI1S : IErrorSource
    {
        public ErrorSourceASXY_TI1S()
        {

        }
        public string ErrorCode
        {
            get { return "ASXY_TI1S"; }
        }
        public int Index
        {
            get { return 7; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0005;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return System.Math.Sin(incl) * System.Math.Cos(incl) / Math.Sqrt(2);
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return (-tanDip * sinI * cosI * sinAm) / Math.Sqrt(2); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Accelerometer Scale Factor error source
    /// </summary>
    public class ErrorSourceASXY_TI2S : IErrorSource
    {
        public ErrorSourceASXY_TI2S()
        {

        }
        public string ErrorCode
        {
            get { return "ASXY_TI2S"; }
        }
        public int Index
        {
            get { return 8; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0005;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return System.Math.Sin(incl) * System.Math.Cos(incl) / 2;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return (-tanDip * sinI * cosI * sinAm) / 2; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Accelerometer Scale Factor error source
    /// </summary>
    public class ErrorSourceASXY_TI3S : IErrorSource
    {
        public ErrorSourceASXY_TI3S()
        {

        }
        public string ErrorCode
        {
            get { return "ASXY_TI3S"; }
        }
        public int Index
        {
            get { return 9; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0005;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return (tanDip * sinI * cosAm - cosI) / 2; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Z-Accelerometer Scale Factor error source
    /// </summary>
    public class ErrorSourceASZ : IErrorSource
    {
        public ErrorSourceASZ()
        {

        }
        public string ErrorCode
        {
            get { return "ASZ"; }
        }
        public int Index
        {
            get { return 10; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0005;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return -System.Math.Sin(incl) * System.Math.Cos(incl);
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return tanDip * sinI * cosI * sinAm; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Magnetometer Bias error source
    /// </summary>
    public class ErrorSourceMBXY_TI1 : IErrorSource
    {
        public ErrorSourceMBXY_TI1()
        {

        }
        public string ErrorCode
        {
            get { return "MBXY_TI1"; }
        }
        public int Index
        {
            get { return 11; }
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
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 70.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double cosDip = System.Math.Cos(Dip);
            return -cosI * sinAm / (BField * cosDip); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Magnetometer Bias error source
    /// </summary>
    public class ErrorSourceMBXY_TI2 : IErrorSource
    {
        public ErrorSourceMBXY_TI2()
        {

        }
        public string ErrorCode
        {
            get { return "MBXY_TI2"; }
        }
        public int Index
        {
            get { return 12; }
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
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 70.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double cosDip = System.Math.Cos(Dip);
            return cosAm / (BField * cosDip); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Z-Magnetometer Bias error source
    /// </summary>
    public class ErrorSourceMBZ : IErrorSource
    {
        public ErrorSourceMBZ()
        {

        }
        public string ErrorCode
        {
            get { return "MBZ"; }
        }
        public int Index
        {
            get { return 13; }
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
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 70.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double cosDip = System.Math.Cos(Dip);
            return -sinI * sinAm / (BField * cosDip); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
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
        public int Index
        {
            get { return 14; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0016;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return sinI * sinAm * (tanDip * cosI + sinI * cosAm) / Math.Sqrt(2); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Magnetometer Scale Factor error source
    /// </summary>
    public class ErrorSourceMSXY_TI2 : IErrorSource
    {
        public ErrorSourceMSXY_TI2()
        {

        }
        public string ErrorCode
        {
            get { return "MSXY_TI2"; }
        }
        public int Index
        {
            get { return 15; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0016;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return sinAm * (tanDip * sinI * cosI - cosI * cosI * cosAm - cosAm) / 2; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD TF Ind: X and Y Magnetometer Scale Factor error source
    /// </summary>
    public class ErrorSourceMSXY_TI3 : IErrorSource
    {
        public ErrorSourceMSXY_TI3()
        {

        }
        public string ErrorCode
        {
            get { return "MSXY_TI3"; }
        }
        public int Index
        {
            get { return 16; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0016;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return (cosI * cosAm * cosAm - cosI * sinAm * sinAm - tanDip * sinI * cosAm) / 2; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
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
            get { return "MSZ"; }
        }
        public int Index
        {
            get { return 17; }
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
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0016;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double cosI = System.Math.Cos(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosAm = System.Math.Cos(az - Declination);
            double tanDip = System.Math.Tan(Dip);
            return -(sinI * cosAm + tanDip * cosI) * sinI * sinAm; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Declination - Uncorrelated error source
    /// </summary>
    public class ErrorSourceDEC_U : IErrorSource
    {
        public ErrorSourceDEC_U()
        {

        }
        public string ErrorCode
        {
            get { return "DEC_U"; }
        }
        public int Index
        {
            get { return 18; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.16 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1.0; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Declination - Crustal Omission SD Ref Models error source
    /// </summary>
    public class ErrorSourceDEC_OS : IErrorSource
    {
        public ErrorSourceDEC_OS()
        {

        }
        public string ErrorCode
        {
            get { return "DEC_OS"; }
        }
        public int Index
        {
            get { return 19; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.24 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1.0; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Declination - Crustal Omission HD Ref Models error source
    /// </summary>
    public class ErrorSourceDEC_OH : IErrorSource
    {
        public ErrorSourceDEC_OH()
        {

        }
        public string ErrorCode
        {
            get { return "DEC_OH"; }
        }
        public int Index
        {
            get { return 20; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.2 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1.0; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Declination - Crustal Omission IFR Ref Models error source
    /// </summary>
    public class ErrorSourceDEC_OI : IErrorSource
    {
        public ErrorSourceDEC_OI()
        {

        }
        public string ErrorCode
        {
            get { return "DEC_OI"; }
        }
        public int Index
        {
            get { return 21; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.05 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1.0; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Declination - Random error source
    /// </summary>
    public class ErrorSourceDECR : IErrorSource
    {
        public ErrorSourceDECR()
        {

        }
        public string ErrorCode
        {
            get { return "DECR"; }
        }
        public int Index
        {
            get { return 22; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1.0; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: BH-Dependent Declination - Uncorrelated error source
    /// </summary>
    public class ErrorSourceDBH_U : IErrorSource
    {
        public ErrorSourceDBH_U()
        {

        }
        public string ErrorCode
        {
            get { return "DBH_U"; }
        }
        public int Index
        {
            get { return 23; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 / Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 2350.0 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1 / (BField * Math.Cos(Dip)); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: BH-Dependent Declination - Crustal Omission SD Ref Models  error source
    /// </summary>
    public class ErrorSourceDBH_OS : IErrorSource
    {
        public ErrorSourceDBH_OS()
        {

        }
        public string ErrorCode
        {
            get { return "DBH_OS"; }
        }
        public int Index
        {
            get { return 24; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 3359.0 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1 / (BField * Math.Cos(Dip)); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: BH-Dependent Declination - Crustal Omission HD Ref Models  error source
    /// </summary>
    public class ErrorSourceDBH_OH : IErrorSource
    {
        public ErrorSourceDBH_OH()
        {

        }
        public string ErrorCode
        {
            get { return "DBH_OH"; }
        }
        public int Index
        {
            get { return 25; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 2840.0 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1 / (BField * Math.Cos(Dip)); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: BH-Dependent Declination - Crustal Omission IFR Ref Models error source
    /// </summary>
    public class ErrorSourceDBH_OI : IErrorSource
    {
        public ErrorSourceDBH_OI()
        {

        }
        public string ErrorCode
        {
            get { return "DBH_OI"; }
        }
        public int Index
        {
            get { return 26; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 356.0 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1 / (BField * Math.Cos(Dip)); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: BH-Dependent Declination - Random error source
    /// </summary>
    public class ErrorSourceDBHR : IErrorSource
    {
        public ErrorSourceDBHR()
        {

        }
        public int Index
        {
            get { return 27; }
        }
        public string ErrorCode
        {
            get { return "DBH_OI"; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 3000.0 * Math.PI / 180.0;

        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1 / (BField * Math.Cos(Dip)); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Axial Interference - SinI.SinA error source
    /// </summary>
    public class ErrorSourceAMIL : IErrorSource
    {
        public ErrorSourceAMIL()
        {

        }
        public string ErrorCode
        {
            get { return "AMIL"; }
        }
        public int Index
        {
            get { return 28; }
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
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 220.0;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            double sinAm = System.Math.Sin(az - Declination);
            double cosDip = System.Math.Cos(Dip);
            return sinI * sinAm / (BField * cosDip); //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the MWD: Sag Enhanced error source
    /// </summary>
    public class ErrorSourceSAGE : IErrorSource
    {
        public ErrorSourceSAGE()
        {

        }
        public string ErrorCode
        {
            get { return "SAGE"; }
        }
        public int Index
        {
            get { return 29; }
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
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.2 * Math.PI / 180.0;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            return Math.Pow(sinI, 0.25);
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0.0; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Misalignment: XY Misalignment 1 error source
    /// </summary>
    public class ErrorSourceXYM1 : IErrorSource
    {
        public ErrorSourceXYM1()
        {

        }
        public string ErrorCode
        {
            get { return "XYM1"; }
        }
        public int Index
        {
            get { return 30; }
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
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double sinI = System.Math.Sin(incl);
            return Math.Abs(sinI);
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0.0; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Misalignment: XY Misalignment 2 error source
    /// </summary>
    public class ErrorSourceXYM2 : IErrorSource
    {
        public ErrorSourceXYM2()
        {

        }
        public string ErrorCode
        {
            get { return "XYM2"; }
        }
        public int Index
        {
            get { return 31; }
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
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return -1; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Misalignment: XY Misalignment 3 error source
    /// </summary>
    public class ErrorSourceXYM3E : IErrorSource
    {
        // NB Singularity when vertical
        public ErrorSourceXYM3E()
        {

        }
        public string ErrorCode
        {
            get { return "XYM3L"; }
        }
        public int Index
        {
            get { return 32; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.3 * Math.PI / 180.0;
        public bool SingularIssues { get; } = true;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Abs(Math.Cos(incl)) * Math.Cos(AzT);
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            if (incl < 0.0001 * Math.PI / 180.0)
            {
                return null;
            }
            else
            {
                return -(Math.Abs(Math.Cos(incl)) * Math.Sin(AzT)) / Math.Sin(incl); //Azimuth
            }
        }
        public double FunctionSingularityNorth(double az) { return 1; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Misalignment: XY Misalignment 4 error source
    /// </summary>
    public class ErrorSourceXYM4E : IErrorSource
    {
        // NB Singularity when vertical
        public ErrorSourceXYM4E()
        {

        }
        public string ErrorCode
        {
            get { return "XYM4L"; }
        }
        public int Index
        {
            get { return 33; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.3 * Math.PI / 180.0;
        public bool SingularIssues { get; } = true;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Abs(Math.Cos(incl)) * Math.Sin(AzT);
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            if (incl < 0.0001 * Math.PI / 180.0)
            {
                return null;
            }
            else
            {
                return (Math.Abs(Math.Cos(incl)) * Math.Cos(AzT)) / Math.Sin(incl); //Azimuth
            }
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 1; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Depth: Long Course Length  Azimuth XCL error source
    /// </summary>
    public class ErrorSourceXCLA : IErrorSource
    {
        // NB Singularity when vertical
        public ErrorSourceXCLA()
        {

        }
        public string ErrorCode
        {
            get { return "XCLA"; }
        }
        public int Index
        {
            get { return 34; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.167;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {          
            return 0;
        }
        public double? FunctionAz(double incl, double az)
        {           
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Depth: Long Course Length High Side XCL  error source
    /// </summary>
    public class ErrorSourceXCLH : IErrorSource
    {
        // NB Singularity when vertical
        public ErrorSourceXCLH()
        {

        }
        public string ErrorCode
        {
            get { return "XCLH"; }
        }
        public int Index
        {
            get { return 35; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.167;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0;
        }
        public double? FunctionAz(double incl, double az)
        {            
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    #region Gyro Error Sources
    /// <summary>
    /// Error due to the 3-axis: xy accelerometer bias  error source
    /// </summary>
    public class ErrorSourceAXYZ_XYB : IErrorSource
    {        
        public ErrorSourceAXYZ_XYB()
        {

        }
        public string ErrorCode
        {
            get { return "AXYZ_XYB"; }
        }
        public int Index
        {
            get { return 36; }
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
        public bool IsContinuous
        {
            get { return true; }
        }
        public bool IsStationary
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; }
        public double Magnitude { get; set; } = 0.005;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return Math.Cos(incl) * Gravity;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: z accelerometer bias  error source
    /// </summary>
    public class ErrorSourceAXYZ_ZB : IErrorSource
    {
        public ErrorSourceAXYZ_ZB()
        {

        }
        public string ErrorCode
        {
            get { return "AXYZ_ZB"; }
        }
        public int Index
        {
            get { return 37; }
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
        public bool IsContinuous
        {
            get { return true; }
        }
        public bool IsStationary
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; }
        public double Magnitude { get; set; } = 0.005;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return Math.Sin(incl) * Gravity;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: accelerometer scale factor error source
    /// </summary>
    public class ErrorSourceAXYZ_SF : IErrorSource
    {
        public ErrorSourceAXYZ_SF()
        {

        }
        public string ErrorCode
        {
            get { return "AXYZ_SF"; }
        }
        public int Index
        {
            get { return 38; }
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
        public bool IsContinuous
        {
            get { return true; }
        }
        public bool IsStationary
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; }
        public double Magnitude { get; set; } = 0.0005;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 1.3 * Math.Sin(incl) * Math.Cos(incl);
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: accelerometer misalignment error source
    /// </summary>
    public class ErrorSourceAXYZ_MIS : IErrorSource
    {
        public ErrorSourceAXYZ_MIS()
        {

        }
        public string ErrorCode
        {
            get { return "AXYZ_MIS"; }
        }
        public int Index
        {
            get { return 39; }
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
        public bool IsContinuous
        {
            get { return true; }
        }
        public bool IsStationary
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; }
        public double Magnitude { get; set; } = 0.05;
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 1.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: accelerometer bias error source
    /// </summary>
    public class ErrorSourceAXY_B : IErrorSource
    {
        public ErrorSourceAXY_B()
        {

        }
        public string ErrorCode
        {
            get { return "AXY_B"; }
        }
        public int Index
        {
            get { return 40; }
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
        public bool IsContinuous
        {
            get { return true; }
        }
        public bool IsStationary
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; }
        public double Magnitude { get; set; } = 0.005;
        public bool SingularIssues { get; } = false;
        public double kOperator { get; set; } = 1; // 1 or -1 depending on inclination
        public double CantAngle { get; set; }
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 1 / (Gravity * Math.Cos(incl - kOperator * CantAngle));
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: accelerometer scale factor error source
    /// </summary>
    public class ErrorSourceAXY_SF : IErrorSource
    {
        public ErrorSourceAXY_SF()
        {

        }
        public string ErrorCode
        {
            get { return "AXY_SF"; }
        }
        public int Index
        {
            get { return 41; }
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
        public bool IsContinuous
        {
            get { return true; }
        }
        public bool IsStationary
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; }
        public double Magnitude { get; set; } = 0.0005;
        public bool SingularIssues { get; } = false;
        public double kOperator { get; set; } = 1; // 1 or -1 depending on inclination
        public double CantAngle { get; set; }
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return Math.Tan(incl - kOperator * CantAngle);
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: accelerometer misalignment error source
    /// </summary>
    public class ErrorSourceAXY_MS : IErrorSource
    {
        public ErrorSourceAXY_MS()
        {

        }
        public string ErrorCode
        {
            get { return "AXY_MS"; }
        }
        public int Index
        {
            get { return 42; }
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
        public bool IsContinuous
        {
            get { return true; }
        }
        public bool IsStationary
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; }
        public double Magnitude { get; set; } = 0.005;
        public bool SingularIssues { get; } = false;
        public double kOperator { get; set; } = 1; // 1 or -1 depending on inclination
        public double CantAngle { get; set; }
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 1;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: Gravity bias error source
    /// </summary>
    public class ErrorSourceAXY_GB : IErrorSource
    {
        public ErrorSourceAXY_GB()
        {

        }
        public string ErrorCode
        {
            get { return "AXY_GB"; }
        }
        public int Index
        {
            get { return 43; }
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
        public bool IsContinuous
        {
            get { return true; }
        }
        public bool IsStationary
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; }
        public double Magnitude { get; set; } = 0.005;
        public bool SingularIssues { get; } = false;
        public double kOperator { get; set; } = 1; // 1 or -1 depending on inclination
        public double CantAngle { get; set; }
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return Math.Tan(incl - kOperator * CantAngle) / Gravity;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: xy bias 1 error source
    /// </summary>
    public class ErrorSourceGXYZ_XYB1 : IErrorSource
    {
        public ErrorSourceGXYZ_XYB1()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_XYB1"; }
        }
        public int Index
        {
            get { return 44; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: xy bias 2 error source
    /// </summary>
    public class ErrorSourceGXYZ_XYB2 : IErrorSource
    {
        public ErrorSourceGXYZ_XYB2()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_XYB2"; }
        }
        public int Index
        {
            get { return 45; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Cos(AzT) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: xy random noise error source
    /// </summary>
    public class ErrorSourceGXYZ_XYRN : IErrorSource
    {
        public ErrorSourceGXYZ_XYRN()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_XYRN"; }
        }
        public int Index
        {
            get { return 44; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double NoiseRedFactor { get; set; } = 1;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sqrt(1-(Math.Sin(AzT) *Math.Sin(AzT)  * Math.Sin(incl) * Math.Sin(incl))) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: xy gyro g-dependent error source 1
    /// </summary>
    public class ErrorSourceGXYZ_XYG1 : IErrorSource
    {
        public ErrorSourceGXYZ_XYG1()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_XYG1"; }
        }
        public int Index
        {
            get { return 47; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Cos(AzT) * Math.Sin(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: xy gyro g-dependent error source 2
    /// </summary>
    public class ErrorSourceGXYZ_XYG2 : IErrorSource
    {
        public ErrorSourceGXYZ_XYG2()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_XYG2"; }
        }
        public int Index
        {
            get { return 48; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Cos(AzT)* Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: xy gyro g-dependent error source 3
    /// </summary>
    public class ErrorSourceGXYZ_XYG3 : IErrorSource
    {
        public ErrorSourceGXYZ_XYG3()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_XYG3"; }
        }
        public int Index
        {
            get { return 49; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) * Math.Cos(incl) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: xy gyro g-dependent error source 4
    /// </summary>
    public class ErrorSourceGXYZ_XYG4 : IErrorSource
    {
        public ErrorSourceGXYZ_XYG4()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_XYG4"; }
        }
        public int Index
        {
            get { return 50; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) * Math.Sin(incl) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: z gyro bias error source
    /// </summary>
    public class ErrorSourceGXYZ_ZB : IErrorSource
    {
        public ErrorSourceGXYZ_ZB()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_ZB"; }
        }
        public int Index
        {
            get { return 51; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) * Math.Sin(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: z gyro random noise error source
    /// </summary>
    public class ErrorSourceGXYZ_ZRN : IErrorSource
    {
        public ErrorSourceGXYZ_ZRN()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_ZRN"; }
        }
        public int Index
        {
            get { return 52; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) * Math.Sin(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: z gyro g-dependent error source 1
    /// </summary>
    public class ErrorSourceGXYZ_ZG1 : IErrorSource
    {
        public ErrorSourceGXYZ_ZG1()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_ZG1"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) * Math.Sin(incl) * Math.Sin(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: z gyro g-dependent error source 2
    /// </summary>
    public class ErrorSourceGXYZ_ZG2 : IErrorSource
    {
        public ErrorSourceGXYZ_ZG2()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_ZG2"; }
        }
        public int Index
        {
            get { return 54; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) * Math.Sin(incl) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: z gyro scalefactor error source
    /// </summary>
    public class ErrorSourceGXYZ_SF : IErrorSource
    {
        public ErrorSourceGXYZ_SF()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_SF"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.001;
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Tan(Latitude) * Math.Sin(AzT) * Math.Sin(incl) * Math.Cos(incl);
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis: stationary: gyro misalignment error source
    /// </summary>
    public class ErrorSourceGXYZ_MIS : IErrorSource
    {
        public ErrorSourceGXYZ_MIS()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_MIS"; }
        }
        public int Index
        {
            get { return 56; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.05 * Math.PI / 180.0;
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return 1 /  Math.Cos(Latitude);
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: stationary: xy gyro bias 1 error source
    /// </summary>
    public class ErrorSourceGXY_B1 : IErrorSource
    {
        public ErrorSourceGXY_B1()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_B1"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) / (EarthRotRate * Math.Cos(Latitude) * Math.Cos(incl));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: stationary: xy gyro bias 2 error source
    /// </summary>
    public class ErrorSourceGXY_B2 : IErrorSource
    {
        public ErrorSourceGXY_B2()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_B2"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Cos(AzT) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: stationary: xy gyro random noise error source
    /// </summary>
    public class ErrorSourceGXY_RN : IErrorSource
    {
        public ErrorSourceGXY_RN()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_RN"; }
        }
        public int Index
        {
            get { return 53; }
        }
        public bool IsSystematic
        {
            get { return false; }
        }
        public bool IsRandom
        {
            get { return true; }
        }
        public bool IsGlobal
        {
            get { return false; }
        }
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double NoiseRedFactor { get; set; } = 1;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return NoiseRedFactor * Math.Sqrt((1 - Math.Cos(AzT) * Math.Cos(AzT) * Math.Sin(incl) * Math.Sin(incl)) / (EarthRotRate * Math.Cos(Latitude) * Math.Cos(incl)));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: stationary: xy gyro g-dependent 1 error source
    /// </summary>
    public class ErrorSourceGXY_G1 : IErrorSource
    {
        public ErrorSourceGXY_G1()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_G1"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Cos(AzT) * Math.Sin(incl)/ (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: stationary: xy gyro g-dependent 2 error source
    /// </summary>
    public class ErrorSourceGXY_G2 : IErrorSource
    {
        public ErrorSourceGXY_G2()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_G2"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Cos(AzT) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: stationary: xy gyro g-dependent 3 error source
    /// </summary>
    public class ErrorSourceGXY_G3 : IErrorSource
    {
        public ErrorSourceGXY_G3()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_G3"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: stationary: xy gyro g-dependent 4 error source
    /// </summary>
    public class ErrorSourceGXY_G4 : IErrorSource
    {
        public ErrorSourceGXY_G4()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_G4"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Sin(AzT) * Math.Tan(incl) / (EarthRotRate * Math.Cos(Latitude));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: stationary: gyro scalefactor error source
    /// </summary>
    public class ErrorSourceGXY_SF : IErrorSource
    {
        public ErrorSourceGXY_SF()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_SF"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.001;
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return Math.Tan(Latitude) * Math.Sin(AzT) * Math.Tan(incl);
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 2-axis: stationary:gyro misalignment error source
    /// </summary>
    public class ErrorSourceGXY_MIS : IErrorSource
    {
        public ErrorSourceGXY_MIS()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_MIS"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.05 * Math.PI / 180.0;
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            double AzT = az + Convergence;
            return 1 / (Math.Cos(Latitude) * Math.Cos(incl));
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the external reference error source
    /// </summary>
    public class ErrorSourceEXT_REF : IErrorSource
    {
        public ErrorSourceEXT_REF()
        {

        }
        public string ErrorCode
        {
            get { return "EXT_REF"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 5.0 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {            
            return 1.0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Un-modelled random azimuth error in tie-ontool error source
    /// </summary>
    public class ErrorSourceEXT_TIE : IErrorSource
    {
        public ErrorSourceEXT_TIE()
        {

        }
        public string ErrorCode
        {
            get { return "EXT_TIE"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.0 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1.0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Misalignment effect at tie-on error source
    /// </summary>
    public class ErrorSourceEXT_MIS : IErrorSource
    {
        public ErrorSourceEXT_MIS()
        {

        }
        public string ErrorCode
        {
            get { return "EXT_MIS"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public bool IsStationary
        {
            get { return true; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.0 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1.0 / Math.Sin(incl);
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis, continuous: xyz gyro drift error source
    /// </summary>
    public class ErrorSourceGXYZ_GD : IErrorSource
    {
        public ErrorSourceGXYZ_GD()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_GD"; }
        }
        public int Index
        {
            get { return 53; }
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
        public bool IsContinuous
        {
            get { return true; }
        }
        public bool IsStationary
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Convergence { get; set; } = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0; //[rad/h]. NB! convert to seconds??
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //[rad/s]
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return 0.0;
        }
        public double? FunctionAz(double incl, double az)
        {
            return 1.0;
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    #endregion Gyro Error Sources
    /// <summary>
    /// ISCWSAErrorData
    /// </summary>
    public class ISCWSAErrorData
    {
        public double[,] Covariance { get; set; }
        public double[] ErrorSum { get; set; }
        public double[,] SigmaErrorRandom { get; set; }
    }
}
