using Serilog;
using SoundMetrics.Aris.Availability;
using SoundMetrics.Aris.Core.Raw;
using SoundMetrics.Aris.Data;
using System;
using System.Diagnostics;
using System.Net;
using System.Reactive.Linq;
using System.Threading;

namespace SoundMetrics.Aris.Connection
{
    [DebuggerDisplay("ArisController for {SerialNumber}")]
    public sealed class ArisController : IArisFrameSource, IDisposable
    {
        /// <summary>
        /// Constructs an ArisController.
        /// </summary>
        /// <param name="arisBeacon">
        /// A beacon from the ARIS to be controlled.
        /// </param>
        /// <param name="availability">
        /// An availability status object. The application owns this object
        /// and passes a reference here; ArisController does not own or
        /// dispose of this object.
        /// </param>
        public ArisController(
            ArisBeacon arisBeacon,
            AvailabilityStatusReporter availability)
            : this(arisBeacon,
                   availability,
                   ValidateSynchronizationContext(
                      SynchronizationContext.Current,
                      "There is no current SynchronizationContext"))
        {
        }

        /// <summary>
        /// Constructs an ArisController.
        /// </summary>
        /// s<param name="arisBeacon">
        /// A beacon from the ARIS to be controlled.
        /// </param>
        /// <param name="availability">
        /// An availability status object. The application owns this object
        /// and passes a reference here; ArisController does not own or
        /// dispose of this object.
        /// </param>
        public ArisController(
            ArisBeacon arisBeacon,
            AvailabilityStatusReporter availability,
            SynchronizationContext syncContext)
        {
            ArgumentNullException.ThrowIfNull(syncContext);

            serialNumber = arisBeacon.SerialNumber;

            // Create the state machine before setting up the inputs
            // that drive it.
            stateMachine = new StateMachine(serialNumber, arisBeacon.SystemType);

            this.availability = availability;
            availabilitySub =
                availability.Changes
                    .Where(change => change.LatestBeacon.SerialNumber == SerialNumber)
                    .ObserveOn(syncContext)
                    .Subscribe(OnBeacon);
        }

        public int ApplySettings(AcousticSettingsRaw settings)
        {
            return stateMachine.ApplySettings(settings);
        }

        public uint SerialNumber => serialNumber;

        public IObservable<Frame> Frames => stateMachine.Frames;

        public ProtocolMetricsOG Stop() => stateMachine.Stop();

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
                        !isNew && !Equals(lastObservedAddress, beacon.IPAddress);
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
                    stateMachine?.Dispose();
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

        private readonly uint serialNumber;
        private readonly AvailabilityStatusReporter availability;
        private readonly IDisposable availabilitySub;
        private readonly StateMachine stateMachine;

        private IPAddress? lastObservedAddress;
        private bool disposed;
    }
}
