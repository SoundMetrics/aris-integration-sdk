#include "Frame.h"
#include "Reorder.h"
#include "FrameHeader.h"
#include <assert.h>

Frame::Frame(std::vector<uint8_t> && header, std::vector<uint8_t> && samples)
  : header_(std::move(header))
  , samples_(std::move(samples))
{
  // The true header is (sizeof ArisFrameHeader) bytes long but is transmitted
  // at it's minimum size over the network, which is less than (sizeof ArisFrameHeader).
  // Zero-extend the header to the correct size.
  header_.resize(sizeof ArisFrameHeader, 0);

  Aris::Reorder(Header(), samples_.data());

  // Express the invariant.
  assert(header_.size() == sizeof ArisFrameHeader);
}
