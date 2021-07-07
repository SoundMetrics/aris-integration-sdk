﻿using System;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine
    {
        internal delegate void OnEnterFn(StateMachineContext data);
        internal delegate void OnLeaveFn(StateMachineContext data);

        internal delegate ConnectionState? DoProcessingFn(StateMachineContext data, IMachineEvent? ev);

        internal sealed class StateHandler
        {
            public StateHandler(
                OnEnterFn? onEnter,
                DoProcessingFn? doProcessing,
                OnLeaveFn? onLeave)
            {
                OnEnter = onEnter;
                DoProcessing = doProcessing;
                OnLeave = onLeave;
            }

            public OnEnterFn? OnEnter { get; }
            public DoProcessingFn? DoProcessing { get; }
            public OnLeaveFn? OnLeave { get; }
        }
    }
}
