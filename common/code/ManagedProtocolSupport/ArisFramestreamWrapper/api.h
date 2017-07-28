// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

#pragma once

#include <cstdint>

typedef void(_stdcall *PFN_FRAMECALLBACK)(INT_PTR hListener, const uint8_t * header, uint32_t headerSize);

extern "C"
INT_PTR __stdcall CreateFrameListener(
  const char * ipAddress, PFN_FRAMECALLBACK frameCallback, /* out */ uint16_t * listenerPort);
