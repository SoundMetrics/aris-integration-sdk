// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

#pragma once

#include "api.h"
#include "FrameStreamListener.h"
#include <boost/asio.hpp>
#include <boost/thread.hpp>
#include <memory>

// This type contains the various parts needed to run an independent
// listener.
class ListenerWrapper
{
public:
  ListenerWrapper(boost::asio::ip::address targetSonar, PFN_FRAMECALLBACK frameCallback);

  ListenerWrapper(const ListenerWrapper &) = delete;
  ListenerWrapper(ListenerWrapper &&) = delete;

  INT_PTR GetHandle() const { return reinterpret_cast<INT_PTR>(this); }

  uint16_t GetListenerPort() const {
    return listener_->LocalEndpoint().port();
  }

private:
  boost::asio::io_service io_; // must be the first constructed, so first in class order.
  std::unique_ptr<boost::thread> thread_;
  std::unique_ptr<Aris::Network::FrameStreamListener> listener_;
};

