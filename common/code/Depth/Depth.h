// Copyright 2015-2017 Sound Metrics corporation

#ifndef __DEPTH_H__
#define __DEPTH_H__

#include <cstdint>

namespace Aris {
  namespace AcousticMath {

    double CalculateDepthM(double pressurePSI, uint32_t salinityPPT, double temperatureC);

  }
}

#endif // __DEPTH_H__
