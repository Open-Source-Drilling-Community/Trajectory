using Microsoft.Extensions.Logging;
using NORCE.Drilling.GlobalAntiCollision;
using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.General.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TrajectoryModel = NORCE.Drilling.Trajectory.Model.Trajectory;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    public readonly record struct AntiCollisionPairMdConstraints(
        double? ReferenceMinimumMD,
        double? ComparisonMinimumMD);

    public static class SidetrackRelationshipResolver
    {
        public static async Task<List<AntiCollisionPairMdConstraints>> GetAntiCollisionPairMdConstraintsAsync(
            TrajectoryModel? referenceTrajectory,
            IReadOnlyList<TrajectoryModel> comparisonTrajectories,
            ILogger? logger = null)
        {
            List<AntiCollisionPairMdConstraints> constraints = Enumerable
                .Repeat(new AntiCollisionPairMdConstraints(null, null), comparisonTrajectories.Count)
                .ToList();

            if (referenceTrajectory == null || referenceTrajectory.WellBoreID == Guid.Empty)
            {
                return constraints;
            }

            IEnumerable<Guid> wellBoreIds = comparisonTrajectories
                .Select(trajectory => trajectory.WellBoreID)
                .Append(referenceTrajectory.WellBoreID)
                .Where(id => id != Guid.Empty)
                .Distinct();
            Task<(Guid ID, WellBore? WellBore)>[] wellBoreTasks = wellBoreIds
                .Select(wellBoreId => GetWellBoreAsync(wellBoreId, logger))
                .ToArray();
            (Guid ID, WellBore? WellBore)[] wellBoreResults = await Task.WhenAll(wellBoreTasks);
            Dictionary<Guid, WellBore> wellBoresById = wellBoreResults
                .Where(result => result.WellBore != null)
                .ToDictionary(result => result.ID, result => result.WellBore!);

            if (!wellBoresById.TryGetValue(referenceTrajectory.WellBoreID, out WellBore? referenceWellBore))
            {
                return constraints;
            }

            for (int i = 0; i < comparisonTrajectories.Count; i++)
            {
                TrajectoryModel comparisonTrajectory = comparisonTrajectories[i];
                if (!wellBoresById.TryGetValue(comparisonTrajectory.WellBoreID, out WellBore? comparisonWellBore))
                {
                    continue;
                }

                if (TryGetAntiCollisionPairMdConstraints(
                    referenceTrajectory,
                    referenceWellBore,
                    comparisonTrajectory,
                    comparisonWellBore,
                    out AntiCollisionPairMdConstraints pairConstraints))
                {
                    constraints[i] = pairConstraints;
                }
            }

            return constraints;
        }

        public static bool TryGetAntiCollisionPairMdConstraints(
            TrajectoryModel referenceTrajectory,
            WellBore referenceWellBore,
            TrajectoryModel comparisonTrajectory,
            WellBore comparisonWellBore,
            out AntiCollisionPairMdConstraints constraints)
        {
            constraints = new AntiCollisionPairMdConstraints(null, null);

            if (IsSidetrackOf(referenceWellBore, comparisonTrajectory.WellBoreID, out double parentTieInMD))
            {
                constraints = new AntiCollisionPairMdConstraints(
                    GetSidetrackTrajectoryMinimumMD(referenceTrajectory, parentTieInMD),
                    parentTieInMD);
                return true;
            }

            if (IsSidetrackOf(comparisonWellBore, referenceTrajectory.WellBoreID, out parentTieInMD))
            {
                constraints = new AntiCollisionPairMdConstraints(
                    parentTieInMD,
                    GetSidetrackTrajectoryMinimumMD(comparisonTrajectory, parentTieInMD));
                return true;
            }

            return false;
        }

        public static bool IsSidetrackOf(
            WellBore possibleSidetrack,
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

        public static double GetSidetrackTrajectoryMinimumMD(
            TrajectoryModel sidetrackTrajectory,
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

        private static async Task<(Guid ID, WellBore? WellBore)> GetWellBoreAsync(Guid wellBoreId, ILogger? logger)
        {
            try
            {
                return (wellBoreId, await APIUtils.ClientWellBore.GetWellBoreByIdAsync(wellBoreId));
            }
            catch (Exception ex)
            {
                logger?.LogWarning(
                    ex,
                    "Could not retrieve WellBore {WellBoreID}; sidetrack tie-in filtering will not be applied for this wellbore",
                    wellBoreId);
                return (wellBoreId, null);
            }
        }
    }
}
