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
    const boost::asio::ip::tcp::endpoint & ep,
    std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion,
    uint32_t systemType,
    aris::Command::SetSalinity::Salinity salinity,
    float initialFocusRange,
    std::string & errorMessage);

  // Use Create() rather than the ctor. Caller gives up ownership of commandSocket.
  Connection(
    boost::asio::io_service & io,
    boost::asio::ip::tcp::socket && commandSocket,
    boost::asio::ip::address receiveFrom,
    std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion,
    uint32_t systemType,
    aris::Command::SetSalinity::Salinity salinity,
    float initialFocusRange);
  ~Connection();

  void SendCommand(const aris::Command & cmd);

private:
  const std::vector<uint8_t> ping_template_;
  const std::function<void(const boost::system::error_code&)> sendPing_;
  const std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion_;
  const uint32_t systemType_;

  bool shutting_down_;
  CookieSequence cookie_;
  aris::Command::SetSalinity::Salinity salinity_;
  boost::asio::io_service & io_;
  boost::asio::ip::tcp::socket commandSocket_;
  boost::asio::deadline_timer ping_timer_;
  Aris::Network::FrameStreamListener frameStreamListener_;

  void HandlePingTimer(const boost::system::error_code& e);
  void HandleCompletedFrame(Aris::Network::FrameBuilder & frameBuilder);
  AcousticSettings SetCookie(const AcousticSettings & settings);

  static std::vector<uint8_t> CreatePingTemplate();
  void SerializeCommand(const aris::Command & cmd, std::vector<uint8_t> & buffer);
};
