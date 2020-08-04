namespace SoundMetrics.Aris.Connection
{
    /*
        This represents states within the connection state machine.

        Expected state transitions are:

            Start
                => WatchingForDevice

            WatchingForDevice
                => AttemptingConnection [found device]

            AttemptingConnection
                => Connected
                => ConnectionAttemptFailed

            Connected
                => ConnectionTerminated

            ConnectionAttemptFailed
                => PrepForRetry

            ConnectionTerminated
                => PrepForRetry

            PrepForRetry
                => WatchingForDevice

            * => End (disposed, no restart)
    */
    internal enum ConnectionState
    {
        Start,
        WatchingForDevice,
        AttemptingConnection,
        Connected,
        ConnectionAttemptFailed,
        ConnectionTerminated,
        PrepForRetry,
        End,
    }
}
