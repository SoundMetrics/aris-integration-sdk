using Serilog;
using System.Diagnostics;
using System.Net;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal sealed class WatchingForDevice
        {
            public static (ConnectionState?, StateMachineData data)
                DoProcessing(StateMachineData data, IMachineEvent ev)
            {
                if (ev is Tick tick && tick.DeviceAddress is IPAddress deviceAddress)
                {
                    Debug.Assert(data is null);
                    Log.Debug("{state} notes device address {deviceAddress}",
                        nameof(WatchingForDevice), deviceAddress);

                    var machineData = new StateMachineData(deviceAddress);
                    return (ConnectionState.AttemptingConnection, machineData);
                }

                return (default, data);
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: default,
                    doProcessing: DoProcessing,
                    onLeave: default);
        }
    }
}
