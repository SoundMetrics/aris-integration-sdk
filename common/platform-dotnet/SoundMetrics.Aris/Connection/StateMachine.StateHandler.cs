using System;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal delegate void OnEnterFn(MachineData data);
        internal delegate void OnLeaveFn(MachineData data);

        internal delegate (ConnectionState? newState, MachineData data)
            DoProcessingFn(MachineData data, IMachineEvent ev);

        internal sealed class StateHandler
        {
            public StateHandler(
                OnEnterFn onEnter,
                DoProcessingFn doProcessing,
                OnLeaveFn onLeave)
            {
                OnEnter = onEnter;
                DoProcessing = doProcessing;
                OnLeave = onLeave;
            }

            public OnEnterFn OnEnter { get; private set; }
            public DoProcessingFn DoProcessing { get; private set; }
            public OnLeaveFn OnLeave { get; private set; }
        }
    }
}
