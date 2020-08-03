using Serilog;
using SoundMetrics.Aris.Availability;
using System;
using System.Net;
using System.Reactive.Linq;
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
            this.syncContext = syncContext;

            availability = new Availability.Availability(
                TimeSpan.FromSeconds(5),
                syncContext);
            availabilitySub =
                availability.Changes
                    .ObserveOn(syncContext)
                    .Subscribe(OnBeacon);
        }

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
            if (!Object.Equals(arisAddress, notice.Beacon.IPAddress))
            {
                Log.Information("ARIS {serialNumber} {action} {ipAddress}",
                    notice.Beacon.SerialNumber,
                    arisAddress is null ? "found at" : "moved to",
                    notice.Beacon.IPAddress);

                arisAddress = notice.Beacon.IPAddress;
                addressAvailable.Set();
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
                    addressAvailable.Dispose();
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
        private readonly SynchronizationContext syncContext;
        private readonly Availability.Availability availability;
        private readonly IDisposable availabilitySub;
        private readonly ManualResetEventSlim addressAvailable = new ManualResetEventSlim(false);

        private IPAddress arisAddress;
        private bool disposed;
    }
}
