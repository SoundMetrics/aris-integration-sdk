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
                => ConnectionTerminated

            Connected
                => ConnectionTerminated

            ConnectionTerminated
                => WatchingForDevice

            * => End (disposed, no restart)
    */
    internal enum ConnectionState
    {
        Start,
        WatchingForDevice,
        AttemptingConnection,
        Connected,
        ConnectionTerminated,
        End,
    }
}
