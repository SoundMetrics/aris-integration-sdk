using SoundMetrics.Aris.Core;
using System;
using System.Net;

namespace SoundMetrics.Aris.Availability
{
    public sealed class ExplorerBeacon : ArisBeacon
    {
        internal ExplorerBeacon(
            DateTimeOffset timestamp,
            IPAddress ipAddress,
            SystemType systemType,
            string serialNumber,
            OnboardSoftwareVersion softwareVersion,
            ConnectionAvailability availability,
            float cpuTemp)
            : base(
                timestamp,
                ipAddress,
                systemType,
                serialNumber,
                softwareVersion,
                availability,
                cpuTemp,
                hasDepthReading: true)
        {

        }
    }
}
