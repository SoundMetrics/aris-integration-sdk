// The MIT License (MIT)
// 
// Copyright (c) 2013-2017 Sound Metrics Corporation. All Rights Reserved.
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

#pragma once

#include <boost/asio/buffer.hpp>
#include <boost/utility.hpp>
#include <vector>
#include <stdint.h>

namespace Aris {
namespace Network {

class FrameBuilder : boost::noncopyable {
public:
  FrameBuilder(int frameIndex, boost::asio::const_buffer header,
               boost::asio::const_buffer firstDataFragment,
               size_t totalDataSize);
  FrameBuilder(FrameBuilder &&other);

  FrameBuilder &operator=(FrameBuilder &&other);

  int FrameIndex() const { return frameIndex; }
  bool IsComplete() const { return dataReceived == totalDataSize; }
  size_t BytesReceived() const { return dataReceived; }
  size_t ExpectedSize() const { return totalDataSize; }

  void AppendFrameData(size_t dataOffset,
                       boost::asio::const_buffer dataFragment);

  const std::vector<uint8_t> &Header() const { return header; }
  std::vector<uint8_t> &&TakeHeader() { return std::move(header); }

  const std::vector<uint8_t> &FrameData() const { return data; }
  std::vector<uint8_t> &&TakeFrameData() { return std::move(data); }

private:
  int frameIndex;
  size_t totalDataSize;
  size_t dataReceived;

  std::vector<uint8_t> header;
  std::vector<uint8_t> data;

  void ClearOtherInstance(FrameBuilder &other);
  void ValidateInputs();

  // Unavailable:
  FrameBuilder(const FrameBuilder &);
  FrameBuilder &operator=(const FrameBuilder &);
};
}
}
