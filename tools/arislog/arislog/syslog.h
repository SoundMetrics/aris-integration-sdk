#pragma once

#include <functional>

class Syslog {
public:
  typedef uint16_t Facility;
  typedef uint16_t Severity;

  typedef std::function<void(const std::string &)> OnMeta;
  typedef std::function<void(const std::string &)> OnError;
  typedef std::function<void(
    Facility, Severity, const boost::asio::ip::address &, const char *)> OnMessage;

  Syslog(boost::asio::io_service &, OnMeta, OnError, OnMessage);

private:
  const OnMessage onMessage_;
  const OnMeta onMeta_;
  const OnError onError_;

  boost::asio::ip::udp::socket syslogRecvSocket_;
  boost::asio::ip::udp::endpoint syslogRemoteEndpoint_;
  std::vector<uint8_t> syslogBuf_;

  void Initialize();
  void StartReceive();
  void HandlePacket(const boost::system::error_code &error, size_t bytesRead);
};
