using System;
using System.ComponentModel;

namespace SoundMetrics.Aris.Network
{
    public struct FrameListenerMetrics : IEquatable<FrameListenerMetrics>
    {
        public FrameListenerMetrics(
            long framesStarted,
            long framesCompleted,
            long packetsReceived,
            long invalidPacketsReceived)
        {
            FramesStarted = framesStarted;
            FramesCompleted = framesCompleted;
            PacketsReceived = packetsReceived;
            InvalidPacketsReceived = invalidPacketsReceived;
        }

#pragma warning disable CA1051 // Do not declare visible instance fields
    public readonly long FramesStarted;
        public readonly long FramesCompleted;
        public readonly long PacketsReceived;
        public readonly long InvalidPacketsReceived;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public static FrameListenerMetrics operator +(FrameListenerMetrics a, FrameListenerMetrics b)
            => Add(a, b);

        public static FrameListenerMetrics Add(FrameListenerMetrics a, FrameListenerMetrics b)
            => new FrameListenerMetrics(
                a.FramesStarted + b.FramesStarted,
                a.FramesCompleted + b.FramesCompleted,
                a.PacketsReceived + b.PacketsReceived,
                a.InvalidPacketsReceived + b.InvalidPacketsReceived);

        public override bool Equals(object? obj)
            => obj is FrameListenerMetrics other && this.Equals(other);

        public bool Equals(FrameListenerMetrics other)
            => this.FramesStarted == other.FramesStarted
                && this.FramesCompleted == other.FramesCompleted
                && this.PacketsReceived == other.PacketsReceived
                && this.InvalidPacketsReceived == other.InvalidPacketsReceived;

        public override int GetHashCode()
            => FramesStarted.GetHashCode()
                ^ FramesCompleted.GetHashCode()
                ^ PacketsReceived.GetHashCode()
                ^ InvalidPacketsReceived.GetHashCode();

        public static bool operator ==(FrameListenerMetrics left, FrameListenerMetrics right)
            => left.Equals(right);

        public static bool operator !=(FrameListenerMetrics left, FrameListenerMetrics right)
            => !(left == right);
    }
}
