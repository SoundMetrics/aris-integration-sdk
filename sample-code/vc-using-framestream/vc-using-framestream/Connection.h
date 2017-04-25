#pragma once

#include "commands.h"
#include "AcousticSettings.h"
#include "FrameStreamListener.h"
#include "UdpListener.h"
#include <memory>
#include <boost/asio.hpp>

namespace aris { class Command; }

class Connection
{
public:
  // Creates the Connection object with a live connection to the ARIS.
  // Returns an unassociated unique_ptr if the connection could not be made.
  static std::unique_ptr<Connection> Create(
    boost::asio::io_service & io, 
    std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion,
    SystemType systemType,
    aris::Command::SetSalinity::Salinity salinity,
    boost::asio::ip::tcp::endpoint targetSonar,
    const Aris::Network::optional<boost::asio::ip::udp::endpoint> & multicastEndpoint,
    float initialFocusRange,
    std::string & errorMessage);

  // Use Create() rather than the ctor. Caller gives up ownership of commandSocket.
  Connection(
    boost::asio::io_service & io,
    boost::asio::ip::tcp::socket && commandSocket,
    std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion,
    SystemType systemType,
    aris::Command::SetSalinity::Salinity salinity,
    boost::asio::ip::address targetSonar,
    const Aris::Network::optional<boost::asio::ip::udp::endpoint> & multicastEndpoint,
    float initialFocusRange);
  ~Connection();

  void SendCommand(const aris::Command & cmd);

  // This sample doesn't provide a means of re-connecting if the TCP command
  // connection goes down for some reason. This will tell you if such an
  // error has occurred.
  bool HasConnectionError() const { return hasConnectionError_; }

  auto GetMetrics() { return frameStreamListener_.GetMetrics(); }

private:
  const std::vector<uint8_t> ping_template_;
  const std::function<void(const boost::system::error_code&)> sendPing_;
  const std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion_;
  const SystemType systemType_;

  bool hasConnectionError_;
  CookieSequence cookie_;
  aris::Command::SetSalinity::Salinity salinity_;
  boost::asio::io_service & io_;
  boost::asio::ip::tcp::socket commandSocket_;
  boost::asio::deadline_timer ping_timer_;
  Aris::Network::FrameStreamListener frameStreamListener_;

  void HandlePingTimer(const boost::system::error_code& e);
  AcousticSettings SetCookie(const AcousticSettings & settings);

  static std::vector<uint8_t> CreatePingTemplate();
  void SerializeCommand(const aris::Command & cmd, std::vector<uint8_t> & buffer);
};
