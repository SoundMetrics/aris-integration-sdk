// Copyright (c) 2010-2016 Sound Metrics Corp.  All rights reserverd.
//
//

#pragma once

#include "Frame.h"

namespace Aris {

void Reorder(Frame & frame);

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

inline uint32_t PingModeToNumBeams(uint32_t pingMode) {
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

}
