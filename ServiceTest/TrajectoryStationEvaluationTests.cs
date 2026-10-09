using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace ServiceTest;

public class TrajectoryStationEvaluationTests
{
    private static OSDC.Drilling.Trajectory.Model.Trajectory Vertical()=>new(){CalculationType=TrajectoryCalculationType.MinimumCurvatureMethod,
        SurveyStationList=[new(){MD=0,Inclination=0,Azimuth=0,X=0,Y=0,Z=0},new(){MD=100,Inclination=0,Azimuth=0,X=0,Y=0,Z=100}]};

    [Test] public void ReplayedUncertaintyIsAppliedOnlyToMatchingCalculatedGeometry()
    {
        var calculated=new List<SurveyStation>{new(){MD=0,Inclination=0,Azimuth=0},new(){MD=100,Inclination=.1,Azimuth=.2},new(){MD=200,Inclination=.2,Azimuth=.3}};
        var replayed=calculated.Select(station=>new SurveyStation(station){Covariance=new(),SurveyTool=new()}).ToList();

        Assert.That(TrajectoryManager.TryApplyRecalculatedUncertainty(calculated,replayed),Is.True);
        Assert.That(calculated,Has.All.Property(nameof(SurveyStation.Covariance)).Not.Null);

        replayed[1].Inclination=.11;
        Assert.That(TrajectoryManager.TryApplyRecalculatedUncertainty(calculated,replayed),Is.False);
    }
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
    [Test] public void ReferencedDepthUsesThePathIntersectionRatherThanVerticalOffset() {
        var t=Vertical();foreach(var p in t.SurveyStationList!){p.Inclination=Math.PI/3;p.X=(p.MD??0)*Math.Sin(Math.PI/3);p.Z=(p.MD??0)*0.5;}
        Assert.That(TrajectoryStationEvaluation.TryEvaluateReferenced(t,30,10,out var result),Is.True);
        Assert.That(result!.OriginNativeAlongHoleDepth,Is.EqualTo(20).Within(1e-6));
        Assert.That(result.Station.MD??result.Station.Abscissa,Is.EqualTo(50).Within(1e-6));
        Assert.That(result.Station.Z,Is.EqualTo(25).Within(1e-6));
        Assert.That(t.SurveyStationList![0].MD,Is.Zero);
    }
    [Test] public void ReferenceIntersectionRejectsExtrapolationAndNonmonotonePaths() {
        Assert.That(TrajectoryStationEvaluation.TryEvaluateReferenced(Vertical(),50,-1,out _),Is.False);
        var t=Vertical();t.SurveyStationList![1].Inclination=Math.PI;
        Assert.That(TrajectoryStationEvaluation.TryEvaluateReferenced(t,10,20,out _),Is.False);
        Assert.That(TrajectoryStationEvaluation.TryEvaluateReferenced(Vertical(),101,0,out _),Is.False);
    }
    [Test] public void VerticalEllipseUsesFullDiametersAndRequestedProbability() {
        var t=Vertical();foreach(var p in t.SurveyStationList!){
            p.Covariance=new OSDC.DotnetLibraries.General.Math.SymmetricMatrix3x3();
            p.Covariance[0,0]=9;p.Covariance[1,1]=4;p.Covariance[2,2]=1;
            p.Covariance[0,1]=p.Covariance[0,2]=p.Covariance[1,2]=0;
        }
        Assert.That(TrajectoryStationEvaluation.TryEvaluateVerticalEllipse(t,50,0.95,out var high),Is.True);
        Assert.That(TrajectoryStationEvaluation.TryEvaluateVerticalEllipse(t,50,0.50,out var low),Is.True);
        Assert.That(high!.VerticalEllipse.MajorAxis/high.VerticalEllipse.MinorAxis,Is.EqualTo(3).Within(1e-8));
        Assert.That(high.VerticalEllipse.OrientationAngle%Math.PI,Is.EqualTo(Math.PI/2).Within(1e-8));
        Assert.That(high.VerticalEllipse.MajorAxis,Is.EqualTo(2*Math.Sqrt(9*OSDC.DotnetLibraries.General.Statistics.Statistics.GetChiSquare3D(0.95))).Within(1e-8));
        Assert.That(low!.VerticalEllipse.MajorAxis,Is.LessThan(high.VerticalEllipse.MajorAxis));
        Assert.That(high.ConfidenceFactor,Is.EqualTo(0.95));Assert.That(t.SurveyStationList,Has.Count.EqualTo(2));
    }
    [TestCase(0)] [TestCase(1)] [TestCase(95)]
    public void EllipseRejectsInvalidProbabilities(double confidence)=>Assert.That(TrajectoryStationEvaluation.TryEvaluateVerticalEllipse(Vertical(),50,confidence,out _),Is.False);
}
