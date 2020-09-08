using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace SoundMetrics.Aris.Connection
{
    internal interface IMachineEvent { }

    /// <summary>
    /// Inserted into the input queue in order to allow additional
    /// processing.
    /// </summary>
    internal sealed class Cycle : IMachineEvent
    {
        public Cycle(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTimeOffset Timestamp { get; }
    }

    /// <summary>
    /// Requests shutdown of the state machine.
    /// </summary>
    internal sealed class Stop : IMachineEvent
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
    internal sealed class ApplySettingsRequest : IMachineEvent, ICommand
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
    /// Represents a clock tick. Some states need to observe
    /// the passage of time.
    /// </summary>
    internal sealed class Tick : IMachineEvent
    {
        public Tick(DateTimeOffset timestamp, IPAddress? deviceAddress)
        {
            Timestamp = timestamp;
            DeviceAddress = deviceAddress;
        }

        public DateTimeOffset Timestamp { get; }
        public IPAddress? DeviceAddress { get; }
    }

    /// <summary>
    /// Indicates that frame data was received.
    /// </summary>
    internal sealed class MarkFrameDataReceived : IMachineEvent
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
    internal sealed class DeviceAddressChanged : IMachineEvent
    {
        public DeviceAddressChanged(IPAddress? oldAddress, IPAddress? targetAddress)
        {
            if (!(oldAddress is null) && object.Equals(oldAddress, targetAddress))
            {
                throw new ArgumentException("Address has not changed");
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
    internal sealed class NetworkAddressChanged : IMachineEvent { }

    /// <summary>
    /// Indicates that the host's network availability has changed.
    /// </summary>
    internal sealed class NetworkAvailabilityChanged : IMachineEvent
    {
        public NetworkAvailabilityChanged(NetworkAvailabilityEventArgs args)
        {
            EventArgs = args;
        }

        public NetworkAvailabilityEventArgs EventArgs { get; private set; }
    }
}
