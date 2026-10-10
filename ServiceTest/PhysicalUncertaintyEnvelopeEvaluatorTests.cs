using System.Text.Json.Nodes;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Mcp.Tools;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace ServiceTest;

public sealed class PhysicalUncertaintyEnvelopeEvaluatorTests
{
    [Test]
    public void ZeroDiameterPreservesTheSourceEllipse()
    {
        var request = Request(UncertaintyProjectionPlane.Horizontal, 4, 2, .3, 0);
        Assert.That(PhysicalUncertaintyEnvelopeEvaluator.TryEvaluate(request, out var result, out _), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result!.CombinedEnvelope.SemiMajorAxis, Is.EqualTo(4).Within(1e-10));
            Assert.That(result.CombinedEnvelope.SemiMinorAxis, Is.EqualTo(2).Within(1e-10));
            Assert.That(AxisDifference(result.CombinedEnvelope.OrientationAngle, .3), Is.LessThan(1e-10));
        });
    }

    [Test]
    public void HorizontalProjectionUsesStationAttitudeAndOuterBoundContainsTheMinkowskiSum()
    {
        var request = Request(UncertaintyProjectionPlane.Horizontal, 3, 1, .25, 2);
        request.Inclination = Math.PI / 3;
        request.Azimuth = 1.1;
        Assert.That(PhysicalUncertaintyEnvelopeEvaluator.TryEvaluate(request, out var result, out _), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result!.ProjectedBoreholeCrossSection.SemiMajorAxis, Is.EqualTo(1).Within(1e-12));
            Assert.That(result.ProjectedBoreholeCrossSection.SemiMinorAxis, Is.EqualTo(.5).Within(1e-12));
            Assert.That(AxisDifference(result.ProjectedBoreholeCrossSection.OrientationAngle, 1.1 - Math.PI / 2), Is.LessThan(1e-12));
        });
        AssertContains(request, result!);
    }

    [Test]
    public void PerpendicularProjectionIsCircularAndVerticalRequiresSectionAzimuth()
    {
        var perpendicular = Request(UncertaintyProjectionPlane.Perpendicular, 2, 1, .4, .4);
        Assert.That(PhysicalUncertaintyEnvelopeEvaluator.TryEvaluate(perpendicular, out var result, out _), Is.True);
        Assert.That(result!.ProjectedBoreholeCrossSection.SemiMajorAxis, Is.EqualTo(.2).Within(1e-12));
        Assert.That(result.ProjectedBoreholeCrossSection.SemiMinorAxis, Is.EqualTo(.2).Within(1e-12));

        var vertical = Request(UncertaintyProjectionPlane.Vertical, 2, 1, .4, .4);
        Assert.That(PhysicalUncertaintyEnvelopeEvaluator.TryEvaluate(vertical, out _, out var error), Is.False);
        Assert.That(error, Does.Contain("Vertical-section azimuth"));
    }

    [Test]
    public void McpContractDeclaresAReadOnlyStatelessEvaluatorAndTypedResult()
    {
        TrajectoryMcpEndpoint endpoint = TrajectoryRestMcpToolRegistrations.Endpoints.Single(value =>
            value.ControllerType.Name == "UncertaintyEnvelopeController" && value.Method.Name == "Evaluate");
        JsonObject semantic = endpoint.InputSchema[SemanticMetadata.ExtensionName]!.AsObject();
        JsonObject output = endpoint.OutputSchema["$defs"]![nameof(PhysicalUncertaintyEnvelopeEvaluation)]![SemanticMetadata.ExtensionName]!.AsObject();
        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Behavior.ReadOnlyHint, Is.True);
            Assert.That(semantic["role"]!.GetValue<string>(), Is.EqualTo(Concepts.CircularUncertaintyEnvelopeDilation));
            Assert.That(output["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.CircularlyDilatedUncertaintyEnvelope));
            Assert.That(endpoint.Description, Does.Contain("Minkowski").Or.Contain("outer-bound"));
        });
    }

    private static PhysicalUncertaintyEnvelopeRequest Request(UncertaintyProjectionPlane projection,
        double major, double minor, double angle, double diameter) => new()
    {
        Projection = projection, UncertaintySemiMajorAxis = major, UncertaintySemiMinorAxis = minor,
        UncertaintyOrientationAngle = angle, Inclination = .6, Azimuth = .8, BoreholeDiameter = diameter
    };

    private static void AssertContains(PhysicalUncertaintyEnvelopeRequest request, PhysicalUncertaintyEnvelopeEvaluation result)
    {
        for (int i = 0; i < 720; i++)
        {
            double direction = i * Math.PI / 720;
            double exact = Support(request.UncertaintySemiMajorAxis, request.UncertaintySemiMinorAxis,
                    request.UncertaintyOrientationAngle, direction) +
                Support(result.ProjectedBoreholeCrossSection.SemiMajorAxis,
                    result.ProjectedBoreholeCrossSection.SemiMinorAxis,
                    result.ProjectedBoreholeCrossSection.OrientationAngle, direction);
            double bound = Support(result.CombinedEnvelope.SemiMajorAxis, result.CombinedEnvelope.SemiMinorAxis,
                result.CombinedEnvelope.OrientationAngle, direction);
            Assert.That(bound + 1e-10, Is.GreaterThanOrEqualTo(exact), $"direction {direction}");
        }
    }

    private static double Support(double major, double minor, double angle, double direction)
    {
        double delta = direction - angle;
        return Math.Sqrt(major * major * Math.Cos(delta) * Math.Cos(delta) +
                         minor * minor * Math.Sin(delta) * Math.Sin(delta));
    }

    private static double AxisDifference(double first, double second)
    {
        double value = Math.Abs(first - second) % Math.PI;
        return Math.Min(value, Math.PI - value);
    }
}
