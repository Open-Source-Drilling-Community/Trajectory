extern alias WebPagesAlias;

using System.Reflection;
using UI = WebPagesAlias::OSDC.Drilling.Trajectory.WebPages;
using DTO = WebPagesAlias::OSDC.Drilling.Trajectory.ModelShared;

namespace OSDC.Drilling.Trajectory.ServiceTest;

[TestFixture]
public class ResourceSelectorTests
{
    [Test]
    public async Task Field_selector_returns_every_choice_and_filters_beyond_the_first_ten()
    {
        var fields = Enumerable.Range(0, 35).Select(i => new DTO.Field
        {
            MetaInfo = new() { ID = Guid.NewGuid() }, Name = $"Field {i:00}"
        }).ToArray();
        var selector = new UI.TrajectoryResourceFilter { Fields = fields };
        var search = typeof(UI.TrajectoryResourceFilter).GetMethod("SearchFieldsAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        async Task<Guid?[]> Find(string term) => (await (Task<IEnumerable<Guid?>>)search.Invoke(selector, [term, CancellationToken.None])!).ToArray();
        Assert.That(await Find(""), Has.Length.EqualTo(35));
        Assert.That(await Find("  FIELD 34  "), Is.EqualTo(new Guid?[] { fields[34].MetaInfo.ID }));
        Assert.That(await Find("missing"), Is.Empty);
    }

    [Test]
    public void Every_shared_hierarchy_dropdown_displays_all_search_matches()
    {
        string root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        string source = File.ReadAllText(Path.Combine(root, "WebPages", "TrajectoryResourceFilter.razor"));
        var controls = System.Text.RegularExpressions.Regex.Matches(source, @"<MudAutocomplete\b[^>]*>");
        Assert.That(controls, Has.Count.EqualTo(5));
        foreach (System.Text.RegularExpressions.Match control in controls)
            Assert.That(control.Value, Does.Contain("MaxItems=\"@((int?)null)\"").And.Contain("SearchFunc="));
    }
}
