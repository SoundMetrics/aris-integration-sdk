using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace SoundMetrics.Aris.Connection
{
    internal enum MachineEventType
    {
        /// <summary>
        /// Represents a clock tick. Some states need to observe
        /// the passage of time.
        /// </summary>
        Tick,

        MarkFrameDataReceived,
        NetworkAddressChanged,
        NetworkAvailabilityChanged,
        Compound,
    }

    // Simple events are stored within a struct, so no extra allocations.
    internal struct MachineEvent
    {
        public MachineEvent(
            MachineEventType eventType,
            DateTimeOffset timestamp,
            IPAddress? deviceAddress,
            ICompoundMachineEvent? compoundEvent = null)
        {
            EventType = eventType;
            Timestamp = timestamp;
            DeviceAddress = deviceAddress;
            CompoundEvent = compoundEvent;
        }

        public MachineEventType EventType { get; set; }
        public DateTimeOffset Timestamp { get; }
        public IPAddress? DeviceAddress { get; }
        public ICompoundMachineEvent? CompoundEvent { get; }

        private string CompoundName =>
            CompoundEvent?.GetType().Name ?? "(null)";

        public string EventName =>
            EventType switch
            {
                MachineEventType.Compound => $"Compound '{CompoundName}'",
                _ => $"{EventType}",
            };
    }

    internal interface ICompoundMachineEvent { }

    /// <summary>
    /// Requests shutdown of the state machine.
    /// </summary>
    internal sealed class Stop : ICompoundMachineEvent
    {
        public Stop(ManualResetEventSlim completeSignal)
        {
            // The caller owns the signal object.
            this.completeSignal = completeSignal;
        }

        public void MarkComplete() => completeSignal.Set();

        private readonly ManualResetEventSlim completeSignal;
    }

    /// <summary>
    /// Represents a request for new settings.
    /// </summary>
    internal sealed class ApplySettingsRequest : ICompoundMachineEvent, ICommand
    {
        public ApplySettingsRequest(int settingsCookie, ISettings settings)
        {
            SettingsCookie = settingsCookie;
            this.settings = settings;
        }

        public int SettingsCookie { get; }

        public Type SettingsType => settings.GetType();

        public string[] GenerateCommand()
        {
            // Command verb is supplied by GenerateCommand()
            return ((ICommand)settings).GenerateCommand()
                    .Concat(new[] { $"settings_cookie {SettingsCookie}" })
                    .ToArray();
        }

        private readonly ISettings settings;
    }

    /// <summary>
    /// Indicates that frame data was received.
    /// This is used to detect when the ARIS has stopped sending frame packets.
    /// The command connection uses keep-alives, but in limited scenarios--
    /// such as killing the arisapp process during development--
    /// a zombie socket is maintained and kept alive. This mechanism detects
    /// when frames are no longer being sent and allows the connection to be
    /// terminated & attempted again.
    /// </summary>
    internal sealed class MarkFrameDataReceived : ICompoundMachineEvent
    {
        public MarkFrameDataReceived(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTimeOffset Timestamp { get; }
    }

    /// <summary>
    /// Indicates that the IP address of the target device changed.
    /// Changing to null/unknown is not interesting and ignored;
    /// changing to a known address causes a new connection.
    /// </summary>
    internal sealed class DeviceAddressChanged : ICompoundMachineEvent
    {
        public DeviceAddressChanged(IPAddress? oldAddress, IPAddress? targetAddress)
        {
            if (!(oldAddress is null) && object.Equals(oldAddress, targetAddress))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Address has not changed");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            OldAddress = oldAddress;
            DeviceAddress = targetAddress;
        }

        public IPAddress? OldAddress { get; }
        public IPAddress? DeviceAddress { get; }
    }

    /// <summary>
    /// Indicates that the host's network configuration has changed.
    /// </summary>
    internal sealed class NetworkAddressChanged : ICompoundMachineEvent { }

    /// <summary>
    /// Indicates that the host's network availability has changed.
    /// </summary>
    internal sealed class NetworkAvailabilityChanged : ICompoundMachineEvent
    {
        public NetworkAvailabilityChanged(NetworkAvailabilityEventArgs args)
        {
            EventArgs = args;
        }

        public NetworkAvailabilityEventArgs EventArgs { get; private set; }
    }
}
