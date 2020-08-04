namespace SoundMetrics.Aris.Connection
{
    internal enum ConnectionState
    {
        Start,
        AttemptingConnection,
        Connected,
        ConnectionFailed,
        WaitingToRetry,
        End,
    }
}
