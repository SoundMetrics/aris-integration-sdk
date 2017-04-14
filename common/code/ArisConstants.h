#pragma once

#include <cstdint>

enum class SonarFrequency : uint32_t { Low = 0, High = 1 };

enum class SystemType : uint32_t {
  Aris1800 = 0,
  Aris3000 = 1,
  Aris1200 = 2,
};

// ARIS broadcasts availability beacons to this port.
constexpr uint16_t kArisBeaconPort = 56124;

// ARIS accepts a TCP connection from a Controller on this port.
constexpr uint16_t kArisCommandPort = 56888;

// ARIS accepts platform header updates on this port.
constexpr uint16_t kArisPlatformHeaderUpdatePort = 700;
