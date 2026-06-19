using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryMinimumDistanceResultChunk
    {
        public Guid OwnerID { get; set; }
        public int ChunkIndex { get; set; }
        public int ResultCount { get; set; }
        public double? StartReferenceMD { get; set; }
        public double? EndReferenceMD { get; set; }
        public List<TrajectoryMinimumDistanceResult>? ResultList { get; set; }

        public void UpdateMetadata()
        {
            ResultCount = ResultList?.Count ?? 0;
            StartReferenceMD = ResultList is { Count: > 0 } ? ResultList[0].ReferenceMD : null;
            EndReferenceMD = ResultList is { Count: > 0 } ? ResultList[^1].ReferenceMD : null;
        }
    }
}
