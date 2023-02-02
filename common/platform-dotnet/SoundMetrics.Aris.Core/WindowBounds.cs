// Copyright (c) 2022 Sound Metrics Corp.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    [DebuggerDisplay("[{WindowStart} - {WindowEnd}]")]
    public struct WindowBounds : IEquatable<WindowBounds>
    {
        /// <summary>
        /// The distance to the nearest edge of the imaging window.
        /// </summary>
        public Distance WindowStart { get; }

        /// <summary>
        /// The distance to the farthest edge of the imaging window.
        /// </summary>
        public Distance WindowEnd { get; }

        /// <summary>
        /// The distance from the nearest edge of the imaging window
        /// to the farthest edge.
        /// </summary>
        public Distance WindowLength => WindowEnd - WindowStart;

        /// <summary>
        /// The distance midway between the nearest edge of the
        /// imaging window to the farthest edge.
        /// </summary>
        public Distance Midpoint => (WindowStart + WindowEnd) / 2;

        public WindowBounds(Distance windowStart, Distance windowEnd)
        {
            WindowStart = windowStart;
            WindowEnd = windowEnd;
            CheckInvariants();
        }

        internal WindowBounds(double windowStart, double windowEnd)
        {
            WindowStart = (Distance)windowStart;
            WindowEnd = (Distance)windowEnd;
            CheckInvariants();
        }

        public WindowBounds(in ValueRange<Distance> windowLimits)
        {
            WindowStart = windowLimits.Minimum;
            WindowEnd = windowLimits.Maximum;
            CheckInvariants();
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

        private void CheckInvariants()
        {
            if (!(WindowStart < WindowEnd))
            {
                var errorMessage =
                    $"{nameof(WindowEnd)} must be greater than {nameof(WindowStart)}";
                throw new ArgumentOutOfRangeException(errorMessage);
            }
        }

        public static implicit operator WindowBounds((float WindowStart, float WindowEnd) bounds)
            => ToWindowBounds(bounds);

        public static WindowBounds ToWindowBounds((float WindowStart, float WindowEnd) bounds)
            => new WindowBounds(bounds.WindowStart, bounds.WindowEnd);

        public WindowBounds MoveStartTo(Distance requestedStart)
            => new WindowBounds(requestedStart, requestedStart + WindowLength);

        public override string ToString()
            => $"WindowStart=[{WindowStart}]; WindowEnd=[{WindowEnd}]; WindowLength=[{WindowLength}]";

        public string ToShortString()
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
