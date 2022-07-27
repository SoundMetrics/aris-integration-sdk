// Copyright (c) 2022 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    public struct WindowBounds : IEquatable<WindowBounds>
    {
        public Distance WindowStart { get; }
        public Distance WindowEnd { get; }
        public Distance WindowLength => WindowEnd - WindowStart;

        public WindowBounds(Distance windowStart, Distance windowEnd)
        {
            WindowStart = windowStart;
            WindowEnd = windowEnd;
        }

        internal WindowBounds(float windowStart, float windowEnd)
        {
            WindowStart = (Distance)windowStart;
            WindowEnd = (Distance)windowEnd;
        }

        public void Deconstruct(out Distance windowStart, out Distance windowEnd)
        {
            windowStart = WindowStart;
            windowEnd = WindowEnd;
        }

        public void Deconstruct(
            out Distance windowStart,
            out Distance windowEnd,
            out Distance windowLength)
        {
            windowStart = WindowStart;
            windowEnd = WindowEnd;
            windowLength = WindowLength;
        }

        public override string ToString()
            => $"WindowStart=[{WindowStart}]; WindowEnd=[{WindowEnd}]";

        public override bool Equals(object obj)
            => obj is WindowBounds other && this.Equals(other);

        public override int GetHashCode()
            => WindowStart.GetHashCode() ^ (17 * WindowEnd.GetHashCode());

        public bool Equals(WindowBounds other)
            => WindowStart == other.WindowStart && WindowEnd == other.WindowEnd;

        public static bool operator ==(WindowBounds left, WindowBounds right)
            => left.Equals(right);

        public static bool operator !=(WindowBounds left, WindowBounds right)
            => !(left == right);
    }
}
