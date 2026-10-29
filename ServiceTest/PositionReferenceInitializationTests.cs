extern alias WebPagesAlias;

using UI = WebPagesAlias::OSDC.Drilling.Trajectory.WebPages;
using DTO = WebPagesAlias::OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
[NonParallelizable]
public class PositionReferenceInitializationTests
{
    [TestCase("TrajectoryEdit.razor")]
    [TestCase("SurveyRunMain.razor")]
    public void Tables_and_horizontal_and_3d_graphs_use_the_selected_position_reference(string page)
    {
        string root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        string source = File.ReadAllText(Path.Combine(root, "WebPages", page));
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("DrillingSignalReference=\"@DrillingSignalReferenceType.Position\" PositionDirection=\"@PositionDirectionType.North\""));
            Assert.That(source, Does.Contain("DrillingSignalReference=\"@DrillingSignalReferenceType.Position\" PositionDirection=\"@PositionDirectionType.East\""));
            Assert.That(System.Text.RegularExpressions.Regex.Matches(source, "XAxisUsesPositionReference=\"@true\"").Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(System.Text.RegularExpressions.Regex.Matches(source, "YAxisUsesPositionReference=\"@true\"").Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(source, Does.Not.Contain("Drilling.Surveying.SurveyPoint surveyPoint"));
        });
    }

    [TestCase("Well-head")]
    [TestCase("Cluster-reference")]
    [TestCase("Field")]
    [TestCase("WGS84")]
    [TestCase("Cartographic")]
    public void Initialization_resolves_origins_and_preserves_selected_reference(string reference)
    {
        var trajectoryId = Guid.NewGuid();
        var boreId = Guid.NewGuid();
        var wellId = Guid.NewGuid();
        var clusterId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var slot = new DTO.Slot
        {
            ID = slotId,
            Latitude = new() { GaussianValue = new() { Mean = 1.0 } },
            Longitude = new() { GaussianValue = new() { Mean = 0.1 } }
        };
        UI.DataUtils.UnitAndReferenceParameters.PositionReferenceName = reference;
        UI.DataUtils.ApplyTrajectoryReferenceValues(trajectoryId,
            [new() { MetaInfo = new() { ID = trajectoryId }, WellBoreID = boreId, FieldID = fieldId }],
            [new() { MetaInfo = new() { ID = boreId }, WellID = wellId }],
            [new() { MetaInfo = new() { ID = wellId }, ClusterID = clusterId, SlotID = slotId }],
            [new() { MetaInfo = new() { ID = clusterId }, ReferencePoint = new() { Latitude = 1.0, Longitude = 0.1 }, Slots = new Dictionary<string, DTO.Slot> { ["slot"] = slot } }],
            [], [new() { MetaInfo = new() { ID = fieldId }, ReferencePoint = new() { RiemannianNorth = 100, RiemannianEast = 200 } }]);

        Assert.Multiple(() =>
        {
            Assert.That(UI.DataUtils.UnitAndReferenceParameters.PositionReferenceName, Is.EqualTo(reference));
            Assert.That(UI.DataUtils.WellHeadPositionReferenceSource.WellHeadNorthPositionReference, Is.LessThan(-6000000));
            Assert.That(UI.DataUtils.WellHeadPositionReferenceSource.WellHeadEastPositionReference, Is.LessThan(-300000));
            Assert.That(UI.DataUtils.ClusterPositionReferenceSource.ClusterNorthPositionReference,
                Is.EqualTo(UI.DataUtils.WellHeadPositionReferenceSource.WellHeadNorthPositionReference));
            Assert.That(UI.DataUtils.ClusterPositionReferenceSource.ClusterEastPositionReference,
                Is.EqualTo(UI.DataUtils.WellHeadPositionReferenceSource.WellHeadEastPositionReference));
            Assert.That(UI.DataUtils.FieldPositionReferenceSource.FieldNorthPositionReference, Is.EqualTo(-100));
            Assert.That(UI.DataUtils.FieldPositionReferenceSource.FieldEastPositionReference, Is.EqualTo(-200));
        });
    }
}
