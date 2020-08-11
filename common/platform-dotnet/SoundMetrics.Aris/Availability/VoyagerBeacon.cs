using SoundMetrics.Aris.Data;
using System;
using System.Net;

namespace SoundMetrics.Aris.Availability
{
    public sealed class VoyagerBeacon : ArisBeacon
    {
        internal VoyagerBeacon(
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
                hasDepthReading: false)
        {
        }
    }
}
