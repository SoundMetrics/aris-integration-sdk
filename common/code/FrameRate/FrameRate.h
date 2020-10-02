// Copyright (c) 2013-2017 Sound Metrics Corporation. All Rights Reserved.

#pragma once

#include "ArisBasics.h"

namespace Aris {
    namespace AcousticMath {

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
