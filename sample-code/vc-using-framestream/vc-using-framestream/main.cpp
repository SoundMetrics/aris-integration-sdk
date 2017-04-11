// vc-using-framestream.cpp : Defines the entry point for the console application.
//

#include "ArisBeacons.h"
#include "args.h"
#include "Connection.h"
#include <iostream>

int main(int argc, char **argv) {

  //---------------------------------------------------------------------------
  // Validate and retrieve command line arguments.

  auto argParseResult = parseArgs(argc, argv);

  if (!argParseResult.success) {
    std::cerr << "error: " << argParseResult.errorMessage << '\n';
    return -1;
  }

  const auto & args = argParseResult.args;

  std::cout << "Target SN: " << args.serialNumber << '\n';
  if (args.useBroadcast.has_value()) {
    std::cout << "  (broadcast setting)" << '\n';
  }

  // If you were using ArisBeacons to collect beacon info you'd need to pump
  // the io_service with either run() or poll()/reset().
  //ArisBeacons beacons(io);
  //io.run();

  //---------------------------------------------------------------------------
  // Here we look up a sonar's IP address (endpoint) by serial number.
  // This relies on waiting for a beacon from the sonar of interest.

  const auto SN = 24u;
  ArisBeacons::endpoint targetEndpoint;
  uint32_t systemType;

  std::tie(targetEndpoint, systemType) = ArisBeacons::FindBySerialNumber(SN);
  std::cout << "  Found target endpoint for SN " << SN << ": " << targetEndpoint.address().to_string() << '\n';

  //---------------------------------------------------------------------------
  // Boost's io_service drives callback handlers and whatnot. The network calls
  // used in this program rely on it.

  boost::asio::io_service io;

  constexpr uint16_t kArisCommandPort = 56888;
  const auto tcpEndpoint = boost::asio::ip::tcp::endpoint(targetEndpoint.address(), kArisCommandPort);
  std::string errorMessage;
  const auto salinity = aris::Command::SetSalinity::Salinity::Command_SetSalinity_Salinity_SALTWATER;
  const float initialFocusRange = 5.0f;

  std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion =
    [](Aris::Network::FrameBuilder& fb) {
    if ((fb.FrameIndex() % 10) == 0) {
      std::cout << "Frame " << fb.FrameIndex() << '\n';
    }
  };

  auto connection = std::move(
    Connection::Create(io, tcpEndpoint, onFrameCompletion, systemType, salinity,
      initialFocusRange, errorMessage));

  if (!connection) {
    std::cerr << "*** No connection could be made: '" << errorMessage << "'." << '\n';
    std::cerr << "*** ep: " << tcpEndpoint.address().to_string() << " port " << tcpEndpoint.port() << '\n';
    return -1;
  }

  io.reset();
  io.run();

  return 0;
}
