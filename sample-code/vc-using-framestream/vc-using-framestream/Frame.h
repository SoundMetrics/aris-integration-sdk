#pragma once

#include "FrameHeader.h"
#include <vector>

class Frame
{
public:
  Frame(std::vector<uint8_t> && header, std::vector<uint8_t> && samples);

  const ArisFrameHeader & Header() const {
    return *reinterpret_cast<const ArisFrameHeader*>(header_.data());
  }

  const std::vector<uint8_t> & Samples() const {
    return samples_;
  }

private:
  std::vector<uint8_t> header_, samples_;

  // Modification allowed only internal to this class.
  ArisFrameHeader & Header() {
    return *reinterpret_cast<ArisFrameHeader*>(header_.data());
  }
};

