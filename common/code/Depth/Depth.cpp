// Copyright 2015-2018 Sound Metrics corporation

#include "Depth.h"
#include <array>
#include <cmath>

namespace Aris {
  namespace AcousticMath {

    namespace {
      // 3 sets of conversion factors for 3 different salinities: fresh, brackish and sea water
      // The implied key is water temperature in Celsius.
      // The value is water density.

      constexpr std::array<float, 7> tempToFreshDepthCFs = {
        1.000f, // 0C
        1.000f, // 5C
        1.000f, // 10C
        0.999f, // 15C
        0.998f, // 20C
        0.997f, // 25C
        0.996f, // 30C
      };

      constexpr std::array<float, 7> tempToBrackishDepthCFs = {
        1.012f, // 0C
        1.012f, // 5C
        1.011f, // 10C
        1.011f, // 15C
        1.010f, // 20C
        1.008f, // 25C
        1.007f, // 30C
      };

      constexpr std::array<float, 7> tempToSeaDepthCFs = {
        1.028f, // 0C
        1.028f, // 5C
        1.027f, // 10C
        1.026f, // 15C
        1.025f, // 20C
        1.023f, // 25C
        1.022f, // 30C
      };

      inline int temperatureToIndex(const std::array<float, 7> & cfs, double temp) {

        const int maxIdx = static_cast<int>(cfs.size() - 1);

        // The temperature is the index into cfs. There are 5-degree ranges for each, so
        // idx = lround(temp / 5).

        temp = std::max(0.0, temp); // Bottom of the scale is zero.

        const int idx = lround(temp / 5); // wraps to really big index on negative
        return std::min(maxIdx, idx);
      }

      inline const std::array<float, 7> & selectCFRange(uint32_t salinityPPT) {

        if (salinityPPT >= 35) {
          return tempToSeaDepthCFs;
        }
        else if (salinityPPT >= 15) {
          return tempToBrackishDepthCFs;
        }
        else /* salinityPPT >= 0 */ {
          return tempToFreshDepthCFs;
        }
      }

      inline float selectCF(const std::array<float, 7> & cfs, double temp) {

        const auto idx = temperatureToIndex(cfs, temp);
        return cfs[idx];
      }

      inline double GetConversionFactor(uint32_t salinityPPT, double temperatureC) {
        return selectCF(selectCFRange(salinityPPT), temperatureC);
      }
    }

    double CalculateDepthM(double pressurePSI, uint32_t salinityPPT, double temperatureC) {

      const auto cf = GetConversionFactor(salinityPPT, temperatureC);

      return (pressurePSI - 14.6959) * 0.702398 / cf;
    }

  }
}
