using SoundMetrics.Aris.Data;
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

    public struct DefenderState
    {
        public DefenderRecordState RecordState;
        public DefenderStorageState StorageState;
        public float StorageLevel;
        public DefenderBatteryState BatteryState;
        public float BatteryLevel;
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

        public DefenderState State { get; }
    }
}
