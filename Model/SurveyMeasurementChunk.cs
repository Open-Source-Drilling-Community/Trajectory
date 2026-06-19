using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Model
{
    public class SurveyMeasurementChunk
    {
        public Guid SurveyRunID { get; set; }
        public int ChunkIndex { get; set; }
        public int MeasurementCount { get; set; }
        public double? StartMD { get; set; }
        public double? EndMD { get; set; }
        public List<SurveyMeasurement>? SurveyMeasurementList { get; set; }

        public void UpdateMetadata()
        {
            MeasurementCount = SurveyMeasurementList?.Count ?? 0;
            StartMD = SurveyMeasurementList is { Count: > 0 }
                ? SurveyMeasurementList[0].MD
                : null;
            EndMD = SurveyMeasurementList is { Count: > 0 }
                ? SurveyMeasurementList[^1].MD
                : null;
        }
    }
}
