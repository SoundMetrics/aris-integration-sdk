// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Runtime.Serialization;

namespace SoundMetrics.Aris.Core
{
    /// <summary>
    /// Identifies a system type, the physical configuration of the device.
    /// System types span across ARIS models, so an Explorer, a Defender, and
    /// a Voyager may all be an ARIS 3000.
    /// </summary>
    [DataContract]
    public struct SystemType : IEquatable<SystemType>
    {
        public static readonly SystemType Aris1200 = new SystemType(2);
        public static readonly SystemType Aris1800 = new SystemType(0);
        public static readonly SystemType Aris3000 = new SystemType(1);

        private static readonly SystemType[] CandidateLookups = new[] { Aris3000, Aris1800, Aris1200 };

        internal static bool TryGetFromIntegralValue(int integralValue, out SystemType systemType)
        {
            foreach (var candidate in CandidateLookups)
            {
                if (integralValue == candidate.IntegralValue)
                {
                    systemType = candidate;
                    return true;
                }
            }

            systemType = default;
            return false;
        }

        internal static SystemType GetFromIntegralValue(int integralValue)
        {
            if (TryGetFromIntegralValue(integralValue, out var systemType))
            {
                return systemType;
            }

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new ArgumentOutOfRangeException(nameof(integralValue), "Unrecognized system type");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }

        private SystemType(int integralValue)
        {
            this.integralValue = integralValue;
        }

        public override string ToString() => $"{HumanReadableString} ({integralValue})";

        public override bool Equals(object obj)
            => obj is SystemType other && this.Equals(other);

        public bool Equals(SystemType other) => this.integralValue == other.integralValue;

        public static bool operator ==(SystemType left, SystemType right)
            => left.Equals(right);

        public static bool operator !=(SystemType left, SystemType right)
            => !(left == right);

        public override int GetHashCode() => integralValue;

        internal int IntegralValue => integralValue;

        public string HumanReadableString
        {
            get
            {
                switch (integralValue)
                {
                    case 0: return "ARIS 1800";
                    case 1: return "ARIS 3000";
                    case 2: return "ARIS 1200";
                    default:
                        throw new InvalidOperationException($"Unexpected system type=[{integralValue}]");
                }
            }
        }

        [DataMember]
        private readonly int integralValue;
    }
}
