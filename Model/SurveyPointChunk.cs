using OSDC.DotnetLibraries.Drilling.Surveying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Model
{
    public class SurveyPointChunk
    {
        public Guid OwnerID { get; set; }
        public string? OwnerType { get; set; }
        public int ChunkIndex { get; set; }
        public int PointCount { get; set; }
        public double? StartMD { get; set; }
        public double? EndMD { get; set; }
        public List<SurveyPoint>? SurveyPointList { get; set; }

        public void UpdateMetadata()
        {
            PointCount = SurveyPointList?.Count ?? 0;
            StartMD = SurveyPointList?.FirstOrDefault()?.MD ?? SurveyPointList?.FirstOrDefault()?.Abscissa;
            EndMD = SurveyPointList?.LastOrDefault()?.MD ?? SurveyPointList?.LastOrDefault()?.Abscissa;
        }
    }
}
