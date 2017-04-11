#include "Frame.h"
#include "FrameHeader.h"

Frame::Frame(std::vector<uint8_t> && header, std::vector<uint8_t> && samples)
  : header_(std::move(header))
  , samples_(std::move(samples))
{
}

Frame::~Frame()
{
}
