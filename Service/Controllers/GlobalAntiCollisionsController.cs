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

            (List<double?> referenceMinimumMDs, List<double?> comparisonMinimumMDs) =
                await GetSidetrackMinimumMDsAsync(referenceTrajectory, comparisonTrajectories);

            value.Calculate(
                referenceSurveyList,
                comparisonSurveyLists,
                referenceMdRanges,
                comparisonMdRanges,
                referenceMinimumMDs,
                comparisonMinimumMDs);
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

        private async Task<(List<double?> ReferenceMinimumMDs, List<double?> ComparisonMinimumMDs)> GetSidetrackMinimumMDsAsync(
            Model.Trajectory? referenceTrajectory,
            List<Model.Trajectory> comparisonTrajectories)
        {
            List<double?> referenceMinimumMDs = Enumerable.Repeat<double?>(null, comparisonTrajectories.Count).ToList();
            List<double?> comparisonMinimumMDs = Enumerable.Repeat<double?>(null, comparisonTrajectories.Count).ToList();
            if (referenceTrajectory == null || referenceTrajectory.WellBoreID == Guid.Empty)
            {
                return (referenceMinimumMDs, comparisonMinimumMDs);
            }

            IEnumerable<Guid> wellBoreIds = comparisonTrajectories
                .Select(trajectory => trajectory.WellBoreID)
                .Append(referenceTrajectory.WellBoreID)
                .Where(id => id != Guid.Empty)
                .Distinct();
            Task<(Guid ID, ModelShared.WellBore? WellBore)>[] wellBoreTasks = wellBoreIds
                .Select(GetWellBoreAsync)
                .ToArray();
            (Guid ID, ModelShared.WellBore? WellBore)[] wellBoreResults = await Task.WhenAll(wellBoreTasks);
            Dictionary<Guid, ModelShared.WellBore> wellBoresById = wellBoreResults
                .Where(result => result.WellBore != null)
                .ToDictionary(result => result.ID, result => result.WellBore!);

            if (!wellBoresById.TryGetValue(referenceTrajectory.WellBoreID, out ModelShared.WellBore? referenceWellBore))
            {
                return (referenceMinimumMDs, comparisonMinimumMDs);
            }

            for (int i = 0; i < comparisonTrajectories.Count; i++)
            {
                Model.Trajectory comparisonTrajectory = comparisonTrajectories[i];
                if (!wellBoresById.TryGetValue(comparisonTrajectory.WellBoreID, out ModelShared.WellBore? comparisonWellBore))
                {
                    continue;
                }

                if (TryGetSidetrackTieIn(
                    referenceTrajectory,
                    referenceWellBore,
                    comparisonTrajectory,
                    comparisonWellBore,
                    out double referenceMinimumMD,
                    out double comparisonMinimumMD))
                {
                    referenceMinimumMDs[i] = referenceMinimumMD;
                    comparisonMinimumMDs[i] = comparisonMinimumMD;
                }
            }

            return (referenceMinimumMDs, comparisonMinimumMDs);
        }

        private async Task<(Guid ID, ModelShared.WellBore? WellBore)> GetWellBoreAsync(Guid wellBoreId)
        {
            try
            {
                return (wellBoreId, await APIUtils.ClientWellBore.GetWellBoreByIdAsync(wellBoreId));
            }
            catch (Exception ex)
            {
                _loggerGlobalAC.LogWarning(
                    ex,
                    "Could not retrieve WellBore {WellBoreID}; sidetrack tie-in filtering will not be applied for this wellbore",
                    wellBoreId);
                return (wellBoreId, null);
            }
        }

        private static bool TryGetSidetrackTieIn(
            Model.Trajectory referenceTrajectory,
            ModelShared.WellBore referenceWellBore,
            Model.Trajectory comparisonTrajectory,
            ModelShared.WellBore comparisonWellBore,
            out double referenceMinimumMD,
            out double comparisonMinimumMD)
        {
            referenceMinimumMD = 0;
            comparisonMinimumMD = 0;

            if (IsSidetrackOf(referenceWellBore, comparisonTrajectory.WellBoreID, out double parentTieInMD))
            {
                referenceMinimumMD = GetSidetrackTrajectoryMinimumMD(referenceTrajectory, parentTieInMD);
                comparisonMinimumMD = parentTieInMD;
                return true;
            }

            if (IsSidetrackOf(comparisonWellBore, referenceTrajectory.WellBoreID, out parentTieInMD))
            {
                referenceMinimumMD = parentTieInMD;
                comparisonMinimumMD = GetSidetrackTrajectoryMinimumMD(comparisonTrajectory, parentTieInMD);
                return true;
            }

            return false;
        }

        private static bool IsSidetrackOf(
            ModelShared.WellBore possibleSidetrack,
            Guid possibleParentWellBoreID,
            out double parentTieInMD)
        {
            parentTieInMD = 0;
            if (!possibleSidetrack.IsSidetrack ||
                possibleSidetrack.ParentWellBoreID != possibleParentWellBoreID ||
                possibleSidetrack.TieInPointAlongHoleDepth?.GaussianValue?.Mean is not double tieInMD ||
                !Numeric.IsDefined(tieInMD) ||
                tieInMD < 0)
            {
                return false;
            }

            parentTieInMD = tieInMD;
            return true;
        }

        private static double GetSidetrackTrajectoryMinimumMD(
            Model.Trajectory sidetrackTrajectory,
            double parentTieInMD)
        {
            double? trajectoryTieInMD = sidetrackTrajectory.TieInPoint?.MD ??
                sidetrackTrajectory.TieInPoint?.Abscissa;
            if (trajectoryTieInMD is double tieInMD && Numeric.IsDefined(tieInMD))
            {
                return tieInMD;
            }

            MeasuredDepthRange? sidetrackRange = RelevantMdRangeCalculator.GetSurveyMdRange(
                sidetrackTrajectory.SurveyStationList);
            if (sidetrackRange == null)
            {
                return parentTieInMD;
            }

            return parentTieInMD >= sidetrackRange.StartMD && parentTieInMD <= sidetrackRange.EndMD
                ? parentTieInMD
                : sidetrackRange.StartMD;
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
