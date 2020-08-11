﻿using Serilog;
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
        public StateMachine(string serialNumber)
        {
            Log.Debug("ARIS {serialNumber} StateMachine.ctor", serialNumber);

            this.serialNumber = serialNumber;
            stateHandlers = InitializeHandlerMap();

            events = new BufferedMessageQueue<IMachineEvent>(ProcessEvent);

            var tickTimerPeriod = TimeSpan.FromSeconds(1);
            var nextDue = tickTimerPeriod;
            tickSource = new Timer(OnTimerTick, default, nextDue, tickTimerPeriod);

            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            Transition(ConnectionState.WatchingForDevice, context: context, ev: null);
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            events.Post(new NetworkAvailabilityChanged(e));
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            events.Post(new NetworkAddressChanged());
        }

        public void SetTargetAddress(IPAddress? targetAddress)
        {
            if (!Object.Equals(this.targetAddress, targetAddress))
            {
                Log.Debug("ARIS {serialNumber} noting address changed from {addr1} to {addr2}",
                    serialNumber, this.targetAddress, targetAddress);
            }

            this.targetAddress = targetAddress;
            events.Post(new DeviceAddressChanged(targetAddress));
        }

        private void OnTimerTick(object? _) =>
            events.Post(new Tick(DateTimeOffset.Now, targetAddress));

        private void ProcessEvent(IMachineEvent ev)
        {
            try
            {
                try
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
                            Transition(ConnectionState.End, context, ev);
                            stop.MarkComplete();
                            Debug.Assert(state == ConnectionState.End);
                            break;

                        case null:
                            InvokeDoProcessing(ev);
                            break;

                        default:
                            throw new ArgumentException(
                                $"Unexpected event type: {ev.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    var evType = ev?.GetType().Name ?? "(null)";
                    Log.Error(
                        "An error occurred while processing an event of type {eventType} "
                        + "during state {state}: {message}\n"
                        + "{stackTrace}",
                        evType, state, ex.Message, ex.StackTrace);
                    throw;
                }
            }
            finally
            {
                (ev as IDisposable)?.Dispose();
            }
        }

        private bool Transition(
            ConnectionState newState,
            StateMachineContext context,
            IMachineEvent? ev)
        {
            var oldState = state;
            if (oldState == newState)
            {
                Log.Debug("Ignoring transition to the same state ({newState})", newState);
                return false;
            }

            Log.Debug("State transition from {oldState} to {newState}", oldState, newState);

            stateHandlers[oldState].OnLeave?.Invoke(context);
            state = newState;
            stateHandlers[newState].OnEnter?.Invoke(context);
            stateHandlers[newState].DoProcessing?.Invoke(context, ev);

            return true;
        }

        private void InvokeDoProcessing(IMachineEvent? ev)
        {
            try
            {
                if (stateHandlers[state].DoProcessing is DoProcessingFn doProcessing)
                {
                    var requestedState = doProcessing(context, ev);
                    if (requestedState is ConnectionState newState)
                    {
                        Transition(newState, context, ev);
                    }
                }
                else
                {
                    // Nothing to do.
                }
            }
            catch (KeyNotFoundException ex)
            {
                var msg = $"Handler for state {state} is not implemented";
                Log.Error(msg);
                throw new NotImplementedException(msg, ex);
            }
        }

        private HandlerMap InitializeHandlerMap()
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
                    ConnectionState.AttemptingConnection,
                    attemptingConnectionHandler.StateHandler
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

        private readonly HandlerMap stateHandlers;
        private readonly AttemptingConnection attemptingConnectionHandler =
            new AttemptingConnection();
        private readonly BufferedMessageQueue<IMachineEvent> events;
        private readonly Timer tickSource;
        private readonly string serialNumber;
        private readonly StateMachineContext context =
            new StateMachineContext() { ReceiverPort = 56999 }; // TODO remove hard-coded receiver port

        private bool disposed;
        private IPAddress? targetAddress;


        private ConnectionState state = ConnectionState.Start;
    }
}
