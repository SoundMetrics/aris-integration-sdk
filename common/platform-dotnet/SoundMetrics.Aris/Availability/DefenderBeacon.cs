using SoundMetrics.Aris.Core;
using System;
using System.Net;

namespace SoundMetrics.Aris.Availability
{
    public enum DefenderRecordState
    {
        Ready = 0,
        Recording = 1,
    }

    public enum DefenderStorageState
    {
        Nominal = 0,
        StorageFull = 1,
        StorageError = 2,
        StorageMissing = 3,
    }

    public enum DefenderBatteryState
    {
        Nominal = 0,
        Low = 1,
        NoPower = 2,
        Missing = 3,
        OnTetherPower = 4,
    }

    public class DefenderState
    {
        public DefenderRecordState RecordState { get; set; }
        public DefenderStorageState StorageState { get; set; }
        public float StorageLevel { get; set; }
        public DefenderBatteryState BatteryState { get; set; }
        public float BatteryLevel { get; set; }
    }

    public sealed class DefenderBeacon : ArisBeacon
    {
        internal DefenderBeacon(
            DateTimeOffset timestamp,
            IPAddress ipAddress,
            SystemType systemType,
            string serialNumber,
            OnboardSoftwareVersion softwareVersion,
            ConnectionAvailability availability,
            float cpuTemp,
            DefenderState state)
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
            State = state;
        }

        public DefenderState State { get; private set; }
    }
}
