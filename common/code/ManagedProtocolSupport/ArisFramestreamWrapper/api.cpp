// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

#include "stdafx.h"
#include "ListenerWrapper.h"
#include "api.h"
#include <boost/asio.hpp>

extern "C"
INT_PTR __stdcall CreateFrameListener(
  const char * ipAddress, PFN_FRAMECALLBACK frameCallback, /* out */ uint16_t * listenerPort) {

  // This code creates the listener and promptly forgets the pointer.
  // This is unsupported test code and has no clean-up strategy other than exiting the process.
  // Please don't write production code like this!

  const boost::asio::ip::address_v4 targetSonar = boost::asio::ip::address_v4::from_string(ipAddress);

  ListenerWrapper * wrapper = new ListenerWrapper(targetSonar, frameCallback);
  *listenerPort = wrapper->GetListenerPort();
  return wrapper->GetHandle();
}
