using MudBlazor;

public class ExpandButton(bool isExpanded = true)
{
    private bool _isExpanded = isExpanded;

    public string PanelText { get => _isExpanded ? "Hide" : "Show"; }
    public string PanelIcon { get => _isExpanded ? Icons.Material.Filled.ExpandMore : Icons.Material.Filled.ExpandLess; }
    public bool IsExpanded { get => _isExpanded; set => _isExpanded = IsExpanded; }

    public static void ExpandPanel(ExpandButton eb)
    {
        eb._isExpanded = !eb._isExpanded;
    }
}