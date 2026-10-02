namespace OSDC.Drilling.Trajectory.WebPages;

/// <summary>Consistent responsive sizing for confirmation and short-input dialogs.</summary>
public static class TrajectoryDialogOptions
{
    public static MudBlazor.DialogOptions Compact => new()
    {
        CloseOnEscapeKey = true,
        MaxWidth = MudBlazor.MaxWidth.ExtraSmall,
        FullWidth = false
    };
}
