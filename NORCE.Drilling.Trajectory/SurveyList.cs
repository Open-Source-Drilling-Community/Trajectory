using System;
using System.Collections.Generic;
using System.Text;
using NORCE.General.Math;
using NORCE.General.Std;
using NORCE.General.Coordinates;
using NORCE.General.Octree;
using NORCE.Drilling.SurveyInstrument.Model;
using NORCE.Drilling.Well.Model;


namespace NORCE.Drilling.Trajectory
{
    public class SurveyList
    {
        /// <summary>
        /// Uncertainty Envelope
        /// </summary>
        public List<UncertaintyEnvelopeEllipse> UncertaintyEnvelope { get; set; }
        private List<SurveyStation> _surveyList = null;
        protected static double[,] _chiSquare3D = new double[,] { { 0.05, 0.10, 0.2, 0.3, 0.5, 0.7, 0.8, 0.9, 0.95, 0.99, 0.999 }, { 0.35, 0.58, 1.01, 1.42, 2.37, 3.66, 4.64, 6.25, 7.82, 11.34, 16.27 } };
        private bool _useWdwCovariance = true;
        public int _ellipseVerticesPhi = 32;
        public int _intermediateEllipseNumbers = 6;
        public double MaxDistanceEllipse { get; set; } = 3;
        public double MaxDistanceCoordinate { get; set; } = 3;
        public CoordinateConverter.UTMCoordinate WellUTMCoordinate { get; set; }
        public Point3D EntryPoint { get; set; }
        public bool UseUncertaintyCylinder { get; set; } = false;
        public int EllipseVerticesPhi
        {
            get { return _ellipseVerticesPhi; }
            set { _ellipseVerticesPhi = value; }
        }
        public int IntermediateEllipseNumbers
        {
            get { return _intermediateEllipseNumbers; }
            set { _intermediateEllipseNumbers = value; }
        }
        public List<SurveyStation> ListOfSurveys
		{
            get { return _surveyList; }
            set { _surveyList = value; }
        }

        public List<SurveyStation> Surveys { get; set; } = new List<SurveyStation>();

        public List<Point3D> pointx = new List<Point3D>();
        public List<Point3D> pointy = new List<Point3D>();
        /// <summary>
        /// default constructor
        /// </summary>
        public SurveyList()
        {
            _surveyList = new List<SurveyStation>();
        }

        public SurveyList(SurveyList item)
        {           
            if (item != null)
            {                
                _surveyList = new List<SurveyStation>();
                for (int i = 0; i < item._surveyList.Count; i++)
                {
                    _surveyList.Add(item._surveyList[i]);
                }
            }
        }
        
        /// <summary>
        /// calculate the trajectory using the minimum curvature method
        /// </summary>
        public void CalculateMinimumCurvatureMethod()
        {
            if (_surveyList != null && _surveyList.Count > 0 && !_surveyList[0].IsUndefined())
            {
                for (int i = 1; i < _surveyList.Count; i++)
                {
                    _surveyList[i-1].MinimumCurvatureMethod(_surveyList[i]);
                }
            }
        }

        /// <summary>
        /// Find the minimum MD-delta between two survey's
        /// </summary>
        public double? MinimumMDBetweenSurveyStations()
        {
            double? minDeltaMD = null;
            if (_surveyList != null && _surveyList.Count > 1)
            {
                for (int i = 0; i < _surveyList.Count - 1; i++)
                {
                    var deltaMD = _surveyList[i + 1].MdWGS84 - _surveyList[i].MdWGS84;
                    if (Numeric.IsDefined(deltaMD) && (minDeltaMD == null || Numeric.LT(deltaMD, minDeltaMD)))
                    {
                        minDeltaMD = deltaMD;
                    }
                }
            }
            return minDeltaMD;
        }

        /// <summary>
        /// Find the maximum MD-delta between two survey's
        /// </summary>
        public double? MaximumMDBetweenSurveyStations()
        {
            double? maxDeltaMD = null;
            if (_surveyList != null && _surveyList.Count > 1)
            {
                for (int i = 0; i < _surveyList.Count - 1; i++)
                {
                    var deltaMD = _surveyList[i + 1].MdWGS84 - _surveyList[i].MdWGS84;
                    if (Numeric.IsDefined(deltaMD) && (maxDeltaMD == null || Numeric.GT(deltaMD, maxDeltaMD)))
                    {
                        maxDeltaMD = deltaMD;
                    }
                }
            }
            return maxDeltaMD;
        }

        public int Count
        {
            get
            {
                if (_surveyList == null)
                {
                    return 0;
                }
                else
                {
                    return _surveyList.Count;
                }
            }
        }

        public SurveyStation this[int index]
        {
            get
            {
                return _surveyList[index];
            }
            set
            {
                _surveyList[index] = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Add(SurveyStation value)
        {
            _surveyList.Add(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void Insert(int index, SurveyStation value)
        {
            _surveyList.Insert(index, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="confidenceFactor"></param>
        /// <param name="scalingFactor"></param>
        /// <param name="bounds"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public Octree<OctreeCode> GetOctree(double confidenceFactor, double scalingFactor, Bounds bounds, int depth)
        {
            Octree<OctreeCode> octree = new Octree<OctreeCode>(bounds);
            return octree;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="confidenceFactor"></param>
        /// <param name="scalingFactor"></param>
        /// <param name="minTVD"></param>
        /// <param name="maxTVD"></param>
        /// <param name="calculateEllipseAreaCoordinates"></param>
        /// <returns></returns>
        public List<UncertaintyEnvelopeEllipse> GetUncertaintyEnvelopeTVD(double confidenceFactor, double scalingFactor = 1.0, double? minTVD = null, double? maxTVD = null, int? maxEllipsesCount = null, bool calculateEllipseAreaCoordinates = false)
        {
            return GetUncertaintyEnvelope(confidenceFactor, scalingFactor, minTVD, maxTVD, null, null, maxEllipsesCount, calculateEllipseAreaCoordinates);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="confidenceFactor"></param>
        /// <param name="scalingFactor"></param>
        /// <param name="minTVD"></param>
        /// <param name="maxTVD"></param>
        /// <param name="calculateEllipseAreaCoordinates"></param>
        /// <returns></returns>
        public List<UncertaintyEnvelopeEllipse> GetUncertaintyEnvelopeMD(double confidenceFactor, double scalingFactor = 1.0, double? minMD = null, double? maxMD = null, bool calculateEllipseAreaCoordinates = false)
        {
            return GetUncertaintyEnvelope(confidenceFactor, scalingFactor, null, null, minMD, maxMD, null, calculateEllipseAreaCoordinates);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="confidenceFactor"></param>
        /// <param name="scalingFactor"></param>
        /// <param name="calculateEllipseAreaCoordinates"></param>
        /// <returns></returns>
        public List<UncertaintyEnvelopeEllipse> GetUncertaintyEnvelope(double confidenceFactor, double scalingFactor = 1.0, bool calculateEllipseAreaCoordinates = false)
        {
            return GetUncertaintyEnvelope(confidenceFactor, scalingFactor, null, null, null, null, null, calculateEllipseAreaCoordinates);
        }

        public List<UncertaintyEnvelopeEllipse> GetUncertaintyEnvelope(double confidenceFactor, double scalingFactor, Well.Model.Well well, Cluster.Model.Cluster cluster)
        {
            return GetUncertaintyEnvelope(confidenceFactor, scalingFactor, null, null, null, null, null, false, well, cluster);
        }

        private List<UncertaintyEnvelopeEllipse> GetUncertaintyEnvelope(double confidenceFactor, double scalingFactor = 1.0, double? minTVD = null, double? maxTVD = null, double? minMD = null, double? maxMD = null, int? maxEllipsesCount = null, bool calculateEllipseAreaCoordinates = false, Well.Model.Well well = null, Cluster.Model.Cluster cluster = null)
        { 
            double[,] A = new double[6, 3];
            for (int i = 0; i < A.GetLength(0); i++)
            {
                for (int j = 0; j < A.GetLength(1); j++)
                {
                    A[i, j] = 0.0;
                }
            }
            List<ISCWSAErrorData> ISCWSAErrorDataTmp = new List<ISCWSAErrorData>();
            List<SurveyStation> surveyList = new List<SurveyStation>();
            // Start from i = 0 to include the first surveystation. This will typically have radius 0
            for (int i = 0; i < _surveyList.Count ; i++)
            {
                bool ok = true;
                if (minTVD != null && maxTVD != null)
                {
                    ok = Numeric.GE(_surveyList[i].TvdWGS84, minTVD) && Numeric.LE(_surveyList[i].TvdWGS84, maxTVD);
                    // We should also inculde the surveys just outside the tvd range to be able to fill the whole requested range
                    if (!ok && i < _surveyList.Count - 1)
                    {
                        if (Numeric.GE(_surveyList[i + 1].TvdWGS84, minTVD) && Numeric.LE(_surveyList[i + 1].TvdWGS84, maxTVD))
                        {
                            // Next survey is ok, then we should also add the current
                            ok = true;
                        }
                    }
                    if (!ok)
                    {
                        if (i > 0 && Numeric.GE(_surveyList[i - 1].TvdWGS84, minTVD) && Numeric.LE(_surveyList[i - 1].TvdWGS84, maxTVD))
                        {
                            // Previous survey was ok, then we should also add the current
                            ok = true;
                        }
                    }
                }
                else if (minMD != null && maxMD != null)
                {
                    ok = Numeric.GE(_surveyList[i].MdWGS84, minMD) && Numeric.LE(_surveyList[i].MdWGS84, maxMD);
                    // We should also inculde the surveys just outside the tvd range to be able to fill the whole requested range
                    if (!ok && i < _surveyList.Count - 1)
                    {
                        if (Numeric.GE(_surveyList[i + 1].MdWGS84, minMD) && Numeric.LE(_surveyList[i + 1].MdWGS84, maxMD))
                        {
                            // Next survey is ok, then we should also add the current
                            ok = true;
                        }
                    }
                    if (!ok)
                    {
                        if (i > 0 && Numeric.GE(_surveyList[i - 1].MdWGS84, minMD) && Numeric.LE(_surveyList[i - 1].MdWGS84, maxMD))
                        {
                            // Previous survey was ok, then we should also add the current
                            ok = true;
                        }
                    }
                }
                if (ok)
                {
                    if(_surveyList[i].Uncertainty==null)
					{
                        WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                        SurveyInstrument.Model.SurveyInstrument surveyTool = new SurveyInstrument.Model.SurveyInstrument(SurveyInstrument.Model.SurveyInstrument.WdWGoodMag);
                        _surveyList[i].SurveyTool = surveyTool;
                        _surveyList[i].Uncertainty = wdwun;
                    }
                    //if (((_useWdwCovariance == _surveyList[i].Uncertainty is WdWSurveyStationUncertainty && i > 0) || (_surveyList.Count>1 && _surveyList[i].Uncertainty.Covariance[0,0]==null )) )
                    //{
                    //    WdWSurveyStationUncertainty wdwSurveyStatoinUncertainty = (WdWSurveyStationUncertainty)_surveyList[i].Uncertainty;
                    //    A = wdwSurveyStatoinUncertainty.CalculateCovariances(_surveyList[i], _surveyList[i - 1], A);
                    //}
                    //if (((_surveyList[i].Uncertainty is ISCWSA_SurveyStationUncertainty && i > 0) || (_surveyList.Count > 1 && _surveyList[i].Uncertainty.Covariance[0, 0] == null)))
                    //{
                    //    ISCWSA_SurveyStationUncertainty ISCWSASurveyStatoinUncertainty = (ISCWSA_SurveyStationUncertainty)_surveyList[i].Uncertainty;
                    //    if (i == _surveyList.Count - 1)
                    //    {
                    //        SurveyStation surveyStationNext = new SurveyStation();
                    //        surveyStationNext.NorthOfWellHead  = 0.0;
                    //        surveyStationNext.EastOfWellHead = 0.0;
                    //        surveyStationNext.Incl = 0.0;
                    //        surveyStationNext.AzWGS84 = 0.0;
                    //        surveyStationNext.MD = 0.0;
                    //        ISCWSASurveyStatoinUncertainty.CalculateCovariance(_surveyList[i], _surveyList[i - 1], surveyStationNext, ISCWSAErrorDataTmp, i);
                    //    }
                    //    else
                    //    {
                    //        ISCWSASurveyStatoinUncertainty.CalculateCovariance(_surveyList[i], _surveyList[i - 1], _surveyList[i + 1], ISCWSAErrorDataTmp, i);
                    //    }

                    //    ISCWSAErrorDataTmp = ISCWSASurveyStatoinUncertainty.ISCWSAErrorDataTmp;
                    //}
                    //Always calculate new Covariances
                    if ((_useWdwCovariance == _surveyList[i].Uncertainty is WdWSurveyStationUncertainty && i > 0))
                    {
                        WdWSurveyStationUncertainty wdwSurveyStatoinUncertainty = (WdWSurveyStationUncertainty)_surveyList[i].Uncertainty;
                        A = wdwSurveyStatoinUncertainty.CalculateCovariances(_surveyList[i], _surveyList[i - 1], A);
                    }
                    if ((_surveyList[i].Uncertainty is ISCWSA_SurveyStationUncertainty && i > 0))
                    {
                        ISCWSA_SurveyStationUncertainty ISCWSASurveyStatoinUncertainty = (ISCWSA_SurveyStationUncertainty)_surveyList[i].Uncertainty;
                        if (i == _surveyList.Count - 1)
                        {
                            SurveyStation surveyStationNext = new SurveyStation();
                            surveyStationNext.NorthOfWellHead  = 0.0;
                            surveyStationNext.EastOfWellHead = 0.0;
                            surveyStationNext.Incl = 0.0;
                            surveyStationNext.AzWGS84 = 0.0;
                            surveyStationNext.MdWGS84 = 0.0;
                            ISCWSASurveyStatoinUncertainty.CalculateCovariance(_surveyList[i], _surveyList[i - 1], surveyStationNext, ISCWSAErrorDataTmp, i);
                        }
                        else
                        {
                            ISCWSASurveyStatoinUncertainty.CalculateCovariance(_surveyList[i], _surveyList[i - 1], _surveyList[i + 1], ISCWSAErrorDataTmp, i);
                        }

                        ISCWSAErrorDataTmp = ISCWSASurveyStatoinUncertainty.ISCWSAErrorDataTmp;
                    }
                    _surveyList[i].Uncertainty.Calculate(_surveyList[i], confidenceFactor, scalingFactor);
                    surveyList.Add(_surveyList[i]);
                    if (UseUncertaintyCylinder)
                    {
                        CalculateUncertaintyCylinder(_surveyList[i], confidenceFactor);
                    }
                }
            }
            
            List<UncertaintyEnvelopeEllipse> uncertaintyEnvelope = new List<UncertaintyEnvelopeEllipse>();

            for (int i = 0; i < surveyList.Count-1; i++)
            {
                UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipse = new UncertaintyEnvelopeEllipse();
                
                Vector2D ellipseRadius = surveyList[i].Uncertainty.EllipseRadius;
                Vector2D ellipseRadiusNext = surveyList[i + 1].Uncertainty.EllipseRadius;
                double distance = (double)surveyList[i + 1].MdWGS84 - (double)surveyList[i].MdWGS84;
                _intermediateEllipseNumbers = (int)(distance / MaxDistanceEllipse);

                uncertaintyEnvelopeEllipse.Azimuth = surveyList[i].AzWGS84;
                uncertaintyEnvelopeEllipse.Inclination = surveyList[i].Incl;
                uncertaintyEnvelopeEllipse.X = surveyList[i].NorthOfWellHead;
                uncertaintyEnvelopeEllipse.Y = surveyList[i].EastOfWellHead;
                uncertaintyEnvelopeEllipse.Z = surveyList[i].TvdWGS84;
                uncertaintyEnvelopeEllipse.MD = surveyList[i].MdWGS84;
                uncertaintyEnvelopeEllipse.LatitudeWGS84 = surveyList[i].LatitudeWGS84;
                uncertaintyEnvelopeEllipse.LongitudeWGS84 = surveyList[i].LongitudeWGS84;
                uncertaintyEnvelopeEllipse.EllipseRadius = ellipseRadius;
                uncertaintyEnvelopeEllipse.PerpendicularDirection = surveyList[i].Uncertainty.PerpendicularDirection;
                List<GlobalCoordinatePoint3D> ellipseCoordinates = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipse);

                if (cluster != null && well != null && cluster.Slots != null && !string.IsNullOrEmpty(well.SlotID))
                {

                    Well.Model.WellCoordinateConversionSet conversionSet = new Well.Model.WellCoordinateConversionSet();
                    conversionSet.Cluster = cluster;
                    conversionSet.Well = well;

                    conversionSet.Field = cluster.Field;

                    foreach (GlobalCoordinatePoint3D gc in ellipseCoordinates)
                    {
                        WellCoordinate coordinate = new WellCoordinate();
                        coordinate.NorthOfWellHead = gc.NorthOfWellHead;
                        coordinate.EastOfWellHead = gc.EastOfWellHead;
                        coordinate.TVDWGS84 = gc.TvdWGS84;
                        conversionSet.WellCoordinates.Add(coordinate);
                    }
                    conversionSet.Calculate();
                    for (int j = 0; j < ellipseCoordinates.Count; j++)
                    {
                        ellipseCoordinates[j].LatitudeWGS84 = conversionSet.WellCoordinates[j].LatitudeWGS84;
                        ellipseCoordinates[j].LongitudeWGS84 = conversionSet.WellCoordinates[j].LongitudeWGS84;
                    }
                }
                uncertaintyEnvelopeEllipse.EllipseCoordinates = ellipseCoordinates;
                if (calculateEllipseAreaCoordinates)
                {
                    List<GlobalCoordinatePoint3D> ellipseAreaCoordinates = GetUncertaintyEllipseAreaCoordinates(uncertaintyEnvelopeEllipse);
                    uncertaintyEnvelopeEllipse.EllipseAreaCoordinates = ellipseAreaCoordinates;
                }
                bool ok = true;
                if (minTVD != null && maxTVD != null)
                {
                    ok = Numeric.GE(uncertaintyEnvelopeEllipse.Z, minTVD) && Numeric.LE(uncertaintyEnvelopeEllipse.Z, maxTVD);
                }
                else if (minMD != null && maxMD != null)
                {
                    ok = Numeric.GE(uncertaintyEnvelopeEllipse.MD, minMD) && Numeric.LE(uncertaintyEnvelopeEllipse.MD, maxMD);
                }
                if (ok)
                {
                    uncertaintyEnvelope.Add(uncertaintyEnvelopeEllipse);
                }

                bool skipLast = false;
                // n = 0 corresponds to the uncertaintyEnvelopeEllipse added above
                for (int n = 1; n < _intermediateEllipseNumbers; n++)
                {
                    Vector2D ellipseR = new Vector2D();
                    ellipseR[0] = ellipseRadius[0] + (double)n * (ellipseRadiusNext[0] - ellipseRadius[0]) / (double)_intermediateEllipseNumbers;
                    ellipseR[1] = ellipseRadius[1] + (double)n * (ellipseRadiusNext[1] - ellipseRadius[1]) / (double)_intermediateEllipseNumbers;
                    double inclination = ((double)surveyList[i].Incl + (double)n * ((double)surveyList[i + 1].Incl - (double)surveyList[i].Incl) / (double)_intermediateEllipseNumbers);
                    double azimuth = ((double)surveyList[i].AzWGS84 + (double)n * ((double)surveyList[i + 1].AzWGS84 - (double)surveyList[i].AzWGS84) / (double)_intermediateEllipseNumbers);
                    double north = (double)surveyList[i].NorthOfWellHead  + (double)n * ((double)surveyList[i + 1].NorthOfWellHead  - (double)surveyList[i].NorthOfWellHead ) / (double)_intermediateEllipseNumbers;
                    double east = (double)surveyList[i].EastOfWellHead + (double)n * ((double)surveyList[i + 1].EastOfWellHead - (double)surveyList[i].EastOfWellHead) / (double)_intermediateEllipseNumbers;
                    double tvd = (double)surveyList[i].TvdWGS84 + (double)n * ((double)surveyList[i + 1].TvdWGS84 - (double)surveyList[i].TvdWGS84) / (double)_intermediateEllipseNumbers;
                    double md = (double)surveyList[i].MdWGS84 + (double)n * ((double)surveyList[i + 1].MdWGS84 - (double)surveyList[i].MdWGS84) / (double)_intermediateEllipseNumbers;
                    double perpendicularDirection = surveyList[i].Uncertainty.PerpendicularDirection + (double)n * (surveyList[i + 1].Uncertainty.PerpendicularDirection - surveyList[i + 1].Uncertainty.PerpendicularDirection) / (double)_intermediateEllipseNumbers;

                    //ellipseR[0] = ellipseRadius[0] + (double)_intermediateEllipseNumbers * (ellipseRadiusNext[0] - ellipseRadius[0]) / (double)_intermediateEllipseNumbers;
                    //ellipseR[1] = ellipseRadius[1] + (double)_intermediateEllipseNumbers * (ellipseRadiusNext[1] - ellipseRadius[1]) / (double)_intermediateEllipseNumbers;
                    ////double inclination = ((double)surveyList[i].Incl + (double)_intermediateEllipseNumbers * ((double)surveyList[i + 1].Incl - (double)surveyList[i].Incl) / (double)_intermediateEllipseNumbers);
                    ////double azimuth = ((double)surveyList[i].AzWGS84 + (double)_intermediateEllipseNumbers * ((double)surveyList[i + 1].AzWGS84 - (double)surveyList[i].AzWGS84) / (double)_intermediateEllipseNumbers);
                    ////double north = (double)surveyList[i].NorthOfWellHead  + (double)_intermediateEllipseNumbers * ((double)surveyList[i + 1].NorthOfWellHead  - (double)surveyList[i].NorthOfWellHead ) / (double)_intermediateEllipseNumbers;
                    ////double east = (double)surveyList[i].EastOfWellHead + (double)_intermediateEllipseNumbers * ((double)surveyList[i + 1].EastOfWellHead - (double)surveyList[i].EastOfWellHead) / (double)_intermediateEllipseNumbers;
                    ////double tvd = (double)surveyList[i].TvdWGS84 + (double)_intermediateEllipseNumbers * ((double)surveyList[i + 1].TvdWGS84 - (double)surveyList[i].TvdWGS84) / (double)_intermediateEllipseNumbers;
                    ////double md = (double)surveyList[i].MD + (double)_intermediateEllipseNumbers * ((double)surveyList[i + 1].MD - (double)surveyList[i].MD) / (double)_intermediateEllipseNumbers;
                    //double perpendicularDirection = surveyList[i].Uncertainty.PerpendicularDirection + (double)_intermediateEllipseNumbers * (surveyList[i + 1].Uncertainty.PerpendicularDirection - surveyList[i + 1].Uncertainty.PerpendicularDirection) / (double)_intermediateEllipseNumbers;


                    UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipseInter = new UncertaintyEnvelopeEllipse();
                    uncertaintyEnvelopeEllipseInter.Azimuth = azimuth;
                    uncertaintyEnvelopeEllipseInter.Inclination = inclination;
                    uncertaintyEnvelopeEllipseInter.X = north;
                    uncertaintyEnvelopeEllipseInter.Y = east;
                    uncertaintyEnvelopeEllipseInter.Z = tvd;
                    uncertaintyEnvelopeEllipseInter.EllipseRadius = ellipseR;
                    uncertaintyEnvelopeEllipseInter.PerpendicularDirection = perpendicularDirection;
                    
                    ok = true;
                    if (minTVD != null && maxTVD != null)
                    {
                        ok = Numeric.GE(uncertaintyEnvelopeEllipseInter.Z, minTVD) && Numeric.LE(uncertaintyEnvelopeEllipseInter.Z, maxTVD);
                    }
                    else if (minMD != null && maxMD != null)
                    {
                        ok = Numeric.GE(md, minMD) && Numeric.LE(md, maxMD);
                    }
                    if (ok)
                    {
                        List<GlobalCoordinatePoint3D> ellipseCoordinatesInter = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipseInter);
                        uncertaintyEnvelopeEllipseInter.EllipseCoordinates = ellipseCoordinatesInter;
                        if (calculateEllipseAreaCoordinates)
                        {
                            List<GlobalCoordinatePoint3D> ellipseAreaCoordinatesInter = GetUncertaintyEllipseAreaCoordinates(uncertaintyEnvelopeEllipseInter);
                            uncertaintyEnvelopeEllipseInter.EllipseAreaCoordinates = ellipseAreaCoordinatesInter;
                        }
                        uncertaintyEnvelope.Add(uncertaintyEnvelopeEllipseInter);
                        if (maxEllipsesCount != null && uncertaintyEnvelope.Count == maxEllipsesCount )
                        {
                            skipLast = true;
                            break;
                        }
                    }
                    
                }
                if (skipLast)
                {
                    break;
                }
                if(i == surveyList.Count - 2)
                {
                    uncertaintyEnvelopeEllipse = new UncertaintyEnvelopeEllipse();
                    //Vector2D ellipseRadius = new Vector2D();
                    ellipseRadius = surveyList[surveyList.Count - 1].Uncertainty.EllipseRadius;

                    uncertaintyEnvelopeEllipse.Azimuth = surveyList[surveyList.Count - 1].AzWGS84;
                    uncertaintyEnvelopeEllipse.Inclination = surveyList[surveyList.Count - 1].Incl;
                    uncertaintyEnvelopeEllipse.X = surveyList[surveyList.Count - 1].NorthOfWellHead ;
                    uncertaintyEnvelopeEllipse.Y = surveyList[surveyList.Count - 1].EastOfWellHead;
                    uncertaintyEnvelopeEllipse.Z = surveyList[surveyList.Count - 1].TvdWGS84;
                    uncertaintyEnvelopeEllipse.MD = surveyList[surveyList.Count - 1].MdWGS84;
                    uncertaintyEnvelopeEllipse.EllipseRadius = ellipseRadius;

                    uncertaintyEnvelopeEllipse.PerpendicularDirection = surveyList[surveyList.Count - 1].Uncertainty.PerpendicularDirection;
                   
                    
                    ok = true;
                    if (minTVD != null && maxTVD != null)
                    {
                        ok = Numeric.GE(uncertaintyEnvelopeEllipse.Z, minTVD) && Numeric.LE(uncertaintyEnvelopeEllipse.Z, maxTVD);
                    }
                    else if (minMD != null && maxMD != null)
                    {
                        ok = Numeric.GE(uncertaintyEnvelopeEllipse.MD, minMD) && Numeric.LE(uncertaintyEnvelopeEllipse.MD, maxMD);
                    }
                    if (ok)
                    {
                        ellipseCoordinates = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipse);
                        uncertaintyEnvelopeEllipse.EllipseCoordinates = ellipseCoordinates;
                        if (calculateEllipseAreaCoordinates)
                        {
                            List<GlobalCoordinatePoint3D> ellipseAreaCoordinates = GetUncertaintyEllipseAreaCoordinates(uncertaintyEnvelopeEllipse);
                            uncertaintyEnvelopeEllipse.EllipseAreaCoordinates = ellipseAreaCoordinates;
                        }
                        uncertaintyEnvelope.Add(uncertaintyEnvelopeEllipse);
                    }

                }
            }
            UncertaintyEnvelope = uncertaintyEnvelope;
            return uncertaintyEnvelope;
        }

        public List<UncertaintyEnvelopeEllipse> GetPlainUncertaintyEnvelope(double confidenceFactor, double scalingFactor, double boreholeRadius, int intermediateEllipseNumbers = 0, double? minimumDistanceMD = null)
        {
            double[,] A = new double[6, 3];
            for (int i = 0; i < A.GetLength(0); i++)
            {
                for (int j = 0; j < A.GetLength(1); j++)
                {
                    A[i, j] = 0.0;
                }
            }

            // Start from i = 0 to include the first surveystation. This will typically have radius 0
            for (int i = 0; i < _surveyList.Count; i++)
            {
                if (_surveyList[i].Uncertainty == null)
                {
                    WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                    SurveyInstrument.Model.SurveyInstrument surveyTool = new SurveyInstrument.Model.SurveyInstrument(SurveyInstrument.Model.SurveyInstrument.WdWGoodMag);
                    _surveyList[i].SurveyTool = surveyTool;
                    _surveyList[i].Uncertainty = wdwun;
                }
                if (((_useWdwCovariance == _surveyList[i].Uncertainty is WdWSurveyStationUncertainty && i > 0) || (_surveyList.Count > 1 && _surveyList[i].Uncertainty.Covariance[0, 0] == null)))
                {
                    WdWSurveyStationUncertainty wdwSurveyStatoinUncertainty = (WdWSurveyStationUncertainty)_surveyList[i].Uncertainty;
                    A = wdwSurveyStatoinUncertainty.CalculateCovariances(_surveyList[i], _surveyList[i - 1], A);
                }
                if (_useWdwCovariance == _surveyList[i].Uncertainty is WdWSurveyStationUncertainty && i > 0)
                {
                    WdWSurveyStationUncertainty wdwSurveyStatoinUncertainty = (WdWSurveyStationUncertainty)_surveyList[i].Uncertainty;
                    A = wdwSurveyStatoinUncertainty.CalculateCovariances(_surveyList[i], _surveyList[i - 1], A);
                }
                _surveyList[i].Uncertainty.Calculate(_surveyList[i], confidenceFactor, scalingFactor, boreholeRadius);
            }

            List<UncertaintyEnvelopeEllipse> uncertaintyEnvelope = new List<UncertaintyEnvelopeEllipse>();

            for (int i = 0; i < _surveyList.Count - 1; i++)
            {
                UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipse = new UncertaintyEnvelopeEllipse();

                Vector2D ellipseRadius = new Vector2D();
                ellipseRadius = _surveyList[i].Uncertainty.EllipseRadius;
                Vector2D ellipseRadiusNext = new Vector2D();
                ellipseRadiusNext = _surveyList[i + 1].Uncertainty.EllipseRadius;
                double distance = (double)_surveyList[i + 1].MdWGS84 - (double)_surveyList[i].MdWGS84;

                uncertaintyEnvelopeEllipse.Azimuth = _surveyList[i].AzWGS84;
                uncertaintyEnvelopeEllipse.Inclination = _surveyList[i].Incl;
                uncertaintyEnvelopeEllipse.X = _surveyList[i].NorthOfWellHead ;
                uncertaintyEnvelopeEllipse.Y = _surveyList[i].EastOfWellHead;
                uncertaintyEnvelopeEllipse.Z = _surveyList[i].TvdWGS84;
                uncertaintyEnvelopeEllipse.MD = _surveyList[i].MdWGS84;
                uncertaintyEnvelopeEllipse.EllipseRadius = ellipseRadius;
                List<GlobalCoordinatePoint3D> ellipseCoordinates = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipse);
                uncertaintyEnvelopeEllipse.EllipseCoordinates = ellipseCoordinates;
                uncertaintyEnvelope.Add(uncertaintyEnvelopeEllipse);

                if (minimumDistanceMD != null)
                {
                    intermediateEllipseNumbers = (int)System.Math.Ceiling(distance / (double)minimumDistanceMD);
                }
                for (int n = 1; n < intermediateEllipseNumbers; n++)
                {
                    Vector2D ellipseR = new Vector2D();
                    ellipseR[0] = ellipseRadius[0] + (double)n * (ellipseRadiusNext[0] - ellipseRadius[0]) / (double)intermediateEllipseNumbers;
                    ellipseR[1] = ellipseRadius[1] + (double)n * (ellipseRadiusNext[1] - ellipseRadius[1]) / (double)intermediateEllipseNumbers;

                    double inclination = ((double)_surveyList[i].Incl + (double)n * ((double)_surveyList[i + 1].Incl - (double)_surveyList[i].Incl) / (double)intermediateEllipseNumbers);
                    double azimuth = ((double)_surveyList[i].AzWGS84 + (double)n * ((double)_surveyList[i + 1].AzWGS84 - (double)_surveyList[i].AzWGS84) / (double)intermediateEllipseNumbers);
                    double north = (double)_surveyList[i].NorthOfWellHead  + (double)n * ((double)_surveyList[i + 1].NorthOfWellHead  - (double)_surveyList[i].NorthOfWellHead ) / (double)intermediateEllipseNumbers;
                    double east = (double)_surveyList[i].EastOfWellHead + (double)n * ((double)_surveyList[i + 1].EastOfWellHead - (double)_surveyList[i].EastOfWellHead) / (double)intermediateEllipseNumbers;
                    double tvd = (double)_surveyList[i].TvdWGS84 + (double)n * ((double)_surveyList[i + 1].TvdWGS84 - (double)_surveyList[i].TvdWGS84) / (double)intermediateEllipseNumbers;
                    double md = (double)_surveyList[i].MdWGS84 + (double)n * ((double)_surveyList[i + 1].MdWGS84 - (double)_surveyList[i].MdWGS84) / (double)intermediateEllipseNumbers;
                    double perpendicularDirection = _surveyList[i].Uncertainty.PerpendicularDirection + (double)n * (_surveyList[i + 1].Uncertainty.PerpendicularDirection - _surveyList[i + 1].Uncertainty.PerpendicularDirection) / (double)intermediateEllipseNumbers;

                    UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipseInter = new UncertaintyEnvelopeEllipse();
                    uncertaintyEnvelopeEllipseInter.Azimuth = azimuth;
                    uncertaintyEnvelopeEllipseInter.Inclination = inclination;
                    uncertaintyEnvelopeEllipseInter.X  = north;
                    uncertaintyEnvelopeEllipseInter.Y = east;
                    uncertaintyEnvelopeEllipseInter.Z = tvd;
                    uncertaintyEnvelopeEllipseInter.MD = md;
                    uncertaintyEnvelopeEllipseInter.EllipseRadius = ellipseR;

                    uncertaintyEnvelopeEllipseInter.PerpendicularDirection = perpendicularDirection;

                    List<GlobalCoordinatePoint3D> ellipseCoordinatesInter = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipseInter);
                    uncertaintyEnvelopeEllipseInter.EllipseCoordinates = ellipseCoordinatesInter;
                    uncertaintyEnvelope.Add(uncertaintyEnvelopeEllipseInter);
                }
                
                if (i == _surveyList.Count - 2)
                {
                    uncertaintyEnvelopeEllipse = new UncertaintyEnvelopeEllipse();
                    //Vector2D ellipseRadius = new Vector2D();
                    ellipseRadius = _surveyList[_surveyList.Count - 1].Uncertainty.EllipseRadius;

                    uncertaintyEnvelopeEllipse.Azimuth = _surveyList[_surveyList.Count - 1].AzWGS84;
                    uncertaintyEnvelopeEllipse.Inclination = _surveyList[_surveyList.Count - 1].Incl;
                    uncertaintyEnvelopeEllipse.X = _surveyList[_surveyList.Count - 1].NorthOfWellHead ;
                    uncertaintyEnvelopeEllipse.Y = _surveyList[_surveyList.Count - 1].EastOfWellHead;
                    uncertaintyEnvelopeEllipse.Z = _surveyList[_surveyList.Count - 1].TvdWGS84;
                    uncertaintyEnvelopeEllipse.MD = _surveyList[_surveyList.Count - 1].MdWGS84;
                    uncertaintyEnvelopeEllipse.EllipseRadius = ellipseRadius;
                    ellipseCoordinates = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipse);
                    uncertaintyEnvelopeEllipse.EllipseCoordinates = ellipseCoordinates;
                    uncertaintyEnvelope.Add(uncertaintyEnvelopeEllipse);
                }
            }
            UncertaintyEnvelope = uncertaintyEnvelope;
            return uncertaintyEnvelope;
        }

        private void CalculateUncertaintyCylinder(SurveyStation surveyStation, double confidenceFactor)
        {
            double xMinEllipsoid = 0.0;
            double xMaxEllipsoid = 0.0;
            double yMinEllipsoid = 0.0;
            double yMaxEllipsoid = 0.0;
            double zMinEllipsoid = 0.0;
            double zMaxEllipsoid = 0.0;
            CalculateExtremumInDepths(surveyStation, confidenceFactor, ref xMinEllipsoid, ref xMaxEllipsoid, ref yMinEllipsoid, ref yMaxEllipsoid, ref zMinEllipsoid, ref zMaxEllipsoid);

            List<UncertaintyEnvelopeEllipse> uncertaintyEnvelope = new List<UncertaintyEnvelopeEllipse>();

            //for (int i = 0; i < surveyList.Count - 1; i++)
            {

                double xMinEllipse = Numeric.MAX_DOUBLE;
                double xMaxEllipse = Numeric.MIN_DOUBLE;
                double yMinEllipse = Numeric.MAX_DOUBLE;
                double yMaxEllipse = Numeric.MIN_DOUBLE;
                double zMinEllipse = Numeric.MAX_DOUBLE;
                double zMaxEllipse = Numeric.MIN_DOUBLE;

                UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipse = new UncertaintyEnvelopeEllipse();

                Vector2D ellipseRadius = surveyStation.Uncertainty.EllipseRadius;
                Vector2D ellipseRadiusNext = surveyStation.Uncertainty.EllipseRadius;
                double distance = 0;// MD - MD;
                double _intermediateEllipseNumbers = 10;

                uncertaintyEnvelopeEllipse.Azimuth = surveyStation.AzWGS84;
                uncertaintyEnvelopeEllipse.Inclination = surveyStation.Incl;
                uncertaintyEnvelopeEllipse.X = surveyStation.NorthOfWellHead ;
                uncertaintyEnvelopeEllipse.Y = surveyStation.EastOfWellHead;
                uncertaintyEnvelopeEllipse.Z = surveyStation.TvdWGS84;
                uncertaintyEnvelopeEllipse.MD = (double)surveyStation.MdWGS84;
                uncertaintyEnvelopeEllipse.EllipseRadius = ellipseRadius;
                uncertaintyEnvelopeEllipse.PerpendicularDirection = surveyStation.Uncertainty.PerpendicularDirection;
                List<GlobalCoordinatePoint3D> ellipseCoordinates = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipse, 0, ref xMinEllipse, ref xMaxEllipse, ref yMinEllipse, ref yMaxEllipse, ref zMinEllipse, ref zMaxEllipse);
                uncertaintyEnvelopeEllipse.EllipseCoordinates = ellipseCoordinates;
                uncertaintyEnvelope.Add(uncertaintyEnvelopeEllipse);                

                double z = 0;
                double zAdd = 2;
                bool increasingZ = false;
                bool increasingX = false;
                bool increasingY = false;
                bool stop = false;
                double zMinPrev = zMinEllipse;
                double xMinPrev = xMinEllipse;
                double yMinPrev = yMinEllipse;
                if (surveyStation.Incl > Numeric.PI / 2)
                {
                    stop = true;
                }
                if (xMinEllipsoid < xMinEllipse || yMinEllipsoid < yMinEllipse || zMinEllipsoid < zMinEllipse)
                {
                    z -= zAdd;                   
                    UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipseInter = new UncertaintyEnvelopeEllipse();
                    uncertaintyEnvelopeEllipseInter.Azimuth = surveyStation.AzWGS84;
                    uncertaintyEnvelopeEllipseInter.Inclination = surveyStation.Incl;
                    uncertaintyEnvelopeEllipseInter.X = surveyStation.NorthOfWellHead ;
                    uncertaintyEnvelopeEllipseInter.Y = surveyStation.EastOfWellHead;
                    uncertaintyEnvelopeEllipseInter.Z = surveyStation.TvdWGS84;
                    uncertaintyEnvelopeEllipseInter.MD = (double)surveyStation.MdWGS84;
                    uncertaintyEnvelopeEllipseInter.EllipseRadius = ellipseRadius;
                    uncertaintyEnvelopeEllipseInter.PerpendicularDirection = surveyStation.Uncertainty.PerpendicularDirection;
                    List<GlobalCoordinatePoint3D> ellipseCoordinatesInter = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipseInter, z, ref xMinEllipse, ref xMaxEllipse, ref yMinEllipse, ref yMaxEllipse, ref zMinEllipse, ref zMaxEllipse);
                    if (zMinPrev < zMinEllipse)
                    {
                        increasingZ = true;
                    }
                    if (xMinPrev <= xMinEllipse)
                    {
                        increasingX = true;
                    }
                    if (yMinPrev <= yMinEllipse)
                    {
                        increasingY = true;
                    }
                    if (zMinPrev == zMinEllipse || surveyStation.Incl > Numeric.PI / 2 || Numeric.EQ(surveyStation.AzWGS84, Numeric.PI, 0.01))
                    {
                        stop = true;
                    }
                    uncertaintyEnvelopeEllipseInter.EllipseCoordinates = ellipseCoordinatesInter;
                    uncertaintyEnvelope.Insert(0, uncertaintyEnvelopeEllipseInter);
                }
                //if (!stop)
                {
                    while ((!stop&& (zMinEllipsoid < zMinEllipse)) || ((!increasingX && Numeric.LT(xMinEllipsoid, xMinEllipse, 0.01)) || (increasingX && Numeric.GT(xMaxEllipsoid, xMaxEllipse, 0.01))) || ((!increasingY && Numeric.LT(yMinEllipsoid, yMinEllipse, 0.01)) || (increasingY && Numeric.GT(yMaxEllipsoid, yMaxEllipse, 0.01))))
                    {
                        z -= zAdd;
                        UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipseInter = new UncertaintyEnvelopeEllipse();
                        uncertaintyEnvelopeEllipseInter.Azimuth = surveyStation.AzWGS84;
                        uncertaintyEnvelopeEllipseInter.Inclination = surveyStation.Incl;
                        uncertaintyEnvelopeEllipseInter.X = surveyStation.NorthOfWellHead ;
                        uncertaintyEnvelopeEllipseInter.Y = surveyStation.EastOfWellHead;
                        uncertaintyEnvelopeEllipseInter.Z = surveyStation.TvdWGS84;
                        uncertaintyEnvelopeEllipseInter.MD = (double)surveyStation.MdWGS84;
                        uncertaintyEnvelopeEllipseInter.EllipseRadius = ellipseRadius;
                        uncertaintyEnvelopeEllipseInter.PerpendicularDirection = surveyStation.Uncertainty.PerpendicularDirection;
                        List<GlobalCoordinatePoint3D> ellipseCoordinatesInter = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipseInter, z, ref xMinEllipse, ref xMaxEllipse, ref yMinEllipse, ref yMaxEllipse, ref zMinEllipse, ref zMaxEllipse);
                        uncertaintyEnvelopeEllipseInter.EllipseCoordinates = ellipseCoordinatesInter;
                        uncertaintyEnvelope.Insert(0, uncertaintyEnvelopeEllipseInter);
                    }
                    z = 0;
                    while (((!increasingX && Numeric.GT(xMaxEllipsoid, xMaxEllipse, 0.01)) || (increasingX && Numeric.LT(xMinEllipsoid, xMinEllipse, 0.01))) || ((!increasingY && Numeric.GT(yMaxEllipsoid, yMaxEllipse, 0.01)) || (increasingY && Numeric.LT(yMinEllipsoid, yMinEllipse, 0.01))) || (!stop && zMaxEllipsoid > zMaxEllipse))
                    //while ( zMax > zMaxEllipse)
                    {
                        z += zAdd;
                        UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipseInter = new UncertaintyEnvelopeEllipse();
                        uncertaintyEnvelopeEllipseInter.Azimuth = surveyStation.AzWGS84;
                        uncertaintyEnvelopeEllipseInter.Inclination = surveyStation.Incl;
                        uncertaintyEnvelopeEllipseInter.X = surveyStation.NorthOfWellHead ;
                        uncertaintyEnvelopeEllipseInter.Y = surveyStation.EastOfWellHead;
                        uncertaintyEnvelopeEllipseInter.Z = surveyStation.TvdWGS84;
                        uncertaintyEnvelopeEllipseInter.MD = (double)surveyStation.MdWGS84;
                        uncertaintyEnvelopeEllipseInter.EllipseRadius = ellipseRadius;
                        uncertaintyEnvelopeEllipseInter.PerpendicularDirection = surveyStation.Uncertainty.PerpendicularDirection;
                        List<GlobalCoordinatePoint3D> ellipseCoordinatesInter = GetUncertaintyEllipseCoordinates(uncertaintyEnvelopeEllipseInter, z, ref xMinEllipse, ref xMaxEllipse, ref yMinEllipse, ref yMaxEllipse, ref zMinEllipse, ref zMaxEllipse);
                        uncertaintyEnvelopeEllipseInter.EllipseCoordinates = ellipseCoordinatesInter;
                        uncertaintyEnvelope.Add(uncertaintyEnvelopeEllipseInter);

                        CalculateExtremumInDepths(surveyStation, confidenceFactor, ref xMinEllipsoid, ref xMaxEllipsoid, ref yMinEllipsoid, ref yMaxEllipsoid, ref zMinEllipsoid, ref zMaxEllipsoid);
                    }
                }
            }
            surveyStation.Uncertainty.UncertaintyCylinder = uncertaintyEnvelope;
        }

        public void CalculateExtremumInDepths(SurveyStation surveyStation, double p, ref double xMin, ref double xMax, ref double yMin, ref double yMax, ref double zMin, ref double zMax)
        {
            // calculate the parameters of the ellipsoid
            double chiSquare = GetChiSquare3D(p);
            // inverse the matrix
            double h11 = (double)surveyStation.Uncertainty.Covariance[0, 0];
            double h12 = (double)surveyStation.Uncertainty.Covariance[0, 1];
            double h13 = (double)surveyStation.Uncertainty.Covariance[0, 2];
            double h22 = (double)surveyStation.Uncertainty.Covariance[1, 1];
            double h23 = (double)surveyStation.Uncertainty.Covariance[1, 2];
            double h33 = (double)surveyStation.Uncertainty.Covariance[2, 2];
            double determinant = (h11 * h22 - h12 * h12) * h33 - h11 * h23 * h23 + 2 * h12 * h13 * h23 - h13 * h13 * h22;
            double H11 = (h22 * h33 - h23 * h23) / determinant;
            double H21 = -(h12 * h33 - h13 * h23) / determinant;
            double H31 = (h12 * h23 - h13 * h22) / determinant;
            double H12 = H21;
            double H22 = (h11 * h33 - h13 * h13) / determinant;
            double H32 = -(h11 * h23 - h12 * h13) / determinant;
            double H13 = H31;
            double H23 = H32;
            double H33 = (h11 * h22 - h12 * h12) / determinant;

            // calculate extremum in Z
            double denominator = H11 * H22 - H12 * H12;
            double dl = Numeric.SqrtEqual(chiSquare * ((H11 * H22 * H33) / denominator - (H12 * H12 * H33) / denominator - (H11 * H23 * H23) / denominator + (2.0 * H12 * H13 * H23) / denominator - (H13 * H13 * H22) / denominator));
            determinant = (H11 * H22 - H12 * H12) * H33 - H11 * H23 * H23 + 2 * H12 * H13 * H23 - H13 * H13 * H22;
            xMin = dl * (H13 * H22 - H12 * H23) / determinant;
            yMin = dl * (-H12 * H13 + H11 * H23) / determinant;
            zMin = dl * (H12 * H12 - H11 * H22) / determinant;
            xMax = -xMin;
            yMax = -yMin;
            zMax = -zMin;

            if (zMin < zMax)
            {
                //swap
                double tt = xMin;
                xMin = xMax;
                xMax = tt;
                tt = yMin;
                yMin = yMax;
                yMax = tt;
                tt = zMin;
                //zMin = zMax;
                //zMax = tt;
            }
            //add bias and survey position
            xMin += (double)surveyStation.NorthOfWellHead ;
            yMin += (double)surveyStation.EastOfWellHead;
            zMin += (double)surveyStation.TvdWGS84;
            xMax += (double)surveyStation.NorthOfWellHead ;
            yMax += (double)surveyStation.EastOfWellHead;
            zMax += (double)surveyStation.TvdWGS84;

            double ellipseVerticesPhi_ = 32;
            double ellipseVerticesTheta_ = 32;
            if (surveyStation.Uncertainty.EllipsoidRadius[0] > 0)
            {
                for (int j = 0; j <= ellipseVerticesPhi_; j++)
                {
                    double phi = (double)j * 2.0 * Math.PI / (double)ellipseVerticesPhi_;
                    for (int k = 0; k <= ellipseVerticesTheta_; k++)
                    {
                        double theta = (double)k * 1.0 * Math.PI / (double)ellipseVerticesTheta_;
                        double UEllipsoid = (double)surveyStation.Uncertainty.EllipsoidRadius[0] * System.Math.Sin(theta) * System.Math.Cos(phi);
                        double VEllipsoid = (double)surveyStation.Uncertainty.EllipsoidRadius[1] * System.Math.Sin(theta) * System.Math.Sin(phi);
                        double WEllipsoid = (double)surveyStation.Uncertainty.EllipsoidRadius[2] * System.Math.Cos(theta);

                        double xEllipsoid = (double)surveyStation.Uncertainty.EigenVectors[0, 0] * UEllipsoid + (double)surveyStation.Uncertainty.EigenVectors[0, 1] * VEllipsoid + (double)surveyStation.Uncertainty.EigenVectors[0, 2] * WEllipsoid;
                        double yEllipsoid = (double)surveyStation.Uncertainty.EigenVectors[1, 0] * UEllipsoid + (double)surveyStation.Uncertainty.EigenVectors[1, 1] * VEllipsoid + (double)surveyStation.Uncertainty.EigenVectors[1, 2] * WEllipsoid;
                        double zEllipsoid = (double)surveyStation.Uncertainty.EigenVectors[2, 0] * UEllipsoid + (double)surveyStation.Uncertainty.EigenVectors[2, 1] * VEllipsoid + (double)surveyStation.Uncertainty.EigenVectors[2, 2] * WEllipsoid;

                        xEllipsoid += (double)surveyStation.NorthOfWellHead ;
                        yEllipsoid += (double)surveyStation.EastOfWellHead;
                        zEllipsoid += (double)surveyStation.TvdWGS84;

                        if (xEllipsoid < xMin)
                        {
                            xMin = xEllipsoid;
                        }
                        if (xEllipsoid > xMax)
                        {
                            xMax = xEllipsoid;
                        }
                        if (yEllipsoid < yMin)
                        {
                            yMin = yEllipsoid;
                        }
                        if (yEllipsoid > yMax)
                        {
                            yMax = yEllipsoid;
                        }
                        if (zEllipsoid < zMin)
                        {
                            zMin = zEllipsoid;
                        }
                        if (zEllipsoid > zMax)
                        {
                            zMax = zEllipsoid;
                        }

                        //double xCyl = (double)uncertainty.EllipsoidRadius[0] * System.Math.Sin(theta) *  System.Math.Cos(Math.PI/2);
                        //double yCyl = (double)uncertainty.EllipsoidRadius[1] * System.Math.Sin(theta) *  System.Math.Sin(Math.PI / 2);
                        //double zCyl = (double)uncertainty.EllipsoidRadius[2] * System.Math.Cos(theta);
                        //double xNEH = (double)H[0, 0] * xCyl + (double)H[0, 1] * yCyl + (double)H[0, 2] * zCyl;
                        //double yNEH = (double)H[1, 0] * xCyl + (double)H[1, 1] * yCyl + (double)H[1, 2] * zCyl;
                        //double zNEH = (double)H[2, 0] * xCyl + (double)H[2, 1] * yCyl + (double)H[2, 2] * zCyl;

                        //xNEH += (double)surveys[i].NorthOfWellHead ;
                        //yNEH += (double)surveys[i].EastOfWellHead;
                        //zNEH += (double)surveys[i].TvdWGS84;
                        //var pEll = new System.Windows.Media.Media3D.Point3D(xNEH - _minNorth, yNEH - _minEast, -zNEH - _minTVD);
                        //pointsEll.Add(pEll);

                    }
                }
            }
        }

        private List<GlobalCoordinatePoint3D> GetUncertaintyEllipseCoordinates(UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipse, double z, ref double xMin, ref double xMax, ref double yMin, ref double yMax, ref double zMin, ref double zMax)
        {
            List<GlobalCoordinatePoint3D> ellipseCoordinates = new List<GlobalCoordinatePoint3D>();

            double sinI = System.Math.Sin((double)uncertaintyEnvelopeEllipse.Inclination);
            double cosI = System.Math.Cos((double)uncertaintyEnvelopeEllipse.Inclination);
            double sinA = System.Math.Sin((double)uncertaintyEnvelopeEllipse.Azimuth);
            double cosA = System.Math.Cos((double)uncertaintyEnvelopeEllipse.Azimuth);
            double xNEH = 0.0;
            double yNEH = 0.0;
            double zNEH = 0.0;
            double xNEHt = 0.0;
            double yNEHt = 0.0;
            double zNEHt = 0.0;

            bool useInclAz = false;
            bool usePhi = true;
            if (useInclAz)
            {
                if (uncertaintyEnvelopeEllipse.EllipseRadius[0] != null && uncertaintyEnvelopeEllipse.EllipseRadius[1] != null)
                {
                    //_ellipseVerticesPhi = (int)Numeric.Max(Numeric.Max(uncertaintyEnvelopeEllipse.EllipseRadius[0], uncertaintyEnvelopeEllipse.EllipseRadius[1]), _ellipseVerticesPhi);
                    double[,] Rz = new double[3, 3];
                    RotationMatrix(ref Rz, (double)uncertaintyEnvelopeEllipse.Azimuth, 3);
                    double[,] Ry = new double[3, 3];
                    RotationMatrix(ref Ry, (double)uncertaintyEnvelopeEllipse.Inclination, 2);
                    double[,] R = new double[3, 3];
                    R = MatrixMuliprication(Rz, Ry);
                    for (int j = 0; j <= 64; j++)
                    {
                        double phi = (double)j * 2.0 * Math.PI / (double)32;
                        double xCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[0] * System.Math.Cos(phi);
                        double yCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[1] * System.Math.Sin(phi);
                        double zCyl = z;
                        xNEH = cosI * cosA * xCyl - sinA * yCyl + sinI * cosA * zCyl;
                        yNEH = cosI * sinA * xCyl + cosA * yCyl + sinI * sinA * zCyl;
                        zNEH = -sinI * xCyl + cosI * zCyl;
                        xNEH = R[0, 0] * xCyl + R[0, 1] * yCyl + R[0, 2] * zCyl;
                        yNEH = R[1, 0] * xCyl + R[1, 1] * yCyl + R[1, 2] * zCyl;
                        zNEH = R[2, 0] * xCyl + R[2, 1] * yCyl + R[2, 2] * zCyl;
                        xNEH += (double)uncertaintyEnvelopeEllipse.X;
                        yNEH += (double)uncertaintyEnvelopeEllipse.Y;
                        zNEH += (double)uncertaintyEnvelopeEllipse.Z;
                        GlobalCoordinatePoint3D point = new GlobalCoordinatePoint3D(xNEH, yNEH, zNEH);
                        ellipseCoordinates.Add(point);
                    }
                }
            }
            else if (usePhi)
            {
                if (uncertaintyEnvelopeEllipse.EllipseRadius[0] != null && uncertaintyEnvelopeEllipse.EllipseRadius[1] != null)
                {
                    //_ellipseVerticesPhi = (int)Numeric.Max(Numeric.Max(uncertaintyEnvelopeEllipse.EllipseRadius[0], uncertaintyEnvelopeEllipse.EllipseRadius[1]), _ellipseVerticesPhi);
                    double sinP = System.Math.Sin((double)uncertaintyEnvelopeEllipse.PerpendicularDirection);
                    double cosP = System.Math.Cos((double)uncertaintyEnvelopeEllipse.PerpendicularDirection);

                    double[,] Rz = new double[3, 3];
                    RotationMatrix(ref Rz, (double)uncertaintyEnvelopeEllipse.Azimuth, 3);
                    double[,] Ry = new double[3, 3];
                    RotationMatrix(ref Ry, (double)uncertaintyEnvelopeEllipse.Inclination, 2);
                    double[,] R0 = new double[3, 3];
                    R0 = MatrixMuliprication(Rz, Ry);
                    double[,] Rz2 = new double[3, 3];
                    RotationMatrix(ref Rz2, (double)uncertaintyEnvelopeEllipse.PerpendicularDirection, 3);
                    //RotationMatrix(ref Rz2, Math.PI/2, 3);
                    double[,] R = new double[3, 3];
                    R = MatrixMuliprication(R0, Rz2);
                    for (int j = 0; j <= 96; j++)
                    {
                        double phi = (double)j * 2.0 * Math.PI / (double)96;
                        double xCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[0] * System.Math.Cos(phi);
                        double yCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[1] * System.Math.Sin(phi);
                        double zCyl = z;
                        double xNEH0 = (cosP * cosI * cosA - sinP * sinA * cosI) * xCyl - (cosP * sinA + sinP * cosA) * yCyl + (cosP * sinI * cosA - sinP * sinA * sinA) * zCyl;
                        double yNEH0 = (sinP * cosI * sinA - cosP * sinA * cosI) * xCyl + (cosP * cosA - sinP * sinA) * yCyl + (cosP * sinI * sinA + sinP * cosA * sinI) * zCyl;
                        double zNEH0 = -sinI * xCyl + cosI * zCyl;
                        xNEH = R[0, 0] * xCyl + R[0, 1] * yCyl + R[0, 2] * zCyl;
                        yNEH = R[1, 0] * xCyl + R[1, 1] * yCyl + R[1, 2] * zCyl;
                        zNEH = R[2, 0] * xCyl + R[2, 1] * yCyl + R[2, 2] * zCyl;
                        if (uncertaintyEnvelopeEllipse.PerpendicularDirection > 0.5)
                        {
                            bool ok = false;
                        }
                        xNEH += (double)uncertaintyEnvelopeEllipse.X;
                        yNEH += (double)uncertaintyEnvelopeEllipse.Y;
                        zNEH += (double)uncertaintyEnvelopeEllipse.Z;
                        GlobalCoordinatePoint3D point = new GlobalCoordinatePoint3D(xNEH, yNEH, zNEH);
                        ellipseCoordinates.Add(point);
                        if (xNEH < xMin) xMin = xNEH;
                        if (xNEH > xMax) xMax = xNEH;
                        if (yNEH < yMin) yMin = yNEH;
                        if (yNEH > yMax) yMax = yNEH;
                        if (zNEH < zMin) zMin = zNEH;
                        if (zNEH > zMax) zMax = zNEH;

                    }
                }
            }
            else
            {
                //_surveyList[9].Uncertainty.EigenVectors
                for (int j = 0; j <= 32; j++)
                {
                    //double phi = (double)j * 2.0 * Math.PI / (double)_ellipseVerticesPhi;
                    //double xCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[0] * System.Math.Cos(phi);
                    //double yCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[1] * System.Math.Sin(phi);
                    //double zCyl = 0.0;

                    //xNEH = H[0, 0] * xCyl + H[0, 1] * yCyl + H[0, 2] * zCyl;
                    //yNEH = H[1, 0] * xCyl + H[1, 1] * yCyl + H[1, 2] * zCyl;
                    //zNEH = H[2, 0] * xCyl + H[2, 1] * yCyl +H[2, 2] * zCyl;
                    //xNEH += (double)uncertaintyEnvelopeEllipse.X;
                    //yNEH += (double)uncertaintyEnvelopeEllipse.Y;
                    //zNEH += (double)uncertaintyEnvelopeEllipse.Z;
                    //Point3D point = new Point3D(xNEH, yNEH, zNEH);
                    //ellipseCoordinates.Add(point);
                }


            }


            return ellipseCoordinates;
        }
        private List<GlobalCoordinatePoint3D> GetUncertaintyEllipseCoordinates(UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipse)
        {
            List<GlobalCoordinatePoint3D> ellipseCoordinates = new List<GlobalCoordinatePoint3D>();

            double sinI = System.Math.Sin((double)uncertaintyEnvelopeEllipse.Inclination);
            double cosI = System.Math.Cos((double)uncertaintyEnvelopeEllipse.Inclination);
            double sinA = System.Math.Sin((double)uncertaintyEnvelopeEllipse.Azimuth);
            double cosA = System.Math.Cos((double)uncertaintyEnvelopeEllipse.Azimuth);
            double xNEH = 0.0;
            double yNEH = 0.0;
            double zNEH = 0.0;
            double xNEHt = 0.0;
            double yNEHt = 0.0;
            double zNEHt = 0.0;

            bool useInclAz = false;
            bool usePhi = true;
            if (useInclAz)
            {
                if (uncertaintyEnvelopeEllipse.EllipseRadius[0] != null && uncertaintyEnvelopeEllipse.EllipseRadius[1] != null)
                {
                    //_ellipseVerticesPhi = (int)Numeric.Max(Numeric.Max(uncertaintyEnvelopeEllipse.EllipseRadius[0], uncertaintyEnvelopeEllipse.EllipseRadius[1]), _ellipseVerticesPhi);
                    double[,] Rz = new double[3, 3];
                    RotationMatrix(ref Rz, (double)uncertaintyEnvelopeEllipse.Azimuth, 3);
                    double[,] Ry = new double[3, 3];
                    RotationMatrix(ref Ry, (double)uncertaintyEnvelopeEllipse.Inclination, 2);
                    double[,] R = new double[3, 3];
                    R = MatrixMuliprication(Rz, Ry);
                    for (int j = 0; j <= _ellipseVerticesPhi; j++)
                    {
                        double phi = (double)j * 2.0 * Math.PI / (double)_ellipseVerticesPhi;
                        double xCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[0] * System.Math.Cos(phi);
                        double yCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[1] * System.Math.Sin(phi);
                        double zCyl = 0.0;
                        xNEH = cosI * cosA * xCyl - sinA * yCyl + sinI * cosA * zCyl;
                        yNEH = cosI * sinA * xCyl + cosA * yCyl + sinI * sinA * zCyl;
                        zNEH = -sinI * xCyl + cosI * zCyl;
                        xNEH = R[0,0] * xCyl + R[0, 1] * yCyl + R[0,2] * zCyl;
                        yNEH = R[1, 0] * xCyl + R[1, 1] * yCyl + R[1, 2] * zCyl;
                        zNEH = R[2, 0] * xCyl + R[2, 1] * yCyl + R[2, 2] * zCyl;
                        xNEH += (double)uncertaintyEnvelopeEllipse.X;
                        yNEH += (double)uncertaintyEnvelopeEllipse.Y;
                        zNEH += (double)uncertaintyEnvelopeEllipse.Z;
                        GlobalCoordinatePoint3D point = new GlobalCoordinatePoint3D(xNEH, yNEH, zNEH);
                        ellipseCoordinates.Add(point);
                    }
                }
            }
            else if(usePhi)
            {
                if (uncertaintyEnvelopeEllipse.EllipseRadius[0] != null && uncertaintyEnvelopeEllipse.EllipseRadius[1] != null)
                {
                    //_ellipseVerticesPhi = (int)Numeric.Max(Numeric.Max(uncertaintyEnvelopeEllipse.EllipseRadius[0], uncertaintyEnvelopeEllipse.EllipseRadius[1]), _ellipseVerticesPhi);
                    double sinP = System.Math.Sin((double)uncertaintyEnvelopeEllipse.PerpendicularDirection);
                    double cosP = System.Math.Cos((double)uncertaintyEnvelopeEllipse.PerpendicularDirection);

                    double[,] Rz = new double[3, 3];
                    RotationMatrix(ref Rz, (double)uncertaintyEnvelopeEllipse.Azimuth, 3);
                    double[,] Ry = new double[3, 3];
                    RotationMatrix(ref Ry, (double)uncertaintyEnvelopeEllipse.Inclination, 2);
                    double[,] R0 = new double[3, 3];
                    R0 = MatrixMuliprication(Rz, Ry);
                    double[,] Rz2 = new double[3, 3];
                    RotationMatrix(ref Rz2, (double)uncertaintyEnvelopeEllipse.PerpendicularDirection, 3);
                    //RotationMatrix(ref Rz2, Math.PI/2, 3);
                    double[,] R = new double[3, 3];
                    R = MatrixMuliprication(R0, Rz2);
                    for (int j = 0; j <= _ellipseVerticesPhi; j++)
                    {
                        double phi = (double)j * 2.0 * Math.PI / (double)_ellipseVerticesPhi;
                        double xCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[0] * System.Math.Cos(phi);
                        double yCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[1] * System.Math.Sin(phi);
                        double zCyl = 0.0;
                        double xNEH0 = (cosP*cosI * cosA-sinP*sinA*cosI) * xCyl - (cosP*sinA+sinP*cosA) * yCyl + (cosP*sinI * cosA-sinP*sinA*sinA) * zCyl;
                        double yNEH0 = (sinP*cosI * sinA -cosP*sinA*cosI)* xCyl + (cosP*cosA-sinP*sinA) * yCyl + (cosP*sinI * sinA +sinP*cosA*sinI)* zCyl;
                        double zNEH0 = -sinI * xCyl + cosI * zCyl;
                        xNEH = R[0, 0] * xCyl + R[0, 1] * yCyl + R[0, 2] * zCyl;
                        yNEH = R[1, 0] * xCyl + R[1, 1] * yCyl + R[1, 2] * zCyl;
                        zNEH = R[2, 0] * xCyl + R[2, 1] * yCyl + R[2, 2] * zCyl;
                        if(uncertaintyEnvelopeEllipse.PerpendicularDirection>0.5)
                        {
                            bool ok = false;
                        }
                        xNEH += (double)uncertaintyEnvelopeEllipse.X;
                        yNEH += (double)uncertaintyEnvelopeEllipse.Y;
                        zNEH += (double)uncertaintyEnvelopeEllipse.Z;
                        GlobalCoordinatePoint3D point = new GlobalCoordinatePoint3D(xNEH, yNEH, zNEH);
                        if(phi==0)
						{
                            pointx.Add(point);

                        }
                        if (phi == Math.PI/2+ Math.PI)
                        {
                            pointy.Add(point);

                        }
                        ellipseCoordinates.Add(point);
                    }
                }
            }
            else
            {
                //_surveyList[9].Uncertainty.EigenVectors
                     for (int j = 0; j <= _ellipseVerticesPhi; j++)
                {
                    //double phi = (double)j * 2.0 * Math.PI / (double)_ellipseVerticesPhi;
                    //double xCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[0] * System.Math.Cos(phi);
                    //double yCyl = (double)uncertaintyEnvelopeEllipse.EllipseRadius[1] * System.Math.Sin(phi);
                    //double zCyl = 0.0;
                    
                    //xNEH = H[0, 0] * xCyl + H[0, 1] * yCyl + H[0, 2] * zCyl;
                    //yNEH = H[1, 0] * xCyl + H[1, 1] * yCyl + H[1, 2] * zCyl;
                    //zNEH = H[2, 0] * xCyl + H[2, 1] * yCyl +H[2, 2] * zCyl;
                    //xNEH += (double)uncertaintyEnvelopeEllipse.X;
                    //yNEH += (double)uncertaintyEnvelopeEllipse.Y;
                    //zNEH += (double)uncertaintyEnvelopeEllipse.Z;
                    //Point3D point = new Point3D(xNEH, yNEH, zNEH);
                    //ellipseCoordinates.Add(point);
                }

                
            }

           
            return ellipseCoordinates;
        }

        private void RotationMatrix(ref double[,]  A,double angle, int rot)
        {           
            double sinA = System.Math.Sin(angle);
            double cosA = System.Math.Cos(angle);
            // x rotation
            if(rot==1)
            {
                A[0, 0] = 1;
                A[0, 1] = 0;
                A[0, 2] = 0;
                A[1, 0] = 0;
                A[1, 1] = cosA;
                A[1, 2] = -sinA; 
                A[2, 0] = 0;
                A[2, 1] = sinA;
                A[2, 2] = cosA;
            }
            // y rotation
            else if (rot==2)
            {
                A[0, 0] = cosA;
                A[0, 1] = 0;
                A[0, 2] = sinA;
                A[1, 0] = 0;
                A[1, 1] = 1;
                A[1, 2] = 0;
                A[2, 0] = -sinA;
                A[2, 1] = 0;
                A[2, 2] = cosA;
            }
            // z rotation
            else
            {
                A[0, 0] = cosA;
                A[0, 1] = -sinA;
                A[0, 2] = 0;
                A[1, 0] = sinA;
                A[1, 1] = cosA;
                A[1, 2] = 0;
                A[2, 0] = 0;
                A[2, 1] = 0;
                A[2, 2] = 1;
            }            
        }
        private List<GlobalCoordinatePoint3D> GetUncertaintyEllipseAreaCoordinates(UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipse)
        {
            List<GlobalCoordinatePoint3D> ellipseAreaCoordinates = new List<GlobalCoordinatePoint3D>();
            /*
            UncertaintyEnvelopeEllipse ellipse = new UncertaintyEnvelopeEllipse();
            ellipse.Azimuth = uncertaintyEnvelopeEllipse.Azimuth;
            ellipse.Inclination = uncertaintyEnvelopeEllipse.Inclination;
            ellipse.X = uncertaintyEnvelopeEllipse.X;
            ellipse.Y = uncertaintyEnvelopeEllipse.Y;
            ellipse.Z = uncertaintyEnvelopeEllipse.Z;
            double minScalingfactor = 0.1;
            int scalingLevels = 9;
            double factor = (1 - minScalingfactor) / scalingLevels;
            if (uncertaintyEnvelopeEllipse.EllipseRadius != null && uncertaintyEnvelopeEllipse.EllipseRadius[0] != null && uncertaintyEnvelopeEllipse.EllipseRadius[1] != null)
            {
                for (int i = 0; i <= scalingLevels; i++)
                {
                    ellipse.EllipseRadius = new Vector2D();
                    ellipse.EllipseRadius[0] = uncertaintyEnvelopeEllipse.EllipseRadius[0] * (1 - i * factor);
                    ellipse.EllipseRadius[1] = uncertaintyEnvelopeEllipse.EllipseRadius[1] * (1 - i * factor);
                    List<Point3D> ellipseCoordinates = GetUncertaintyEllipseCoordinates(ellipse);
                    for (int j = 0; j < ellipseCoordinates.Count; j++)
                    {
                        ellipseAreaCoordinates.Add(ellipseCoordinates[j]);
                    }
                }
            }
            */
            return ellipseAreaCoordinates;
        }
        public bool IsPartOfUncertaintyEnvelope(CoordinateConverter.WGS84Coordinate wGS84Coordinate, double tvd)
        {
            bool isPartOf = false;
            CoordinateConverter converter = new CoordinateConverter();
            CoordinateConverter.UTMCoordinate utmCoordinate = converter.WGStoUTM(wGS84Coordinate);

            for (int i = 0; i < UncertaintyEnvelope.Count - 1; i++)
            {
                UncertaintyEnvelopeEllipse uncertaintyEnvelopeEllipse = UncertaintyEnvelope[i];
                if (uncertaintyEnvelopeEllipse.EllipseRadius != null && uncertaintyEnvelopeEllipse.EllipseRadius[0] != null && Numeric.IsDefined(uncertaintyEnvelopeEllipse.EllipseRadius[0]))
                {
                    double azimuth = (double)uncertaintyEnvelopeEllipse.Azimuth;
                    double inclination = (double)uncertaintyEnvelopeEllipse.Inclination;
                    double xc = (double)uncertaintyEnvelopeEllipse.X;
                    double yc = (double)uncertaintyEnvelopeEllipse.Y;
                    double zc = (double)uncertaintyEnvelopeEllipse.Z;
                    Vector2D ellipseR = uncertaintyEnvelopeEllipse.EllipseRadius;
                    double[,] T = new double[3, 3];
                    double sinI = System.Math.Sin(inclination);
                    double cosI = System.Math.Cos(inclination);
                    double sinA = System.Math.Sin(azimuth);
                    double cosA = System.Math.Cos(azimuth);
                    T[0, 0] = cosI * cosA;
                    T[1, 0] = -sinA;
                    T[2, 0] = sinI * cosA;
                    T[0, 1] = cosI * sinA;
                    T[1, 1] = cosA;
                    T[2, 1] = sinI * sinA;
                    T[0, 2] = -sinI;
                    T[1, 2] = 0;
                    T[2, 2] = cosI;
                    double xutm = utmCoordinate.X - WellUTMCoordinate.X;
                    double yutm = utmCoordinate.Y - WellUTMCoordinate.Y;
                    double x = T[0, 0] * xutm + T[0, 1] * yutm + T[0, 2] * tvd;
                    double y = T[1, 0] * xutm + T[1, 1] * yutm + T[1, 2] * tvd;
                    double z = T[2, 0] * xutm + T[2, 1] * yutm + T[2, 2] * tvd;
                    double val = Math.Pow((x - xc), 2) / Math.Pow((double)ellipseR[0], 2) + Math.Pow((y - yc), 2) / Math.Pow((double)ellipseR[1], 2);
                    if (val <= 1)
                    {
                        isPartOf = true;
                        break;
                    }
                    else
                    {
                        isPartOf = false;
                    }
                }
            }
            return isPartOf;
        }

        protected static double GetChiSquare3D(double p)
        {
            if (Numeric.IsUndefined(p))
            {
                return Numeric.UNDEF_DOUBLE;
            }
            else
            {
                int last = _chiSquare3D.GetLength(1) - 1;
                if (p < _chiSquare3D[0, 0])
                {
                    double factor = (p - _chiSquare3D[0, 0]) / (_chiSquare3D[0, 1] - _chiSquare3D[0, 0]);
                    return _chiSquare3D[1, 0] + factor * (_chiSquare3D[1, 1] - _chiSquare3D[1, 0]);
                }
                else if (p >= _chiSquare3D[0, last])
                {
                    double factor = (p - _chiSquare3D[0, last - 1]) / (_chiSquare3D[0, last] - _chiSquare3D[0, last - 1]);
                    return _chiSquare3D[1, last - 1] + factor * (_chiSquare3D[1, last] - _chiSquare3D[1, last - 1]);
                }
                else
                {
                    for (int i = 0; i < last; i++)
                    {
                        if (p >= _chiSquare3D[0, i] && p < _chiSquare3D[0, i + 1])
                        {
                            double factor = (p - _chiSquare3D[0, i]) / (_chiSquare3D[0, i + 1] - _chiSquare3D[0, i]);
                            return _chiSquare3D[1, i] + factor * (_chiSquare3D[1, i + 1] - _chiSquare3D[1, i]);
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
