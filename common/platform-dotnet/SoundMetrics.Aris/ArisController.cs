using Serilog;
using SoundMetrics.Aris.Availability;
using SoundMetrics.Aris.Connection;
using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.Network;
using System;
using System.Diagnostics;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace SoundMetrics.Aris
{
    [DebuggerDisplay("ArisController for {SerialNumber}")]
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
                    .Where(change => change.LatestBeacon.SerialNumber == SerialNumber)
                    .ObserveOn(syncContext)
                    .Subscribe(OnBeacon);
        }

        public int ApplySettings(ISettings settings)
        {
            return stateMachine.ApplySettings(settings);
        }

        public string SerialNumber => serialNumber;

        public IObservable<Frame> Frames => stateMachine.Frames;

        public FrameListenerMetrics Stop() => stateMachine.Stop();

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
                    var version = beacon.SoftwareVersion;

                    if (isNew || addressChanged)
                    {
                        var fmt =
                            addressChanged
                                ? "ARIS {serialNumber} ({version}) moved to {ipAddress}"
                                : "ARIS {serialNumber} ({version}) found at {ipAddress}";
                        Log.Information(fmt, beacon.SerialNumber, version, beacon.IPAddress);
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

        private IPAddress? lastObservedAddress;
        private bool disposed;
    }
}
