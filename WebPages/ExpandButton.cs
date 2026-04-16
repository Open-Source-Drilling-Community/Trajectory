using MudBlazor;

namespace NORCE.Drilling.Trajectory.WebPages;

public class ExpandButton(bool isExpanded = true)
{
    private bool _isExpanded = isExpanded;

    public string PanelText => _isExpanded ? "Hide" : "Show";
    public string PanelIcon => _isExpanded ? Icons.Material.Filled.ExpandMore : Icons.Material.Filled.ExpandLess;
    public bool IsExpanded { get => _isExpanded; set => _isExpanded = value; }

    public static void ExpandPanel(ExpandButton eb)
    {
        eb._isExpanded = !eb._isExpanded;
    }
}
