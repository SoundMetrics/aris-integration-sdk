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

    }
}
