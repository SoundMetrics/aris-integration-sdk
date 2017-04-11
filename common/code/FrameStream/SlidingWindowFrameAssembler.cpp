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

#include "SlidingWindowFrameAssembler.h"
#include "frame_stream.h"
#include <boost/asio/buffer.hpp>
#include <functional>

namespace {
// Execute the lambda expression at the end of the scope.
class scoped_guard : boost::noncopyable
{
    std::function<void()> lambda;

public:
    scoped_guard(std::function<void()> action)
        : lambda(action)
    {
    }

    ~scoped_guard()
    {
        try {
            lambda();
        }
        catch (...) {
            // Ignore.
        }
    }
};
}

namespace Aris {
namespace Network {

using namespace boost;
using namespace boost::asio;

namespace {
SlidingWindowFrameAssembler::Metrics
operator+(const SlidingWindowFrameAssembler::Metrics &a,
          const SlidingWindowFrameAssembler::Metrics &b) {
  auto m = a;
  m.uniqueFrameIndexCount += b.uniqueFrameIndexCount;
  m.finishedFrameCount += b.finishedFrameCount;
  m.completeFrameCount += b.completeFrameCount;
  m.skippedFrameCount += b.skippedFrameCount;
  m.totalExpectedFrameSize += b.totalExpectedFrameSize;
  m.totalReceivedFrameSize += b.totalReceivedFrameSize;
  m.totalPacketsReceived += b.totalPacketsReceived;
  m.totalPacketsAccepted += b.totalPacketsAccepted;
  m.totalPacketsIgnored += b.totalPacketsIgnored;
  m.invalidPacketCount += b.invalidPacketCount;

  return m;
}
}

SlidingWindowFrameAssembler::SlidingWindowFrameAssembler(
    std::function<void(int, int)> sendAck,
    std::function<void(FrameBuilder &)> onFrameFinished)
    : sendAck(sendAck), onFrameFinished(onFrameFinished), currentFrameIndex(-1),
      lastFinishedFrameIndex(-1), expectedDataOffset(0) {
  Metrics emptyMetrics = {};
  metrics = emptyMetrics;
}

void SlidingWindowFrameAssembler::ProcessPacket(const_buffer data) {

  bool acceptedPacket = false, invalidPacket = false;
  uint32_t skippedFrameCount = 0;
  scoped_guard updateMetrics([&]() {
    Metrics update = {};
    update.skippedFrameCount = skippedFrameCount;
    update.totalPacketsReceived = 1;
    update.totalPacketsAccepted = acceptedPacket ? 1 : 0;
    update.totalPacketsIgnored = acceptedPacket ? 0 : 1;
    update.invalidPacketCount = invalidPacket ? 1 : 0;
    UpdateMetrics(update);
  });

  recursive_mutex::scoped_lock lock(stateGuard);

  frame_stream::FramePart framePart;
  if (!framePart.ParseFromArray(buffer_cast<const uint8_t *>(data),
                                buffer_size(data))) {
    invalidPacket = true;
    // TODO log
    return;
  }

  const int incomingFrameIndex = framePart.frame_index();
  const int incomingDataOffset = framePart.data_offset();

  if (incomingFrameIndex > currentFrameIndex) {
    // sender moved on to the next frame
    Flush();
    skippedFrameCount = incomingFrameIndex - currentFrameIndex - 1;
    currentFrameIndex = incomingFrameIndex;
    expectedDataOffset = 0;
  } else if (incomingFrameIndex <= lastFinishedFrameIndex) {
    return; // duplicate packet from finished frame
  }

  if (!currentFrame) {
    if (incomingDataOffset == 0) {
      currentFrame = std::unique_ptr<FrameBuilder>(new FrameBuilder(
          incomingFrameIndex,
          buffer(framePart.header().c_str(), framePart.header().size()),
          buffer(framePart.data().c_str(), framePart.data().size()),
          framePart.total_data_size()));
      expectedDataOffset = framePart.data().size();
      acceptedPacket = true;
    } else {
      // Ack will go out asking for the first part of the frame to be resent.
    }
  } else {
    if (incomingDataOffset == expectedDataOffset) {
      currentFrame->AppendFrameData(
          incomingDataOffset,
          buffer(framePart.data().c_str(), framePart.data().size()));
      expectedDataOffset += framePart.data().size();
      acceptedPacket = true;
    } else {
      // Missed a part.
    }
  }

  // NOTE: we're always acking each packet for now; this should change when we
  // develop strategies for retrying packets.
  sendAck(incomingFrameIndex, expectedDataOffset);

  if (expectedDataOffset == framePart.total_data_size())
    Flush();
}

void SlidingWindowFrameAssembler::Flush() {
  recursive_mutex::scoped_lock lock(stateGuard);

  if (currentFrame) {
    std::unique_ptr<FrameBuilder> frame;
    frame.swap(currentFrame);
    assert(!currentFrame);

    UpdateMetrics(GetMetricsForFinishedFrame(*frame));

    lastFinishedFrameIndex = frame->FrameIndex();
    onFrameFinished(*frame);
  }
}

SlidingWindowFrameAssembler::Metrics SlidingWindowFrameAssembler::GetMetrics() {
  recursive_mutex::scoped_lock lock(metricsGuard);
  return metrics;
}

void SlidingWindowFrameAssembler::UpdateMetrics(const Metrics &update) {
  recursive_mutex::scoped_lock lock(metricsGuard);
  metrics = metrics + update;
}

/* static */
SlidingWindowFrameAssembler::Metrics
SlidingWindowFrameAssembler::GetMetricsForFinishedFrame(
    const FrameBuilder &frame) {
  Metrics metrics = {};
  metrics.uniqueFrameIndexCount = 1;
  metrics.finishedFrameCount = 1;
  metrics.completeFrameCount = frame.IsComplete() ? 1 : 0;
  metrics.totalExpectedFrameSize = frame.ExpectedSize();
  metrics.totalReceivedFrameSize = frame.BytesReceived();

  return metrics;
}
}
}
