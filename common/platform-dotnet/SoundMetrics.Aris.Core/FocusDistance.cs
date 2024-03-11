// Copyright 2022 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Diagnostics;

namespace SoundMetrics.Aris.Core
{
    public struct FocusDistance : IEquatable<FocusDistance>
    {
        public FocusDistance(Distance distance)
        {
            Distance = distance;
            FocusUnits = default;

            CheckInvariants(this);
        }

        private FocusDistance(uint focusUnits)
        {
            FocusUnits = focusUnits;
            Distance = default;

            CheckInvariants(this);
        }

        public Distance? Distance { get; private set; }

        /// <summary>
        /// Internal use only, not supported.
        /// </summary>
        public uint? FocusUnits { get; private set; }

        public static implicit operator FocusDistance(Distance distance)
            => ToFocusDistance(distance);

        public static FocusDistance ToFocusDistance(Distance distance)
            => new FocusDistance(distance);

        public static implicit operator Distance(in FocusDistance fd)
            => fd.Distance.Value;

        public static Distance ToDistance(in FocusDistance fd)
            => fd.Distance.Value;

        /// <summary>
        /// Internal device-specific construction. Use a Distance instead.
        /// </summary>
        /// <param name="focusUnits"></param>
        /// <returns></returns>
        public static FocusDistance FromFocusUnits(uint focusUnits)
            => new FocusDistance(focusUnits);

        public override string ToString()
            => Distance is Distance d
                ? d.ToString()
                : (FocusUnits is uint u
                    ? $"{u} focus units"
                    : "<invalid>");

        public override bool Equals(object obj)
            => obj is FocusDistance fd && this.Equals(fd);

        public bool Equals(FocusDistance other)
            => (this.Distance.HasValue && this.Distance == other.Distance)
                || (this.FocusUnits.HasValue && this.FocusUnits == other.FocusUnits);

        public override int GetHashCode()
        {
            Debug.Assert(Distance.HasValue || FocusUnits.HasValue);

            return
                Distance is Distance d
                    ? d.GetHashCode()
                    : (FocusUnits is uint u ? u.GetHashCode() : 0);
        }

        public static bool operator ==(FocusDistance left, FocusDistance right)
            => left.Equals(right);

        public static bool operator !=(FocusDistance left, FocusDistance right)
            => !(left == right);

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Globalization",
            "CA1303:Do not pass literals as localized parameters",
            Justification = "Library isn't localized")]
        private static void CheckInvariants(in FocusDistance fd)
        {
            if (fd.Distance.HasValue && fd.FocusUnits.HasValue)
            {
                throw new Exception("Both value types are set");
            }

            if (!fd.Distance.HasValue && !fd.FocusUnits.HasValue)
            {
                throw new Exception("Neither value type is set");
            }
        }
    }
}
