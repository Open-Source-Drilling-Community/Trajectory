using System.Text.Json;
using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.ModelTest;

public class SurveyMeasurementCompatibilityTests
{
    [Test]
    public void LegacySurveyRunKeepsCanonicalReferenceDefaults()
    {
        SurveyRunLight? run = JsonSerializer.Deserialize<SurveyRunLight>("{}");

        Assert.Multiple(() =>
        {
            Assert.That(run, Is.Not.Null);
            Assert.That(run!.DefaultInclinationReference, Is.EqualTo(SurveyInclinationReference.GeodeticVertical));
            Assert.That(run.DefaultAzimuthReference, Is.EqualTo(SurveyAzimuthReference.TrueNorth));
            Assert.That(run.AcquisitionStartUtc, Is.Null);
            Assert.That(run.AcquisitionEndUtc, Is.Null);
        });
    }

    [Test]
    public void LegacyMeasurementDefaultsToRunInheritanceAndRetainsCanonicalAngles()
    {
        SurveyMeasurement? measurement = JsonSerializer.Deserialize<SurveyMeasurement>(
            "{\"MD\":100.0,\"Inclination\":0.1,\"Azimuth\":0.2}");

        Assert.Multiple(() =>
        {
            Assert.That(measurement, Is.Not.Null);
            Assert.That(measurement!.InclinationReference, Is.EqualTo(SurveyInclinationReference.InheritRun));
            Assert.That(measurement.AzimuthReference, Is.EqualTo(SurveyAzimuthReference.InheritRun));
            Assert.That(measurement.Inclination, Is.EqualTo(0.1));
            Assert.That(measurement.Azimuth, Is.EqualTo(0.2));
            Assert.That(measurement.ObservedInclination, Is.Null);
            Assert.That(measurement.ObservedAzimuth, Is.Null);
            Assert.That(measurement.Origin, Is.EqualTo(SurveyMeasurementOrigin.Measured));
        });
    }
}
