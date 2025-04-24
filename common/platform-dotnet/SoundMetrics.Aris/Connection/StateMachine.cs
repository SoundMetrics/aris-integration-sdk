using Serilog;
using SoundMetrics.Aris.Core;
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

            events = new BufferedMessageQueue<MachineEvent>(DispatchEvent);

            var tickTimerPeriod = TimeSpan.FromSeconds(1);
            var nextDue = tickTimerPeriod;
            tickSource = new Timer(OnTimerTick, default, nextDue, tickTimerPeriod);

            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            Transition(
                ConnectionState.WatchingForDevice,
                context: context,
                ev: MakeEvent(MachineEventType.Tick));
        }

        public int ApplySettings(ISettings settings)
        {
            var newSettingsCookie = Interlocked.Increment(ref settingsCookie);
            events.Post(
                MakeEvent(new ApplySettingsRequest(newSettingsCookie, settings)));
            return newSettingsCookie;
        }

        private void NetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            events.Post(
                MakeEvent(MachineEventType.NetworkAvailabilityChanged));
        }

        private void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
        {
            events.Post(
                MakeEvent(MachineEventType.NetworkAddressChanged));
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
            events.Post(
                MakeEvent(new DeviceAddressChanged(oldTargetAddress, targetAddress)));
        }

        public IObservable<Frame> Frames => frameSubject;

        private void OnTimerTick(object? _) =>
            events.Post(MakeEvent(MachineEventType.Tick));

        private void DispatchEvent(MachineEvent ev)
        {
            try
            {
                try
                {
                    switch (ev.EventType, ev.CompoundEvent)
                    {
                        case (MachineEventType.Compound, ApplySettingsRequest request):
                            InvokeStateProcessing(ev);
                            context.LatestSettingsRequest = request;
                            break;

                        case (MachineEventType.Compound, Stop stop):
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

                        case (MachineEventType.Compound, ICompoundMachineEvent evt):
                            InvokeStateProcessing(ev);
                            break;

                        case (MachineEventType.Tick, _):
                        case (MachineEventType.NetworkAddressChanged, _):
                        case (MachineEventType.NetworkAvailabilityChanged, _):
                        case (MachineEventType.MarkFrameDataReceived, _):
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
            in MachineEvent ev)
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
                stateHandlers[next].OnEnter(context);

                nextState = stateHandlers[next].DoProcessing(context, ev);
            }

            return true;
        }

        private void InvokeStateProcessing(MachineEvent ev)
        {
            try
            {
                var requestedState = stateHandlers[state].DoProcessing(context, ev);
                if (requestedState is ConnectionState newState)
                {
                    Transition(newState, context, ev);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Exception during state transition: [{ex.Message}]");
                Log.Warning("Terminating connection");
                Transition(ConnectionState.ConnectionTerminated, context, ev);
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
                    { ConnectionState.End, new StateHandlerEnd() },
                    { ConnectionState.ConnectionTerminated, new StateHandlerConnectionTerminated() }
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
                events.Post(MakeEvent(new Stop(doneSignal)));
                if (!doneSignal.Wait(TimeSpan.FromSeconds(30)))
                {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    throw new Exception("ShutDown timed out");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
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
                frameListener = new FrameListener(IPAddress.Any, frameSubject);
                validPacketSub =
                    frameListener.ValidPacketReceived
                        // Sample every second for marking receipt; this
                        // implies that timing out the connection must happen
                        // at a period greater than the sample period.
                        .Sample(TimeSpan.FromSeconds(1))
                        .Subscribe(timestamp =>
                            events.Post(MakeEvent(MachineEventType.MarkFrameDataReceived))
                    );
                context.ReceiverPort = frameListener.LocalEndPoint.Port;
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

        private MachineEvent MakeEvent(MachineEventType eventType) =>
            eventType switch
            {
                MachineEventType.Compound =>
                    throw new ArgumentException($"Call not valid for event type {eventType}"),

                _ => new MachineEvent(
                        eventType,
                        DateTimeOffset.Now,
                        targetAddress),
            };

        private MachineEvent MakeEvent(ICompoundMachineEvent compoundEvent) =>
            new MachineEvent(
                MachineEventType.Compound,
                DateTimeOffset.Now,
                targetAddress,
                compoundEvent);

        private readonly HandlerMap stateHandlers;
        private readonly BufferedMessageQueue<MachineEvent> events;
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
