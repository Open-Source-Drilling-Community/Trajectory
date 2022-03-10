using System;
using System.Collections.Generic;
using System.Text;
using NORCE.General.Std;
using NORCE.General.Math;
using NORCE.Drilling.SurveyInstrument.Model;

namespace NORCE.Drilling.Trajectory
{
    public class ISCWSA_SurveyStationUncertainty : SurveyStationUncertainty
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
        /// Latitude [rad]
        /// </summary>
        public double Latitude { get; set; } = 0;
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
			if (ErrorSources == null)
            {
                //NB! Make this Configurable

                double startInclinationSta = 0.0 * Math.PI / 180.0;
                double startInclination = 17.0 * Math.PI / 180.0;
                double endInclinationSta = 150.0 * Math.PI / 180.0;
                double endInclination = 150.0 * Math.PI / 180.0;
                double initInclination = 17.0 * Math.PI / 180.0;
                double startInclinationZ = 0.0 * Math.PI / 180.0; ;
                double endInclinationZ = 0.0 * Math.PI / 180.0;
                double cantAngle = 0.0;
                double kOperator = 1;
                
                if (surveyStation.SurveyTool.ModelType is SurveyInstrumentModelType.ISCWSA_Gyro_Ex2)
				{
                    startInclinationSta = 0.0 * Math.PI / 180.0;
                    startInclinationZ = 0.0 * Math.PI / 180.0;
                    endInclinationSta = 17.0 * Math.PI / 180.0;
                    endInclinationZ = 150.0 * Math.PI / 180.0;
                    initInclination = 0.0 * Math.PI / 180.0;
                }
                if (surveyStation.SurveyTool.ModelType is SurveyInstrumentModelType.ISCWSA_Gyro_Ex3)
                {
                    startInclinationSta = 0.0 * Math.PI / 180.0;
                    startInclination = 17.0 * Math.PI / 180.0;
                    endInclinationSta = 17.0 * Math.PI / 180.0;
                    endInclination = 150.0 * Math.PI / 180.0;
                    initInclination = 17.0 * Math.PI / 180.0;
                }
                if (surveyStation.SurveyTool.ModelType is SurveyInstrumentModelType.ISCWSA_Gyro_Ex4)
                {
                    startInclinationSta = 0.0 * Math.PI / 180.0;
                    startInclination = 17.0 * Math.PI / 180.0;
                    endInclinationSta = 0.0 * Math.PI / 180.0;
                    endInclination = 150.0 * Math.PI / 180.0;
                    initInclination = 3.0 * Math.PI / 180.0;
                    startInclinationZ = 0.0 * Math.PI / 180.0; ;
                    endInclinationZ = 17.0 * Math.PI / 180.0;
                    cantAngle = 17.0 * Math.PI / 180.0;
                    kOperator = 0;
                }
                if (surveyStation.SurveyTool.ModelType is SurveyInstrumentModelType.ISCWSA_Gyro_Ex5)
                {
                    startInclinationSta = 0.0 * Math.PI / 180.0;
                    startInclination = 0.0 * Math.PI / 180.0;
                    endInclinationSta = 0.0 * Math.PI / 180.0;
                    endInclination = 150.0 * Math.PI / 180.0;
                    initInclination = -1 * Math.PI / 180.0;
                }
                if (surveyStation.SurveyTool.ModelType is SurveyInstrumentModelType.ISCWSA_Gyro_Ex6)
                {
                    startInclinationSta = 0.0 * Math.PI / 180.0;
                    startInclination = 0.0 * Math.PI / 180.0;
                    endInclinationSta = 150.0 * Math.PI / 180.0;
                    endInclination = 150.0 * Math.PI / 180.0;
                    initInclination = -1 * Math.PI / 180.0;
                }

                //Use all error sources
                ErrorSources = new List<IErrorSource>();
                bool GyroEx1 = true;
                bool GyroEx2 = false;
                if(this is ISCWSA_SurveyStationUncertainty)
                { }
                double earthRotRate = 7.2921159e-5; //7.367e-5; //7.27e-5;//7.292115e-5; //[rad/s]
                if (surveyStation.SurveyTool.UseXYM1)
                {
                    ErrorSourceXYM1 errorSourceXYM1 = new ErrorSourceXYM1();
                    errorSourceXYM1.Magnitude = (double)surveyStation.SurveyTool.XYM1;
                    ErrorSources.Add(errorSourceXYM1);
                }
                if (surveyStation.SurveyTool.UseXYM2)
                {
                    ErrorSourceXYM2 errorSourceXYM2 = new ErrorSourceXYM2();
                    errorSourceXYM2.Magnitude = (double)surveyStation.SurveyTool.XYM2;
                    ErrorSources.Add(errorSourceXYM2);
                }
                if (surveyStation.SurveyTool.UseXYM3)
                {
                    ErrorSourceXYM3 errorSourceXYM3 = new ErrorSourceXYM3(); //NB!
					errorSourceXYM3.Convergence = 0 * Math.PI / 180;
					errorSourceXYM3.Magnitude = (double)surveyStation.SurveyTool.XYM3;
                    ErrorSources.Add(errorSourceXYM3);
                }
                if (surveyStation.SurveyTool.UseXYM4)
                {
                    ErrorSourceXYM4 errorSourceXYM4 = new ErrorSourceXYM4();
					errorSourceXYM4.Convergence = 0* Math.PI / 180; ;
					errorSourceXYM4.Magnitude = (double)surveyStation.SurveyTool.XYM4;
                    ErrorSources.Add(errorSourceXYM4);
                }
                if (surveyStation.SurveyTool.UseSAG )
                {
                    ErrorSourceSAG errorSourceSAG = new ErrorSourceSAG();
                    errorSourceSAG.Magnitude = (double)surveyStation.SurveyTool.SAG;
                    ErrorSources.Add(errorSourceSAG);
                }
                if (surveyStation.SurveyTool.UseDRFR)
                {
                    ErrorSourceDRFR errorSourceDRFR = new ErrorSourceDRFR();
                    errorSourceDRFR.Magnitude = (double)surveyStation.SurveyTool.DRFR;
                    ErrorSources.Add(errorSourceDRFR);
                }
                if (surveyStation.SurveyTool.UseDRFS )
                {
                    ErrorSourceDRFS errorSourceDRFS = new ErrorSourceDRFS();
                    errorSourceDRFS.Magnitude = (double)surveyStation.SurveyTool.DRFS;
                    ErrorSources.Add(errorSourceDRFS);
                }
                if (surveyStation.SurveyTool.UseDSFS )
                {
                    ErrorSourceDSFS errorSourceDSFS = new ErrorSourceDSFS();
                    errorSourceDSFS.Magnitude = (double)surveyStation.SurveyTool.DSFS;
                    ErrorSources.Add(errorSourceDSFS);
                }
                if (surveyStation.SurveyTool.UseDSTG )
                {
                    ErrorSourceDSTG errorSourceDSTG = new ErrorSourceDSTG();
                    errorSourceDSTG.Magnitude = (double)surveyStation.SurveyTool.DSTG;
                    ErrorSources.Add(errorSourceDSTG);
                }
                if (surveyStation.SurveyTool.UseXYM3E)
                {
                    ErrorSourceXYM3E errorSourceXYM3E = new ErrorSourceXYM3E();
                    errorSourceXYM3E.Magnitude = (double)surveyStation.SurveyTool.XYM3E;
                    ErrorSources.Add(errorSourceXYM3E);
                }
                if (surveyStation.SurveyTool.UseXYM4E )
                {
                    ErrorSourceXYM4E errorSourceXYM4E = new ErrorSourceXYM4E();
                    errorSourceXYM4E.Magnitude = (double)surveyStation.SurveyTool.XYM4E;
                    ErrorSources.Add(errorSourceXYM4E);
                }
                if (surveyStation.SurveyTool.UseSAGE )
                {
                    ErrorSourceSAGE errorSourceSAGE = new ErrorSourceSAGE();
                    errorSourceSAGE.Magnitude = (double)surveyStation.SurveyTool.SAGE;
                    ErrorSources.Add(errorSourceSAGE);
                }
                if (surveyStation.SurveyTool.UseXCLH )
                {
                    ErrorSourceXCLH errorSourceXCLH = new ErrorSourceXCLH();
                    errorSourceXCLH.Magnitude = (double)surveyStation.SurveyTool.XCLH;
                    ErrorSources.Add(errorSourceXCLH);
                }
                if (surveyStation.SurveyTool.UseXCLL )
                {
                    ErrorSourceXCLA errorSourceXCLA = new ErrorSourceXCLA();
                    errorSourceXCLA.Magnitude = (double)surveyStation.SurveyTool.XCLL;
                    ErrorSources.Add(errorSourceXCLA);
                }
                if (surveyStation.SurveyTool.UseABXY_TI1S)
                {
                    ErrorSourceABXY_TI1S errorSourceABXY_TI1S = new ErrorSourceABXY_TI1S();
                    errorSourceABXY_TI1S.Magnitude = (double)surveyStation.SurveyTool.ABXY_TI1S;
                    errorSourceABXY_TI1S.Dip = Dip;
                    errorSourceABXY_TI1S.Gravity = Gravity;
                    errorSourceABXY_TI1S.Declination = Declination;
                    ErrorSources.Add(errorSourceABXY_TI1S);
                }
                if (surveyStation.SurveyTool.UseABXY_TI2S )
                {
                    ErrorSourceABXY_TI2S errorSourceABXY_TI2S = new ErrorSourceABXY_TI2S();
                    errorSourceABXY_TI2S.Magnitude = (double)surveyStation.SurveyTool.ABXY_TI2S;
                    errorSourceABXY_TI2S.Dip = Dip;
                    errorSourceABXY_TI2S.Gravity = Gravity;
                    errorSourceABXY_TI2S.Declination = Declination;
                    ErrorSources.Add(errorSourceABXY_TI2S);
                }
                if (surveyStation.SurveyTool.UseABZ )
                {
                    ErrorSourceABZ errorSourceABZ = new ErrorSourceABZ();
                    errorSourceABZ.Magnitude = (double)surveyStation.SurveyTool.ABZ;
                    errorSourceABZ.Dip = Dip;
                    errorSourceABZ.Gravity = Gravity;
                    errorSourceABZ.Declination = Declination;
                    ErrorSources.Add(errorSourceABZ);
                }
                if (surveyStation.SurveyTool.UseASXY_TI1S )
                {
                    ErrorSourceASXY_TI1S errorSourceASXY_TI1S = new ErrorSourceASXY_TI1S();
                    errorSourceASXY_TI1S.Magnitude = (double)surveyStation.SurveyTool.ASXY_TI1S;
                    errorSourceASXY_TI1S.Dip = Dip;
                    errorSourceASXY_TI1S.Declination = Declination;
                    ErrorSources.Add(errorSourceASXY_TI1S);
                }
                if (surveyStation.SurveyTool.UseASXY_TI2S)
                {
                    ErrorSourceASXY_TI2S errorSourceASXY_TI2S = new ErrorSourceASXY_TI2S();
                    errorSourceASXY_TI2S.Magnitude = (double)surveyStation.SurveyTool.ASXY_TI2S;
                    errorSourceASXY_TI2S.Dip = Dip;
                    errorSourceASXY_TI2S.Declination = Declination;
                    ErrorSources.Add(errorSourceASXY_TI2S);
                }
                if (surveyStation.SurveyTool.UseASXY_TI3S)
                {
                    ErrorSourceASXY_TI3S errorSourceASXY_TI3S = new ErrorSourceASXY_TI3S();
                    errorSourceASXY_TI3S.Magnitude = (double)surveyStation.SurveyTool.ASXY_TI3S;
                    errorSourceASXY_TI3S.Dip = Dip;
                    errorSourceASXY_TI3S.Declination = Declination;
                    ErrorSources.Add(errorSourceASXY_TI3S);
                }
                if (surveyStation.SurveyTool.UseASZ )
                {
                    ErrorSourceASZ errorSourceASZ = new ErrorSourceASZ();
                    errorSourceASZ.Magnitude = (double)surveyStation.SurveyTool.ASZ;
                    errorSourceASZ.Dip = Dip;
                    errorSourceASZ.Declination = Declination;
                    ErrorSources.Add(errorSourceASZ);
                }
                if (surveyStation.SurveyTool.UseMBXY_TI1S)
                {
                    ErrorSourceMBXY_TI1 errorSourceMBXY_TI1 = new ErrorSourceMBXY_TI1();
                    errorSourceMBXY_TI1.Magnitude = (double)surveyStation.SurveyTool.MBXY_TI1S;
                    errorSourceMBXY_TI1.Dip = Dip;
                    errorSourceMBXY_TI1.Declination = Declination;
                    errorSourceMBXY_TI1.BField = BField;
                    ErrorSources.Add(errorSourceMBXY_TI1);
                }
                if (surveyStation.SurveyTool.UseMBXY_TI2S)
                {
                    ErrorSourceMBXY_TI2 errorSourceMBXY_TI2 = new ErrorSourceMBXY_TI2();
                    errorSourceMBXY_TI2.Magnitude = (double)surveyStation.SurveyTool.MBXY_TI2S;
                    errorSourceMBXY_TI2.Dip = Dip;
                    errorSourceMBXY_TI2.Declination = Declination;
                    errorSourceMBXY_TI2.BField = BField;
                    ErrorSources.Add(errorSourceMBXY_TI2);
                }
                if (surveyStation.SurveyTool.UseMBZ)
                {
                    ErrorSourceMBZ errorSourceMBZ = new ErrorSourceMBZ();
                    errorSourceMBZ.Magnitude = (double)surveyStation.SurveyTool.MBZ;
                    errorSourceMBZ.Dip = Dip;
                    errorSourceMBZ.Declination = Declination;
                    errorSourceMBZ.BField = BField;
                    ErrorSources.Add(errorSourceMBZ);
                }
                if (surveyStation.SurveyTool.UseMSXY_TI1S)
                {
                    ErrorSourceMSXY_TI1 errorSourceMSXY_TI1 = new ErrorSourceMSXY_TI1();
                    errorSourceMSXY_TI1.Magnitude = (double)surveyStation.SurveyTool.MSXY_TI1S;
                    errorSourceMSXY_TI1.Dip = Dip;
                    errorSourceMSXY_TI1.Declination = Declination;
                    ErrorSources.Add(errorSourceMSXY_TI1);
                }
                if (surveyStation.SurveyTool.UseMSXY_TI2S)
                {
                    ErrorSourceMSXY_TI2 errorSourceMSXY_TI2 = new ErrorSourceMSXY_TI2();
                    errorSourceMSXY_TI2.Magnitude = (double)surveyStation.SurveyTool.MSXY_TI2S;
                    errorSourceMSXY_TI2.Dip = Dip;
                    errorSourceMSXY_TI2.Declination = Declination;
                    ErrorSources.Add(errorSourceMSXY_TI2);
                }
                if (surveyStation.SurveyTool.UseMSXY_TI3S)
                {
                    ErrorSourceMSXY_TI3 errorSourceMSXY_TI3 = new ErrorSourceMSXY_TI3();
                    errorSourceMSXY_TI3.Magnitude = (double)surveyStation.SurveyTool.MSXY_TI3S;
                    errorSourceMSXY_TI3.Dip = Dip;
                    errorSourceMSXY_TI3.Declination = Declination;
                    ErrorSources.Add(errorSourceMSXY_TI3);
                }
                if (surveyStation.SurveyTool.UseMSZ)
                {
                    ErrorSourceMSZ errorSourceMSZ = new ErrorSourceMSZ();
                    errorSourceMSZ.Magnitude = (double)surveyStation.SurveyTool.MSZ;
                    errorSourceMSZ.Dip = Dip;
                    errorSourceMSZ.Declination = Declination;
                    ErrorSources.Add(errorSourceMSZ);
                }
                if (surveyStation.SurveyTool.UseAMIL)
                {
                    ErrorSourceAMIL errorSourceAMIL = new ErrorSourceAMIL();
                    errorSourceAMIL.Magnitude = (double)surveyStation.SurveyTool.AMIL;
                    errorSourceAMIL.Dip = Dip;
                    errorSourceAMIL.BField = BField;
                    errorSourceAMIL.Declination = Declination;
                    ErrorSources.Add(errorSourceAMIL);
                }
                if (surveyStation.SurveyTool.UseABIXY_TI1S)
                {
                    //ErrorSourceABIXY_TI1S errorSourceABIXY_TI1S = new ErrorSourceABIXY_TI1S();
                    //errorSourceABIXY_TI1S.Magnitude = (double)surveyStation.SurveyTool.ABIXY_TI1S;
                    //ErrorSources.Add(errorSourceABIXY_TI1S);
                }
                if (surveyStation.SurveyTool.UseABIXY_TI2S)
                {
                    //ErrorSourceABIXY_TI2S errorSourceABIXY_TI2S = new ErrorSourceABIXY_TI2S();
                    //errorSourceABIXY_TI2S.Magnitude = (double)surveyStation.SurveyTool.ABIXY_TI2S;
                    //ErrorSources.Add(errorSourceABIXY_TI2S);
                }
                if (surveyStation.SurveyTool.UseABIZ)
                {
                    //ErrorSourceABIZ errorSourceABIZ = new ErrorSourceABIZ();
                    //errorSourceABIZ.Magnitude = (double)surveyStation.SurveyTool.ABIZ;
                    //ErrorSources.Add(errorSourceABIZ);
                }
                if (surveyStation.SurveyTool.UseASIXY_TI1S)
                {
                    //ErrorSourceASIXY_TI1S errorSourceASIXY_TI1S = new ErrorSourceASIXY_TI1S();
                    //errorSourceASIXY_TI1S.Magnitude = (double)surveyStation.SurveyTool.ASIXY_TI1S;
                    //ErrorSources.Add(errorSourceASIXY_TI1S);
                }
                if (surveyStation.SurveyTool.UseASIXY_TI2S)
                {
                    //ErrorSourceASIXY_TI2S errorSourceASIXY_TI2S = new ErrorSourceASIXY_TI2S();
                    //errorSourceASIXY_TI2S.Magnitude = (double)surveyStation.SurveyTool.ASIXY_TI2S;
                    //ErrorSources.Add(errorSourceASIXY_TI2S);
                }
                if (surveyStation.SurveyTool.UseASIXY_TI3S)
                {
                    //ErrorSourceASIXY_TI3S errorSourceASIXY_TI3S = new ErrorSourceASIXY_TI3S();
                    //errorSourceASIXY_TI3S.Magnitude = (double)surveyStation.SurveyTool.ASIXY_TI3S;
                    //ErrorSources.Add(errorSourceASIXY_TI3S);
                }
                if (surveyStation.SurveyTool.UseASIZ)
                {
                    //ErrorSourceASIZ errorSourceASIZ = new ErrorSourceASIZ();
                    //errorSourceASIZ.Magnitude = (double)surveyStation.SurveyTool.ASIZ;
                    //ErrorSources.Add(errorSourceASIZ);
                }
                if (surveyStation.SurveyTool.UseMBIXY_TI1S)
                {
                    //ErrorSourceMBIXY_TI1S errorSourceMBIXY_TI1S = new ErrorSourceMBIXY_TI1S();
                    //errorSourceMBIXY_TI1S.Magnitude = (double)surveyStation.SurveyTool.MBIXY_TI1S;
                    //ErrorSources.Add(errorSourceMBIXY_TI1S);
                }
                if (surveyStation.SurveyTool.UseMBIXY_TI2S)
                {
                    //ErrorSourceMBIXY_TI2S errorSourceMBIXY_TI2S = new ErrorSourceMBIXY_TI2S();
                    //errorSourceMBIXY_TI2S.Magnitude = (double)surveyStation.SurveyTool.MBIXY_TI2S;
                    //ErrorSources.Add(errorSourceMBIXY_TI2S);
                }
                if (surveyStation.SurveyTool.UseMSIXY_TI1S)
                {
                    //ErrorSourceMSIXY_TI1S errorSourceMSIXY_TI1S = new ErrorSourceMSIXY_TI1S();
                    //errorSourceMSIXY_TI1S.Magnitude = (double)surveyStation.SurveyTool.MSIXY_TI1S;
                    //ErrorSources.Add(errorSourcMSIXY_TI1S);
                }
                if (surveyStation.SurveyTool.UseMSIXY_TI2S)
                {
                    //ErrorSourceMSIXY_TI2S errorSourceMSIXY_TI2S = new ErrorSourceMSIXY_TI2S();
                    //errorSourceMSIXY_TI2S.Magnitude = (double)surveyStation.SurveyTool.MSIXY_TI2S;
                    //ErrorSources.Add(errorSourceMSIXY_TI2S);
                }
                if (surveyStation.SurveyTool.UseMSIXY_TI3S)
                {
                    //ErrorSourceMSIXY_TI3S errorSourceMSIXY_TI3S = new ErrorSourceMSIXY_TI3S();
                    //errorSourceMSIXY_TI3S.Magnitude = (double)surveyStation.SurveyTool.MSIXY_TI3S;
                    //ErrorSources.Add(errorSourceMSIXY_TI3S);
                }
                if (surveyStation.SurveyTool.UseDEC_U)
                {
                    ErrorSourceDEC_U errorSourceDEC_U = new ErrorSourceDEC_U();
                    errorSourceDEC_U.Magnitude = (double)surveyStation.SurveyTool.DEC_U;
                    ErrorSources.Add(errorSourceDEC_U);
                }
                if (surveyStation.SurveyTool.UseDEC_OS)
                {
                    ErrorSourceDEC_OS errorSourceDEC_OS = new ErrorSourceDEC_OS();
                    errorSourceDEC_OS.Magnitude = (double)surveyStation.SurveyTool.DEC_OS;
                    ErrorSources.Add(errorSourceDEC_OS);
                }
                if (surveyStation.SurveyTool.UseDEC_OH)
                {
                    ErrorSourceDEC_OH errorSourceDEC_OH = new ErrorSourceDEC_OH();
                    errorSourceDEC_OH.Magnitude = (double)surveyStation.SurveyTool.DEC_OH;
                    ErrorSources.Add(errorSourceDEC_OH);
                }
                if (surveyStation.SurveyTool.UseDEC_OI)
                {
                    ErrorSourceDEC_OI errorSourceDEC_OI = new ErrorSourceDEC_OI();
                    errorSourceDEC_OI.Magnitude = (double)surveyStation.SurveyTool.DEC_OI;
                    ErrorSources.Add(errorSourceDEC_OI);
                }
                if (surveyStation.SurveyTool.UseDECR)
                {
                    ErrorSourceDECR errorSourceDECR = new ErrorSourceDECR();
                    errorSourceDECR.Magnitude = (double)surveyStation.SurveyTool.DECR;
                    ErrorSources.Add(errorSourceDECR);
                }
                if (surveyStation.SurveyTool.UseDBH_U)
                {
                    ErrorSourceDBH_U errorSourceDBH_U = new ErrorSourceDBH_U();
                    errorSourceDBH_U.Magnitude = (double)surveyStation.SurveyTool.DBH_U;
                    errorSourceDBH_U.Dip = Dip;
                    errorSourceDBH_U.BField = BField;
                    ErrorSources.Add(errorSourceDBH_U);
                }
                if (surveyStation.SurveyTool.UseDBH_OS)
                {
                    ErrorSourceDBH_OS errorSourceDBH_OS = new ErrorSourceDBH_OS();
                    errorSourceDBH_OS.Magnitude = (double)surveyStation.SurveyTool.DBH_OS;
                    errorSourceDBH_OS.Dip = Dip;
                    errorSourceDBH_OS.BField = BField;
                    ErrorSources.Add(errorSourceDBH_OS);
                }
                if (surveyStation.SurveyTool.UseDBH_OH)
                {
                    ErrorSourceDBH_OH errorSourceDBH_OH = new ErrorSourceDBH_OH();
                    errorSourceDBH_OH.Magnitude = (double)surveyStation.SurveyTool.DBH_OH;
                    errorSourceDBH_OH.Dip = Dip;
                    errorSourceDBH_OH.BField = BField;
                    ErrorSources.Add(errorSourceDBH_OH);
                }
                if (surveyStation.SurveyTool.UseDBH_OI)
                {
                    ErrorSourceDBH_OI errorSourceDBH_OI = new ErrorSourceDBH_OI();
                    errorSourceDBH_OI.Magnitude = (double)surveyStation.SurveyTool.DBH_OI;
                    errorSourceDBH_OI.Dip = Dip;
                    errorSourceDBH_OI.BField = BField;
                    ErrorSources.Add(errorSourceDBH_OI);
                }
                if (surveyStation.SurveyTool.UseDBHR)
                {
                    ErrorSourceDBHR errorSourceDBHR = new ErrorSourceDBHR();
                    errorSourceDBHR.Magnitude = (double)surveyStation.SurveyTool.DBHR;
                    errorSourceDBHR.Dip = Dip;
                    errorSourceDBHR.BField = BField;
                    ErrorSources.Add(errorSourceDBHR);
                }
                if (surveyStation.SurveyTool.UseMFI)
                {
                    //ErrorSourceMFI errorSourceMFI = new ErrorSourceMFI();
                    //UseMFI.Magnitude = (double)surveyStation.SurveyTool.MFI;
                    //ErrorSources.Add(errorSourceMFI);
                }
                if (surveyStation.SurveyTool.UseMDI)
                {
                    //ErrorSourceMDI errorSourceMDI = new ErrorSourceMDI();
                    //errorSourceMDI.Magnitude = (double)surveyStation.SurveyTool.MDI;
                    //ErrorSources.Add(errorSourceMDI);
                }
                if (surveyStation.SurveyTool.UseAXYZ_XYB)
                {
                    ErrorSourceAXYZ_XYB errorSourceAXYZ_XYB = new ErrorSourceAXYZ_XYB();
                    errorSourceAXYZ_XYB.Magnitude = (double)surveyStation.SurveyTool.AXYZ_XYB;
                    errorSourceAXYZ_XYB.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXYZ_XYB);
                }
                if (surveyStation.SurveyTool.UseAXYZ_ZB)
                {
                    ErrorSourceAXYZ_ZB errorSourceAXYZ_ZB = new ErrorSourceAXYZ_ZB();
                    errorSourceAXYZ_ZB.Magnitude = (double)surveyStation.SurveyTool.AXYZ_ZB;
                    errorSourceAXYZ_ZB.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXYZ_ZB);
                }
                if (surveyStation.SurveyTool.UseAXYZ_SF)
                {
                    ErrorSourceAXYZ_SF errorSourceAXYZ_SF = new ErrorSourceAXYZ_SF();
                    errorSourceAXYZ_SF.Magnitude = (double)surveyStation.SurveyTool.AXYZ_SF;
                    errorSourceAXYZ_SF.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXYZ_SF);
                }
                if (surveyStation.SurveyTool.UseAXYZ_MIS)
                {
                    ErrorSourceAXYZ_MIS errorSourceAXYZ_MIS = new ErrorSourceAXYZ_MIS();
                    errorSourceAXYZ_MIS.Magnitude = (double)surveyStation.SurveyTool.AXYZ_MIS;
                    errorSourceAXYZ_MIS.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXYZ_MIS);
                }
                if (surveyStation.SurveyTool.UseAXY_B)
                {
                    ErrorSourceAXY_B errorSourceAXY_B = new ErrorSourceAXY_B();
                    errorSourceAXY_B.Magnitude = (double)surveyStation.SurveyTool.AXY_B;
                    errorSourceAXY_B.Gravity = Gravity;
                    errorSourceAXY_B.CantAngle = cantAngle;
                    errorSourceAXY_B.kOperator = kOperator;
                    ErrorSources.Add(errorSourceAXY_B);
                }
                if (surveyStation.SurveyTool.UseAXY_SF)
                {
                    ErrorSourceAXY_SF errorSourceAXY_SF = new ErrorSourceAXY_SF();
                    errorSourceAXY_SF.Magnitude = (double)surveyStation.SurveyTool.AXY_SF;
                    errorSourceAXY_SF.Gravity = Gravity;
                    errorSourceAXY_SF.CantAngle = cantAngle;
                    errorSourceAXY_SF.kOperator = kOperator;
                    ErrorSources.Add(errorSourceAXY_SF);
                }
                if (surveyStation.SurveyTool.UseAXY_MS)
                {
                    ErrorSourceAXY_MS errorSourceAXY_MS = new ErrorSourceAXY_MS();
                    errorSourceAXY_MS.Magnitude = (double)surveyStation.SurveyTool.AXY_MS;
                    ErrorSources.Add(errorSourceAXY_MS);
                }
                if (surveyStation.SurveyTool.UseAXY_GB)
                {
                    ErrorSourceAXY_GB errorSourceAXY_GB = new ErrorSourceAXY_GB();
                    errorSourceAXY_GB.Magnitude = (double)surveyStation.SurveyTool.AXY_GB;
                    errorSourceAXY_GB.Gravity = Gravity;
                    errorSourceAXY_GB.CantAngle = cantAngle;
                    errorSourceAXY_GB.kOperator = kOperator;
                    ErrorSources.Add(errorSourceAXY_GB);
                }
                if (surveyStation.SurveyTool.UseGXYZ_XYB1)
                {
                    ErrorSourceGXYZ_XYB1 errorSourceGXYZ_XYB1 = new ErrorSourceGXYZ_XYB1();
                    errorSourceGXYZ_XYB1.Magnitude = (double)surveyStation.SurveyTool.GXYZ_XYB1;
                    errorSourceGXYZ_XYB1.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_XYB1.Latitude = Latitude;
                    errorSourceGXYZ_XYB1.StartInclination = startInclinationSta;
                    errorSourceGXYZ_XYB1.EndInclination = endInclinationSta;
                    errorSourceGXYZ_XYB1.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_XYB1);
                }
                if (surveyStation.SurveyTool.UseGXYZ_XYB2)
                {
                    ErrorSourceGXYZ_XYB2 errorSourceGXYZ_XYB2 = new ErrorSourceGXYZ_XYB2();
                    errorSourceGXYZ_XYB2.Magnitude = (double)surveyStation.SurveyTool.GXYZ_XYB2;
                    errorSourceGXYZ_XYB2.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_XYB2.Latitude = Latitude;
                    errorSourceGXYZ_XYB2.StartInclination = startInclinationSta;
                    errorSourceGXYZ_XYB2.EndInclination = endInclinationSta;
                    errorSourceGXYZ_XYB2.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_XYB2);
                }
                if (surveyStation.SurveyTool.UseGXYZ_XYRN)
                {
                    ErrorSourceGXYZ_XYRN errorSourceGXYZ_XYRN = new ErrorSourceGXYZ_XYRN();
                    errorSourceGXYZ_XYRN.Magnitude = (double)surveyStation.SurveyTool.GXYZ_XYRN;
                    errorSourceGXYZ_XYRN.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_XYRN.Latitude = Latitude;
                    errorSourceGXYZ_XYRN.StartInclination = startInclinationSta;
                    errorSourceGXYZ_XYRN.EndInclination = endInclinationSta;
                    errorSourceGXYZ_XYRN.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_XYRN);
                }
                if (surveyStation.SurveyTool.UseGXYZ_XYG1)
                {
                    ErrorSourceGXYZ_XYG1 errorSourceGXYZ_XYG1 = new ErrorSourceGXYZ_XYG1();
                    errorSourceGXYZ_XYG1.Magnitude = (double)surveyStation.SurveyTool.GXYZ_XYG1;
                    errorSourceGXYZ_XYG1.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_XYG1.Latitude = Latitude;
                    errorSourceGXYZ_XYG1.StartInclination = startInclinationSta;
                    errorSourceGXYZ_XYG1.EndInclination = endInclinationSta;
                    errorSourceGXYZ_XYG1.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_XYG1);
                }
                if (surveyStation.SurveyTool.UseGXYZ_XYG2)
                {
                    ErrorSourceGXYZ_XYG2 errorSourceGXYZ_XYG2 = new ErrorSourceGXYZ_XYG2();
                    errorSourceGXYZ_XYG2.Magnitude = (double)surveyStation.SurveyTool.GXYZ_XYG2;
                    errorSourceGXYZ_XYG2.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_XYG2.Latitude = Latitude;
                    errorSourceGXYZ_XYG2.StartInclination = startInclinationSta;
                    errorSourceGXYZ_XYG2.EndInclination = endInclinationSta;
                    errorSourceGXYZ_XYG2.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_XYG2);
                }
                if (surveyStation.SurveyTool.UseGXYZ_XYG3)
                {
                    ErrorSourceGXYZ_XYG3 errorSourceGXYZ_XYG3 = new ErrorSourceGXYZ_XYG3();
                    errorSourceGXYZ_XYG3.Magnitude = (double)surveyStation.SurveyTool.GXYZ_XYG3;
                    errorSourceGXYZ_XYG3.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_XYG3.Latitude = Latitude;
                    errorSourceGXYZ_XYG3.StartInclination = startInclinationSta;
                    errorSourceGXYZ_XYG3.EndInclination = endInclinationSta;
                    errorSourceGXYZ_XYG3.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_XYG3);
                }
                if (surveyStation.SurveyTool.UseGXYZ_XYG4)
                {
                    ErrorSourceGXYZ_XYG4 errorSourceGXYZ_XYG4 = new ErrorSourceGXYZ_XYG4();
                    errorSourceGXYZ_XYG4.Magnitude = (double)surveyStation.SurveyTool.GXYZ_XYG4;
                    errorSourceGXYZ_XYG4.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_XYG4.Latitude = Latitude;
                    errorSourceGXYZ_XYG4.StartInclination = startInclinationSta;
                    errorSourceGXYZ_XYG4.EndInclination = endInclinationSta;
                    errorSourceGXYZ_XYG4.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_XYG4);
                }
                if (surveyStation.SurveyTool.UseGXYZ_ZB)
                {
                    ErrorSourceGXYZ_ZB errorSourceGXYZ_ZB = new ErrorSourceGXYZ_ZB();
                    errorSourceGXYZ_ZB.Magnitude = (double)surveyStation.SurveyTool.GXYZ_ZB;
                    errorSourceGXYZ_ZB.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_ZB.Latitude = Latitude;
                    errorSourceGXYZ_ZB.StartInclination = startInclinationSta;
                    errorSourceGXYZ_ZB.EndInclination = endInclinationSta;
                    errorSourceGXYZ_ZB.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_ZB);
                }
                if (surveyStation.SurveyTool.UseGXYZ_ZRN)
                {
                    ErrorSourceGXYZ_ZRN errorSourceGXYZ_ZRN = new ErrorSourceGXYZ_ZRN();
                    errorSourceGXYZ_ZRN.Magnitude = (double)surveyStation.SurveyTool.GXYZ_ZRN;
                    errorSourceGXYZ_ZRN.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_ZRN.Latitude = Latitude;
                    errorSourceGXYZ_ZRN.StartInclination = startInclinationSta;
                    errorSourceGXYZ_ZRN.EndInclination = endInclinationSta;
                    errorSourceGXYZ_ZRN.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_ZRN);
                }
                if (surveyStation.SurveyTool.UseGXYZ_ZG1)
                {
                    ErrorSourceGXYZ_ZG1 errorSourceGXYZ_ZG1 = new ErrorSourceGXYZ_ZG1();
                    errorSourceGXYZ_ZG1.Magnitude = (double)surveyStation.SurveyTool.GXYZ_ZG1;
                    errorSourceGXYZ_ZG1.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_ZG1.Latitude = Latitude;
                    errorSourceGXYZ_ZG1.StartInclination = startInclinationSta;
                    errorSourceGXYZ_ZG1.EndInclination = endInclinationSta;
                    errorSourceGXYZ_ZG1.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_ZG1);
                }
                if (surveyStation.SurveyTool.UseGXYZ_ZG2)
                {
                    ErrorSourceGXYZ_ZG2 errorSourceGXYZ_ZG2 = new ErrorSourceGXYZ_ZG2();
                    errorSourceGXYZ_ZG2.Magnitude = (double)surveyStation.SurveyTool.GXYZ_ZG2;
                    errorSourceGXYZ_ZG2.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_ZG2.Latitude = Latitude;
                    errorSourceGXYZ_ZG2.StartInclination = startInclinationSta;
                    errorSourceGXYZ_ZG2.EndInclination = endInclinationSta;
                    errorSourceGXYZ_ZG2.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_ZG2);
                }
                if (surveyStation.SurveyTool.UseGXYZ_SF)
                {
                    ErrorSourceGXYZ_SF errorSourceGXYZ_SF = new ErrorSourceGXYZ_SF();
                    errorSourceGXYZ_SF.Magnitude = (double)surveyStation.SurveyTool.GXYZ_SF;
                    errorSourceGXYZ_SF.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_SF.Latitude = Latitude;
                    errorSourceGXYZ_SF.StartInclination = startInclinationSta;
                    errorSourceGXYZ_SF.EndInclination = endInclinationSta;
                    errorSourceGXYZ_SF.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_SF);
                }
                if (surveyStation.SurveyTool.UseGXYZ_MIS)
                {
                    ErrorSourceGXYZ_MIS errorSourceGXYZ_MIS = new ErrorSourceGXYZ_MIS();
                    errorSourceGXYZ_MIS.Magnitude = (double)surveyStation.SurveyTool.GXYZ_MIS;
                    errorSourceGXYZ_MIS.EarthRotRate = earthRotRate;
                    errorSourceGXYZ_MIS.StartInclination = startInclinationSta;
                    errorSourceGXYZ_MIS.EndInclination = endInclinationSta;
                    errorSourceGXYZ_MIS.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_MIS);
                }
                if (surveyStation.SurveyTool.UseGXY_B1)
                {
                    ErrorSourceGXY_B1 errorSourceGXY_B1 = new ErrorSourceGXY_B1();
                    errorSourceGXY_B1.Magnitude = (double)surveyStation.SurveyTool.GXY_B1;
                    errorSourceGXY_B1.EarthRotRate = earthRotRate;
                    errorSourceGXY_B1.Latitude = Latitude;
                    errorSourceGXY_B1.Convergence = Convergence;
                    errorSourceGXY_B1.StartInclination = startInclinationSta;
                    errorSourceGXY_B1.EndInclination = endInclinationSta;
                    errorSourceGXY_B1.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_B1);
                }
                if (surveyStation.SurveyTool.UseGXY_B2)
                {
                    ErrorSourceGXY_B2 errorSourceGXY_B2 = new ErrorSourceGXY_B2();
                    errorSourceGXY_B2.Magnitude = (double)surveyStation.SurveyTool.GXY_B2;
                    errorSourceGXY_B2.EarthRotRate = earthRotRate;
                    errorSourceGXY_B2.Latitude = Latitude;
                    errorSourceGXY_B2.StartInclination = startInclinationSta;
                    errorSourceGXY_B2.EndInclination = endInclinationSta;
                    errorSourceGXY_B2.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_B2);
                }
                if (surveyStation.SurveyTool.UseGXY_RN)
                {
                    ErrorSourceGXY_RN errorSourceGXY_RN = new ErrorSourceGXY_RN();
                    errorSourceGXY_RN.Magnitude = (double)surveyStation.SurveyTool.GXY_RN;
                    errorSourceGXY_RN.EarthRotRate = earthRotRate;
                    errorSourceGXY_RN.Latitude = Latitude;
                    errorSourceGXY_RN.StartInclination = startInclinationSta;
                    errorSourceGXY_RN.EndInclination = endInclinationSta;
                    errorSourceGXY_RN.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_RN);
                }
                if (surveyStation.SurveyTool.UseGXY_G1)
                {
                    ErrorSourceGXY_G1 errorSourceGXY_G1 = new ErrorSourceGXY_G1();
                    errorSourceGXY_G1.Magnitude = (double)surveyStation.SurveyTool.GXY_G1;
                    errorSourceGXY_G1.EarthRotRate = earthRotRate;
                    errorSourceGXY_G1.Latitude = Latitude;
                    errorSourceGXY_G1.StartInclination = startInclinationSta;
                    errorSourceGXY_G1.EndInclination = endInclinationSta;
                    errorSourceGXY_G1.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_G1);
                }
                if (surveyStation.SurveyTool.UseGXY_G2)
                {
                    ErrorSourceGXY_G2 errorSourceGXY_G2 = new ErrorSourceGXY_G2();
                    errorSourceGXY_G2.Magnitude = (double)surveyStation.SurveyTool.GXY_G2;
                    errorSourceGXY_G2.EarthRotRate = earthRotRate;
                    errorSourceGXY_G2.Latitude = Latitude;
                    errorSourceGXY_G2.StartInclination = startInclinationSta;
                    errorSourceGXY_G2.EndInclination = endInclinationSta;
                    errorSourceGXY_G2.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_G2);
                }
                if (surveyStation.SurveyTool.UseGXY_G3)
                {
                    ErrorSourceGXY_G3 errorSourceGXY_G3 = new ErrorSourceGXY_G3();
                    errorSourceGXY_G3.Magnitude = (double)surveyStation.SurveyTool.GXY_G3;
                    errorSourceGXY_G3.EarthRotRate = earthRotRate;
                    errorSourceGXY_G3.Latitude = Latitude;
                    errorSourceGXY_G3.StartInclination = startInclinationSta;
                    errorSourceGXY_G3.EndInclination = endInclinationSta;
                    errorSourceGXY_G3.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_G3);
                }
                if (surveyStation.SurveyTool.UseGXY_G4)
                {
                    ErrorSourceGXY_G4 errorSourceGXY_G4 = new ErrorSourceGXY_G4();
                    errorSourceGXY_G4.Magnitude = (double)surveyStation.SurveyTool.GXY_G4;
                    errorSourceGXY_G4.EarthRotRate = earthRotRate;
                    errorSourceGXY_G4.Latitude = Latitude;
                    errorSourceGXY_G4.StartInclination = startInclinationSta;
                    errorSourceGXY_G4.EndInclination = endInclinationSta;
                    errorSourceGXY_G4.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_G4);
                }
                if (surveyStation.SurveyTool.UseGXY_SF)
                {
                    ErrorSourceGXY_SF errorSourceGXY_SF = new ErrorSourceGXY_SF();
                    errorSourceGXY_SF.Magnitude = (double)surveyStation.SurveyTool.GXY_SF;
                    errorSourceGXY_SF.EarthRotRate = earthRotRate;
                    errorSourceGXY_SF.Latitude = Latitude;
                    errorSourceGXY_SF.StartInclination = startInclinationSta;
                    errorSourceGXY_SF.EndInclination = endInclinationSta;
                    errorSourceGXY_SF.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_SF);
                }
                if (surveyStation.SurveyTool.UseGXY_MIS)
                {
                    ErrorSourceGXY_MIS errorSourceGXY_MIS = new ErrorSourceGXY_MIS();
                    errorSourceGXY_MIS.Magnitude = (double)surveyStation.SurveyTool.GXY_MIS;
                    errorSourceGXY_MIS.EarthRotRate = earthRotRate;
                    errorSourceGXY_MIS.Latitude = Latitude;
                    errorSourceGXY_MIS.StartInclination = startInclinationSta;
                    errorSourceGXY_MIS.EndInclination = endInclinationSta;
                    errorSourceGXY_MIS.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_MIS);
                }
                if (surveyStation.SurveyTool.UseEXT_REF )
                {
                    ErrorSourceEXT_REF errorSourceEXT_REF = new ErrorSourceEXT_REF();
                    errorSourceEXT_REF.Magnitude = (double)surveyStation.SurveyTool.EXT_REF;
                    ErrorSources.Add(errorSourceEXT_REF);
                }
                if (surveyStation.SurveyTool.UseEXT_TIE)
                {
                    ErrorSourceEXT_TIE errorSourceEXT_TIE = new ErrorSourceEXT_TIE();
                    errorSourceEXT_TIE.Magnitude = (double)surveyStation.SurveyTool.EXT_TIE;
                    ErrorSources.Add(errorSourceEXT_TIE);
                }
                if (surveyStation.SurveyTool.UseEXT_MIS)
                {
                    ErrorSourceEXT_MIS errorSourceEXT_MIS = new ErrorSourceEXT_MIS();
                    errorSourceEXT_MIS.Magnitude = (double)surveyStation.SurveyTool.EXT_MIS;
                    ErrorSources.Add(errorSourceEXT_MIS);
                }
                if (surveyStation.SurveyTool.UseGXYZ_GD)
                {
                    ErrorSourceGXYZ_GD errorSourceGXYZ_GD = new ErrorSourceGXYZ_GD();
                    errorSourceGXYZ_GD.Magnitude = (double)surveyStation.SurveyTool.GXYZ_GD;
                    errorSourceGXYZ_GD.StartInclination = startInclination; //NB!
                    errorSourceGXYZ_GD.EndInclination = endInclination;
                    //errorSourceGXYZ_GD.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_GD);
                }
                if (surveyStation.SurveyTool.UseGXYZ_RW)
                {
                    ErrorSourceGXYZ_RW errorSourceGXYZ_RW = new ErrorSourceGXYZ_RW();
                    errorSourceGXYZ_RW.Magnitude = (double)surveyStation.SurveyTool.GXYZ_RW;
                    errorSourceGXYZ_RW.StartInclination = startInclination; //NB!
                    errorSourceGXYZ_RW.EndInclination = endInclination;
                    //errorSourceGXYZ_RW.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXYZ_RW);
                }
                if (surveyStation.SurveyTool.UseGXY_GD)
                {
                    ErrorSourceGXY_GD errorSourceGXY_GD = new ErrorSourceGXY_GD();
                    errorSourceGXY_GD.Magnitude = (double)surveyStation.SurveyTool.GXY_GD;
                    errorSourceGXY_GD.StartInclination = startInclination; //NB!
                    errorSourceGXY_GD.EndInclination = endInclination;
                    //errorSourceGXY_GD.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_GD);
                }
                if (surveyStation.SurveyTool.UseGXY_RW)
                {
                    ErrorSourceGXY_RW errorSourceGXY_RW = new ErrorSourceGXY_RW();
                    errorSourceGXY_RW.Magnitude = (double)surveyStation.SurveyTool.GXY_RW;
                    errorSourceGXY_RW.StartInclination = startInclination; //NB!
                    errorSourceGXY_RW.EndInclination = endInclination;
                    //errorSourceGXY_RW.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGXY_RW);
                }
                if (surveyStation.SurveyTool.UseGZ_GD)
                {
                    ErrorSourceGZ_GD errorSourceGZ_GD = new ErrorSourceGZ_GD();
                    errorSourceGZ_GD.Magnitude = (double)surveyStation.SurveyTool.GZ_GD;
                    errorSourceGZ_GD.StartInclination = startInclinationZ; //NB!
                    errorSourceGZ_GD.EndInclination = endInclinationZ;
                    //errorSourceGZ_GD.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGZ_GD);
                }
                if (surveyStation.SurveyTool.UseGZ_RW)
                {
                    ErrorSourceGZ_RW errorSourceGZ_RW = new ErrorSourceGZ_RW();
                    errorSourceGZ_RW.Magnitude = (double)surveyStation.SurveyTool.GZ_RW;
                    errorSourceGZ_RW.StartInclination = startInclinationZ; //NB!
                    errorSourceGZ_RW.EndInclination = endInclinationZ;
                    //errorSourceGZ_RW.InitInclination = initInclination;
                    ErrorSources.Add(errorSourceGZ_RW);
                }
				#region keep until new code veryfied
				if (false &&GyroEx1)
                {
                    ErrorSources.Clear();
                    ErrorSourceAXY_B errorSourceAXY_B = new ErrorSourceAXY_B();
                    errorSourceAXY_B.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXY_B);
                    ErrorSourceAXY_SF errorSourceAXY_SF = new ErrorSourceAXY_SF();
                    errorSourceAXY_SF.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXY_SF);
                    ErrorSourceAXY_MS errorSourceAXY_MS = new ErrorSourceAXY_MS();
                    ErrorSources.Add(errorSourceAXY_MS);
                    ErrorSourceAXY_GB errorSourceAXY_GB = new ErrorSourceAXY_GB();
                    errorSourceAXY_GB.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXY_GB);
                    ErrorSourceGXY_B1 errorSourceGXY_B1 = new ErrorSourceGXY_B1();
                    errorSourceGXY_B1.Latitude = Latitude;
                    errorSourceGXY_B1.Convergence = Convergence;
                    ErrorSources.Add(errorSourceGXY_B1);
                    ErrorSourceGXY_B2 errorSourceGXY_B2 = new ErrorSourceGXY_B2();
                    errorSourceGXY_B2.Latitude = Latitude;
                    ErrorSources.Add(errorSourceGXY_B2);
                    ErrorSourceGXY_RN errorSourceGXY_RN = new ErrorSourceGXY_RN();
                    errorSourceGXY_RN.Latitude = Latitude;
                    ErrorSources.Add(errorSourceGXY_RN);
                    ErrorSourceGXY_G1 errorSourceGXY_G1 = new ErrorSourceGXY_G1();
                    errorSourceGXY_G1.Latitude = Latitude;
                    ErrorSources.Add(errorSourceGXY_G1);
                    ErrorSourceGXY_G2 errorSourceGXY_G2 = new ErrorSourceGXY_G2();
                    errorSourceGXY_G2.Latitude = Latitude;
                    ErrorSources.Add(errorSourceGXY_G2);
                    ErrorSourceGXY_G3 errorSourceGXY_G3 = new ErrorSourceGXY_G3();
                    errorSourceGXY_G3.Latitude = Latitude;
                    ErrorSources.Add(errorSourceGXY_G3);
                    ErrorSourceGXY_G4 errorSourceGXY_G4 = new ErrorSourceGXY_G4();
                    errorSourceGXY_G4.Latitude = Latitude;
                    ErrorSources.Add(errorSourceGXY_G4);
                    ErrorSourceGXY_SF errorSourceGXY_SF = new ErrorSourceGXY_SF();
                    errorSourceGXY_SF.Latitude = Latitude;
                    ErrorSources.Add(errorSourceGXY_SF);
                    ErrorSourceGXY_MIS errorSourceGXY_MIS = new ErrorSourceGXY_MIS();
                    errorSourceGXY_MIS.Latitude = Latitude;
                    ErrorSources.Add(errorSourceGXY_MIS);
                    ErrorSourceXYM1 errorSourceXYM1 = new ErrorSourceXYM1();
                    errorSourceXYM1.Magnitude = 0.1 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceXYM1);
                    ErrorSourceXYM2 errorSourceXYM2 = new ErrorSourceXYM2();
                    errorSourceXYM2.Magnitude = 0.1 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceXYM2);
                    ErrorSourceXYM3 errorSourceXYM3 = new ErrorSourceXYM3();
                    errorSourceXYM3.Magnitude = 0.2 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceXYM3);
                    ErrorSourceXYM4 errorSourceXYM4 = new ErrorSourceXYM4();
                    errorSourceXYM4.Magnitude = 0.2 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceXYM4);
                    ErrorSourceSAG errorSourceSAG = new ErrorSourceSAG();
                    errorSourceSAG.Magnitude = 0.1 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceSAG);                    
                    ErrorSourceDRFR errorSourceDRFR = new ErrorSourceDRFR();
                    errorSourceDRFR.Magnitude = 0.5;
                    ErrorSources.Add(errorSourceDRFR);
                    ErrorSourceDRFS errorSourceDRFS = new ErrorSourceDRFS();
                    errorSourceDRFS.Magnitude = 0.5;
                    ErrorSources.Add(errorSourceDRFS);
                    ErrorSourceDSFS errorSourceDSFS = new ErrorSourceDSFS();
                    errorSourceDSFS.Magnitude = 0.001;
                    ErrorSources.Add(errorSourceDSFS);
                    ErrorSourceDSTG errorSourceDSTG = new ErrorSourceDSTG();
                    errorSourceDSTG.Magnitude = 5.0E-7;
                    ErrorSources.Add(errorSourceDSTG);

                }

                if (false && GyroEx2)
                {
                    ErrorSourceAXY_B errorSourceAXY_B = new ErrorSourceAXY_B();
                    errorSourceAXY_B.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXY_B);
                    ErrorSourceAXY_SF errorSourceAXY_SF = new ErrorSourceAXY_SF();
                    errorSourceAXY_SF.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXY_SF);
                    ErrorSourceAXY_MS errorSourceAXY_MS = new ErrorSourceAXY_MS();
                    ErrorSources.Add(errorSourceAXY_MS);
                    ErrorSourceAXY_GB errorSourceAXY_GB = new ErrorSourceAXY_GB();
                    errorSourceAXY_GB.Gravity = Gravity;
                    ErrorSources.Add(errorSourceAXY_GB);
                    ErrorSourceEXT_REF errorSourceEXT_REF = new ErrorSourceEXT_REF();
                    ErrorSources.Add(errorSourceEXT_REF);
                    ErrorSourceEXT_TIE errorSourceEXT_TIE = new ErrorSourceEXT_TIE();
                    ErrorSources.Add(errorSourceEXT_TIE);
                    ErrorSourceEXT_MIS errorSourceEXT_MIS = new ErrorSourceEXT_MIS();
                    ErrorSources.Add(errorSourceEXT_MIS);
                    ErrorSourceGZ_GD errorSourceGZ_GD = new ErrorSourceGZ_GD();
                    ErrorSources.Add(errorSourceGZ_GD);
                    ErrorSourceGZ_RW errorSourceGZ_RW = new ErrorSourceGZ_RW();
                    ErrorSources.Add(errorSourceGZ_RW);
                    ErrorSourceXYM1 errorSourceXYM1 = new ErrorSourceXYM1();
                    errorSourceXYM1.Magnitude = 0.1 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceXYM1);
                    ErrorSourceXYM2 errorSourceXYM2 = new ErrorSourceXYM2();
                    errorSourceXYM2.Magnitude = 0.1 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceXYM2);
                    ErrorSourceXYM3 errorSourceXYM3 = new ErrorSourceXYM3();
                    errorSourceXYM3.Magnitude = 0.2 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceXYM3);
                    ErrorSourceXYM4 errorSourceXYM4 = new ErrorSourceXYM4();
                    errorSourceXYM4.Magnitude = 0.2 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceXYM4);
                    ErrorSourceSAG errorSourceSAG = new ErrorSourceSAG();
                    errorSourceSAG.Magnitude = 0.1 * Math.PI / 180.0;
                    ErrorSources.Add(errorSourceSAG);
                    ErrorSourceDRFR errorSourceDRFR = new ErrorSourceDRFR();
                    errorSourceDRFR.Magnitude = 0.5;
                    ErrorSources.Add(errorSourceDRFR);
                    ErrorSourceDRFS errorSourceDRFS = new ErrorSourceDRFS();
                    errorSourceDRFS.Magnitude = 0.5;
                    ErrorSources.Add(errorSourceDRFS);
                    ErrorSourceDSFS errorSourceDSFS = new ErrorSourceDSFS();
                    errorSourceDSFS.Magnitude = 0.001;
                    ErrorSources.Add(errorSourceDSFS);
                    ErrorSourceDSTG errorSourceDSTG = new ErrorSourceDSTG();
                    errorSourceDSTG.Magnitude = 5.0E-7;
                    ErrorSources.Add(errorSourceDSTG);

                }
                else if (false && ErrorIndices == null)
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
                else if(false)
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
                #endregion
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
            bool allSystematic = false;
            for(int e = 0;e< ISCWSAErrorDataTmp.Count;e++)
			{
                if(ISCWSAErrorDataTmp[e].IsInitialized)
				{
                    allSystematic = true;
				}
            }
            for (int i = 0; i < errorSources.Count; i++)
            {
                bool isInitialized = ISCWSAErrorDataTmp[i].IsInitialized;
                sigmaerandom = ISCWSAErrorDataTmp[i].SigmaErrorRandom;
                bool singular = false;                
               
                double[] dpde = new double[3]; //weighting function – the effect of the ith error source on the survey measurement vector
                double? depth = 0.0;
                depth = errorSources[i].FunctionDepth(surveyStation.MD, (double)surveyStation.Z); //Depth
                if (false && errorSources[i] is ErrorSourceDSFS)
                {
                    ErrorSourceDSFS ds = new ErrorSourceDSFS();
                    depth = ds.FunctionDepthGyro(surveyStation.MD, (double)surveyStation.Z, (double)surveyStationPrev.MD, (double)surveyStation.Incl); //Depth
                }
                if (false && errorSources[i] is ErrorSourceDSTG)
                {
                    ErrorSourceDSTG ds = new ErrorSourceDSTG();
                    depth = ds.FunctionDepthGyro(surveyStation.MD, (double)surveyStation.Z, (double)surveyStationPrev.MD, (double)surveyStation.Incl); //Depth
                }
                dpde[0] = (double)depth;
                double? inclination = errorSources[i].FunctionInc((double)surveyStation.Incl, (double)surveyStation.Az); //Inclination
                dpde[1] = (double)inclination;
                
                double? azimuth = errorSources[i].FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az); //Azimuth
                double initializationDepth = ISCWSAErrorDataTmp[i].InitializationDepth;
                double minDistance = 999999;// 99999.0; //Minimum distance between initializations. NB! make configurable
                bool reInitialize = ReInitialize((double)surveyStation.Incl, (double)surveyStationPrev.Incl, errorSources, surveyStation.MD - ISCWSAErrorDataTmp[i].InitializationDepth, minDistance);
                if (errorSources[i].IsContinuous)
                {
                    double deltaD = (double)surveyStation.MD - (double)surveyStationPrev.MD;
                    double c_gyro = 0.6; //Running speed. NB! make configurable
                    if ((isInitialized && (surveyStation.Incl < errorSources[i].StartInclination || surveyStation.Incl > errorSources[i].EndInclination))) //NB! include initialization inclination code
                    {
						isInitialized = false;            //New            
					}
					if ((!isInitialized && surveyStation.Incl>=errorSources[i].StartInclination && surveyStation.Incl <= errorSources[i].EndInclination) || (isInitialized && (surveyStation.MD- ISCWSAErrorDataTmp[i].InitializationDepth)> minDistance)) //NB! include initialization inclination code
                    {
                        isInitialized = true;
                        ISCWSAErrorDataTmp[i].GyroH = 0.0;
                        initializationDepth = surveyStation.MD;
                    }
                    if( false && reInitialize) //New
					{
                        ISCWSAErrorDataTmp[i].GyroH = 0.0;
                        initializationDepth = surveyStation.MD;
                    }
                    
                    double h = ISCWSAErrorDataTmp[i].GyroH;
                    if (errorSources[i] is ErrorSourceGXYZ_GD)
                    {
                        ErrorSourceGXYZ_GD da = new ErrorSourceGXYZ_GD();
                        da.StartInclination = errorSources[i].StartInclination;
                        da.EndInclination = errorSources[i].EndInclination;
                        h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az, h, c_gyro, deltaD);
                    }
                    if (errorSources[i] is ErrorSourceGXYZ_RW)
                    {
                        ErrorSourceGXYZ_RW da = new ErrorSourceGXYZ_RW();
                        da.StartInclination = errorSources[i].StartInclination;
                        da.EndInclination = errorSources[i].EndInclination;
                        h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az, h, c_gyro, deltaD);
                    }
                    if (errorSources[i] is ErrorSourceGXY_GD)
                    {
                        ErrorSourceGXY_GD da = new ErrorSourceGXY_GD();
                        da.StartInclination = errorSources[i].StartInclination;
                        da.EndInclination = errorSources[i].EndInclination;
                        h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az, h, c_gyro, deltaD, (double)surveyStationPrev.Incl);
                        if (h == 0 && ISCWSAErrorDataTmp[i].GyroH!=0)
                        {
                            h = ISCWSAErrorDataTmp[i].GyroH;
                        }
                    }
                    if (errorSources[i] is ErrorSourceGXY_RW)
                    {
                        ErrorSourceGXY_RW da = new ErrorSourceGXY_RW();
                        da.StartInclination = errorSources[i].StartInclination;
                        da.EndInclination = errorSources[i].EndInclination;
                        h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az, h, c_gyro, deltaD, (double)surveyStationPrev.Incl);
                        if (h == 0 && ISCWSAErrorDataTmp[i].GyroH != 0)
                        {
                            h = ISCWSAErrorDataTmp[i].GyroH;
                        }
                    }
                    if (errorSources[i] is ErrorSourceGZ_GD)
                    {
                        ErrorSourceGZ_GD da = new ErrorSourceGZ_GD();
                        da.StartInclination = errorSources[i].StartInclination;
                        da.EndInclination = errorSources[i].EndInclination;
                        h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az, h, c_gyro, deltaD, (double)surveyStationPrev.Incl);
                        if(h==0 && ISCWSAErrorDataTmp[i].GyroH != 0)
						{
                            h = ISCWSAErrorDataTmp[i].GyroH;
                        }
                    }
                    if (errorSources[i] is ErrorSourceGZ_RW)
                    {
                        ErrorSourceGZ_RW da = new ErrorSourceGZ_RW();
                        da.StartInclination = errorSources[i].StartInclination;
                        da.EndInclination = errorSources[i].EndInclination;
                        h = (double)da.FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az, h, c_gyro, deltaD, (double)surveyStationPrev.Incl);
                        if (h == 0 && ISCWSAErrorDataTmp[i].GyroH != 0)
                        {
                            h = ISCWSAErrorDataTmp[i].GyroH;
                        }
                    }
                    //ISCWSAErrorDataTmp[i].GyroH = h;
                    azimuth = h;
                    ISCWSAErrorDataTmp[i].IsInitialized = isInitialized;
                }
                continuousMode_ = IsContinuousMode(errorSources, (double)surveyStation.Incl);
                if (IsStationary(errorSources[i]) && (isInitialized && surveyStation.Incl < errorSources[i].EndInclination)) //NB! include initialization inclination code
                {
                    isInitialized = false;
                }
                if (IsStationary(errorSources[i]) && isInitialized && (surveyStation.MD - ISCWSAErrorDataTmp[i].InitializationDepth) > minDistance)
				{
                    azimuth = errorSources[i].FunctionAz((double)errorSources[i].InitInclination, (double)surveyStation.Az); //Azimuth NB! Unsure
                    ISCWSAErrorDataTmp[i].GyroH = (double)azimuth;
                    initializationDepth = surveyStation.MD;
                }
                if (IsStationary(errorSources[i]) && (isInitialized || (!isInitialized && surveyStation.Incl > errorSources[i].EndInclination) || continuousMode_) )
				{
                    if(false &&reInitialize) //New
					{						
						azimuth = errorSources[i].FunctionAz(errorSources[i].InitInclination, (double)surveyStation.Az); //Azimuth
                        //azimuth = errorSources[i].FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az); //Azimuth
                        initializationDepth = surveyStation.MD;
                    }
                    //if (false && errorSources[i].InitInclination < 0 && !isInitialized) //New
                    //{
                    //    //double tmp = errorSources[i].EndInclination;
                    //    //errorSources[i].EndInclination = 1.0;
                    //    //azimuth = errorSources[i].FunctionAz((double)surveyStation.Incl, (double)surveyStation.Az); //Azimuth
                    //    //errorSources[i].EndInclination = tmp;
                    //}
                    else
                    {
                        azimuth = ISCWSAErrorDataTmp[i].GyroH;
                    }
                    if (!isInitialized  && (surveyStation.Incl > errorSources[i].EndInclination|| errorSources[i].InitInclination < 0))
                    {
                        if (errorSources[i] is ErrorSourceGXY_RN || errorSources[i] is ErrorSourceGXYZ_XYRN)
                        {
                            double noiseredFactor = 1.0;// 0.5; //NB! Configurable
                            azimuth = noiseredFactor * azimuth;// noiseredFactor * ISCWSAErrorDataTmp[i].GyroH;
                        }
                        initializationDepth = surveyStation.MD;
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
                if (errorSources[i].IsRandom && !allSystematic)
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
                if (ISCWSAErrorDataTmp[i].IsInitialized && errorSources[i].IsContinuous)
				{
					isContinuous = true;
				}
                if (errorSources[i].IsContinuous && incl >= errorSources[i].StartInclination) //New
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
                if ((IsStationary(errorSources[i]) && incl <= errorSources[i].InitInclination && inclPrev > errorSources[i].InitInclination) || dist > minDist)
                {
                    reInitialize = true;
                }
            }
            return reInitialize;
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
        bool IsContinuous { get; }
        double Magnitude { get; set; }
        double StartInclination { get; set; } 
        double EndInclination { get; set; } 
        double InitInclination { get; set; }
        double? FunctionDepth(double md, double tvd);
        double? FunctionInc(double incl, double az);
        double? FunctionAz(double incl, double az);
        double FunctionSingularityNorth(double az);
        double FunctionSingularityEast(double az);
        double FunctionSingularityVert();
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double w12 = Math.Sin(incl); //NB! Make configurable
            return w12;
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            double w12 = Math.Sin(incl); //NB! Make configurable
            //return -w12 / Math.Sin(incl); //Azimuth //NB! what about when incl=0?
            return -1; //Azimuth
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Misalignment: XY Misalignment 3 error source
    /// </summary>
    public class ErrorSourceXYM3 : IErrorSource
    {
        public ErrorSourceXYM3()
        {

        }
        public string ErrorCode
        {
            get { return "XYM3"; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Convergence = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = true;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double w34 = Math.Abs(Math.Cos(incl));//NB!  Make configurable New
            return w34 * Math.Cos(az+Convergence);
        }
        public double? FunctionAz(double incl, double az)
        {
            if (incl < 0.0001 * Math.PI / 180.0)
            {
                return null;

            }
            else
            {
                double w34 = Math.Abs(Math.Cos(incl)); //NB!  Make configurable New
                return -w34 * Math.Sin(az+Convergence) / Math.Sin(incl); ; //Azimuth //NB! Convergence
            }
        }
        public double FunctionSingularityNorth(double az) { return 1; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the Misalignment: XY Misalignment 4 error source
    /// </summary>
    public class ErrorSourceXYM4 : IErrorSource
    {
        public ErrorSourceXYM4()
        {

        }
        public string ErrorCode
        {
            get { return "XYM4"; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Convergence = 0.0 * Math.PI / 180.0;
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        
        public bool SingularIssues { get; } = true;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double w34 = Math.Abs(Math.Cos(incl)); //NB!  Make configurable New
            return w34 * Math.Sin(az+Convergence);
        }
        public double? FunctionAz(double incl, double az)
        {
            if (incl < 0.0001 * Math.PI / 180.0)
            {
                return null;

            }
            else
            {
                double w34 = Math.Abs(Math.Cos(incl)); //NB!  Make configurable New
                return w34 * Math.Cos(az+Convergence) / Math.Sin(incl); //Azimuth
            }
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 1; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to theVertical Sag error source
    /// </summary>
    public class ErrorSourceSAG : IErrorSource
    {
        public ErrorSourceSAG()
        {

        }
        public string ErrorCode
        {
            get { return "SAG"; }
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return Math.Sin(incl);
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
        public bool IsContinuous
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
    /// Error due to the Depth: Depth Reference - Systematic error source
    /// </summary>
    public class ErrorSourceDRFS : IErrorSource
    {
        public ErrorSourceDRFS()
        {

        }
        public string ErrorCode
        {
            get { return "DRFS"; }
        }
        public int Index
        {
            get { return 1; }
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
        public bool SingularIssues { get; } = false;
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.5;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
       
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.00056;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            //return md-mdPrev; //NB! 
			return md; //NB! 
		}
        public double? FunctionDepthGyro(double md, double tvd, double mdPrev, double incl)
        {
            return md - mdPrev; //NB! 
            //return md; //NB! 
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
            get { return true; }
        }
        public bool IsRandom
        {
            get { return false; }
        }
        public bool IsGlobal
        {
            get { return true; }
        }
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.00000025;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
			return md * tvd;//NB!
							//return (md*Math.Cos(incl) +tvd)*(md-mdPrev);
		}
        public double? FunctionDepthGyro(double md, double tvd, double mdPrev, double incl)
        {
            //return md * tvd;//NB!
            return (md * Math.Cos(incl) + tvd) * (md - mdPrev);
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
    /// Error due to the Misalignment: XY Misalignment 3E error source
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
        public bool IsContinuous
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
    /// Error due to the Misalignment: XY Misalignment 4E error source
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
        public bool IsContinuous
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.2 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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

    //NB! XCLI1/XCLI2
    
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.004;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.004;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]

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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.004;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0005;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0005;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0005;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0005;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 70.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 70.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 70.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0016;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0016;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0016;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.0016;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 220.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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

    //NB! ABIXY-TI1/ABIXY-TI2/ABIZ
    //NB! ASIXY-TI1/ASIXY-TI2/ASIXY-TI3/ASIZ
    //NB! MBIXY-TI1/MBIXY-TI2/MBIZ
    //NB! MSIXY-TI1/MSIXY-TI2/MSIXY-TI3/
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.16 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.24 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.2 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.05 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; }
        public double Dip { get; set; }
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 / Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 2350.0 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 3359.0 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 2840.0 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 356.0 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public bool IsContinuous
        {
            get { return false; }
        }
        public double Latitude { get; set; }
        public double Gravity { get; set; }
        public double BField { get; set; } = 50000;
        public double Dip { get; set; } = 72.0 * Math.PI / 180.0;
        public double Declination { get; set; }
        public double Magnitude { get; set; } = 3000.0 * Math.PI / 180.0;
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
     //NB! MFI
     //NB! MDI
    
    
    
    
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
            get { return false; }
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return Math.Cos(incl) / Gravity;
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
            get { return false; }
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            return Math.Sin(incl) / Gravity;
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
            get { return false; }
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            get { return false; }
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            get { return false; }
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double kOperator { get; set; } = 1; // 1 or -1 depending on inclination
        public double CantAngle { get; set; }
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double k = 1;
            if (kOperator == 0 && incl > 90.0 * Math.PI / 180.0)
            {
                k = -1;
            }
            return 1 / (Gravity * Math.Cos(incl - k * CantAngle));
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
            get { return false; }
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double kOperator { get; set; } = 1; // 1 or -1 depending on inclination
        public double CantAngle { get; set; }
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double k = 1;
            if (kOperator == 0 && incl > 90.0 * Math.PI / 180.0)
            {
                k = -1;
            }
            return Math.Tan(incl - k * CantAngle);
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
            get { return false; }
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
        public double Magnitude { get; set; } = 0.05 * Math.PI / 180.0; //NB! Check
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            get { return false; }
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double kOperator { get; set; } = 1; // 1 or -1 depending on inclination
        public double CantAngle { get; set; }
        public double? FunctionDepth(double md, double tvd)
        {
            return 0.0;
        }
        public double? FunctionInc(double incl, double az)
        {
            double k = 1;
            if (kOperator == 0 && incl > 90.0 * Math.PI / 180.0)
            {
                k = -1;
            }
            return Math.Tan(incl - k * CantAngle) / Gravity;
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
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0 / 3600; //[rad/s]. 
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Sin(AzT) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
            }
			else
			{
                return 0.0;
			}
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
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Cos(AzT) / (EarthRotRate * Math.Cos(Latitude));
            }
			else
			{
                return 0.0;
			}
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
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Sqrt(1 - (Math.Sin(AzT) * Math.Sin(AzT) * Math.Sin(incl) * Math.Sin(incl))) / (EarthRotRate * Math.Cos(Latitude));
            }
			else
			{
                return 0.0;
			}
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        {if (true || incl < EndInclination)
			{
				double AzT = az + Convergence;
				return Math.Cos(AzT) * Math.Sin(incl) / (EarthRotRate * Math.Cos(Latitude));
			}
			else
			{
				return 0.0;
			}        
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        {if (true || incl < EndInclination)
			{
				double AzT = az + Convergence;
				return Math.Cos(AzT) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
			}
			else
			{
				return 0.0;
			}        
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Sin(AzT) * Math.Cos(incl) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
            }
			else
			{
                return 0.0;
			}
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Sin(AzT) * Math.Sin(incl) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
            }
            else
            {
                return 0.0;
            }
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
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Sin(AzT) * Math.Sin(incl) / (EarthRotRate * Math.Cos(Latitude));
            }
			else
			{
                return 0.0;
			}
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
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Sin(AzT) * Math.Sin(incl) / (EarthRotRate * Math.Cos(Latitude));
            }
            else
            {
                return 0.0;
            }

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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Sin(AzT) * Math.Sin(incl) * Math.Sin(incl) / (EarthRotRate * Math.Cos(Latitude));
            }
			else
			{
                return 0.0;
			}
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Sin(AzT) * Math.Sin(incl) * Math.Cos(incl) / (EarthRotRate * Math.Cos(Latitude));
            }
            else
            {
                return 0.0;
            }
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return Math.Tan(Latitude) * Math.Sin(AzT) * Math.Sin(incl) * Math.Cos(incl);
            }
			else
			{
                return 0.0;
			}
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (true || incl < EndInclination)
            {
                double AzT = az + Convergence;
                return 1 / Math.Cos(Latitude);
            }
            else {
                return 0.0;
            }
				
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
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
        public bool SingularIssues { get; } = false;
        public double EarthRotRate { get; set; } = 7.292115e-5; //7.292115e-5; //[rad/s]
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
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public double Magnitude { get; set; } = 0.1 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
			//return NoiseRedFactor * Math.Sqrt((1 - Math.Cos(AzT) * Math.Cos(AzT) * Math.Sin(incl) * Math.Sin(incl))) / (EarthRotRate * Math.Cos(Latitude) * Math.Cos(incl));
			return Math.Sqrt((1 - Math.Cos(AzT) * Math.Cos(AzT) * Math.Sin(incl) * Math.Sin(incl))) / (EarthRotRate * Math.Cos(Latitude) * Math.Cos(incl));
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public double Magnitude { get; set; } = 5.0 * Math.PI / 180.0; //[rad]. 
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public double Magnitude { get; set; } = 0.0 * Math.PI / 180.0; //[rad]. 
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
        public double Magnitude { get; set; } = 0.0 * Math.PI / 180.0; //[rad].
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            if (Magnitude > 0) //NB!
            {
                return 1.0 / Math.Sin(incl);
            }
			else
			{
                return 0;
			}
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            return 0.0;
        }
        public double? FunctionAz(double incl, double az, double h, double c, double deltaD)
        {
            if (incl >= StartInclination && incl < EndInclination)
            {
                return h + deltaD / c;
            }
			else
			{
                return 0.0;
			}
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis, continuous: xyz gyro random walk error source
    /// </summary>
    public class ErrorSourceGXYZ_RW : IErrorSource
    {
        public ErrorSourceGXYZ_RW()
        {

        }
        public string ErrorCode
        {
            get { return "GXYZ_RW"; }
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / Math.Sqrt(3600); //[rad/sqrt(s)]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            return 0.0;
        }
        public double? FunctionAz(double incl, double az, double h, double c, double deltaD)
        {
            if (incl >= StartInclination && incl < EndInclination)
            {
                return Math.Sqrt((h * h) + deltaD / c);
            }
			else 
            { 
                return 0.0;
            }
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis, continuous: xy gyro drift error source
    /// </summary>
    public class ErrorSourceGXY_GD : IErrorSource
    {
        public ErrorSourceGXY_GD()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_GD"; }
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
       
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
            return 0.0;
        }
        public double? FunctionAz(double incl, double az, double h, double c, double deltaD, double inclPrev)
        {
            if (incl >= StartInclination && incl < EndInclination)
            {
                return h + ((1 / Math.Sin((inclPrev + incl) / 2)) * deltaD / c);
            }
			else
			{
                return 0.0;
			}
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the 3-axis, continuous: xy gyro random walk error source
    /// </summary>
    public class ErrorSourceGXY_RW : IErrorSource
    {
        public ErrorSourceGXY_RW()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_RW"; }
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
        public double Magnitude { get; set; } = 0.5 * Math.PI / 180.0 / Math.Sqrt(3600); //[rad/sqrt(s)]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            return 0.0;
        }
        public double? FunctionAz(double incl, double az, double h, double c, double deltaD, double inclPrev)
        {
            if (incl >= StartInclination && incl < EndInclination)
            {
                return Math.Sqrt((h * h) + ((1 / (Math.Sin((inclPrev + incl) / 2) * Math.Sin((inclPrev + incl) / 2))) * deltaD / c));
            }
			else
			{
                return 0.0;
			}
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the z-axis, continuous: z gyro drift error source
    /// </summary>
    public class ErrorSourceGZ_GD : IErrorSource
    {
        public ErrorSourceGZ_GD()
        {

        }
        public string ErrorCode
        {
            get { return "GXY_GD"; }
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
        public double Magnitude { get; set; } = 1.0 * Math.PI / 180.0 / 3600; //[rad/s]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            return 0.0;
        }
        public double? FunctionAz(double incl, double az, double h, double c, double deltaD, double inclPrev)
        {
            if (incl >= StartInclination && incl < EndInclination)
            {
                return h + ((1 / Math.Cos((inclPrev + incl) / 2)) * deltaD / c);
            }
			else
			{
                return 0.0;
			}
        }
        public double FunctionSingularityNorth(double az) { return 0; }
        public double FunctionSingularityEast(double az) { return 0; }
        public double FunctionSingularityVert() { return 0; }
    }
    /// <summary>
    /// Error due to the z-axis, continuous: z gyro random walk error source
    /// </summary>
    public class ErrorSourceGZ_RW : IErrorSource
    {
        public ErrorSourceGZ_RW()
        {

        }
        public string ErrorCode
        {
            get { return "GZ_RW"; }
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
        public double Magnitude { get; set; } = 1 * Math.PI / 180.0 / Math.Sqrt(3600); //[rad/sqrt(s)]
        public double StartInclination { get; set; } = 0; //[rad]
        public double EndInclination { get; set; } = 0; //[rad]
        public double InitInclination { get; set; } = 0; //[rad]
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
            return 0.0;
        }
        public double? FunctionAz(double incl, double az, double h, double c, double deltaD, double inclPrev)
        {
            if (incl >= StartInclination && incl < EndInclination)
            {
                return Math.Sqrt((h * h) + ((1 / (Math.Cos((inclPrev + incl) / 2) * Math.Cos((inclPrev + incl) / 2))) * deltaD / c));
            }
            else
            {
                return 0.0;
            }
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
        public double GyroH { get; set; }
        public bool IsInitialized = false;
        public double InitializationDepth = 0.0;
    }
}
