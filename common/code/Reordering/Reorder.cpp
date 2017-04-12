// Copyright (c) 2010-2017 Sound Metrics Corp.  All rights reserverd.
//
//

#include "Reorder.h"
#include <assert.h>
#include <cstdint>
#include <cstring>
#include <vector>

namespace Aris {

inline uint32_t PingModeToPingsPerFrame(uint32_t pingMode) {
    if (pingMode == 1) {
        return 3;
    } else if (pingMode == 3) {
        return 6;
    } else if (pingMode == 6) {
        return 4;
    } else if (pingMode == 9) {
        return 8;
    }

    return 0;
}

uint32_t PingModeToNumBeams(uint32_t pingMode) {
    if (pingMode == 1) {
        return 48;
    } else if (pingMode == 3) {
        return 96;
    } else if (pingMode == 6) {
        return 64;
    } else if (pingMode == 9) {
        return 128;
    }

    return 0;
}

void Reorder(ArisFrameHeader & header, uint8_t * samples) {

    assert(samples != nullptr);

    if (header.ReorderedSamples) {
        return;
    }

    const uint32_t samplesPerBeam = header.SamplesPerBeam;
    const uint32_t pingMode = header.PingMode;
    const uint32_t pingsPerFrame = PingModeToPingsPerFrame(pingMode);
    const uint32_t numBeams = PingModeToNumBeams(pingMode);
    const int32_t beamsPerPing = 16;
    const int32_t chRvMap[beamsPerPing] = {10, 2, 14, 6, 8, 0, 12, 4, 11, 3, 15, 7, 9, 1, 13, 5};

    int32_t chRvMultMap[beamsPerPing];

    for (uint32_t chIdx = 0; chIdx < beamsPerPing; ++chIdx) {
        chRvMultMap[chIdx] = chRvMap[chIdx] * pingsPerFrame;
    }

    auto inputBuf = std::vector<uint8_t>(numBeams * samplesPerBeam, 0);
    uint8_t * inputByte = &(inputBuf[0]);
    uint8_t * const outputBuf = samples;

    memcpy(&(inputBuf[0]), outputBuf, numBeams * samplesPerBeam);

    for (uint32_t pingIdx = 0; pingIdx < pingsPerFrame; ++pingIdx) {
        for (uint32_t sampleIdx = 0; sampleIdx < samplesPerBeam; ++sampleIdx) {
            const int32_t composed = sampleIdx * numBeams + pingIdx;
            outputBuf[composed + chRvMultMap[0]] = inputByte [0];
            outputBuf[composed + chRvMultMap[1]] = inputByte [1];
            outputBuf[composed + chRvMultMap[2]] = inputByte [2];
            outputBuf[composed + chRvMultMap[3]] = inputByte [3];
            outputBuf[composed + chRvMultMap[4]] = inputByte [4];
            outputBuf[composed + chRvMultMap[5]] = inputByte [5];
            outputBuf[composed + chRvMultMap[6]] = inputByte [6];
            outputBuf[composed + chRvMultMap[7]] = inputByte [7];
            outputBuf[composed + chRvMultMap[8]] = inputByte [8];
            outputBuf[composed + chRvMultMap[9]] = inputByte [9];
            outputBuf[composed + chRvMultMap[10]] = inputByte[10];
            outputBuf[composed + chRvMultMap[11]] = inputByte[11];
            outputBuf[composed + chRvMultMap[12]] = inputByte[12];
            outputBuf[composed + chRvMultMap[13]] = inputByte[13];
            outputBuf[composed + chRvMultMap[14]] = inputByte[14];
            outputBuf[composed + chRvMultMap[15]] = inputByte[15];
            inputByte += beamsPerPing;
        }
    }

    header.ReorderedSamples = 1;
}

}
