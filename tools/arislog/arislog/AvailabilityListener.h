#pragma once

#include "AvailableSonars.h"
#include <functional>

class AvailabilityListener {
public:
  typedef std::function<void(const std::string &)> OnError;
  typedef std::function<void(unsigned)> OnExpired;

  AvailabilityListener(
    boost::asio::io_service & io,
    AvailableSonars::AddCallback,
    AvailableSonars::UpdateCallback,
    OnExpired,
    OnError);

  auto GetSerialNumber(const boost::asio::ip::address & addr) const {
    return availableSonars_.GetSerialNumber(addr);
  }

private:
  boost::asio::io_service & io_;
  AvailableSonars::AddCallback onAdd_;
  AvailableSonars::UpdateCallback onUpdate_;
  OnExpired onExpired_;
  OnError onError_;

  boost::asio::ip::udp::socket availabilitySocket_;
  boost::asio::ip::udp::endpoint availabilityRemoteEndpoint_;
  boost::asio::deadline_timer expire_timer_;
  std::vector<uint8_t> availabilityBuf_;
  AvailableSonars availableSonars_;

  void Initialize();
  void StartReceive();
  void HandlePacket(const boost::system::error_code &error, size_t bytesRead);
  void HandleExpiration(const boost::system::error_code & e);

  unsigned ParseBeacon(const char * buffer, size_t bufferSize);
};
