using System;
using System.Collections.Generic;
using System.Text;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal static class EndHandler
        {
            public static (ConnectionState?, MachineData data)
                OnDoProcessing(MachineData data, IMachineEvent _)
            {
                throw new NotImplementedException();
                data?.Dispose();
                return (default, default);
            }

            public static StateHandler StateHandler =>
                new StateHandler(
                    onEnter: default,
                    doProcessing: OnDoProcessing,
                    onLeave: default);
        }
    }
}
