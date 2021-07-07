using Serilog;
using System.Net;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal sealed class WatchingForDevice
        {
            private static void OnEnter(StateMachineContext _)
            {
                Log.Information("Watching for device");
            }

            private static ConnectionState? DoProcessing(
                StateMachineContext context, IMachineEvent? ev)
            {
                if (ev is Tick tick && tick.DeviceAddress is IPAddress deviceAddress)
                {
                    Log.Debug("{state} notes device address {deviceAddress}",
                        nameof(WatchingForDevice), deviceAddress);

                    context.DeviceAddress = deviceAddress;
                    return ConnectionState.AttemptingConnection;
                }

                return default;
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: OnEnter,
                    doProcessing: DoProcessing,
                    onLeave: default);
        }
    }
}
