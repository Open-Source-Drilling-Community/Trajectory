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
        Assert.That(station.Latitude,Is.EqualTo(0).Within(1e-10));Assert.That(station.Longitude,Is.EqualTo(0).Within(1e-10));
        Assert.That(trajectory.SurveyStationList,Has.Count.EqualTo(2));
    }
    [Test] public void InterpolatedRiemannianPositionHasEquivalentGeographicCoordinates()
    {
        var start=new OSDC.DotnetLibraries.General.Math.Point3DGlobalCoordinates();start.SetRiemannianNorthEast(58.95*Math.PI/180,5.73*Math.PI/180);
        var trajectory=Vertical();foreach(var point in trajectory.SurveyStationList!){point.X=start.X;point.Y=start.Y;point.Latitude=null;point.Longitude=null;}
        Assert.That(TrajectoryStationEvaluation.TryEvaluate(trajectory,50,out var station),Is.True);
        Assert.That(station!.Latitude,Is.EqualTo(58.95*Math.PI/180).Within(1e-10));
        Assert.That(station.Longitude,Is.EqualTo(5.73*Math.PI/180).Within(1e-10));
        Assert.That(station.X,Is.EqualTo(start.X).Within(1e-6));Assert.That(station.Y,Is.EqualTo(start.Y).Within(1e-6));
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
