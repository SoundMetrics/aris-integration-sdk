// The MIT License (MIT)
// 
// Copyright (c) 2013-2014 Sound Metrics Corporation. All Rights Reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#include "FrameBuilder.h"
#include <assert.h>

namespace Aris {
namespace Network {

using namespace boost::asio;

struct bad_frame_builder_inputs_exception : std::exception {};

FrameBuilder::FrameBuilder(int frameIndex, const_buffer header,
                           const_buffer firstDataFragment, size_t totalDataSize)
    : frameIndex(frameIndex), totalDataSize(totalDataSize),
      header(buffer_cast<const uint8_t *>(header),
             buffer_cast<const uint8_t *>(header) + buffer_size(header)),
      data(totalDataSize), dataReceived(0) {
  ValidateInputs();
  AppendFrameData(0, firstDataFragment);
}

FrameBuilder::FrameBuilder(FrameBuilder &&other)
    : frameIndex(other.frameIndex), totalDataSize(other.totalDataSize),
      header(move(other.header)), data(move(other.data)),
      dataReceived(other.dataReceived) {
  ClearOtherInstance(other);
}

FrameBuilder &FrameBuilder::operator=(FrameBuilder &&other) {
  frameIndex = other.frameIndex;
  totalDataSize = other.totalDataSize;
  dataReceived = other.dataReceived;
  header = move(other.header);
  data = move(other.data);

  ClearOtherInstance(other);
  return *this;
}

void FrameBuilder::ClearOtherInstance(FrameBuilder &other) {
  other.frameIndex = -1;
  other.totalDataSize = 0;
  other.dataReceived = 0;
}

void FrameBuilder::ValidateInputs() {
  if (frameIndex < 0 || header.size() == 0)
    throw bad_frame_builder_inputs_exception();
}

void FrameBuilder::AppendFrameData(size_t dataOffset,
                                   const_buffer dataFragment) {
  // The sliding window code elsewhere deals with whether we succeed or
  // fail on missing data; here we just store it away.

  auto target =
      mutable_buffer(data.data() + dataOffset, data.size() - dataOffset);
  dataReceived += buffer_copy(target, dataFragment);
}
}
}
