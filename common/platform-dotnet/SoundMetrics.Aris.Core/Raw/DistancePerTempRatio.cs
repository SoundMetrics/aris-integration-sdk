// Copyright (c) 2022 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Core.Raw
{
    // Unfortunately C# doesn't deal with units as a first class concept.
    // So here's a shim.
    internal struct DistancePerTempRatio
    {
        public DistancePerTempRatio(Distance distance) : this(distance, (Temperature)1) { }

        public DistancePerTempRatio(Distance distance, Temperature temperature)
        {
            this.distance = distance;
            this.temperature = temperature;
        }

        public static implicit operator DistancePerTempRatio(double d)
            => new DistancePerTempRatio((Distance)d);

        public static Distance operator *(DistancePerTempRatio dpt, Temperature t)
            => (t / dpt.temperature) * dpt.distance;

        public static Distance operator *(Temperature t, DistancePerTempRatio dpt)
            => (t / dpt.temperature) * dpt.distance;

        private readonly Distance distance;
        private readonly Temperature temperature;
    }
}
