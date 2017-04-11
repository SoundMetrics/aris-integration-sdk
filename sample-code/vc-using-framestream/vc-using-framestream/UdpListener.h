#pragma once

#include <boost/asio.hpp>
#include <functional>

class UdpListener
{
public:
  typedef boost::asio::ip::basic_endpoint<boost::asio::ip::udp> endpoint;
  typedef boost::asio::ip::udp udp;
  typedef std::function<void(const boost::system::error_code & error, endpoint ep,
    const uint8_t * buffer, size_t bufferSize)>
    receive_handler;

  UdpListener(boost::asio::io_service & io, uint16_t port, bool reuseAddress, size_t maxSize,
    receive_handler handler);
  ~UdpListener();

private:
  const receive_handler handler_;
  udp::socket socket_;
  std::vector<uint8_t> recvBuffer_;
  endpoint senderEndpoint_; // Must outlive the async calls, so it's a field.
  bool shuttingDown_; // Used to ignore delayed packet handling upon destruction.

  void StartAsyncReceive();
  void HandleReceiveFrom(const boost::system::error_code& error, size_t bytesRead);
  void ShutDown();
};

