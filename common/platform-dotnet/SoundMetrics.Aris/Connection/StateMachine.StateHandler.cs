using System;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal delegate void OnEnterFn(StateMachineData data);
        internal delegate void OnLeaveFn(StateMachineData data);

        internal delegate (ConnectionState? newState, StateMachineData data)
            DoProcessingFn(StateMachineData data, IMachineEvent ev);

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
