using Serilog;
using SoundMetrics.Aris.Connection.Commands;
using SoundMetrics.Aris.Connection.StateHandlers;
using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Core.Raw;
using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace SoundMetrics.Aris.Connection
{
    using HandlerMap = Dictionary<ConnectionState, IStateHandler>;

    internal sealed partial class StateMachine : IDisposable
    {
        public StateMachine(string serialNumber, SystemType systemType)
        {
            Log.Debug("ARIS {serialNumber} StateMachine.ctor", serialNumber);

            this.serialNumber = serialNumber;
            context.SystemType = systemType;

            stateHandlers = MakeHandlerMap();

            events = new BufferedMessageQueue<StateMachineEvent>(DispatchEvent);

            var tickTimerPeriod = TimeSpan.FromSeconds(1);
            var nextDue = tickTimerPeriod;
            tickSource = new Timer(OnTimerTick, default, nextDue, tickTimerPeriod);

            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            Transition(
                ConnectionState.WatchingForDevice,
                context: context,
                ev: new StateMachineEvent(
                        StateMachineEventType.Tick,
                        DateTimeOffset.Now,
                        targetAddress));
        }

        public int ApplySettings(AcousticSettingsRaw settings)
        {
            var newSettingsCookie = Interlocked.Increment(ref settingsCookie);
            PostEvent(new ApplySettingsRequest((uint)newSettingsCookie, settings));
            return newSettingsCookie;
        }

        private void NetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            PostEvent(StateMachineEventType.NetworkAvailabilityChanged);
        }

        private void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
        {
            PostEvent(StateMachineEventType.NetworkAddressChanged);
        }

        public void SetTargetAddress(IPAddress? targetAddress)
        {
            var oldTargetAddress = this.targetAddress;

            if (Object.Equals(oldTargetAddress, targetAddress))
            {
                return;
            }

            Log.Debug("ARIS {serialNumber} noting address changed from {addr1} to {addr2}",
                serialNumber, this.targetAddress, targetAddress);

            this.targetAddress = targetAddress;

            SetFrameListener(oldTargetAddress, targetAddress);
            PostEvent(new DeviceAddressChanged(oldTargetAddress, targetAddress));
        }

        public IObservable<Frame> Frames => frameSubject;

        private void OnTimerTick(object? _) => PostEvent(StateMachineEventType.Tick);

        private void DispatchEvent(StateMachineEvent ev)
        {
            try
            {
                try
                {
                    // ### TODO This switch could stand to disappear, as there's little to
                    // ### differentiate how the event types are handled.
                    switch (ev.EventType, ev.CompoundEvent)
                    {
                        case (StateMachineEventType.Compound, ApplySettingsRequest request):
                            InvokeStateProcessing(ev);
                            context.LatestSettingsRequest = request;
                            break;

                        case (StateMachineEventType.Compound, Stop stop):
                            try
                            {
                                Transition(ConnectionState.End, context, ev);
                                Debug.Assert(state == ConnectionState.End);
                            }
                            finally
                            {
                                stop.MarkComplete();
                            }
                            break;

                        case (StateMachineEventType.Compound, ICompoundMachineEvent evt):
                            InvokeStateProcessing(ev);
                            break;

                        case (StateMachineEventType.Tick, _):
                        case (StateMachineEventType.NetworkAddressChanged, _):
                        case (StateMachineEventType.NetworkAvailabilityChanged, _):
                        case (StateMachineEventType.MarkFrameDataReceived, _):
                            InvokeStateProcessing(ev);
                            break;

                        default:
                            throw new ArgumentException(
                                $"Unexpected event type: {ev.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        "An error occurred while processing an event of type {eventType} "
                        + "during state {state}: {message}\n"
                        + "{stackTrace}",
                        ev.EventName, state, ex.Message, ex.StackTrace);
                    throw;
                }
            }
            finally
            {
                (ev.CompoundEvent as IDisposable)?.Dispose();
            }
        }

        private bool Transition(
            ConnectionState newState,
            StateMachineContext context,
            in StateMachineEvent ev)
        {
            var oldState = state;
            if (oldState == newState)
            {
                Log.Debug("Ignoring transition to the same state ({newState})", newState);
                return false;
            }

            ConnectionState? nextState = newState;

            while (nextState is ConnectionState next && oldState != next)
            {
                Log.Debug("State transition from {oldState} to {newState}",
                    oldState, next);

                oldState = state;

                stateHandlers[oldState].OnLeave(context);
                state = next;
                Log.Debug("Entering state [{newState}]", next);
                stateHandlers[next].OnEnter(context);

                nextState = stateHandlers[next].DoProcessing(state, context, ev);
            }

            return true;
        }

        private void InvokeStateProcessing(StateMachineEvent ev)
        {
            try
            {
                var requestedState = stateHandlers[state].DoProcessing(state, context, ev);
                if (requestedState is ConnectionState newState)
                {
                    Transition(newState, context, ev);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Exception during state transition: [{ex.Message}]");
                Log.Warning("Terminating connection");
                Transition(ConnectionState.ConnectionLost, context, ev);
            }
        }

        private static HandlerMap MakeHandlerMap()
        {
            return new HandlerMap
                {
                    { ConnectionState.Start, new StateHandlerNull() },
                    { ConnectionState.WatchingForDevice, new StateHandlerWatchingForDevice() },
                    { ConnectionState.AttemptingConnection, new StateHandlerAttemptingConnection() },
                    { ConnectionState.Connected, new StateHandlerConnected() },
                    { ConnectionState.DeviceAddressChanged, new FastTransitionTo(ConnectionState.WatchingForDevice) },
                    { ConnectionState.End, new StateHandlerEnd() },
                    { ConnectionState.ConnectionLost, new StateHandlerConnectionLost() }
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

                    validPacketSub?.Dispose();
                    tickSource.Dispose();

                    ShutDown();
                    events.Dispose();

                    frameSubject.OnCompleted();
                    frameSubject.Dispose();

                    context.Dispose();
                }

                // no unmanaged resources
                disposed = true;
            }
        }

        private void ShutDown()
        {
            using (var doneSignal = new ManualResetEventSlim(false))
            {
                PostEvent(new Stop(doneSignal));
                if (!doneSignal.Wait(TimeSpan.FromSeconds(30)))
                {
                    throw new Exception("ShutDown timed out");
                }
            }

            StopFrameListener();
        }

        public FrameListenerMetrics Stop()
        {
            ShutDown();
            return frameListenerMetrics;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SetFrameListener(
            IPAddress? oldTargetAddress,
            IPAddress? newTargetAddress)
        {
            if (object.Equals(oldTargetAddress, newTargetAddress))
            {
                return;
            }

            StopFrameListener();

            if (!(newTargetAddress is null))
            {
                var listenerAddress =
                    NetworkSupport.FindLocalIPAddress(newTargetAddress, IPAddress.Any);
                frameListener = new FrameListener(listenerAddress, frameSubject);
                validPacketSub =
                    frameListener.ValidPacketReceived
                        // Sample every second for marking receipt; this
                        // implies that timing out the connection must happen
                        // at a period greater than the sample period.
                        .Sample(TimeSpan.FromSeconds(1))
                        .Subscribe(timestamp =>
                            PostEvent(StateMachineEventType.MarkFrameDataReceived)
                    );
                context.ReceiverEndPoint = frameListener.LocalEndPoint;
            }
        }

        private void StopFrameListener()
        {
            if (frameListener is FrameListener)
            {
                validPacketSub?.Dispose();
                validPacketSub = null;

                frameListenerMetrics += frameListener.Metrics;

                frameListener.Dispose();
                frameListener = null;
            }
        }

        private void PostEvent(StateMachineEventType eventType)
        {
            var stateMacineEvent =
                eventType switch
                {
                    StateMachineEventType.Compound =>
                        throw new ArgumentException($"Call not valid for event type {eventType}"),

                    _ => new StateMachineEvent(
                            eventType,
                            DateTimeOffset.Now,
                            targetAddress),
                };
            events.Post(stateMacineEvent);
        }

        private void PostEvent(ICompoundMachineEvent compoundEvent)
        {
            var stateMachineEvent =
                new StateMachineEvent(
                    StateMachineEventType.Compound,
                    DateTimeOffset.Now,
                    targetAddress,
                    compoundEvent);
            events.Post(stateMachineEvent);
        }

        private readonly HandlerMap stateHandlers;
        private readonly BufferedMessageQueue<StateMachineEvent> events;
        private readonly Timer tickSource;
        private readonly string serialNumber;
        private readonly Subject<Frame> frameSubject = new Subject<Frame>();
        private readonly StateMachineContext context = new StateMachineContext();

        private bool disposed;
        private IDisposable? validPacketSub;
        private IPAddress? targetAddress;
        private FrameListener? frameListener;
        private FrameListenerMetrics frameListenerMetrics = default;
        private int settingsCookie;

        private ConnectionState state = ConnectionState.Start;
    }
}
