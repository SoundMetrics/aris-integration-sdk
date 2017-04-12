// Copyright (c) 2010-2017 Sound Metrics Corp.  All rights reserverd.
//
//

#include "Frame.h"
#include <cstring>

namespace Aris {

Frame::Frame(const uint8_t * buffer, const size_t size)
: _frameData(buffer + kFrameHeaderSize, buffer + size) {
    memcpy(&_frameHeader, buffer, kFrameHeaderSize);
}

}
