using SoundMetrics.Aris.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace SoundMetrics.Aris.Availability
{
    using SerialNumber = String;

    public enum AvailabilityChangeType
    {
        BeginAvailability,
        EndAvailability,
    };

    public struct AvailabilityChange
    {
        public AvailabilityChangeType ChangeType;
        public ArisBeacon Beacon;
    }

    public sealed class Availability : IDisposable
    {
        public Availability(TimeSpan timeout)
            : this(timeout, SynchronizationContext.Current)
        {
        }

        public Availability(TimeSpan timeout, SynchronizationContext syncContext)
        {
            if (syncContext is null)
            {
                throw new ArgumentNullException(nameof(syncContext));
            }

            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    $"{nameof(timeout)} cannot be zero or less");
            }

            this.timeout = timeout;
            Initialize(syncContext, out beaconListener, out observerSub);
        }

        public IObservable<AvailabilityChange> Changes => changeSubject;

        private void Initialize(
            SynchronizationContext syncContext,
            out BeaconListener beaconSource,
            out IDisposable observerSub)
        {
            beaconSource = new BeaconListener(syncContext);

            observerSub =
                Observable.Merge<(DateTimeOffset, ArisBeacon)>(
                    Observable.Interval(TimeSpan.FromSeconds(1.0))
                        .Select<long, (DateTimeOffset, ArisBeacon)>(
                            _ => (DateTimeOffset.Now, null)),
                    beaconSource.Beacons
                        .Select(beacon => (DateTimeOffset.Now, beacon))
                    )
                    .ObserveOn(syncContext)
                    .Subscribe(HandleTimestampOrBeacon);

            void HandleTimestampOrBeacon((DateTimeOffset now, ArisBeacon beacon) input)
            {
                if (input.beacon is null)
                {
                    OnTimer(input.now);
                }
                else
                {
                    OnBeacon(input.now, input.beacon);
                }
            }
        }

        private void OnBeacon(DateTimeOffset now, ArisBeacon beacon)
        {
            // No synchronization to data necessary as we always run
            // on a single synchronization context.

            var key = beacon.SerialNumber;

            if (IsNewDevice())
            {
                AddNewDevice();
            }
            else
            {
                UpdateDevice();
            }

            bool IsNewDevice() => devices.ContainsKey(key);

            void AddNewDevice()
            {
                devices[key] =
                    new DeviceState
                    {
                        LastHeard = now,
                        LatestBeacon = beacon,
                    };
                throw new NotImplementedException("Needs to udpate subject");
            }

            void UpdateDevice()
            {
                var hasIPAddressChanged =
                    beacon.IPAddress != devices[key].LatestBeacon.IPAddress;

                devices[key] =
                    new DeviceState
                    {
                        LastHeard = now,
                        LatestBeacon = beacon
                    };

                if (hasIPAddressChanged)
                {
                    throw new NotImplementedException("Needs to report change in IP address");
                }
            }
        }

        private void OnTimer(DateTimeOffset now)
        {
            CullExpiredDevices();

            void CullExpiredDevices()
            {
                var expiration = now - timeout;
                var cachedExpiredKeys =
                    devices.Where(kvp => kvp.Value.LastHeard < expiration)
                        .Select(kvp => kvp.Key)
                        .ToArray();

                foreach (var key in cachedExpiredKeys)
                {
                    RemoveDevice(key);
                }
            }

            void RemoveDevice(SerialNumber key)
            {
                devices.Remove(key);
                throw new NotImplementedException("Needs to udpate subject");
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    beaconListener.Dispose();

                    changeSubject.OnCompleted();
                    changeSubject.Dispose();
                }

                // no unmanaged resources
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private struct DeviceState
        {
            public DateTimeOffset LastHeard;
            public ArisBeacon LatestBeacon;
        }

        private readonly Dictionary<SerialNumber, DeviceState> devices = new Dictionary<SerialNumber, DeviceState>();
        private readonly BeaconListener beaconListener;
        private readonly Subject<AvailabilityChange> changeSubject = new Subject<AvailabilityChange>();
        private readonly IDisposable observerSub;
        private readonly TimeSpan timeout;
        private bool disposedValue;
    }
}
