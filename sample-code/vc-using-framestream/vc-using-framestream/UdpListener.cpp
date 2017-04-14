#include "UdpListener.h"

UdpListener::UdpListener(boost::asio::io_service & io, uint16_t port, bool reuseAddress, size_t maxSize,
  receive_handler handler)
  : socket_(io)
  , recvBuffer_(maxSize)
  , handler_(handler)
  , shuttingDown_(false)
{
  assert(handler_);

  socket_.open(udp::v4());
  socket_.set_option(boost::asio::socket_base::reuse_address(reuseAddress));
  socket_.bind(udp::endpoint(udp::v4(), port));
  socket_.set_option(boost::asio::socket_base::receive_buffer_size(recvBuffer_.capacity()));

  StartAsyncReceive();
}


UdpListener::~UdpListener()
{
  try {
    ShutDown();
  }
  catch (...) {
  }
}

void UdpListener::StartAsyncReceive()
{
  socket_.async_receive_from(boost::asio::buffer(recvBuffer_.data(), recvBuffer_.capacity()),
    senderEndpoint_,
    [this](auto error, auto bytes_transferred) { this->HandleReceiveFrom(error, bytes_transferred); });
}

void UdpListener::HandleReceiveFrom(const boost::system::error_code & error, size_t bytesRead)
{
  if (shuttingDown_) {
    return;
  }

  handler_(error, senderEndpoint_, recvBuffer_.data(), bytesRead);
  StartAsyncReceive();
}

void UdpListener::ShutDown()
{
  shuttingDown_ = true;

  // VC++:
  // UdpListener.cpp(34) : warning C4996 :
  // 'boost::asio::basic_socket<Protocol,DatagramSocketService>::cancel' :
  // By default, this function always fails with operation_not_supported when used on Windows XP, Windows Server 2003, or earlier.Consult documentation for details.
#pragma warning (suppress: 4996)
  socket_.cancel();

  boost::system::error_code ec;
  socket_.shutdown(udp::socket::shutdown_both, ec);
  socket_.close();
}
