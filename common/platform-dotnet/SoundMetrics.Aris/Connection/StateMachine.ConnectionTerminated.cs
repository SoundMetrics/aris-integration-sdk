using Serilog;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal static class ConnectionTerminated
        {
            private static void OnEnter(StateMachineContext context)
            {
                Log.Information("Connection terminated");

                context.CommandConnection?.Dispose();
                context.CommandConnection = null;
            }

            private static ConnectionState? DoProcessing(
                StateMachineContext context, IMachineEvent? ev)
            {
                return ConnectionState.WatchingForDevice;
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: OnEnter,
                    doProcessing: DoProcessing,
                    onLeave: default);
        }
    }
}
