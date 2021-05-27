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

#include "FrameStreamListener.h"
#include "frame_stream.h"
#include <boost/asio/buffer.hpp>
#include <vector>

namespace Aris {
namespace Network {

using namespace boost::asio;
using namespace boost::asio::ip;

FrameStreamListener::FrameStreamListener(
    io_service &io
    , std::function<void(FrameBuilder &)> onFrameComplete
    , std::function<size_t()> getReadBufferSize
    , address targetSonar
    , optional<udp::endpoint> receiveFrom
    )
    : socket(io), readBuffer(getReadBufferSize()), sonarFilter(targetSonar),
      frameAssembler(
        [this](int frameIndex, int dataOffset) { SendAck(frameIndex, dataOffset); },
        onFrameComplete) {
  assert(onFrameComplete);
  assert(getReadBufferSize);

  socket.open(udp::v4());

  const bool useMulticast =
    receiveFrom.has_value() ? receiveFrom.value().address().is_multicast() : false;
  const auto bindEndpoint =
    useMulticast ? udp::endpoint(udp::v4(), receiveFrom.value().port()) : udp::endpoint(udp::v4(), 0);

  socket.set_option(socket_base::reuse_address(true));
  socket.set_option(socket_base::receive_buffer_size(static_cast<int>(readBuffer.size())));
  socket.bind(bindEndpoint);

  if (useMulticast) {
    socket.set_option(multicast::join_group(receiveFrom.value().address()));
  }

  StartReceiveAsync();
}

FrameStreamListener::~FrameStreamListener() {
  // See SendAck for a discussion of closing the socket.
  std::lock_guard<std::mutex> guard(socketMutex);
  socket.close();
}

void FrameStreamListener::StartReceiveAsync() {
  socket.async_receive_from(
      buffer(readBuffer.data(), readBuffer.size()), remoteEndpoint,
      socket_base::message_flags(0),
      [this](const boost::system::error_code& error, size_t bytesRead) {
        HandlePacketFrom(error, bytesRead);
      });
}

void
FrameStreamListener::HandlePacketFrom(const boost::system::error_code &error,
                                      size_t bytesRead) {
  switch (error.value()) {
  case boost::system::errc::success: {
    if (!sonarFilter.has_value() || remoteEndpoint.address() == sonarFilter.value()) {
      frameAssembler.ProcessPacket(const_buffers_1(readBuffer.data(), bytesRead));
    }
    StartReceiveAsync();
  } break;

  // Other results indicate failure or we're shutting down.
  case boost::system::errc::operation_canceled: {
    // Timer cancelled
  } break;

  default: { // Happens on normal shutdown
  } break;
  }
}

void FrameStreamListener::SendAck(int frameIndex, int dataOffset) {
  frame_stream::FramePartAck ack;
  ack.set_frame_index(frameIndex);
  ack.set_data_offset(dataOffset);

  std::vector<uint8_t> buf(ack.ByteSizeLong());
  ack.SerializeToArray(buf.data(), static_cast<int>(buf.size()));

  // BUG: found in rev 7046 via Windows Error Reporting.
  // If the socket is closed before we send the ack we suffer from
  // the classic race condition and an exception is thrown (the socket
  // is closed). So we use a mutex to guard use of the socket, allowing
  // us to both send and close successfully, but not concurrently.
  //
  // PERF: it would be nice if there were a std::spinlock as there is zero
  // contention on this until we actually close the socket.
  std::lock_guard<std::mutex> guard(socketMutex);
  if (socket.is_open()) {
    socket.send_to(buffer(buf.data(), buf.size()), remoteEndpoint);
  }
}
}
}
