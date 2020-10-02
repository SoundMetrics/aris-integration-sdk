// Copyright (c) 2013-2017 Sound Metrics Corporation. All Rights Reserved.

#include "FrameRate.h"
#include "../Reordering/Reorder.h"
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

        double CalculateMaxFrameRate(const Aris::Common::SystemType systemType,
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

        namespace {

            const uint32_t CyclePeriodFudgeUs = 420;

            double DetermineCyclePeriodAdjustmentFactor(
                Aris::Common::SystemType systemType,
                uint32_t samplePeriodUs)
            {
                const bool isSmallSamplePeriod = samplePeriodUs <= 4;

                switch (systemType)
                {
                case Aris::Common::SystemType::Aris3000:
                    return isSmallSamplePeriod ? 0.076 : 0.026 ;

                case Aris::Common::SystemType::Aris1800:
                    return isSmallSamplePeriod ? 0.053 : 0.026 ;

                case Aris::Common::SystemType::Aris1200:
                    return 0.011;

                default:
                    return 0.011; // You should never find yourself here.
                }
            }


            double CalculateMinimumFramePeriod(
                double ppf,
                double mcp,
                double cpa1)
            {
                return ppf * (mcp + cpa1);
            }

            double CalculateMinimumFramePeriodWithDelay(
                uint32_t ppf,
                double mcp,
                double cpa1,
                uint32_t nob,
                uint32_t spb,
                uint32_t interpacketDelayUs)
            {
                const auto id = interpacketDelayUs;
                const auto headroom = 16.6;

                return
                    ppf * (mcp + cpa1)
                    + (((nob * spb) + 1024) / 1392) * (headroom + id);
            }

        }

        double CalculateMaxFrameRate(
            Aris::Common::SystemType systemType,
            uint32_t pingMode,
            int samplesPerBeam,
            uint32_t sampleStartDelayUs,
            uint32_t samplePeriodUs,
            uint32_t antiAliasingUs,
            bool enableInterpacketDelay,
            uint32_t interpacketDelayUs)
        {
            // Aliases to match bill's doc; the function interface shouldn't use these.

            const auto ssd = sampleStartDelayUs;
            const auto spb = samplesPerBeam;
            const auto sp = samplePeriodUs;
            const auto ppf = PingModeToPingsPerFrame(pingMode);
            const auto aa = antiAliasingUs;
            const auto nob = PingModeToNumBeams(pingMode);

            // from the document

            const auto mcp = ssd + (sp * spb) + CyclePeriodFudgeUs;

            const auto cpaFactor =
                DetermineCyclePeriodAdjustmentFactor(systemType, sp);
            const auto cpa = mcp * cpaFactor;
            const auto cpa1 = cpa + aa;

            const auto mfp = enableInterpacketDelay
                ? CalculateMinimumFramePeriodWithDelay(
                    ppf, mcp, cpa1, nob, spb, interpacketDelayUs)
                : CalculateMinimumFramePeriod(ppf, mcp, cpa1);

            // De-alias
            const auto maxFramePeriod = mfp;

            const auto  maximumFrameRate = 1e6 / maxFramePeriod;
            const auto  trueMin = 1.0;
            const auto  trueMax = 15.0;
            const auto  limitedRate = std::max(trueMin, std::min(maximumFrameRate, trueMax));

            return limitedRate;
        }
    }
}
