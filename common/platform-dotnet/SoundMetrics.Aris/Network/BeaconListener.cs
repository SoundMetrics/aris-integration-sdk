using SoundMetrics.Aris.Availability;
using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace SoundMetrics.Aris.Network
{
    public sealed class BeaconListener : IDisposable
    {
        public BeaconListener()
            : this(ValidateSyncContext(SynchronizationContext.Current))
        {
        }

        private static SynchronizationContext ValidateSyncContext(SynchronizationContext? syncContext)
            => syncContext ?? throw new ArgumentNullException(nameof(syncContext));

        public BeaconListener(SynchronizationContext syncContext)
        {
            if (syncContext is null)
            {
                throw new ArgumentNullException(nameof(syncContext));
            }

            bufferedQueue = new BufferedMessageQueue<UdpReceived>(ParseUdpPacket);

            udpListeners = new UdpListener[]
            {
                new UdpListener(
                    IPAddress.Any,
                    NetworkConstants.ArisAvailabilityListenerPortV2,
                    reuseAddress: true),
                // Not currently supported.
                //new UdpListener(
                //    IPAddress.Any,
                //    NetworkConstants.ArisDefenderBeaconPort,
                //    reuseAddress: true),
            };

            beaconSubscriptions =
                udpListeners
                    .Select(listener =>
                        listener.Packets
                            .ObserveOn(syncContext)
                            .Subscribe(ReceiveUdpPacket))
                    .ToArray();
        }

        private void ReceiveUdpPacket(UdpReceived udpReceived)
        {
            bufferedQueue.Post(udpReceived);
        }

        private void ParseUdpPacket(UdpReceived udpReceived)
        {
            switch (udpReceived.LocalPort)
            {
                case NetworkConstants.ArisAvailabilityListenerPortV2:
                    // Explorer, Defender, Voyager. Ignore Defender, we
                    // use the additional Defender beacon to identify them.
                    try
                    {
                        var beacon =
                            global::Aris.Availability.Parser.ParseFrom(udpReceived.Received.Buffer);

                        if (SystemType.TryGetFromIntegralValue(
                            (int)beacon.SystemType,
                            out var systemType))
                        {
                            if (!beacon.IsDiverHeld)
                            {
                                var variants =
                                    (IEnumerable<string>?)beacon.SystemVariants?.Enabled ?? Array.Empty<string>();
                                var onboardVersion =
                                    new OnboardSoftwareVersion(
                                        beacon.SoftwareVersion.Major,
                                        beacon.SoftwareVersion.Minor,
                                        beacon.SoftwareVersion.Buildnumber);

                                bool isVoyager = variants.Contains(VariantFlags.VoyagerVariant);

                                if (isVoyager)
                                {
                                    beaconSubject.OnNext(
                                        new VoyagerBeacon(
                                            udpReceived.Timestamp,
                                            udpReceived.Received.RemoteEndPoint.Address,
                                            systemType,
                                            beacon.SerialNumber.ToString(CultureInfo.InvariantCulture),
                                            onboardVersion,
                                            (ConnectionAvailability)beacon.ConnectionState,
                                            beacon.CpuTemp)
                                    );
                                }
                                else
                                {
                                    beaconSubject.OnNext(
                                        new ExplorerBeacon(
                                            udpReceived.Timestamp,
                                            udpReceived.Received.RemoteEndPoint.Address,
                                            systemType,
                                            beacon.SerialNumber.ToString(CultureInfo.InvariantCulture),
                                            onboardVersion,
                                            (ConnectionAvailability)beacon.ConnectionState,
                                            beacon.CpuTemp)
                                    );
                                }
                            }
                        }
                        else
                        {
                            // Unrecognizable beacon or packet from another source
                        }
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        // Unrecognizable beacon or packet from another source
                    }
                    break;

                // Not currently supported.
                //case NetworkConstants.ArisDefenderBeaconPort:
            }
        }

        public IObservable<ArisBeacon> Beacons => beaconSubject;

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (var udp in udpListeners)
                    {
                        udp.Dispose();
                    }

                    foreach (var sub in beaconSubscriptions)
                    {
                        sub.Dispose();
                    }

                    beaconSubject.OnCompleted();
                    beaconSubject.Dispose();
                    bufferedQueue.Dispose();
                }

                // no unmanaged resources
                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private readonly Subject<ArisBeacon> beaconSubject = new Subject<ArisBeacon>();
        private readonly UdpListener[] udpListeners;
        private readonly IDisposable[] beaconSubscriptions;
        private readonly BufferedMessageQueue<UdpReceived> bufferedQueue;
        private bool disposed;
    }
}
