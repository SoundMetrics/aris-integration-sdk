using Serilog;
using SoundMetrics.Aris.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
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

        public StateMachine(string serialNumber)
        {
            Log.Debug("ARIS {serialNumber} StateMachine.ctor", serialNumber);

            this.serialNumber = serialNumber;

            events = new BufferedMessageQueue<IMachineEvent>(ProcessEvent);

            var tickTimerPeriod = TimeSpan.FromSeconds(1);
            var nextDue = tickTimerPeriod;
            tickSource = new Timer(OnTimerTick, default, nextDue, tickTimerPeriod);

            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            Transition(ConnectionState.WatchingForDevice, null);
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            events.Post(new NetworkAvailabilityChanged(e));
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            events.Post(new NetworkAddressChanged());
        }

        public void SetTargetAddress(IPAddress targetAddress)
        {
            if (!Object.Equals(this.targetAddress, targetAddress))
            {
                Log.Debug("ARIS {serialNumber} noting address changed from {addr1} to {addr2}",
                    serialNumber, this.targetAddress, targetAddress);
            }

            this.targetAddress = targetAddress;
            events.Post(new DeviceAddressChanged(targetAddress));
        }

        private void OnTimerTick(object _) =>
            events.Post(new Tick(DateTimeOffset.Now, targetAddress));

        private void ProcessEvent(IMachineEvent ev)
        {
            switch (ev)
            {
                case DeviceAddressChanged _:
                case Cycle _:
                case NetworkAddressChanged _:
                case NetworkAvailabilityChanged _:
                    InvokeDoProcessing(ev);
                    break;

                case Tick _:
                    InvokeDoProcessing(ev);
                    break;

                case Stop stop:
                    Transition(ConnectionState.End, ev);
                    stop.MarkComplete();
                    Debug.Assert(state.ConnectionState == ConnectionState.End);
                    break;

                case null:
                    InvokeDoProcessing(ev);
                    break;

                default:
                    throw new ArgumentException(
                        $"Unexpected event type: {ev.GetType().Name}");
            }

            (ev as IDisposable)?.Dispose();
        }

        private bool Transition(ConnectionState newState, IMachineEvent ev)
        {
            var oldState = state.ConnectionState;
            if (oldState == newState)
            {
                Log.Debug("Ignoring transition to the same state ({newState})", newState);
                return false;
            }

            Log.Debug("State transition from {oldState} to {newState}", oldState, newState);

            stateHandlers[oldState].OnLeave?.Invoke(state.Data);
            state = new State(newState, state.Data);
            stateHandlers[newState].OnEnter?.Invoke(state.Data);
            stateHandlers[newState].DoProcessing?.Invoke(state.Data, ev);

            return true;
        }

        private void InvokeDoProcessing(IMachineEvent ev)
        {
            if (stateHandlers[state.ConnectionState].DoProcessing is DoProcessingFn fn)
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
                    ConnectionState.WatchingForDevice,
                    WatchingForDevice.StateHandler
                },
                {
                    ConnectionState.End,
                    End.StateHandler
                }
            };
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
                    NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;

                    tickSource.Dispose();

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
        private readonly string serialNumber;

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
