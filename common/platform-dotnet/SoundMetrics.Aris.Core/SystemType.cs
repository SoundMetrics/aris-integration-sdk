// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;

namespace SoundMetrics.Aris.Core
{
    /// <summary>
    /// Identifies a system type, the physical configuration of the device.
    /// System types span across ARIS models, so an Explorer, a Defender, and
    /// a Voyager may all be an ARIS 3000.
    /// </summary>
    public sealed class SystemType : IEquatable<SystemType>
    {
        public static readonly SystemType Aris1200 = new SystemType(2, "ARIS 1200");
        public static readonly SystemType Aris1800 = new SystemType(0, "ARIS 1800");
        public static readonly SystemType Aris3000 = new SystemType(1, "ARIS 3000");

        private static readonly SystemType[] CandidateLookups = new[] { Aris3000, Aris1800, Aris1200 };

        public static bool TryGetFromIntegralValue(int integralValue, out SystemType systemType)
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

        public static SystemType GetFromIntegralValue(int integralValue)
        {
            if (TryGetFromIntegralValue(integralValue, out var systemType))
            {
                return systemType;
            }

            throw new ArgumentOutOfRangeException(nameof(integralValue), "Unrecognized system type");
        }

        private SystemType(int integralValue, string humanReadableString)
        {
            this.integralValue = integralValue;
            this.humanReadableString = humanReadableString;
        }

        public override string ToString() => $"{humanReadableString} ({integralValue})";

        public override bool Equals(object obj) => Equals(obj as SystemType);

        public bool Equals(SystemType other)
        {
            if (other is null)
            {
                return false;
            }

            // There are only 3 instances of SystemType, nobody else may create them.
            // So just do a reference check.
            return Object.ReferenceEquals(this, other);
        }

        public override int GetHashCode() => integralValue;

        internal int IntegralValue => integralValue;

        public string HumanReadableString => humanReadableString;

        private readonly int integralValue;
        private readonly string humanReadableString;
    }
    }
