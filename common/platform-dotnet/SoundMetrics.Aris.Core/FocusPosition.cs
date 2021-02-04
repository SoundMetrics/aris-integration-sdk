// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    public abstract class FocusPosition : IEquatable<FocusPosition>
    {
        public static FocusPosition Automatic => FocusPositionAutomatic.Instance;

        public static FocusPosition At(Distance distance) => new FocusPositionManual(distance);
        public abstract bool Equals(FocusPosition other);

        internal FocusPosition() { }
    }

    public sealed class FocusPositionManual : FocusPosition, IEquatable<FocusPositionManual>
    {
        internal FocusPositionManual(Distance focusPosition)
        {
            Value = focusPosition;
        }

        public Distance Value { get; private set; }

        public override bool Equals(object obj) => Equals(obj as FocusPositionManual);

        public override bool Equals(FocusPosition other) => Equals(other as FocusPositionManual);

        public bool Equals(FocusPositionManual other)
        {
            if (other is null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            return this.Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public sealed class FocusPositionAutomatic : FocusPosition, IEquatable<FocusPositionAutomatic>
    {
        public static readonly FocusPositionAutomatic Instance = new FocusPositionAutomatic();

        private FocusPositionAutomatic() { }

        public override bool Equals(object obj) => Equals(obj as FocusPositionAutomatic);

        public override bool Equals(FocusPosition other) => Equals(other as FocusPositionAutomatic);

        public bool Equals(FocusPositionAutomatic other) => Object.ReferenceEquals(this, other);

        public override int GetHashCode() => base.GetHashCode();
    }
}
