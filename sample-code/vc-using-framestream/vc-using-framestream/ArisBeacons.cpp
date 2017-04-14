#include "ArisBeacons.h"
#include "ArisConstants.h"
#include "UdpListener.h"
#include <iostream>

/* static */
std::pair<ArisBeacons::endpoint, SystemType> ArisBeacons::FindBySerialNumber(uint32_t serialNumber)
{
  boost::asio::io_service io;

  volatile bool found = false;
  endpoint targetEndpoint; // assignment on the endpoint type doesn't like volatile
  SystemType systemType;

  UdpListener udpListener(io, kArisBeaconPort, true, 1024,
    [&targetEndpoint, &systemType, &found, serialNumber](auto error, auto ep, auto buffer, auto bufferSize) {

      if (error.value()) {
        return;
      }

      aris::Availability message;

      if (message.ParseFromArray(buffer, bufferSize) && message.has_serialnumber()) {
        if (message.serialnumber() == serialNumber) {
          targetEndpoint = ep;
          systemType = static_cast<SystemType>(message.systemtype());
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
