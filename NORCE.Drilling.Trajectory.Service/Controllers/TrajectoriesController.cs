using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NORCE.Drilling.Trajectory.Model;
using NORCE.Drilling.SurveyInstrument;
using System.Net.Http;

namespace NORCE.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("/[controller]")]
    [ApiController]
    public class TrajectoriesController : ControllerBase
    {
        // GET api/trajectories
        [HttpGet]
        public IEnumerable<int> Get()
        {            
            var ids = TrajectoryManager.Instance.GetIDs();
            return ids;
        }
        // GET api/trajectories/id
        [HttpGet("{id}")]
        public Model.Trajectory Get(int id)
        {
            Model.Trajectory trajectory = TrajectoryManager.Instance.Get(id);            
            return trajectory;
        }

		// GET api/trajectories/id/
		[HttpGet("{id}/{confidenceFactor}/{scalingFactor}")]
        public List<UncertaintyEnvelopeEllipse> Get(int id, double confidenceFactor, double scalingFactor)
        {
            Model.Trajectory trajectory = TrajectoryManager.Instance.Get(id);           
            SurveyList surveyList = new SurveyList();            
            if (trajectory.SurveyList != null)
            {
                surveyList = trajectory.SurveyList;
                if (surveyList.ListOfSurveys != null)
                {
                    for(int i=0;i< surveyList.ListOfSurveys.Count;i++)
					{
                        if (surveyList.ListOfSurveys[i].SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.WolffDeWardt)
                        {
                            WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                            surveyList.ListOfSurveys[i].Uncertainty = wdwun;
                        }
                        else
                        {
                            ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                            surveyList.ListOfSurveys[i].Uncertainty = iscwsaun;
                        }
                        //Not able to deserialize matrix yet. Adding values to covatiance matrix
                        if (surveyList.ListOfSurveys[i].Uncertainty != null && surveyList.ListOfSurveys[i].Uncertainty.Covariance != null &&
                            surveyList.ListOfSurveys[i].Uncertainty.C11 != null)
                        {
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

                    }   
                    if(surveyList.EllipseVerticesPhi == 0.0)
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
                    surveyList.GetUncertaintyEnvelope(confidenceFactor, scalingFactor);
                }
            }            
            return surveyList.UncertaintyEnvelope;
        }

        // GET api/trajectories/id/MaXDistanceEllipses
        [HttpGet("{id}/{confidenceFactor}/{scalingFactor}/{maxDistanceEllipse}")]
        public List<UncertaintyEnvelopeEllipse> Get(int id, double confidenceFactor, double scalingFactor, double maxDistanceEllipse)
        {
            Model.Trajectory trajectory = TrajectoryManager.Instance.Get(id);
            SurveyList surveyList = new SurveyList();
            if (trajectory.SurveyList != null)
            {
                surveyList = trajectory.SurveyList;
                if (surveyList.ListOfSurveys != null)
                {
                    for (int i = 0; i < surveyList.ListOfSurveys.Count; i++)
                    {
                        if (surveyList.ListOfSurveys[i].SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.WolffDeWardt)
                        {
                            WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                            surveyList.ListOfSurveys[i].Uncertainty = wdwun;
                        }
                        else
                        {
                            ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                            surveyList.ListOfSurveys[i].Uncertainty = iscwsaun;
                        }
                        //Not able to deserialize matrix yet. Adding values to covatiance matrix
                        if (surveyList.ListOfSurveys[i].Uncertainty != null && surveyList.ListOfSurveys[i].Uncertainty.Covariance != null &&
                            surveyList.ListOfSurveys[i].Uncertainty.C11 != null)
                        {
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
                    surveyList.MaxDistanceEllipse = maxDistanceEllipse;
                    if (surveyList.MaxDistanceEllipse == 0.0)
                    {
                        surveyList.MaxDistanceEllipse = 3;

                    }
                    surveyList.GetUncertaintyEnvelope(confidenceFactor, scalingFactor);
                }
            }
            return surveyList.UncertaintyEnvelope;
        }

        //Return list of principal axes radiuses for uncertainty ellipses
        [HttpGet("{id}/{confidenceFactor}")]
        public List<List<double?>> Get(int id, double confidenceFactor)
        { 
            Model.Trajectory trajectory = TrajectoryManager.Instance.Get(id);			
			SurveyList surveyList = new SurveyList();
			if (trajectory.SurveyList != null)
            {
                surveyList = trajectory.SurveyList;
                if (surveyList.ListOfSurveys != null)                   
                {
                    for(int i=0;i< surveyList.ListOfSurveys.Count;i++)
					{                       
                        if (surveyList.ListOfSurveys[i].SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.WolffDeWardt)
                        {
                            WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                            surveyList.ListOfSurveys[i].Uncertainty = wdwun;
                        }
                        else
                        {
                            ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                            surveyList.ListOfSurveys[i].Uncertainty = iscwsaun;
                        }
                        //Not able to deserialize matrix yet. Adding values to covatiance matrix
                        if (surveyList.ListOfSurveys[i].Uncertainty != null && surveyList.ListOfSurveys[i].Uncertainty.Covariance != null &&
                            surveyList.ListOfSurveys[i].Uncertainty.C11 !=null)
                        {
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
                    }    
                    surveyList.GetUncertaintyEnvelope(confidenceFactor, 1);
                }
            }
            List<List < double ?> >listAll = new List<List<double?>>();
            for (int i = 0; i < surveyList.ListOfSurveys.Count; i++)
            {
                List<double?> list = new List<double?>();
                list.Add(surveyList.ListOfSurveys[i].Uncertainty.EllipseRadius[0]);
                list.Add(surveyList.ListOfSurveys[i].Uncertainty.EllipseRadius[1]);
                listAll.Add(list);
            }
            return listAll;
        }
        // POST api/trajectories
        [HttpPost]
        public void Post([FromBody] Model.Trajectory value)
        {
            if (value != null)
            {
                Model.Trajectory trajectory = TrajectoryManager.Instance.Get(value.ID);
                if (trajectory == null)
                {                    
                    if (value.SurveyList != null)
                    {
                        if (value.SurveyList.ListOfSurveys != null && value.SurveyList.ListOfSurveys.Count > 0)
                        {
                            for (int i = 0; i < value.SurveyList.ListOfSurveys.Count; i++)
                            {
                                SurveyInstrument.Model.SurveyInstrument surveyInstrument = new SurveyInstrument.Model.SurveyInstrument();
                                string name = value.SurveyList.ListOfSurveys[i].SurveyTool.Name;
                                foreach (SurveyInstrument.Model.SurveyInstrument si in SurveyInstrument.Model.SurveyInstrument.DefaultList)
                                {
                                    if (si.Name == name)
                                    {
                                        surveyInstrument = new SurveyInstrument.Model.SurveyInstrument(si);
                                        value.SurveyList.ListOfSurveys[i].SurveyTool = surveyInstrument;
                                        break;
                                    }
                                }
                            }
                            SurveyList sl = new SurveyList();
                            value.SurveyList.CalculateMinimumCurvatureMethod();
                            for (int i = 0; i < value.SurveyList.ListOfSurveys.Count; i++)
                            {
                                SurveyStation st = new SurveyStation();
                                double? incl = value.SurveyList.ListOfSurveys[i].Incl;
                                double? az = value.SurveyList.ListOfSurveys[i].AzWGS84;
                                double? md = value.SurveyList.ListOfSurveys[i].MdWGS84;
                                double? tvd = value.SurveyList.ListOfSurveys[i].TvdWGS84;
                                double? X = value.SurveyList.ListOfSurveys[i].NorthOfWellHead ;
                                double? Y = value.SurveyList.ListOfSurveys[i].EastOfWellHead;
                                double? Z = value.SurveyList.ListOfSurveys[i].TvdWGS84;
                                st.AzWGS84 = az;
                                st.Incl = incl;
                                st.NorthOfWellHead  = X;
                                st.EastOfWellHead = Y;
                                st.TvdWGS84 = tvd;
                                st.MdWGS84 = (double)md;
                                st.LatitudeWGS84 = value.SurveyList.ListOfSurveys[i].LatitudeWGS84;
                                st.LongitudeWGS84 = value.SurveyList.ListOfSurveys[i].LongitudeWGS84;
                                //NB: Need to update when more uncertaintymodels are available and SurveyTools included

                                //SurveyInstrument.Model.SurveyInstrument surveyTool = new SurveyInstrument.Model.SurveyInstrument(SurveyInstrument.Model.SurveyInstrument.WdWGoodMag);
                                st.SurveyTool = value.SurveyList.ListOfSurveys[i].SurveyTool;
                                if (st.SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.WolffDeWardt)
                                {
                                    WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                                    st.Uncertainty = wdwun;
                                }
                                else if (st.SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.ISCWC || st.SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.ISCWC_MWD_Rev5
                                    || st.SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.ISCWSA_Gyro_Ex1)
                                {
                                    ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                    st.Uncertainty = iscwsaun;
                                }
                                else
                                {
                                    ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                    st.Uncertainty = iscwsaun;
                                }
                                sl.Add(st);
                            }
                            //To calculate Covariance matrices                            
                            value.SurveyList = sl;
                            value.SurveyList.ListOfSurveys = sl.ListOfSurveys;
                            value.SurveyList.GetUncertaintyEnvelope(0.95, 1);
                        }
                    }
                    TrajectoryManager.Instance.Add(value);
                }
            }
        }
        // PUT api/trajectories/id
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] Model.Trajectory value)
        {
            if (value != null)
            {
                Model.Trajectory trajectory = TrajectoryManager.Instance.Get(id);
                if (trajectory != null)
                {
                    if (value.SurveyList != null)
                    {
                        if (value.SurveyList.ListOfSurveys != null && value.SurveyList.ListOfSurveys.Count > 0)
                        {
                            for (int i = 0; i < value.SurveyList.ListOfSurveys.Count; i++)
                            {
                                SurveyInstrument.Model.SurveyInstrument surveyInstrument = new SurveyInstrument.Model.SurveyInstrument();
                                string name = value.SurveyList.ListOfSurveys[i].SurveyTool.Name;
                                foreach (SurveyInstrument.Model.SurveyInstrument si in SurveyInstrument.Model.SurveyInstrument.DefaultList)
                                {
                                    if (si.Name == name)
                                    {
                                        surveyInstrument = new SurveyInstrument.Model.SurveyInstrument(si);
                                        value.SurveyList.ListOfSurveys[i].SurveyTool = surveyInstrument;
                                        break;
                                    }
                                }                                
                                if (value.SurveyList.ListOfSurveys[i].SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.WolffDeWardt)
                                {
                                    WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                                    value.SurveyList.ListOfSurveys[i].Uncertainty = wdwun;
                                }
                                else if (value.SurveyList.ListOfSurveys[i].SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.ISCWC_MWD_Rev5)
                                {
                                    ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                    value.SurveyList.ListOfSurveys[i].Uncertainty = iscwsaun;
                                }
                                else
                                {
                                    ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                    value.SurveyList.ListOfSurveys[i].Uncertainty = iscwsaun;
                                }
                            }
                            if (value.SurveyList.ListOfSurveys[0].Uncertainty == null || value.SurveyList.ListOfSurveys[0].Uncertainty.C11 == null)
                            {
                                value.SurveyList.CalculateMinimumCurvatureMethod();
                                SurveyList sl = new SurveyList();
                                for (int i = 0; i < value.SurveyList.ListOfSurveys.Count; i++)
                                {
                                    SurveyStation st = new SurveyStation();
                                    double? incl = value.SurveyList.ListOfSurveys[i].Incl;
                                    double? az = value.SurveyList.ListOfSurveys[i].AzWGS84;
                                    double? md = value.SurveyList.ListOfSurveys[i].MdWGS84;
                                    double? tvd = value.SurveyList.ListOfSurveys[i].TvdWGS84;
                                    double? X = value.SurveyList.ListOfSurveys[i].NorthOfWellHead ;
                                    double? Y = value.SurveyList.ListOfSurveys[i].EastOfWellHead;
                                    double? Z = value.SurveyList.ListOfSurveys[i].TvdWGS84;
                                    st.AzWGS84 = az;
                                    st.Incl = incl;
                                    st.NorthOfWellHead  = X;
                                    st.EastOfWellHead = Y;
                                    st.TvdWGS84 = tvd;
                                    st.MdWGS84 = (double)md;
                                    st.LatitudeWGS84 = value.SurveyList.ListOfSurveys[i].LatitudeWGS84;
                                    st.LongitudeWGS84 = value.SurveyList.ListOfSurveys[i].LongitudeWGS84;
                                    //SurveyInstrument.Model.SurveyInstrument surveyTool = new SurveyInstrument.Model.SurveyInstrument(SurveyInstrument.Model.SurveyInstrument.WdWGoodMag);
                                    st.SurveyTool = value.SurveyList.ListOfSurveys[i].SurveyTool;
                                    if (st.SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.WolffDeWardt)
                                    {
                                        WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                                        st.Uncertainty = wdwun;
                                    }
                                    else if (st.SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.ISCWC_MWD_Rev5)
                                    {
                                        ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                        st.Uncertainty = iscwsaun;
                                    }
                                    else
                                    {
                                        ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                        st.Uncertainty = iscwsaun;
                                    }
                                    sl.Add(st);
                                }
                                //To calculate Covariance matrices                                
                                value.SurveyList = sl;
                                value.SurveyList.ListOfSurveys = sl.ListOfSurveys;
                                value.SurveyList.GetUncertaintyEnvelope(0.95, 1);
                            }
                        }
                    }
					TrajectoryManager.Instance.Update(id, value);
                }
                else
                {
                    if (value.SurveyList != null)
                    {
                        if (value.SurveyList.ListOfSurveys != null && value.SurveyList.ListOfSurveys.Count > 0)
                        {
                            for (int i = 0; i < value.SurveyList.ListOfSurveys.Count; i++)
                            {
                                SurveyInstrument.Model.SurveyInstrument surveyInstrument = new SurveyInstrument.Model.SurveyInstrument();
                                string name = value.SurveyList.ListOfSurveys[i].SurveyTool.Name;
                                foreach (SurveyInstrument.Model.SurveyInstrument si in SurveyInstrument.Model.SurveyInstrument.DefaultList)
                                {
                                    if (si.Name == name)
                                    {
                                        surveyInstrument = new SurveyInstrument.Model.SurveyInstrument(si);
                                        value.SurveyList.ListOfSurveys[i].SurveyTool = surveyInstrument;
                                        break;
                                    }
                                }
                                if (value.SurveyList.ListOfSurveys[i].SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.WolffDeWardt)
                                {
                                    WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                                    value.SurveyList.ListOfSurveys[i].Uncertainty = wdwun;
                                }
                                else if (value.SurveyList.ListOfSurveys[i].SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.ISCWC_MWD_Rev5)
                                {
                                    ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                    value.SurveyList.ListOfSurveys[i].Uncertainty = iscwsaun;
                                }
                                else
                                {
                                    ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                    value.SurveyList.ListOfSurveys[i].Uncertainty = iscwsaun;
                                }
                            }
                            if (value.SurveyList.ListOfSurveys[0].Uncertainty == null || value.SurveyList.ListOfSurveys[0].Uncertainty.C11 == null)
                            {
                                value.SurveyList.CalculateMinimumCurvatureMethod();
                                SurveyList sl = new SurveyList();
                                for (int i = 0; i < value.SurveyList.ListOfSurveys.Count; i++)
                                {
                                    SurveyStation st = new SurveyStation();
                                    double? incl = value.SurveyList.ListOfSurveys[i].Incl;
                                    double? az = value.SurveyList.ListOfSurveys[i].AzWGS84;
                                    double? md = value.SurveyList.ListOfSurveys[i].MdWGS84;
                                    double? tvd = value.SurveyList.ListOfSurveys[i].TvdWGS84;
                                    double? X = value.SurveyList.ListOfSurveys[i].NorthOfWellHead ;
                                    double? Y = value.SurveyList.ListOfSurveys[i].EastOfWellHead;
                                    double? Z = value.SurveyList.ListOfSurveys[i].TvdWGS84;
                                    st.AzWGS84 = az;
                                    st.Incl = incl;
                                    st.NorthOfWellHead  = X;
                                    st.EastOfWellHead = Y;
                                    st.TvdWGS84 = tvd;
                                    st.MdWGS84 = (double)md;
                                    st.LatitudeWGS84 = value.SurveyList.ListOfSurveys[i].LatitudeWGS84;
                                    st.LongitudeWGS84 = value.SurveyList.ListOfSurveys[i].LongitudeWGS84;
                                    st.SurveyTool = value.SurveyList.ListOfSurveys[i].SurveyTool;
                                    if (st.SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.WolffDeWardt)
                                    {
                                        WdWSurveyStationUncertainty wdwun = new WdWSurveyStationUncertainty();
                                        st.Uncertainty = wdwun;
                                    }
                                    else if (st.SurveyTool.ModelType == SurveyInstrument.Model.SurveyInstrumentModelType.ISCWC_MWD_Rev5)
                                    {
                                        ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                        st.Uncertainty = iscwsaun;
                                    }
                                    else
                                    {
                                        ISCWSA_SurveyStationUncertainty iscwsaun = new ISCWSA_SurveyStationUncertainty();
                                        st.Uncertainty = iscwsaun;
                                    }
                                    sl.Add(st);
                                }
                                value.SurveyList = sl;
                                value.SurveyList.ListOfSurveys = sl.ListOfSurveys;
                                value.SurveyList.GetUncertaintyEnvelope(0.95, 1);
                            }
                        }
                    }
                    TrajectoryManager.Instance.Add(value);
                }
            }
        }
        // DELETE api/trajectories/id
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            TrajectoryManager.Instance.Remove(id);
        }
       
    }
}
