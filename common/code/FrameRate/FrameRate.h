// Copyright (c) 2013-2017 Sound Metrics Corporation. All Rights Reserved.

#pragma once

#include "ArisBasics.h"

namespace Aris {
    namespace AcousticMath {

        double CalculateMaxFrameRate(const Aris::Common::SystemType systemType,
                                     const uint32_t samplePeriod,
                                     const uint32_t samplesPerBeam,
                                     const uint32_t cyclePeriod,
                                     const uint32_t pingsPerFrame);

        // Effective as of ARIScope/ArisApp 2.8.y.z.
        double CalculateMaxFrameRate(
            Aris::Common::SystemType systemType,
            uint32_t pingMode,
            int samplesPerBeam,
            uint32_t sampleStartDelayUs,
            uint32_t samplePeriodUs,
            uint32_t antiAliasingUs,
            bool enableInterpacketDelay,
            uint32_t interpacketDelayUs);
    }
}
