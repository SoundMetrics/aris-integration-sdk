using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal static class AttemptingConnection
        {
            public static void OnEnter(MachineData data)
            {
                Log.Information(
                    "Attempting connection to {deviceAddress}",
                    data.DeviceAddress);
            }

            public static (ConnectionState?, MachineData data)
                DoProcessing(MachineData data, IMachineEvent _)
            {
                throw new NotImplementedException();
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: OnEnter,
                    doProcessing: DoProcessing,
                    onLeave: default);
        }
    }
}
