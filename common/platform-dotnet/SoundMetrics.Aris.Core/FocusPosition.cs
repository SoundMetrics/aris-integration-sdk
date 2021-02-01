// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core
{
    public abstract class FocusPosition
    {
        public static FocusPosition Automatic => FocusPositionAutomatic.Instance;

        public static FocusPosition At(Distance distance) => new FocusPositionManual(distance);

        internal FocusPosition() { }
    }

    public sealed class FocusPositionManual : FocusPosition
    {
        internal FocusPositionManual(Distance focusPosition)
        {
            Value = focusPosition;
        }

        public Distance Value { get; private set; }
    }

    public sealed class FocusPositionAutomatic : FocusPosition
    {
        public static readonly FocusPositionAutomatic Instance = new FocusPositionAutomatic();

        private FocusPositionAutomatic() { }
    }
}
