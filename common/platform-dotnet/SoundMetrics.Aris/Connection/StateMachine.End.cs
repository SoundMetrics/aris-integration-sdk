using System;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal static class End
        {
            public static (ConnectionState?, StateMachineData data)
                DoProcessing(StateMachineData data, IMachineEvent _)
            {
                data?.Dispose();
                return (ConnectionState.End, null);
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: default,
                    doProcessing: DoProcessing,
                    onLeave: default);
        }
    }
}
