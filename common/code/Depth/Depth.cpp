// Copyright 2015 Sound Metrics corporation

#include "Depth.h"
#include <map>
#include <cmath>

namespace Aris {
    namespace AcousticMath {

        // 3 sets of conversion factors for 3 different salinities: fresh, brackish and sea water
        // key is water temperature in Celsius
        // value is water density
        static const std::map<float, float> tempToFreshDepthCFs = {
            {0, 1}, {5, 1}, {10, 1}, {15, 0.999}, {20, 0.998}, {25, 0.997}, {30, 0.996} };

        static const std::map<float, float> tempToBrackishDepthCFs = {
            {0, 1.012}, {5, 1.012}, {10, 1.011}, {15, 1.011}, {20, 1.010}, {25, 1.008}, {30, 1.007} };

        static const std::map<float, float> tempToSeaDepthCFs = {
            {0, 1.028}, {5, 1.028}, {10, 1.027}, {15, 1.026}, {20, 1.025}, {25, 1.023}, {30, 1.022} };

        static float GetConversionFactor(uint32_t salinityPPT, float temperatureC)
        {
            // Find depth conversion factor for water temperature in map
            auto findCF = [] (const std::map<float, float> & tempToDepthCFs, float temperatureC)
            {
                auto current = tempToDepthCFs.begin();
                auto previous = tempToDepthCFs.end();

                while (current != tempToDepthCFs.end()) {
                    if ((int32_t)std::floor(current->first) > (int32_t)std::floor(temperatureC)) {
                        if (current == tempToDepthCFs.begin()) {
                            // water temperature below 0 celsius
                            return current->second;
                        } else {
                            return previous->second;
                        }
                    }
                    previous = current;
                    ++current;
                }

                // water temperature above 30 celsius
                return previous->second;
            };

            float cf;

            if (salinityPPT >= 35) {
                cf = findCF(tempToSeaDepthCFs, temperatureC);
            } else if (salinityPPT >= 15) {
                cf = findCF(tempToBrackishDepthCFs, temperatureC);
            } else if (salinityPPT >= 0) {
                cf = findCF(tempToFreshDepthCFs, temperatureC);
            }

            return cf;
        }

        float CalculateDepthM(float pressurePSI, uint32_t salinityPPT, float temperatureC)
        {
            const auto cf = GetConversionFactor(salinityPPT, temperatureC);
            
            return (pressurePSI - 14.6959f) * 0.702398f / cf;
        }

    }
}

