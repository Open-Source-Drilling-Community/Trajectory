using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class GlobalAntiCollisionsController : ControllerBase
    {
        private readonly ILogger<TrajectoryManager> _loggerTrajectory;
        private readonly ILogger<GlobalAntiCollisionManager> _loggerGlobalAC;
        private readonly ILogger<OctreeManager> _loggerOctree;
        private readonly TrajectoryManager _trajectoryManager;
        private readonly GlobalAntiCollisionManager _globalAntiCollisionManager;
        private readonly OctreeManager _octreeManager;

        public GlobalAntiCollisionsController(ILogger<TrajectoryManager> loggerTrajectory, ILogger<GlobalAntiCollisionManager> loggerGlobalAC, ILogger<OctreeManager> loggerOctree, Managers.SqlConnectionManager connectionManagerTrajectory, SqlConnectionManagerSeparationFactorResults connectionManagerGlobalAC, SqlConnectionManagerOctree connectionManagerOctree)
        {
            _loggerTrajectory = loggerTrajectory;
            _trajectoryManager = TrajectoryManager.GetInstance(_loggerTrajectory, connectionManagerTrajectory);

            _loggerGlobalAC = loggerGlobalAC;
            _globalAntiCollisionManager = GlobalAntiCollisionManager.GetInstance(_loggerGlobalAC, connectionManagerGlobalAC);

            _loggerOctree = loggerOctree;
            _octreeManager = OctreeManager.GetInstance(_loggerOctree, connectionManagerOctree);
        }

        // GET api/globalanticollisions
        [HttpGet]
        public IEnumerable<string> Get()
        {
            var ids = _globalAntiCollisionManager.GetIDs();
            return ids;
        }

        // GET api/globalanticollisions/id
        [HttpGet("{id}")]
        public GlobalAntiCollision.GlobalAntiCollision? Get(string id)
        {
            return _globalAntiCollisionManager.Get(id);
        }

        // POST api/globalanticollisions
        [HttpPost]
        public void Post([FromBody] GlobalAntiCollision.GlobalAntiCollision? value)
        {
            if (value == null)
            {
                _loggerGlobalAC.LogWarning("Post value is null");
                return;
            }

            GlobalAntiCollision.GlobalAntiCollision? globalAntiCollision = _globalAntiCollisionManager.Get(value.ID);
            if (globalAntiCollision == null)
            {
                try
                {
                    PrepareCalculationInput(value, out List<SurveyStation>? referenceSurveyList);
                    CalculateIfPossible(value, referenceSurveyList);
                    _globalAntiCollisionManager.Add(value);
                }
                catch (Exception ex)
                {
                    _loggerGlobalAC.LogError(ex, "Post Exception");
                }
            }
            else
            {
                _loggerGlobalAC.LogInformation("GlobalAntiCollision with ID {Id} already exists", value.ID);
            }
        }

        // PUT api/globalanticollisions/id
        [HttpPut("{id}")]
        public void Put(string id, [FromBody] GlobalAntiCollision.GlobalAntiCollision? value)
        {
            if (value == null)
            {
                _loggerGlobalAC.LogWarning("Put value is null");
                return;
            }

            try
            {
                PrepareCalculationInput(value, out List<SurveyStation>? referenceSurveyList);
                CalculateIfPossible(value, referenceSurveyList);

                GlobalAntiCollision.GlobalAntiCollision? globalAntiCollision = _globalAntiCollisionManager.Get(id);
                if (globalAntiCollision != null)
                {
                    _globalAntiCollisionManager.Update(id, value);
                }
                else
                {
                    _globalAntiCollisionManager.Add(value);
                }
            }
            catch (Exception ex)
            {
                _loggerGlobalAC.LogError(ex, "Put Exception");
            }
        }

        // DELETE api/globalanticollisions/id
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            _globalAntiCollisionManager.Remove(id);
        }

        private void PrepareCalculationInput(GlobalAntiCollision.GlobalAntiCollision value, out List<SurveyStation>? referenceSurveyList)
        {
            referenceSurveyList = null;
            if (!value.ReferenceWellPathID.Equals(Guid.Empty))
            {
                #region Load WellPath and Architecture
                referenceSurveyList = null;
                #endregion

                #region Use the SurveyList and Architecture to extract leaves
                List<OctreeCodeLong>? leaves = referenceSurveyList != null ? _octreeManager.GetLeavesFromSurveyList(referenceSurveyList) : null;
                #endregion

                value.ComparisonTrajectoryIDs = _octreeManager.Search(leaves, false, true, true, null);
                value.ReferenceTrajectoryID = Guid.Empty;
            }
            else if (!value.ReferenceTrajectoryID.Equals(Guid.Empty))
            {
                #region Load Trajectory from the microservices
                referenceSurveyList = _trajectoryManager.GetTrajectoryById(value.ReferenceTrajectoryID)?.SurveyStationList;
                #endregion

                value.ComparisonTrajectoryIDs = _octreeManager.Search(_octreeManager.Get(value.ReferenceTrajectoryID), false, true, true, value.ReferenceTrajectoryID);
                value.ReferenceWellPathID = Guid.Empty;
            }
        }

        private void CalculateIfPossible(GlobalAntiCollision.GlobalAntiCollision value, List<SurveyStation>? referenceSurveyList)
        {
            List<List<SurveyStation>> comparisonSurveyLists = GetComparisonSurveyLists(value.ComparisonTrajectoryIDs);
            if (comparisonSurveyLists.Count == 0)
            {
                return;
            }

            if (Numeric.IsUndefined(value.ConfidenceFactor) || value.ConfidenceFactor <= 0 || value.ConfidenceFactor > 0.999)
            {
                value.ConfidenceFactor = 0.999;
            }
            value.Calculate(referenceSurveyList, comparisonSurveyLists);
        }

        private List<List<SurveyStation>> GetComparisonSurveyLists(List<Guid>? comparisonTrajectoryIds)
        {
            if (comparisonTrajectoryIds == null || comparisonTrajectoryIds.Count == 0)
            {
                return [];
            }

            List<Model.Trajectory>? comparisonTrajectories = _trajectoryManager.GetListOfTrajectoryById(comparisonTrajectoryIds);
            if (comparisonTrajectories == null)
            {
                return [];
            }

            List<List<SurveyStation>> comparisonSurveyLists = [];
            foreach (Model.Trajectory comparisonTrajectory in comparisonTrajectories)
            {
                if (comparisonTrajectory?.SurveyStationList != null)
                {
                    comparisonSurveyLists.Add(comparisonTrajectory.SurveyStationList);
                }
            }
            return comparisonSurveyLists;
        }
    }
}
