using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Availability
{
    [DebuggerDisplay("v {Major}.{Minor}.{BuildNumber}")]
    public struct OnboardSoftwareVersion : IEquatable<OnboardSoftwareVersion>
    {
        public OnboardSoftwareVersion(uint major, uint minor, uint buildNumber)
        {
            Major = major;
            Minor = minor;
            BuildNumber = buildNumber;
        }

#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly uint Major;
        public readonly uint Minor;
        public readonly uint BuildNumber;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public override string ToString()
        {
            return $"{Major}.{Minor}.{BuildNumber}";
        }

        public override bool Equals(object? obj)
            => obj is OnboardSoftwareVersion other && this.Equals(other);

        public bool Equals(OnboardSoftwareVersion other)
            => this.Major == other.Major
                && this.Minor == other.Minor
                && this.BuildNumber == other.BuildNumber;

        public override int GetHashCode()
            => Major.GetHashCode() ^ Minor.GetHashCode() ^ BuildNumber.GetHashCode();

        public static bool operator ==(OnboardSoftwareVersion left, OnboardSoftwareVersion right)
            => left.Equals(right);

        public static bool operator !=(OnboardSoftwareVersion left, OnboardSoftwareVersion right)
            => !(left == right);
    }
}
