using OSDC.Drilling.Trajectory.Service;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace ServiceTest;

public class TrajectoryStationEvaluationTests
{
    private static OSDC.Drilling.Trajectory.Model.Trajectory Vertical()=>new(){CalculationType=TrajectoryCalculationType.MinimumCurvatureMethod,
        SurveyStationList=[new(){MD=0,Inclination=0,Azimuth=0,X=0,Y=0,Z=0},new(){MD=100,Inclination=0,Azimuth=0,X=0,Y=0,Z=100}]};
    [Test] public void EvaluatesACompleteStationWithTheAuthoritativeMethod() {
        var trajectory=Vertical();
        Assert.That(TrajectoryStationEvaluation.TryEvaluate(trajectory,50,out var station),Is.True);
        Assert.That(station!.MD??station.Abscissa,Is.EqualTo(50).Within(1e-8));
        Assert.That(station.Z,Is.EqualTo(50).Within(1e-6));
        Assert.That(station.Inclination,Is.EqualTo(0).Within(1e-10));
        Assert.That(trajectory.SurveyStationList,Has.Count.EqualTo(2));
    }
    [TestCase(-1)] [TestCase(101)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
    public void RejectsNonfiniteDepthsAndExtrapolation(double depth)
        =>Assert.That(TrajectoryStationEvaluation.TryEvaluate(Vertical(),depth,out _),Is.False);
    [Test] public void RejectsAmbiguousStationOrdering() {
        var trajectory=Vertical();trajectory.SurveyStationList!.Reverse();
        Assert.That(TrajectoryStationEvaluation.TryEvaluate(trajectory,50,out _),Is.False);
    }
    [Test] public void RejectsAnUnfinishedCalculation() {
        var trajectory=Vertical();trajectory.CalculationState=OSDC.Drilling.Trajectory.Model.CalculationState.Running;
        Assert.That(TrajectoryStationEvaluation.TryEvaluate(trajectory,50,out _),Is.False);
    }
}
