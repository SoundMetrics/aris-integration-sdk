using Serilog;
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

        public int ApplySettings(ISettings settings)
        {
            var newSettingsCookie = Interlocked.Increment(ref settingsCookie);
            events.Post(new ApplySettingsRequest(newSettingsCookie, settings));
            return newSettingsCookie;
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
            var oldTargetAddress = this.targetAddress;

            if (Object.Equals(oldTargetAddress, targetAddress))
            {
                return;
            }

            Log.Debug("ARIS {serialNumber} noting address changed from {addr1} to {addr2}",
                serialNumber, this.targetAddress, targetAddress);

            this.targetAddress = targetAddress;

            SetFrameListener(oldTargetAddress, targetAddress);
            events.Post(new DeviceAddressChanged(oldTargetAddress, targetAddress));
        }

        public IObservable<Frame> Frames => frameSubject;

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
                        case MarkFrameDataReceived _:
                        case Tick _:
                        case null:
                            InvokeDoProcessing(ev);
                            break;

                        case ApplySettingsRequest request:
                            InvokeDoProcessing(ev);
                            context.LatestSettingsRequest = request;
                            break;

                        case Stop stop:
                            Transition(ConnectionState.End, context, ev);
                            stop.MarkComplete();
                            Debug.Assert(state == ConnectionState.End);
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

            ConnectionState? nextState = newState;

            while (nextState is ConnectionState next && oldState != next)
            {
                Log.Debug("State transition from {oldState} to {newState}",
                    oldState, next);

                oldState = state;

                stateHandlers[oldState].OnLeave?.Invoke(context);
                state = next;
                stateHandlers[next].OnEnter?.Invoke(context);

                nextState = stateHandlers[next].DoProcessing?.Invoke(context, ev);
            }

            return true;
        }

        private void InvokeDoProcessing(IMachineEvent? ev)
        {
            try
            {
                if (stateHandlers[state].DoProcessing is DoProcessingFn doProcessing)
                {
                    try
                    {
                        var requestedState = doProcessing(context, ev);
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
                    ConnectionState.Connected,
                    Connected.StateHandler
                },
                {
                    ConnectionState.End,
                    End.StateHandler
                },
                {
                    ConnectionState.ConnectionTerminated,
                    ConnectionTerminated.StateHandler
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

                    validPacketSub?.Dispose();
                    tickSource.Dispose();

                    ShutDown();
                    events.Dispose();

                    frameSubject.OnCompleted();
                    frameSubject.Dispose();
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
                        events.Post(new MarkFrameDataReceived(timestamp))
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

        private readonly HandlerMap stateHandlers;
        private readonly AttemptingConnection attemptingConnectionHandler =
            new AttemptingConnection();
        private readonly BufferedMessageQueue<IMachineEvent> events;
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
