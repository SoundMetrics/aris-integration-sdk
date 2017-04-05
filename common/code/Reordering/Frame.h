// Copyright (c) 2010-2017 Sound Metrics Corp.  All rights reserverd.
//
//

#pragma once

#include "FrameHeader.h"
#include <cstdlib>
#include <vector>

namespace Aris {

static const uint32_t kFrameHeaderSize = 1024;

class Frame {
public:
    Frame(const uint8_t * buffer, const size_t size);
    inline ArisFrameHeader & GetHeader() { return _frameHeader; }
    inline size_t GetDataSize() { return _frameData.size(); }
    inline uint8_t * GetData() { return &_frameData[0]; }
    Frame() = delete;
    Frame(const Frame &) = delete;
    Frame & operator = (const Frame &) = delete;
private:
    ArisFrameHeader _frameHeader;
    std::vector<uint8_t> _frameData;
};

}
