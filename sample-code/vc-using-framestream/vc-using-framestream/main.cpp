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

  //---------------------------------------------------------------------------
  // Here we look up a sonar's IP address (endpoint) by serial number.
  // This relies on waiting for a beacon from the sonar of interest.

  const auto SN = 24u;
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
    [&writingFile, &file](Aris::Network::FrameBuilder& fb) {
    // Log every tenth frame to stdout
    if ((fb.FrameIndex() % 10) == 0) {
      std::cout << "Frame " << fb.FrameIndex() << '\n';
    }

    // Write the .aris file
    if (writingFile) {
      const Frame frame(fb.TakeHeader(), fb.TakeFrameData());
      if (writingFile) {
        writingFile = file->WriteFrame(frame);
      }
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

  wire_ctrlc_handler();
  io.reset();
  io.run();

  return 0;
}
