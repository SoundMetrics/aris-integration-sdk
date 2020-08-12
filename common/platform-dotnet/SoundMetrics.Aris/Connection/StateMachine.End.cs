using System;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal static class End
        {
            private static void OnEnter(StateMachineContext context)
            {
                context.CommandConnection?.Dispose();
                context.CommandConnection = null;
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: OnEnter,
                    doProcessing: default,
                    onLeave: default);
        }
    }
}
