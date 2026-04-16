using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Octree;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class OctreesController : ControllerBase
    {
        private readonly ILogger<TrajectoryManager> _loggerTrajectory;
        private readonly ILogger<OctreeManager> _loggerOctree;
        private readonly TrajectoryManager _trajectoryManager;
        private readonly OctreeManager _octreeManager;


        public OctreesController(ILogger<TrajectoryManager> loggerTrajectory, ILogger<OctreeManager> loggerOctree, Managers.SqlConnectionManager connectionManagerTrajectory, SqlConnectionManagerOctree connectionManagerOctree)
        {
            _loggerTrajectory = loggerTrajectory;
            _trajectoryManager = TrajectoryManager.GetInstance(_loggerTrajectory, connectionManagerTrajectory);
            
            _loggerOctree = loggerOctree;
            _octreeManager = OctreeManager.GetInstance(_loggerOctree, connectionManagerOctree);
        }

        // GET api/Octrees
        [HttpGet]
        public IEnumerable<Guid> Get()
        {
            var ids = _octreeManager.GetIDs();
            return ids;
        }
        // GET api/Octrees/id
        [HttpGet("{id}")]
        public List<OctreeCodeLong> Get(Guid id)
        {
            return _octreeManager.Get(id);
        }
        // POST api/Octrees
        [HttpPost("{id}")]
        public void Post(Guid id)
        {
            if (!id.Equals(Guid.Empty))
            {
                bool inDatabase = _octreeManager.Contains(id);
                if (!inDatabase)
                {
                    List<OctreeCodeLong>? leaves = GetLeavesFromTrajectory(id);

                    #region Save to database
                    if (leaves != null)
                    {
                        if (_octreeManager.Contains(id))
                        {
                            _octreeManager.Update(id, leaves);
                        }
                        else
                        {
                            _octreeManager.Add(leaves, id, false, true, true);
                        }
                    }
                    #endregion
                }
                else
                {
                    // We require that trajectories are registered in the trajectory database before we add them to the octree database
                }
            }
        }
        // PUT api/Octrees/id
        [HttpPut("{id}")]
        public void Put(Guid id)
        {
            if (!id.Equals(Guid.Empty))
            {
                bool inDatabase = _octreeManager.Contains(id);
                if (!inDatabase)
                {
                    List<OctreeCodeLong>? leaves = GetLeavesFromTrajectory(id);

                    #region Save to database
                    if (leaves != null)
                    {
                        if (_octreeManager.Contains(id))
                        {
                            _octreeManager.Update(id, leaves);
                        }
                        else
                        {
                            _octreeManager.Add(leaves, id, false, true, true);
                        }
                    }
                    #endregion
                }
                else
                {
                    // We require that trajectories are registered in the trajectory database before we add them to the octree database
                }
            }
        }
        // DELETE api/Octrees/id
        [HttpDelete("{id}")]
        public void Delete(Guid id)
        {
            _octreeManager.Delete(id);
        }

        private List<OctreeCodeLong>? GetLeavesFromTrajectory(Guid trajectoryId)
        {
            #region Load Trajectory from the microservices
            List<SurveyStation>? surveyList = _trajectoryManager.GetTrajectoryById(trajectoryId)?.SurveyStationList;
            #endregion

            #region Use the SurveyList to extract leaves
            return surveyList != null ? _octreeManager.GetLeavesFromSurveyList(surveyList) : null;
            #endregion
        }
    }
}
