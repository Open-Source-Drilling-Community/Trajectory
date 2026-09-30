using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.ModelTest;

public class SurveyReferenceCorrectionCalculatorTests
{
    [Test]
    public void GeodeticTrueNorthInputIsUnchanged()
    {
        bool success = SurveyReferenceCorrectionCalculator.TryCorrect(
            Math.PI / 3.0, Math.PI / 4.0,
            SurveyInclinationReference.GeodeticVertical,
            SurveyAzimuthReference.TrueNorth,
            null, null, out double inclination, out double azimuth);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(inclination, Is.EqualTo(Math.PI / 3.0).Within(1e-12));
            Assert.That(azimuth, Is.EqualTo(Math.PI / 4.0).Within(1e-12));
        });
    }

    [Test]
    public void MagneticNorthUsesFullFieldVectorProjectedOnReferencePlane()
    {
        double declination = 10.0 * Math.PI / 180.0;
        (double North, double East, double Down) field =
            (Math.Cos(declination), Math.Sin(declination), 0.4);

        bool success = SurveyReferenceCorrectionCalculator.TryCorrect(
            Math.PI / 2.0, 0.0,
            SurveyInclinationReference.GeodeticVertical,
            SurveyAzimuthReference.MagneticNorth,
            null, field, out double inclination, out double azimuth);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(inclination, Is.EqualTo(Math.PI / 2.0).Within(1e-12));
            Assert.That(azimuth, Is.EqualTo(declination).Within(1e-12));
        });
    }

    [Test]
    public void GravityTiltChangesGeodeticInclination()
    {
        double tilt = 2.0 * Math.PI / 180.0;
        (double North, double East, double Down) gravity =
            (Math.Sin(tilt), 0.0, Math.Cos(tilt));

        bool success = SurveyReferenceCorrectionCalculator.TryCorrect(
            0.0, 0.0,
            SurveyInclinationReference.GravityVertical,
            SurveyAzimuthReference.TrueNorth,
            gravity, null, out double inclination, out double azimuth);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(inclination, Is.EqualTo(tilt).Within(1e-12));
            Assert.That(azimuth, Is.EqualTo(0.0).Within(1e-12));
        });
    }

    [Test]
    public void MissingRequiredReferenceVectorFails()
    {
        bool success = SurveyReferenceCorrectionCalculator.TryCorrect(
            0.1, 0.2,
            SurveyInclinationReference.GravityVertical,
            SurveyAzimuthReference.MagneticNorth,
            null, null, out _, out _);

        Assert.That(success, Is.False);
    }
}
