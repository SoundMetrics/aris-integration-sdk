using Serilog;
using SoundMetrics.Aris.Connection.Commands;
using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Core.Raw;
using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Network;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace SoundMetrics.Aris.Connection
{
    internal sealed partial class StateMachine : IDisposable
    {
        public StateMachine(uint serialNumber, SystemType systemType)
        {
            Log.Debug("ARIS {serialNumber} StateMachine.ctor", serialNumber);

            this.serialNumber = serialNumber;
            eventProcessor = new(systemType);

            var tickTimerPeriod = TimeSpan.FromSeconds(1);
            var nextDue = tickTimerPeriod;
            tickSource = new Timer(OnTimerTick, default, nextDue, tickTimerPeriod);

            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            eventProcessor.PostEvent(StateMachineEventType.StartStateMachine);
        }

        public int ApplySettings(AcousticSettingsRaw settings)
        {
            var newSettingsCookie = Interlocked.Increment(ref settingsCookie);
            eventProcessor.PostEvent(new ApplySettingsRequest((uint)newSettingsCookie, settings));
            return newSettingsCookie;
        }

        private void NetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            eventProcessor.PostEvent(StateMachineEventType.NetworkAvailabilityChanged);
        }

        private void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
        {
            eventProcessor.PostEvent(StateMachineEventType.NetworkAddressChanged);
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
            eventProcessor.PostEvent(new DeviceAddressChanged(oldTargetAddress, targetAddress));
        }

        public IObservable<Frame> Frames => frameSubject;

        private void OnNewFrame(Frame frame)
        {
            if (frameSubject.HasObservers)
            {
                frameSubject.OnNext(frame);
            }
        }

        private void OnTimerTick(object? _) => eventProcessor.PostEvent(StateMachineEventType.Tick);

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

                    StopFrameListener();

                    frameSubject.OnCompleted();
                    frameSubject.Dispose();

                    eventProcessor.Dispose();
                }

                // no unmanaged resources
                disposed = true;
            }
        }

        private void ShutDown()
        {
            using (var doneSignal = new ManualResetEventSlim(false))
            {
                eventProcessor.PostEvent(new Stop(doneSignal));
                if (!doneSignal.Wait(TimeSpan.FromSeconds(30)))
                {
                    throw new Exception("ShutDown timed out");
                }
            }

            StopFrameListener();
        }

        public ProtocolMetricsOG Stop()
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
                frameListener =
                    new FrameStreamListenerOG(
                        listenerAddress,
                        OnNewFrame,
                        FrameStreamReliabilityPolicy.DropPartialFrames);

                validPacketSub =
                    frameListener.ValidPacketReceived
                        // Sample every second for marking receipt; this
                        // implies that timing out the connection must happen
                        // at a period greater than the sample period.
                        .Sample(TimeSpan.FromSeconds(1))
                        .Subscribe(timestamp =>
                            eventProcessor.PostEvent(StateMachineEventType.MarkFrameDataReceived)
                    );

                eventProcessor.PostEvent(new NewReceiverEndPoint(frameListener.LocalEndPoint));
            }
        }

        private void StopFrameListener()
        {
            if (frameListener is not null)
            {
                validPacketSub?.Dispose();
                validPacketSub = null;

                frameListenerMetrics += frameListener.Metrics;

                frameListener.Dispose();
                frameListener = null;
            }
        }

        private readonly Timer tickSource;
        private readonly uint serialNumber;
        private readonly Subject<Frame> frameSubject = new Subject<Frame>();
        private readonly StateMachineEventProcessor eventProcessor;

        private bool disposed;
        private IDisposable? validPacketSub;
        private IPAddress? targetAddress;
        private FrameStreamListenerOG? frameListener;
        private ProtocolMetricsOG frameListenerMetrics = ProtocolMetricsOG.Empty;
        private int settingsCookie = 2;
    }
}
