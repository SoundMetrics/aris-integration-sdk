// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

#include "stdafx.h"
#include "ListenerWrapper.h"

static void run_thread(boost::asio::io_service & io) {
  while (true) {
    io.run();
  }
  // this never returns.
}

ListenerWrapper::ListenerWrapper(boost::asio::ip::address targetSonar, PFN_FRAMECALLBACK frameCallback)
{
  // construct everything but the io **after** the io is constructed.

  boost::asio::io_service & io = io_;

  thread_ = std::make_unique<boost::thread>([&io]() { run_thread(io); });
  listener_ = std::make_unique<Aris::Network::FrameStreamListener>(io,
    [frameCallback, this](Aris::Network::FrameBuilder & builder) { // onFrameComplete
      auto header = std::move(builder.TakeHeader());
      header.resize(1024); // fill out to the full header size
      frameCallback(GetHandle(), &header[0], header.size());
    },
    []() { return 4 * 1024 * 1024; }, // getReadBufferSize
    targetSonar,
    Aris::Network::optional<boost::asio::ip::udp::endpoint>::none()
    );
}
