// send-header-update.cpp : Defines the entry point for the console application.
//

#include "UpdateFrameHeader/ArisHeaderUpdate.h"
#include <boost/asio.hpp>
#include <boost/asio/buffer.hpp>
#include <cstdint>
#include <cstdio>
#include <cstdlib>

bool parse_args(int argc, char **argv, boost::asio::ip::address & ipAddr) {

  if (argc != 2) {
    return false;
  }

  const auto addrArg = argv[1];
  ipAddr = boost::asio::ip::address::from_string(addrArg);
  return true;
}


void SendHeaderUpdate(boost::asio::ip::udp::socket & socket, boost::asio::ip::address addr) {

  ArisHeaderUpdate::ArisFrameHeaderUpdateMsg msg;
  ArisHeaderUpdate::InitializeArisFrameHeaderUpdateMsg(&msg);

  SetArisFrameHeader_LatLong(&msg, 12, 345);

  const auto target = boost::asio::ip::udp::endpoint(addr, ARIS_HEADER_UPDATE_PORT);
  const auto buf = boost::asio::buffer(&msg, sizeof msg);

  socket.send_to(buf, target);
}

int main(int argc, char **argv) {

  boost::asio::ip::address ipAddr;

  if (!parse_args(argc, argv, ipAddr)) {
    printf("ERROR: Couldn't parse IP address argument.\n");
    return 1;
  }

  boost::asio::io_service io;
  boost::asio::ip::udp::socket socket(io);

  socket.open(boost::asio::ip::udp::v4());
  SendHeaderUpdate(socket, ipAddr);

  return 0;
}
