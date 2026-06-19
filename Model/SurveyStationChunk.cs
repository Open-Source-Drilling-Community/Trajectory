using OSDC.DotnetLibraries.Drilling.Surveying;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    public class SurveyStationChunk
    {
        public Guid OwnerID { get; set; }
        public string? OwnerType { get; set; }
        public int ChunkIndex { get; set; }
        public int StationCount { get; set; }
        public double? StartMD { get; set; }
        public double? EndMD { get; set; }
        public List<SurveyStation>? SurveyStationList { get; set; }

        public void UpdateMetadata()
        {
            StationCount = SurveyStationList?.Count ?? 0;
            StartMD = SurveyStationList is { Count: > 0 }
                ? SurveyStationList[0].MD ?? SurveyStationList[0].Abscissa
                : null;
            EndMD = SurveyStationList is { Count: > 0 }
                ? SurveyStationList[^1].MD ?? SurveyStationList[^1].Abscissa
                : null;
        }
    }
}
