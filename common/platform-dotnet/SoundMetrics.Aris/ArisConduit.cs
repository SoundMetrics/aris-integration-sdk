using Serilog;
using SoundMetrics.Aris.Availability;
using SoundMetrics.Aris.Connection;
using SoundMetrics.Aris.Data;
using System;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace SoundMetrics.Aris
{
    public sealed class ArisConduit : IDisposable
    {
        public ArisConduit(string serialNumber)
            : this(serialNumber,
                   ValidateSynchronizationContext(
                      SynchronizationContext.Current,
                      "There is no current SynchronizationContext"))
        {
        }

        public ArisConduit(
            string serialNumber,
            SynchronizationContext syncContext)
        {
            if (!uint.TryParse(serialNumber, out var _))
            {
                throw new ArgumentException(nameof(serialNumber));
            }

            if (syncContext is null)
            {
                throw new ArgumentNullException(nameof(syncContext));
            }

            this.serialNumber = serialNumber;

            // Create the state machine before setting up the inputs
            // that drive it.
            stateMachine = new StateMachine(serialNumber);

            availability = new Availability.Availability(
                TimeSpan.FromSeconds(5),
                syncContext);
            availabilitySub =
                availability.Changes
                    .ObserveOn(syncContext)
                    .Subscribe(OnBeacon);
        }

        public IObservable<Frame> Frames => frameSubject;

        private static SynchronizationContext ValidateSynchronizationContext(
            SynchronizationContext syncContext,
            string errorMessage)
        {
            if (syncContext is null)
            {
                Log.Error(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            return syncContext;
        }

        private void OnBeacon(AvailabilityChange notice)
        {
            if (notice.Beacon is ArisBeacon beacon)
            {
                var isNew = lastObservedAddress is null;
                var addressChanged =
                    !isNew && !Object.Equals(lastObservedAddress, beacon.IPAddress);

                if (isNew || addressChanged)
                {
                    var fmt =
                        addressChanged
                            ? "ARIS {serialNumber} moved to {ipAddress}"
                            : "ARIS {serialNumber} found at {ipAddress}";
                    Log.Information(fmt, beacon.SerialNumber, beacon.IPAddress);
                }

                lastObservedAddress = notice.Beacon.IPAddress;
                stateMachine.SetTargetAddress(notice.Beacon.IPAddress);
            }
            else
            {
                Log.Information("ARIS {serialNumber} is no longer heard",
                    this.serialNumber);
                lastObservedAddress = default;
                stateMachine.SetTargetAddress(default);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    availabilitySub.Dispose();
                    availability.Dispose();
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

        private readonly string serialNumber;
        private readonly Availability.Availability availability;
        private readonly IDisposable availabilitySub;
        private readonly StateMachine stateMachine;
        private readonly Subject<Frame> frameSubject = new Subject<Frame>();

        private IPAddress lastObservedAddress;
        private bool disposed;
    }
}
