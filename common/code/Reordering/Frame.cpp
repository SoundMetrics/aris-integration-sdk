// Copyright (c) 2010-2016 Sound Metrics Corp.  All rights reserverd.
//
//

#include "Frame.h"
#include <algorithm>

namespace Aris {

Frame::Frame(const uint8_t * buffer, const size_t size)
: _frameData(buffer + kFrameHeaderSize, buffer + size) {
    std::copy(buffer, buffer + kFrameHeaderSize, (uint8_t *)&_frameHeader);
}

}
