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

#include "SlidingWindowFrameAssembler.h"
#include <boost/asio.hpp>
#include <mutex>
#include <vector>

namespace Aris {
namespace Network {

class FrameBuilder;

// Helper class. optional<T> is in the C++17 standard, but may not be
// available on an integrator's toolchain. This is not intended to be
// functionally identical to std::optional.
template <typename T>
class optional {
public:
  optional() : hasValue_(false), value_{} { }
  optional(const T &value) : hasValue_(true), value_(value) { }

  bool has_value() const { return hasValue_; }
  const T& value() const { return value_; }

  static optional<T> none() { return optional<T>(); }

private:
  bool hasValue_;
  T value_;
};

class FrameStreamListener : boost::noncopyable {
public:
  FrameStreamListener(boost::asio::io_service &io
                      , std::function<void(FrameBuilder &)> onFrameComplete
                      , std::function<size_t()> getReadBufferSize // network buffer size
                      , boost::asio::ip::address targetSonar
                      // Most applications do not need to specify the receiveFrom
                      // endpoint. This is primarily used for multicasting where
                      // the multicast group and port number must be decided ahead
                      // of time. Otherwise, pass an empty optional<>().
                      , optional<boost::asio::ip::udp::endpoint> receiveFrom
                      );
  ~FrameStreamListener();

  boost::asio::ip::udp::endpoint LocalEndpoint() const {
    return socket.local_endpoint();
  }

  typedef SlidingWindowFrameAssembler::Metrics Metrics;
  Metrics GetMetrics() { return frameAssembler.GetMetrics(); }

private:
  const optional<boost::asio::ip::address> sonarFilter;
  std::vector<uint8_t> readBuffer;
  boost::asio::ip::udp::socket socket;
  boost::asio::ip::udp::endpoint remoteEndpoint;
  SlidingWindowFrameAssembler frameAssembler;
  std::mutex socketMutex;

  void StartReceiveAsync();
  void HandlePacketFrom(const boost::system::error_code &error,
                        size_t bytesRead);
  void SendAck(int frameIndex, int dataOffset);
};
}
}
