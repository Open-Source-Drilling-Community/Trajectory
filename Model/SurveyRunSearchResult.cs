using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Model;

/// <summary>A deterministic bounded page of matching survey runs.</summary>
public sealed class SurveyRunSearchResult
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public List<SurveyRunLight> Items { get; set; } = [];
}
