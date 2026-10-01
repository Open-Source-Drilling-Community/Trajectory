extern alias ModelSharedOutAlias;

using System.Text.Json;
using System.Text.Json.Serialization;
using ClientModel = ModelSharedOutAlias::OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public sealed class GeneratedClientPolymorphismTests
{
    [Test]
    public void Generated_polymorphic_models_emit_one_leading_discriminator_after_round_trip()
    {
        JsonSerializerOptions options = new();
        options.Converters.Add(new JsonStringEnumConverter());

        AssertRoundTripDiscriminator(
            new ClientModel.FixedLengthExtrapolationSpecification
            {
                Length = 30,
                ExtensionType = ClientModel.FixedLengthExtrapolationType.Straight
            },
            typeof(ClientModel.TrajectoryExtrapolationSpecification), "Mode", options);

        AssertRoundTripDiscriminator(
            new ClientModel.CircularArcWellPathSectionSpecification
            {
                SectionID = Guid.NewGuid(),
                Length = 30,
                Curvature = 0.001
            },
            typeof(ClientModel.WellPathSectionSpecification), "CurveType", options);

        AssertRoundTripDiscriminator(
            new ClientModel.DepartureGeosteeringExtentConstraint
            {
                DepartureDistance = 100,
                DepartureBearing = 0.5
            },
            typeof(ClientModel.GeosteeringExtentConstraint), "ExtentType", options);
    }

    private static void AssertRoundTripDiscriminator(
        object value,
        Type declaredType,
        string discriminatorName,
        JsonSerializerOptions options)
    {
        string initialJson = JsonSerializer.Serialize(value, declaredType, options);
        object roundTripped = JsonSerializer.Deserialize(initialJson, declaredType, options)!;
        string savedJson = JsonSerializer.Serialize(roundTripped, declaredType, options);
        using JsonDocument document = JsonDocument.Parse(savedJson);
        JsonProperty[] properties = document.RootElement.EnumerateObject().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(properties[0].Name, Is.EqualTo(discriminatorName));
            Assert.That(properties.Count(property => property.Name == discriminatorName), Is.EqualTo(1));
        });
    }
}
