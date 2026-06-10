using System;

namespace NORCE.Drilling.Trajectory.Model
{
    public class MinimumDistanceReferenceInterval
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public double? StartMD { get; set; }
        public double? EndMD { get; set; }

        public MinimumDistanceReferenceInterval()
        {
        }
    }
}
