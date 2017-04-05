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

#include "FrameBuilder.h"
#include <boost/function.hpp>
#include <boost/thread/recursive_mutex.hpp>

namespace Aris {
namespace Network {

// Assembles frames from multiple packets. The term "finished" means we've
// stopped assembling packets; the frame may yet be incomplete. The term
// "complete" means we've received all the packets for the frame.
class SlidingWindowFrameAssembler : boost::noncopyable {
public:
  SlidingWindowFrameAssembler(
      boost::function<void(int, int)> sendAck,
      boost::function<void(FrameBuilder &)> onFrameFinished);

  void ProcessPacket(boost::asio::const_buffer data);
  void Flush();

  struct Metrics {
    uint32_t uniqueFrameIndexCount;  // # of frame indexes seen
    uint32_t finishedFrameCount;     // # of frames finished (not necessarily
                                     // complete)
    uint32_t completeFrameCount;     // # of complete frames finished
    uint32_t skippedFrameCount;      // # of frame indexes skipped
    uint64_t totalExpectedFrameSize; // expected size of frame bytes
    uint64_t totalReceivedFrameSize; // actual size of frame bytes
    uint64_t invalidPacketCount;     // # of invalid (unparsable) packets
    uint64_t totalPacketsReceived;   // # of packets received
    uint64_t totalPacketsAccepted;   // # of packets accepted (in sequence)
    uint64_t totalPacketsIgnored;    // # of packets ignored
  };

  Metrics GetMetrics();

private:
  const boost::function<void(int, int)> sendAck;
  const boost::function<void(FrameBuilder &)> onFrameFinished;

  boost::recursive_mutex stateGuard, metricsGuard;
  int currentFrameIndex, lastFinishedFrameIndex;
  int expectedDataOffset;
  std::unique_ptr<FrameBuilder> currentFrame;
  Metrics metrics;

  void UpdateMetrics(const Metrics &update);
  static Metrics GetMetricsForFinishedFrame(const FrameBuilder &frame);
};
}
}
