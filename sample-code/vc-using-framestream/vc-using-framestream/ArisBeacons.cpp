#include "ArisBeacons.h"
#include "UdpListener.h"
#include <iostream>

constexpr uint16_t kArisBeaconPort = 56124;

/* static */
std::pair<ArisBeacons::endpoint, uint32_t> ArisBeacons::FindBySerialNumber(uint32_t serialNumber)
{
  boost::asio::io_service io;

  endpoint targetEndpoint; // assignment on the endpoint type doesn't like volatile
  uint32_t systemType;
  volatile bool found = false;

  UdpListener udpListener(io, kArisBeaconPort, true, 1024,
    [&targetEndpoint, &systemType, &found, serialNumber](auto error, auto ep, auto buffer, auto bufferSize) {

      if (error.value()) {
        return;
      }

      aris::Availability message;

      if (message.ParseFromArray(buffer, bufferSize) && message.has_serialnumber()) {
        if (message.serialnumber() == serialNumber) {
          targetEndpoint = ep;
          systemType = message.systemtype();
          found = true;
        }
      }
  });

  while (!found) {
    io.run_one();
    io.reset();
  }

  return std::make_pair(targetEndpoint, systemType);
}
