namespace SoundMetrics.Aris.Connection;

internal record struct StateProcessingResult(
    StateEventStatus Status,
    ConnectionState? StateOverride)
{
    public override string ToString()
    { 
        string stateOverride = $"{StateOverride}";
        return $"status=[{Status}]; state override=[{stateOverride}]";}
}
