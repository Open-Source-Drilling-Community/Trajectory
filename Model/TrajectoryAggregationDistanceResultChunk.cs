using System;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryAggregationDistanceResultChunk
    {
        public Guid OwnerID { get; set; }
        public int ChunkIndex { get; set; }
        public int ResultCount { get; set; }
        public double? StartReferenceMD { get; set; }
        public double? EndReferenceMD { get; set; }
        public List<TrajectoryAggregationDistanceResult>? ResultList { get; set; }

        public void UpdateMetadata()
        {
            ResultCount = ResultList?.Count ?? 0;
            StartReferenceMD = ResultList?.FirstOrDefault()?.ReferenceMD;
            EndReferenceMD = ResultList?.LastOrDefault()?.ReferenceMD;
        }
    }
}
