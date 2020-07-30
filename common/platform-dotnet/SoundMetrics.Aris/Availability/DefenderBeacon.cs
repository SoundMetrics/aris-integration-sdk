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
        internal DefenderBeacon()
            : base(hasDepthReading: true)
        {

        }

        public DefenderState State { get; }
    }
}
