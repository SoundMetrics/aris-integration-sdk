namespace SoundMetrics.Aris.Data;

public record ProtocolMetricsOG(
    ulong UuniqueFrameIndexCount,
    ulong ProcessedFrameCount,
    /// Fully complete frames processed.
    ulong CompleteFrameCount,
    ulong SkippedFrameCount,
    ulong TotalExpectedFrameSize,
    ulong TotalReceivedFrameSize,
    ulong TotalPacketsReceived,
    ulong TotalPacketsAccepted,
    ulong UnparsablePackets)
{
    public static ProtocolMetricsOG Empty = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    public static ProtocolMetricsOG operator +(ProtocolMetricsOG a, ProtocolMetricsOG b)
    {
        return new(
            UuniqueFrameIndexCount: a.UuniqueFrameIndexCount + b.UuniqueFrameIndexCount,
            ProcessedFrameCount: a.ProcessedFrameCount + b.ProcessedFrameCount,
            CompleteFrameCount: a.CompleteFrameCount + b.CompleteFrameCount,
            SkippedFrameCount: a.SkippedFrameCount + b.SkippedFrameCount,
            TotalExpectedFrameSize: a.TotalExpectedFrameSize + b.TotalExpectedFrameSize,
            TotalReceivedFrameSize: a.TotalReceivedFrameSize + b.TotalReceivedFrameSize,
            TotalPacketsReceived: a.TotalPacketsReceived + b.TotalPacketsReceived,
            TotalPacketsAccepted: a.TotalPacketsAccepted + b.TotalPacketsAccepted,
            UnparsablePackets: a.UnparsablePackets + b.UnparsablePackets
        );
    }
}
