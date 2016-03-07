// Copyright 2015 Sound Metrics corporation

#ifndef __DEPTH_H__
#define __DEPTH_H__

#include <cstdint>
 
namespace Aris {
    namespace AcousticMath {

        float CalculateDepthM(float pressurePSI, uint32_t salinityPPT, float temperatureC);
 
    }
}

#endif // __DEPTH_H__
