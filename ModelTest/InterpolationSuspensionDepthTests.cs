using System.Security.Cryptography;
using System.Text.Json;
using OSDC.Drilling.Trajectory.WebPages;
using OSDC.Drilling.Trajectory.Model;
using OSDC.DotnetLibraries.Drilling.Surveying;

namespace OSDC.Drilling.Trajectory.ModelTest;

public class InterpolationSuspensionDepthTests
{
    [Test]
    public void CapturedU3SectionsSupplySuspensionWhenSeparateWellheadMarkerIsEmpty()
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Ullrigg", "Interpolation-U3-Architecture.json");
        Assert.That(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
            Is.EqualTo("B437D2333BA30E68F1AFCAB1036590469F9B6D136604C8730CC4A031EC333972"));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var architecture = document.RootElement;
        Assert.That(architecture.GetProperty("WellBoreID").GetGuid(), Is.EqualTo(Guid.Parse("9ac48b0f-61bd-46de-a6ae-3ba26ff8f8d7")));
        var tops = architecture.GetProperty("CasingSections").EnumerateArray()
            .Select(s => (double?)s.GetProperty("TopDepth").GetProperty("GaussianValue").GetProperty("Mean").GetDouble()).ToArray();
        Assert.That(architecture.GetProperty("WellHead").GetProperty("CasingHangerDepth").GetProperty("DiracDistributionValue").GetProperty("Value").ValueKind,
            Is.EqualTo(JsonValueKind.Null));
        Assert.That(InterpolationSuspensionDepths.Resolve(tops, null), Is.EqualTo(new[] { -90.97 }));
    }

    [Test]
    public void MultipleStringsAndExplicitWellheadMarkerAreRetainedWithoutInternalIntervalTops()
    {
        double?[] sectionTops = [-90.97, 240, 240, 400];
        var before = sectionTops.ToArray();
        Assert.That(InterpolationSuspensionDepths.Resolve(sectionTops, -91), Is.EqualTo(new[] { -91d, -90.97, 240, 400 }));
        Assert.That(sectionTops, Is.EqualTo(before));
        Assert.That(InterpolationSuspensionDepths.Resolve(null, 12), Is.EqualTo(new[] { 12d }));
    }

    [Test]
    public void MissingInvalidAndCoincidentSuspensionsDoNotInventZeroOrDuplicateStations()
    {
        Assert.That(InterpolationSuspensionDepths.Resolve([null, double.NaN, double.PositiveInfinity, double.NegativeInfinity], null), Is.Empty);
        Assert.That(InterpolationSuspensionDepths.Resolve(null, null), Is.Empty);
        Assert.That(InterpolationSuspensionDepths.Resolve([10, 10.0000001, 20], 10), Is.EqualTo(new[] { 10d, 20d }));
        Assert.That(InterpolationSuspensionDepths.Resolve([0], null), Is.EqualTo(new[] { 0d }), "A defined zero is a valid suspension depth.");
    }

    [Test]
    public void ResolvedSuspensionIsAnActualInterpolationTargetBetweenRegularGridStations()
    {
        var source = new Model.Trajectory
        {
            SurveyStationList = [
                new() { MD = 0, Abscissa = 0, Inclination = 0, Azimuth = 0, TVD = 0, X = 0, Y = 0, Z = 0, RiemannianNorth = 0, RiemannianEast = 0 },
                new() { MD = 100, Abscissa = 100, Inclination = 0, Azimuth = 0, TVD = 100, X = 0, Y = 0, Z = 100, RiemannianNorth = 0, RiemannianEast = 0 }
            ]
        };
        var calculation = new InterpolatedTrajectory
        {
            InterpolationStep = 10, InterpolationReferenceDepth = 0, InterpolateAtLinerHangerDepths = true,
            InternalAdditionalAbscissaList = InterpolationSuspensionDepths.Resolve([15, 15], null)
                .Select(depth => new AnnotatedAbscissa { Abscissa = depth, Annotation = "Casing hanger" }).ToList()
        };
        Assert.That(calculation.Calculate(source), Is.True);
        var stations = calculation.SurveyStationList!;
        var depths = stations.Select(s => s.MD!.Value).ToArray();
        Assert.That(depths.Count(d => Math.Abs(d - 15) < 1e-6), Is.EqualTo(1));
        Assert.That(depths, Does.Contain(10d)); Assert.That(depths, Does.Contain(20d));
        Assert.That(stations.Single(s => Math.Abs(s.MD!.Value - 15) < 1e-6).Annotation, Is.EqualTo("Casing hanger"));
    }
}
