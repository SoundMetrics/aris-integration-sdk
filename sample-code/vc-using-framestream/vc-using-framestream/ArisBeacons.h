#pragma once

#include "ArisBasics.h"
#include "availability.h"
#include <boost/asio.hpp>

// This type provides a means to find an IP address (endpoint) for the serial number you
// seek.
class ArisBeacons
{
public:
  typedef boost::asio::ip::basic_endpoint<boost::asio::ip::udp> endpoint;

  ArisBeacons() = delete;

  // Blocks until it finds it's target. You could probably add a timeout.
  // Returns endpoint and system type.
  static std::pair<endpoint, Aris::Common::SystemType> FindBySerialNumber(uint32_t serialNumber);
};

