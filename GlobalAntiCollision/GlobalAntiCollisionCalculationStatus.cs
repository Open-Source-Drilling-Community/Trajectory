namespace OSDC.Drilling.GlobalAntiCollision;

public class GlobalAntiCollisionCalculationStatus
{
    public string ID { get; set; } = string.Empty;
    public GlobalAntiCollisionCalculationState CalculationState { get; set; } = GlobalAntiCollisionCalculationState.Completed;
    public double CalculationProgress { get; set; } = 1.0;
    public string? CalculationMessage { get; set; }
}
