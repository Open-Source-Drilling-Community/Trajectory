namespace NORCE.Drilling.GlobalAntiCollision
{
    public struct SeparationFactorPoint
    {
        public SeparationFactorPoint(double referenceMD, double comparisonMD, double separationFactor)
        {
            ReferenceMD = referenceMD;
            ComparisonMD = comparisonMD;
            SeparationFactor = separationFactor;
        }

        public double ReferenceMD { get; set; }

        public double ComparisonMD { get; set; }

        public double SeparationFactor { get; set; }
    }
}
