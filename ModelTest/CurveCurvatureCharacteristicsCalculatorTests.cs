using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.ModelTest;

public class CurveCurvatureCharacteristicsCalculatorTests
{
    [TestCase(ExtrapolationCurveType.CircularArc)]
    [TestCase(ExtrapolationCurveType.ConstantCurvatureAndToolface)]
    public void ConstantCurvatureDoubleCurveHasEqualCharacteristics(ExtrapolationCurveType type)
    {
        const double curvature = 0.0123;
        var sections = new[]
        {
            Section(0, TrajectoryExtrapolationSectionRole.UpstreamSteeringSection, type, 0.2, 0.4, curvature),
            Section(1, TrajectoryExtrapolationSectionRole.DownstreamSteeringSection, type, 0.4, 0.6, curvature)
        };

        CurveCurvatureCharacteristics? result = CurveCurvatureCharacteristicsCalculator.Calculate(sections);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.MinimumCurvature, Is.EqualTo(curvature));
            Assert.That(result.JunctionCurvature, Is.EqualTo(curvature));
            Assert.That(result.MaximumCurvature, Is.EqualTo(curvature));
        });
    }

    [Test]
    public void BuildTurnDoubleCurveUsesAnalyticalExtremaAndContinuousJunction()
    {
        const double build = 0.01;
        const double turn = 0.02;
        var sections = new[]
        {
            BuildTurnSection(0, TrajectoryExtrapolationSectionRole.UpstreamSteeringSection,
                0.0, Math.PI / 2.0, build, turn),
            BuildTurnSection(1, TrajectoryExtrapolationSectionRole.DownstreamSteeringSection,
                Math.PI / 2.0, Math.PI, -build, turn)
        };

        CurveCurvatureCharacteristics? result = CurveCurvatureCharacteristicsCalculator.Calculate(sections);

        double maximum = Math.Sqrt(build * build + turn * turn);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.MinimumCurvature, Is.EqualTo(build).Within(1e-12));
            Assert.That(result.JunctionCurvature, Is.EqualTo(maximum).Within(1e-12));
            Assert.That(result.MaximumCurvature, Is.EqualTo(maximum).Within(1e-12));
        });
    }

    [Test]
    public void LeadInIsExcludedFromSteeringCharacteristics()
    {
        var sections = new[]
        {
            Section(0, TrajectoryExtrapolationSectionRole.LeadInContinuation,
                ExtrapolationCurveType.CircularArc, 0.1, 0.2, 0.5),
            Section(1, TrajectoryExtrapolationSectionRole.UpstreamSteeringSection,
                ExtrapolationCurveType.CircularArc, 0.2, 0.3, 0.02),
            Section(2, TrajectoryExtrapolationSectionRole.DownstreamSteeringSection,
                ExtrapolationCurveType.CircularArc, 0.3, 0.4, 0.02)
        };

        CurveCurvatureCharacteristics? result = CurveCurvatureCharacteristicsCalculator.Calculate(sections);

        Assert.That(result!.MaximumCurvature, Is.EqualTo(0.02));
    }

    private static TrajectoryExtrapolationSolvedSection Section(int index,
        TrajectoryExtrapolationSectionRole role, ExtrapolationCurveType type,
        double startInclination, double endInclination, double curvature) => new()
    {
        SectionIndex = index,
        Role = role,
        CurveType = type,
        Start = new SurveyStation { Inclination = startInclination },
        End = new SurveyStation { Inclination = endInclination },
        CircularArcCurvature = type == ExtrapolationCurveType.CircularArc ? curvature : null,
        ConstantCurvature = type == ExtrapolationCurveType.ConstantCurvatureAndToolface ? curvature : null
    };

    private static TrajectoryExtrapolationSolvedSection BuildTurnSection(int index,
        TrajectoryExtrapolationSectionRole role, double startInclination, double endInclination,
        double build, double turn) => new()
    {
        SectionIndex = index,
        Role = role,
        CurveType = ExtrapolationCurveType.ConstantBuildAndTurn,
        Start = new SurveyStation { Inclination = startInclination },
        End = new SurveyStation { Inclination = endInclination },
        ConstantBuildRate = build,
        ConstantTurnRate = turn
    };
}
