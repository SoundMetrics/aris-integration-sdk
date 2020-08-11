using System;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal static class End
        {
            public static ConnectionState? DoProcessing(StateMachineContext context, IMachineEvent? _)
            {
                context.CommandConnection?.Dispose();
                context.CommandConnection = null;
                return ConnectionState.End;
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: default,
                    doProcessing: DoProcessing,
                    onLeave: default);
        }
    }
}
