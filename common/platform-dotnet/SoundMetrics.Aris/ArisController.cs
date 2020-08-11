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
    public sealed class ArisController : IDisposable
    {
        public ArisController(string serialNumber)
            : this(serialNumber,
                   ValidateSynchronizationContext(
                      SynchronizationContext.Current,
                      "There is no current SynchronizationContext"))
        {
        }

        public ArisController(
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

        public void ApplySettings(ISettings settings)
        {
            stateMachine.ApplySettings(settings);
        }

        public IObservable<Frame> Frames => frameSubject;

        private static SynchronizationContext ValidateSynchronizationContext(
            SynchronizationContext? syncContext,
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
            switch (notice.ChangeType)
            {
                case AvailabilityChangeType.Available:
                    var beacon = notice.LatestBeacon;
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

                    lastObservedAddress = beacon.IPAddress;
                    stateMachine.SetTargetAddress(beacon.IPAddress);
                    break;

                case AvailabilityChangeType.NotAvailable:
                    Log.Information(
                        "ARIS {serialNumber} ({ipAddress}) is no longer heard",
                        serialNumber, notice.LatestBeacon.IPAddress);

                    lastObservedAddress = null;
                    stateMachine.SetTargetAddress(null);
                    break;

                default:
                    throw new Exception($"Unhandled value: {notice.ChangeType}");
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

        private IPAddress? lastObservedAddress;
        private bool disposed;
    }
}
