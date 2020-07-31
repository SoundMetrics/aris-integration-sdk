using SoundMetrics.Aris.Availability;
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
            : this(SynchronizationContext.Current)
        {
        }

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

                        if (!beacon.IsDiverHeld)
                        {
                            var variants =
                                (IEnumerable<string>)beacon.SystemVariants?.Enabled ?? new string[0];
                            bool isVoyager = variants.Contains(VariantFlags.VoyagerVariant);

                            if (isVoyager)
                            {
                                beaconSubject.OnNext(new VoyagerBeacon
                                {
                                    Timestamp = udpReceived.Timestamp,
                                    IPAddress = udpReceived.Received.RemoteEndPoint.Address,
                                    SystemType = (Data.SystemType)beacon.SystemType,
                                    SerialNumber = beacon.SerialNumber.ToString(CultureInfo.InvariantCulture),
                                    SoftwareVersion = new OnboardSoftwareVersion
                                    {
                                        Major = beacon.SoftwareVersion.Major,
                                        Minor = beacon.SoftwareVersion.Minor,
                                        BuildNumber = beacon.SoftwareVersion.Buildnumber,
                                    },
                                    Availability = (ConnectionAvailability)beacon.ConnectionState,
                                    CpuTemp = beacon.CpuTemp,
                                });
                            }
                            else
                            {
                                beaconSubject.OnNext(new ExplorerBeacon
                                {
                                    Timestamp = udpReceived.Timestamp,
                                    IPAddress = udpReceived.Received.RemoteEndPoint.Address,
                                    SystemType = (Data.SystemType)beacon.SystemType,
                                    SerialNumber = beacon.SerialNumber.ToString(CultureInfo.InvariantCulture),
                                    SoftwareVersion = new OnboardSoftwareVersion
                                    {
                                        Major = beacon.SoftwareVersion.Major,
                                        Minor = beacon.SoftwareVersion.Minor,
                                        BuildNumber = beacon.SoftwareVersion.Buildnumber,
                                    },
                                    Availability = (ConnectionAvailability)beacon.ConnectionState,
                                    CpuTemp = beacon.CpuTemp,
                                });
                            }
                        }
                    }
                    catch
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
