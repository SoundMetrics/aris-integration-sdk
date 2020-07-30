using SoundMetrics.Aris.Data;
using System;
using System.Net;

namespace SoundMetrics.Aris.Availability
{
    public abstract class ArisBeacon
    {
        internal ArisBeacon(bool hasDepthReading)
        {
            this.hasDepthReading = hasDepthReading;
        }


        public DateTimeOffset Timestamp { get; internal set; }
        public IPAddress IPAddress { get; internal set; }
        public SystemType SystemType { get; internal set; }
        public string SerialNumber { get; internal set; }
        public OnboardSoftwareVersion SoftwareVersion { get; internal set; }
        public ConnectionAvailability Availability { get; internal set; }
        public float CpuTemp { get; internal set; }

        public bool HasDepthReading => hasDepthReading;

        private readonly bool hasDepthReading;
    }
}
