// vc-using-framestream.cpp : Defines the entry point for the console application.
//

#include "ArisBasics.h"
#include "ArisBeacons.h"
#include "args.h"
#include "Connection.h"
#include "ArisRecording.h"
#include "Frame.h"
#include <iostream>
#include <stdlib.h>

//---------------------------------------------------------------------------
// Boost's io_service drives callback handlers and whatnot. The network calls
// used in this program rely on it. It is passed by reference to functions
// that need it, and is outside the scope of main() only so crtc_handler()
// can stop it when the user wishes to exit.

static boost::asio::io_service io;

//---------------------------------------------------------------------------
// Windows-specific handling for Ctrl-C, Ctrl-Break, etc.

BOOL WINAPI ctrlc_handler(DWORD /*dwCtrlType*/) {
  io.stop();
  constexpr DWORD kWaitForCleanupMs = 1000;
  Sleep(kWaitForCleanupMs);
  return TRUE;
}

void wire_ctrlc_handler() {

  // This is Windows-specific. For *nix-related OSes, see
  // http://stackoverflow.com/questions/1641182/how-can-i-catch-a-ctrl-c-event-c

  SetConsoleCtrlHandler(ctrlc_handler, TRUE);
}

//---------------------------------------------------------------------------

void logFrameStreamMetrics(Connection &);

int main(int argc, char **argv) {

  //---------------------------------------------------------------------------
  // Validate and retrieve command line arguments.

  auto argParseResult = parseArgs(argc, argv);

  if (!argParseResult.success) {
    std::cerr << "error: " << argParseResult.errorMessage << '\n';
    return -1;
  }

  const auto & args = argParseResult.args;

  // When multicasting, the cooperating applications will likely want to
  // choose a receive port in advance so the non-controlling application
  // knows where to listen for packets.
  const bool useMulticast = args.useMulticast.has_value() && args.useMulticast.value();
  const auto receiveFromAddr =
    useMulticast
    ? boost::asio::ip::udp::endpoint(boost::asio::ip::address::from_string("239.0.0.42"), 59595)
    : Aris::Network::optional<boost::asio::ip::udp::endpoint>();

  std::cout << "Target SN: " << args.serialNumber << '\n';
  if (useMulticast) {
    std::cout << "  (multicast to " << receiveFromAddr.value().address().to_string()
      << " port " << receiveFromAddr.value().port() << ")\n";
  }

  //---------------------------------------------------------------------------
  // Here we look up a sonar's IP address (endpoint) by serial number.
  // This relies on waiting for a beacon from the sonar of interest.

  const auto SN = args.serialNumber;
  ArisBeacons::endpoint targetEndpoint;
  SystemType systemType;

  std::tie(targetEndpoint, systemType) = ArisBeacons::FindBySerialNumber(SN);
  std::cout << "  Found target endpoint for SN " << SN << ": " << targetEndpoint.address().to_string() << '\n';

  //---------------------------------------------------------------------------

  const auto tcpEndpoint = boost::asio::ip::tcp::endpoint(targetEndpoint.address(), kArisCommandPort);
  std::string errorMessage;
  const auto salinity = aris::Command::SetSalinity::Salinity::Command_SetSalinity_Salinity_SALTWATER;
  const float initialFocusRange = 5.0f;

  auto file = std::move(WriteableArisRecording::Create(".\\output.aris"));
  auto writingFile = true;

  std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion =
    [&writingFile, &file](Aris::Network::FrameBuilder & fb) {

    std::cout << (fb.IsComplete() ? "+" : "-");

    // Write the .aris file
    if (writingFile && fb.IsComplete()) {
      const Frame frame(fb.TakeHeader(), fb.TakeFrameData());
      writingFile = file->WriteFrame(frame);
    }
  };

  auto connection = std::move(
    Connection::Create(io, onFrameCompletion, systemType, salinity,
      tcpEndpoint, receiveFromAddr, initialFocusRange, errorMessage));

  if (!connection) {
    std::cerr << '\n';
    std::cerr << "*** No connection could be made: '" << errorMessage << "'." << '\n';
    std::cerr << "*** target sonar: " << tcpEndpoint.address().to_string() << '\n';

    if (useMulticast) {
      std::cerr << "*** multicast: " << receiveFromAddr.value().address().to_string() << " port " << receiveFromAddr.value().port() << '\n';
    }

    return -1;
  }

  wire_ctrlc_handler();
  io.reset();
  io.run();

  logFrameStreamMetrics(*connection);

  return 0;
}

void logFrameStreamMetrics(Connection & connection) {

  const auto metrics = connection.GetMetrics();

  const auto droppedFrames = metrics.finishedFrameCount - metrics.completeFrameCount;
  const auto droppedFramesRatio = (double)droppedFrames / metrics.finishedFrameCount;
  const auto acceptedPacketsRatio = (double)metrics.totalPacketsAccepted / metrics.totalPacketsReceived;

  std::cout
    << '\n'
    << "frame stream: unique frame indices: " << metrics.uniqueFrameIndexCount
    << "; finished frames: " << metrics.finishedFrameCount
    << "; complete frames: " << metrics.completeFrameCount
    << "; skipped frames: " << metrics.skippedFrameCount
    << "; dropped frames: " << droppedFrames << " (" << (100.0 * droppedFramesRatio) << " %)"
    << "; packets received: " << metrics.totalPacketsReceived
    << "; packets accepted: " << metrics.totalPacketsAccepted << " (" << (100.0 * acceptedPacketsRatio) << " %)"
    << "; packets ignored: " << metrics.totalPacketsIgnored
    << "; invalid packets: " << metrics.invalidPacketCount
    << '\n'
    ;
}
