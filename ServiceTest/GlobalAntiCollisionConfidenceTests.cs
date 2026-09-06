using OSDC.Drilling.Trajectory.Service.Controllers;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace ServiceTest;

[TestFixture]
public sealed class GlobalAntiCollisionConfidenceTests
{
    [Test]
    public void Separation_factor_limit_matches_octree_encoding_confidence()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision.DefaultConfidenceFactor,
                Is.EqualTo(0.95));
            Assert.That(
                OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision.MaximumConfidenceFactor,
                Is.EqualTo(OctreeManager.ConfidenceFactor));
        });
    }

    [TestCase(0.95, true)]
    [TestCase(0.999, true)]
    [TestCase(1.0, false)]
    [TestCase(0.0, false)]
    [TestCase(-0.1, false)]
    public void Rest_validation_enforces_supported_confidence_interval(double confidenceFactor, bool expected)
    {
        Assert.That(GlobalAntiCollisionsController.IsValidConfidenceFactor(confidenceFactor), Is.EqualTo(expected));
    }

    [Test]
    public void Rest_validation_rejects_non_finite_confidence_factors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GlobalAntiCollisionsController.IsValidConfidenceFactor(double.NaN), Is.False);
            Assert.That(GlobalAntiCollisionsController.IsValidConfidenceFactor(double.PositiveInfinity), Is.False);
            Assert.That(GlobalAntiCollisionsController.IsValidConfidenceFactor(double.NegativeInfinity), Is.False);
        });
    }

    [Test]
    public void Domain_calculation_rejects_confidence_above_octree_encoding_confidence()
    {
        var calculation = new OSDC.Drilling.GlobalAntiCollision.GlobalAntiCollision
        {
            ConfidenceFactor = OctreeManager.ConfidenceFactor + 0.0001
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => calculation.Calculate(null, null));
    }
}
