using SoundMetrics.Aris.Data;
using System;
using System.Net;

namespace SoundMetrics.Aris.Availability
{
    public abstract class ArisBeacon
    {
        internal ArisBeacon(
            DateTimeOffset timestamp,
            IPAddress ipAddress,
            SystemType systemType,
            string serialNumber,
            OnboardSoftwareVersion softwareVersion,
            ConnectionAvailability availability,
            float cpuTemp,
            bool hasDepthReading)
        {
            Timestamp = timestamp;
            IPAddress = ipAddress;
            SystemType = systemType;
            SerialNumber = serialNumber;
            SoftwareVersion = softwareVersion;
            Availability = availability;
            CpuTemp = cpuTemp;
            this.hasDepthReading = hasDepthReading;
        }


        public DateTimeOffset Timestamp { get; }
        public IPAddress IPAddress { get; }
        public SystemType SystemType { get; }
        public string SerialNumber { get; }
        public OnboardSoftwareVersion SoftwareVersion { get; }
        public ConnectionAvailability Availability { get; }
        public float CpuTemp { get; }

        public bool HasDepthReading => hasDepthReading;

        private readonly bool hasDepthReading;
    }
}
