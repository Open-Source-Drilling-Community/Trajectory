using System;

namespace NORCE.Drilling.GlobalAntiCollision
{
    public class MeasuredDepthRange : ICloneable
    {
        public double StartMD { get; set; }

        public double EndMD { get; set; }

        public MeasuredDepthRange()
        {
        }

        public MeasuredDepthRange(double startMD, double endMD)
        {
            StartMD = startMD;
            EndMD = endMD;
        }

        public MeasuredDepthRange(MeasuredDepthRange? src)
        {
            if (src != null)
            {
                StartMD = src.StartMD;
                EndMD = src.EndMD;
            }
        }

        public object Clone()
        {
            return new MeasuredDepthRange(this);
        }
    }
}
