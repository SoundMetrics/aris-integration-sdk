namespace SoundMetrics.Aris.Connection.StateLogging;

internal record struct LogGrouping(
    int LogNumber,
    int SubPartNumber)
{
    public override string ToString() => $"{LogNumber}.{SubPartNumber}";
}
