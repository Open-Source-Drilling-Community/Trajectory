using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NORCE.Drilling.Trajectory.Model;

namespace NORCE.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("Trajectory/api/[controller]")]
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
        // GET api/trajectories/5
        [HttpGet("{id}")]
        public Model.Trajectory Get(int id)
        {
            return TrajectoryManager.Instance.Get(id);
        }
		//// GET api/trajectories/5
		//[HttpGet("{ids}")]
		//public List<Model.Trajectory> Get(List<int> ids)
		//{
		//	List<Model.Trajectory> trajectoryList = new List<Model.Trajectory>();
		//	for (int i = 0; i < ids.Count; i++)
		//	{
		//		Model.Trajectory trajectory = TrajectoryManager.Instance.Get(ids[i]);
		//		trajectoryList.Add(trajectory);
		//	}
		//	return trajectoryList;
		//}
		//// GET api/trajectories/5
		//[HttpGet("{id}/{confidenceFactor}/{scalingFactor}")]
		//public Model.Trajectory Get(int id, double confidenceFactor, double scalingFactor)
		//{
		//    Model.Trajectory trajectory = TrajectoryManager.Instance.Get(id);
		//    trajectory.SurveyList.GetUncertaintyEnvelopeTVD(confidenceFactor, scalingFactor);
		//    trajectory.SurveyList.UncertaintyEnvelope;
		//    trajectory.SurveyList.Surveys

		//    return trajectory;
		//}
		// GET api/trajectories/5/
		[HttpGet("{id}/{confidenceFactor}/{scalingFactor}")]
        public List<UncertaintyEnvelopeEllipse> Get(int id, double confidenceFactor, double scalingFactor)
        {
            Model.Trajectory trajectory = TrajectoryManager.Instance.Get(id);
            trajectory.SurveyList.GetUncertaintyEnvelopeTVD(confidenceFactor, scalingFactor);
            return trajectory.SurveyList.UncertaintyEnvelope;
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
                    TrajectoryManager.Instance.Add(value);
                }
            }
        }
        // PUT api/trajectories/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] Model.Trajectory value)
        {
            if (value != null)
            {
                Model.Trajectory trajectory = TrajectoryManager.Instance.Get(id);
                if (trajectory != null)
                {
                    TrajectoryManager.Instance.Update(id, value);
                }
                else
                {
                    TrajectoryManager.Instance.Add(value);
                }
            }
        }
        // DELETE api/trajectories/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            TrajectoryManager.Instance.Remove(id);
        }
    }
}
