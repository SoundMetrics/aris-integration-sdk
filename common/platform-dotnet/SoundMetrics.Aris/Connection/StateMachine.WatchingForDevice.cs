using System;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal sealed class WatchingForDevice
        {
            public static (ConnectionState?, MachineData data)
                DoProcessing(MachineData data, IMachineEvent ev)
            {
                if (ev is Tick _)
                {
                    return (default, data);
                }
                else
                {
                    return (default, data);
                }
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: default,
                    doProcessing: DoProcessing,
                    onLeave: default);
        }
    }
}
