﻿using Serilog;
using SoundMetrics.Aris.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace SoundMetrics.Aris.Connection
{
    using HandlerMap = Dictionary<ConnectionState, StateMachine.StateHandler>;

    internal sealed partial class StateMachine : IDisposable
    {
        static StateMachine()
        {
            stateHandlers = InitializeHandlerMap();
        }

        public StateMachine()
        {
            events = new BufferedMessageQueue<IMachineEvent>(ProcessEvent);

            var tickTimerPeriod = TimeSpan.FromSeconds(1);
            var nextDue = tickTimerPeriod;
            tickSource = new Timer(OnTimerTick, default, nextDue, tickTimerPeriod);
        }

        public void SetTargetAddress(IPAddress targetAddress)
        {
            this.targetAddress = targetAddress;
            events.Post(new AddressChanged(targetAddress));
        }

        private void OnTimerTick(object _) =>
            events.Post(new Tick(DateTimeOffset.Now));

        private void ProcessEvent(IMachineEvent ev)
        {
            switch (ev)
            {
                case AddressChanged _:
                case Tick _:
                case Cycle _:
                    InvokeDoProcessing(ev);
                    break;

                case Stop stop:
                    Transition(state.ConnectionState, ConnectionState.End);
                    InvokeDoProcessing(ev);

                    stop.MarkComplete();
                    Debug.Assert(state.ConnectionState == ConnectionState.End);
                    break;

                default:
                    throw new ArgumentException(
                        $"Unexpected event type: {ev.GetType().Name}");
            }

            (ev as IDisposable)?.Dispose();
        }

        private bool Transition(ConnectionState oldState, ConnectionState newState)
        {
            if (oldState == newState)
            {
                Log.Debug("Ignoring transition to the same state ({newState})", newState);
                return false;
            }

            stateHandlers[oldState].OnLeave?.Invoke(state.Data);
            stateHandlers[newState].OnEnter?.Invoke(state.Data);

            return true;
        }

        private void InvokeDoProcessing(IMachineEvent ev)
        {
            if (stateHandlers[state.ConnectionState].DoProcessing is OnDoProcessingFn fn)
            {
                var (requestedState, newData) = fn(state.Data, ev);
                if (requestedState is ConnectionState newState)
                {
                    state = new State(newState, newData);
                    AllowMoreWork();
                }
            }
            else
            {
                // Nothing to do.
            }
        }

        private void AllowMoreWork()
        {
            events.Post(Cycle.Singleton);
        }

        private static HandlerMap InitializeHandlerMap()
        {
            return new HandlerMap
            {
                {
                    ConnectionState.Start,
                    new StateHandler(default, default, default)
                },
                {
                    ConnectionState.End,
                    EndHandler.StateHandler
                }
            };
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ShutDown();
                    events.Dispose();
                }

                // no unmanaged resources
                disposed = true;
            }
        }

        private void ShutDown()
        {
            using (var doneSignal = new ManualResetEventSlim(false))
            {
                events.Post(new Stop(doneSignal));
                if (!doneSignal.Wait(TimeSpan.FromSeconds(30)))
                {
                    throw new Exception("ShutDown timed out");
                }
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static readonly HandlerMap stateHandlers;
        private readonly BufferedMessageQueue<IMachineEvent> events;
        private readonly Timer tickSource;

        private bool disposed;
        private IPAddress targetAddress;

        internal struct State
        {
            public State(
                ConnectionState connectionState,
                MachineData data)
            {
                ConnectionState = connectionState;
                Data = data;
            }

            public ConnectionState ConnectionState { get; private set; }
            public MachineData Data { get; private set; }
        }

        private State state = new State(ConnectionState.Start, default);
    }
}
