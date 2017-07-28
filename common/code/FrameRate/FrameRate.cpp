// Copyright (c) 2013-2017 Sound Metrics Corporation. All Rights Reserved.

#include "FrameRate.h"
#include <algorithm>

namespace Aris {
    namespace AcousticMath {

        // The cyclePeriodFactor is a value based on measured maximum frame rates as a function of
        // samplePeriod and samplesPerBeam.
        //
        // From measured achievable frame rates, there is a rough break in the slope of frame rate
        // versus samplesPerBeam at samplesPerBeam == 2000.  Use a higher (boostFactor = 2) slope
        // when samplesPerBeam is greater than 2000, and a lower slope for samplesPerBeam < 2000
        double CalcCyclePeriodFactor(const uint32_t samplePeriod, const uint32_t samplesPerBeam)
        {
            const uint32_t samplesPerBeamThreshold = 2000;
            const uint32_t fastSamplePeriodLimit = 7;
            const uint32_t fastestSamplePeriodLimit = 4;
            const double minFactor = 1400.0;
            const double boostFactor = 2.0;
            const double fastSamplePeriodFactor = 3000.0;
            const double fastestSamplePeriodFactor = 400.0;

            double cyclePeriodFactor = (samplesPerBeam > samplesPerBeamThreshold)
                ? minFactor + boostFactor * minFactor
                * (double)(samplesPerBeam - samplesPerBeamThreshold)
                / (double)samplesPerBeamThreshold
                : minFactor
                * (double)samplesPerBeam / (double)samplesPerBeamThreshold;

            // Add some additional delay at samplePeriod == [4, 5, 6] based on samplePeriod value
            // to account for observed performance loss at these sample periods
            cyclePeriodFactor += (samplePeriod < fastSamplePeriodLimit)
                ? fastSamplePeriodFactor / (double)samplePeriod
                : 0.0;

            // Add more delay at shortest samplePeriod == 4 to constrain to achievable frame rates
            cyclePeriodFactor += (samplePeriod == fastestSamplePeriodLimit)
                ? fastestSamplePeriodFactor
                : 0.0;

            return cyclePeriodFactor;
        }

        double CalculateMaxFrameRate(const SystemType systemType,
            const uint32_t samplePeriod,
            const uint32_t samplesPerBeam,
            const uint32_t cyclePeriod,
            const uint32_t pingsPerFrame)
        {
            static const uint32_t max_cycle_periods[3] = {
                80000, 40000, 150000  // ARIS1800, ARIS3000, ARIS1200
            };

            const uint32_t maxAllowedCyclePeriod = max_cycle_periods[(int32_t)systemType];
            const uint32_t cyclePeriodFactor = CalcCyclePeriodFactor(samplePeriod, samplesPerBeam);
            const uint32_t unboundedCyclePeriod = cyclePeriod + cyclePeriodFactor;
            const uint32_t minCyclePeriod = std::min(maxAllowedCyclePeriod, unboundedCyclePeriod);

            return 1000000.0 / (double)(minCyclePeriod * pingsPerFrame);
        }

    }
}
