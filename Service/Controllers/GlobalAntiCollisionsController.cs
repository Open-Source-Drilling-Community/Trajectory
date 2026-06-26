using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.GlobalAntiCollision;
using NORCE.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task Post([FromBody] GlobalAntiCollision.GlobalAntiCollision? value)
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
                    Model.Trajectory? referenceTrajectory = PrepareCalculationInput(value, out List<SurveyStation>? referenceSurveyList);
                    await CalculateIfPossibleAsync(value, referenceTrajectory, referenceSurveyList);
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
        public async Task Put(string id, [FromBody] GlobalAntiCollision.GlobalAntiCollision? value)
        {
            if (value == null)
            {
                _loggerGlobalAC.LogWarning("Put value is null");
                return;
            }

            try
            {
                Model.Trajectory? referenceTrajectory = PrepareCalculationInput(value, out List<SurveyStation>? referenceSurveyList);
                await CalculateIfPossibleAsync(value, referenceTrajectory, referenceSurveyList);

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

        private Model.Trajectory? PrepareCalculationInput(
            GlobalAntiCollision.GlobalAntiCollision value,
            out List<SurveyStation>? referenceSurveyList)
        {
            Model.Trajectory? referenceTrajectory = null;
            referenceSurveyList = null;
            List<Guid>? requestedComparisonTrajectoryIds =
                value.ComparisonTrajectoryIDs is { Count: > 0 }
                    ? [.. value.ComparisonTrajectoryIDs.Where(id => id != Guid.Empty)]
                    : null;

            if (!value.ReferenceWellPathID.Equals(Guid.Empty))
            {
                #region Load WellPath and Architecture
                referenceSurveyList = null;
                #endregion

                #region Use the SurveyList and Architecture to extract leaves
                List<OctreeCodeLong>? leaves = referenceSurveyList != null ? _octreeManager.GetLeavesFromSurveyList(referenceSurveyList) : null;
                #endregion

                value.ComparisonTrajectoryIDs = FilterComparisonTrajectoryIds(
                    _octreeManager.Search(leaves, false, true, true, null),
                    requestedComparisonTrajectoryIds);
                value.ReferenceTrajectoryID = Guid.Empty;
            }
            else if (!value.ReferenceTrajectoryID.Equals(Guid.Empty))
            {
                #region Load Trajectory from the microservices
                referenceTrajectory = _trajectoryManager.GetTrajectoryById(value.ReferenceTrajectoryID);
                referenceSurveyList = referenceTrajectory?.SurveyStationList;
                #endregion

                value.ComparisonTrajectoryIDs = FilterComparisonTrajectoryIds(
                    _octreeManager.Search(_octreeManager.Get(value.ReferenceTrajectoryID), false, true, true, value.ReferenceTrajectoryID),
                    requestedComparisonTrajectoryIds);
                value.ReferenceWellPathID = Guid.Empty;
            }

            return referenceTrajectory;
        }

        private static List<Guid> FilterComparisonTrajectoryIds(List<Guid>? candidateTrajectoryIds, List<Guid>? requestedComparisonTrajectoryIds)
        {
            if (candidateTrajectoryIds == null || candidateTrajectoryIds.Count == 0)
            {
                return [];
            }

            if (requestedComparisonTrajectoryIds == null || requestedComparisonTrajectoryIds.Count == 0)
            {
                return candidateTrajectoryIds;
            }

            HashSet<Guid> requestedIds = [.. requestedComparisonTrajectoryIds];
            return candidateTrajectoryIds.Where(id => requestedIds.Contains(id)).ToList();
        }

        private async Task CalculateIfPossibleAsync(
            GlobalAntiCollision.GlobalAntiCollision value,
            Model.Trajectory? referenceTrajectory,
            List<SurveyStation>? referenceSurveyList)
        {
            List<Model.Trajectory> comparisonTrajectories = GetComparisonTrajectories(value.ComparisonTrajectoryIDs);
            if (comparisonTrajectories.Count == 0)
            {
                return;
            }

            await _trajectoryManager.EnsureBoreholeRadiiAsync(referenceTrajectory);
            await Task.WhenAll(comparisonTrajectories
                .Select(trajectory => _trajectoryManager.EnsureBoreholeRadiiAsync(trajectory)));

            List<List<SurveyStation>> comparisonSurveyLists = comparisonTrajectories
                .Select(trajectory => trajectory.SurveyStationList!)
                .ToList();

            if (Numeric.IsUndefined(value.ConfidenceFactor) || value.ConfidenceFactor <= 0 || value.ConfidenceFactor > 0.999)
            {
                value.ConfidenceFactor = 0.999;
            }

            List<MeasuredDepthRange?> referenceMdRanges = [];
            List<MeasuredDepthRange?> comparisonMdRanges = [];
            BuildRelevantMdRanges(referenceSurveyList, comparisonSurveyLists, value.ConfidenceFactor, referenceMdRanges, comparisonMdRanges);

            List<AntiCollisionPairMdConstraints> pairMdConstraints =
                await SidetrackRelationshipResolver.GetAntiCollisionPairMdConstraintsAsync(
                    referenceTrajectory,
                    comparisonTrajectories,
                    _loggerGlobalAC);

            value.Calculate(
                referenceSurveyList,
                comparisonSurveyLists,
                referenceMdRanges,
                comparisonMdRanges,
                pairMdConstraints.Select(constraints => constraints.ReferenceMinimumMD).ToList(),
                pairMdConstraints.Select(constraints => constraints.ComparisonMinimumMD).ToList());
        }

        private List<Model.Trajectory> GetComparisonTrajectories(List<Guid>? comparisonTrajectoryIds)
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

            Dictionary<Guid, Model.Trajectory> trajectoriesById = [];
            foreach (Model.Trajectory comparisonTrajectory in comparisonTrajectories)
            {
                if (comparisonTrajectory?.MetaInfo?.ID is Guid comparisonTrajectoryId && comparisonTrajectoryId != Guid.Empty)
                {
                    trajectoriesById[comparisonTrajectoryId] = comparisonTrajectory;
                }
            }

            List<Model.Trajectory> filteredComparisonTrajectories = [];
            List<Guid> filteredComparisonTrajectoryIds = [];
            foreach (Guid comparisonTrajectoryId in comparisonTrajectoryIds)
            {
                if (trajectoriesById.TryGetValue(comparisonTrajectoryId, out Model.Trajectory? comparisonTrajectory) &&
                    comparisonTrajectory.SurveyStationList != null)
                {
                    filteredComparisonTrajectories.Add(comparisonTrajectory);
                    filteredComparisonTrajectoryIds.Add(comparisonTrajectoryId);
                }
            }

            comparisonTrajectoryIds.Clear();
            comparisonTrajectoryIds.AddRange(filteredComparisonTrajectoryIds);
            return filteredComparisonTrajectories;
        }

        private static void BuildRelevantMdRanges(
            List<SurveyStation>? referenceSurveyList,
            List<List<SurveyStation>> comparisonSurveyLists,
            double confidenceFactor,
            List<MeasuredDepthRange?> referenceMdRanges,
            List<MeasuredDepthRange?> comparisonMdRanges)
        {
            foreach (List<SurveyStation> comparisonSurveyList in comparisonSurveyLists)
            {
                if (RelevantMdRangeCalculator.TryGetRelevantMdRanges(
                    referenceSurveyList,
                    comparisonSurveyList,
                    confidenceFactor,
                    out MeasuredDepthRange? referenceRange,
                    out MeasuredDepthRange? comparisonRange))
                {
                    referenceMdRanges.Add(referenceRange);
                    comparisonMdRanges.Add(comparisonRange);
                }
                else
                {
                    referenceMdRanges.Add(null);
                    comparisonMdRanges.Add(null);
                }
            }
        }
    }
}
