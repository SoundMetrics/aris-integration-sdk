#pragma once

#include "availability.h"
#include <boost/asio.hpp>

// This type provides a way to monitor ARIS beacons. At present there's no callback facility
// to receive each beacon, but you could add one. (This is an asynchronous service, beware
// shared mutable state.)
//
// This also provides a means to find an IP address (endpoint) for the serial number you
// seek.
class ArisBeacons
{
public:
  typedef boost::asio::ip::basic_endpoint<boost::asio::ip::udp> endpoint;

  ArisBeacons() = delete;

  // Blocks until it finds it's target. You could probably add a timeout.
  // Returns endpoint and system type.
  static std::pair<endpoint, uint32_t> FindBySerialNumber(uint32_t serialNumber);
};

