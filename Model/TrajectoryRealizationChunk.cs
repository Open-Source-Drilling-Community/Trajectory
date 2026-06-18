using OSDC.DotnetLibraries.Drilling.Surveying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Model
{
    public class TrajectoryRealizationChunk
    {
        public Guid OwnerID { get; set; }
        public int ChunkIndex { get; set; }
        public int RealizationCount { get; set; }
        public int SurveyPointCount { get; set; }
        public double? StartMD { get; set; }
        public double? EndMD { get; set; }
        public List<List<SurveyPoint>>? RealizationList { get; set; }

        public void UpdateMetadata()
        {
            RealizationCount = RealizationList?.Count ?? 0;
            SurveyPointCount = RealizationList?.Sum(realization => realization?.Count ?? 0) ?? 0;
            List<SurveyPoint>? firstRealization = RealizationList?.FirstOrDefault(realization => realization is { Count: > 0 });
            StartMD = firstRealization is { Count: > 0 } ? firstRealization[0].MD ?? firstRealization[0].Abscissa : null;
            EndMD = firstRealization is { Count: > 0 } ? firstRealization[^1].MD ?? firstRealization[^1].Abscissa : null;
        }
    }
}
