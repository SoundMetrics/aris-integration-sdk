// Copyright (c) 2010-2017 Sound Metrics Corp.  All rights reserverd.
//
//

#pragma once

#include "FrameHeader.h"

namespace Aris {

void Reorder(ArisFrameHeader & header,  uint8_t * samples);

uint32_t PingModeToNumBeams(uint32_t pingMode);
uint32_t PingModeToPingsPerFrame(uint32_t pingMode); // Not generally needed.

}
