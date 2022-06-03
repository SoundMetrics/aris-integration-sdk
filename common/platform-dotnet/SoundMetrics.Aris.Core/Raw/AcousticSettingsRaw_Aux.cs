// Copyright (c) 2010-2022 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static System.Math;

namespace SoundMetrics.Aris.Core.Raw
{
    internal static class AcousticSettingsRaw_Aux
    {
        // ********************************************************************
        // Per internal documentation. See
        // ARIScope V2.9 Acoustic Settings Level 1 (Auto) Calculations.
        // ********************************************************************

        private static readonly
            ReadOnlyDictionary<(SystemType, Salinity), (Distance, DistancePerTempRatio)>
            frequencyCrossoverLookup =
                new ReadOnlyDictionary<(SystemType, Salinity), (Distance, DistancePerTempRatio)>(
                    new Dictionary<(SystemType, Salinity), (Distance, DistancePerTempRatio)>
                    {
                        { (SystemType.Aris3000, Salinity.Fresh), ((Distance)5, 0.125) },
                        { (SystemType.Aris3000, Salinity.Brackish), ((Distance)5, 0.125) },
                        { (SystemType.Aris3000, Salinity.Seawater), ((Distance)5, 0.120) },

                        { (SystemType.Aris1800, Salinity.Fresh), ((Distance)15, 0.375) },
                        { (SystemType.Aris1800, Salinity.Brackish), ((Distance)14.5, 0.360) },
                        { (SystemType.Aris1800, Salinity.Seawater), ((Distance)14, 0.294) },

                        { (SystemType.Aris1200, Salinity.Fresh), ((Distance)25, 0.571) },
                        { (SystemType.Aris1200, Salinity.Brackish), ((Distance)24, 0.444) },
                        { (SystemType.Aris1200, Salinity.Seawater), ((Distance)22, 0.350) },
                    });

        private static readonly Temperature referenceTemp = (Temperature)15;

        public static Distance CalculateFrequencyCrossoverDistance(
            SystemType systemType,
            Temperature temperature,
            Salinity salinity)
        {
            var key = (systemType, salinity);
            var (baseDistance, slope) = frequencyCrossoverLookup[key];

            return baseDistance + ((temperature - referenceTemp) * slope);
        }

        public static FineDuration CalculateAutoPulseWidth(
            SystemType systemType,
            Temperature temperature,
            Salinity salinity,
            Distance windowEnd)
        {
            var crossoverRange =
                CalculateFrequencyCrossoverDistance(systemType, temperature, salinity);
            var isHighFrequency = windowEnd <= crossoverRange;

            if (systemType == SystemType.Aris3000)
            {
                if (isHighFrequency)
                {
                    var slope = Max(2, 2 * 5.00 / crossoverRange.Meters);
                    return (FineDuration)
                        Round(
                            Min(16, Max(5, slope * windowEnd.Meters)));
                }
                else
                {
                    var slope = Max(1.75, 8.75 / crossoverRange.Meters);
                    return (FineDuration)
                        Round(
                            Min(24, Max(8, 7 + (slope * (windowEnd.Meters - 4)))));
                }
            }
            else if (systemType == SystemType.Aris1800)
            {
                if (isHighFrequency)
                {
                    var slope = Max(2, 2 * 15.00 / crossoverRange.Meters);
                    return (FineDuration)
                        Round(
                            Min(25, Max(6, slope * (windowEnd.Meters - 5))));
                }
                else
                {
                    var slope = Max(1, 15.00 / crossoverRange.Meters);
                    return (FineDuration)
                        Round(
                            Min(40, Max(5, 7 + (slope * windowEnd.Meters))));

                }
            }
            else if (systemType == SystemType.Aris1200)
            {
                return (FineDuration)
                    Round(
                        Min(80, Max(8, windowEnd.Meters)));
            }
            else
            {
                throw new ArgumentException($"Unhandled system type: [{systemType}]");
            }
        }

        public static FineDuration CalculateAutoSamplePeriod(
            SystemType systemType,
            Temperature temperature,
            Distance windowEnd)
        {
            if (systemType == SystemType.Aris3000)
            {
                // No units given.
                double slope =
                    temperature < referenceTemp
                        ? 0.9 + (0.035 * (referenceTemp - temperature).DegreesCelsius)
                        : 0.9 - (0.020 * (temperature - referenceTemp).DegreesCelsius);
                double offset = 1.5 - (0.075 * (25 - temperature.DegreesCelsius));
                return (FineDuration)
                    Round(
                        Min(19, Max(4, slope * (windowEnd.Meters + offset))));
            }
            else if (systemType == SystemType.Aris1800)
            {
                var slope = 0.5;
                var offset = 3.00;
                return (FineDuration)
                    Round(
                        Min(20, Max(4, slope * (windowEnd.Meters + offset))));
            }
            if (systemType == SystemType.Aris1200)
            {
                var slope = 0.5;
                var offset = 0.0;
                return (FineDuration)
                    Round(
                        Min(40, Max(4, slope * (windowEnd.Meters + offset))));
            }
            else
            {
                throw new ArgumentException($"Unhandled system type: [{systemType}]");
            }
        }
    }
}
