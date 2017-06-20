#include "stdafx.h"
#include "syslog.h"

using namespace boost::asio;
using namespace boost::asio::ip;

constexpr size_t syslogBufSize = 16 * 1024; // Way bigger than syslog
constexpr uint16_t SyslogPort = 514;

namespace {

  size_t suffix_size(const std::vector<uint8_t> & buf, size_t size) {

    size_t suffixSize = 0;

    for (int idx = size - 1; idx >= 0; --idx) {
      if (isspace(buf[idx])) {
        ++suffixSize;
      }
      else {
        break;
      }
    }

    return suffixSize;
  }

  constexpr auto kBadPrefixResult =
    std::make_tuple<bool, size_t, Syslog::Facility, Syslog::Severity>(
      false, 0, static_cast<Syslog::Facility>(0), static_cast<Syslog::Severity>(0));

  std::tuple<bool, size_t, Syslog::Facility, Syslog::Severity>
    get_prefix(const std::vector<uint8_t> & buf, size_t size) {
    if (buf[0] != '<') {
      return kBadPrefixResult;
    }

    const auto prefixLast = std::find(std::next(buf.begin()), buf.begin() + size, '>');
    if (prefixLast == buf.begin() + size) {
      return kBadPrefixResult;
    }

    // Prival prefix is between <0> and <191>.
    // https://tools.ietf.org/html/rfc5424
    const auto prefixSize = prefixLast - buf.begin() + 1;
    if (prefixSize < 3 || prefixSize > 5) {
      return kBadPrefixResult;
    }

    const auto beginPrival = std::next(buf.begin());
    const auto privalStr = std::string(beginPrival, beginPrival + prefixSize - 2);


    const int prival = atoi(privalStr.c_str());
    const auto facility = static_cast<Syslog::Facility>(prival / 8);
    const auto severity = static_cast<Syslog::Severity>(prival % 8);

    return { true, prefixSize, facility, severity };
  }
}

Syslog::Syslog(boost::asio::io_service & io, OnMeta onMeta, OnError onError, OnMessage onMessage)
: onMessage_(onMessage)
, onMeta_(onMeta)
, onError_(onError)
, syslogRecvSocket_(io)
, syslogBuf_(syslogBufSize)
{
  Initialize();
}

void Syslog::Initialize() {
  syslogRecvSocket_.open(udp::v4());
  syslogRecvSocket_.set_option(socket_base::reuse_address(true));
  syslogRecvSocket_.set_option(socket_base::receive_buffer_size(syslogBuf_.size()));
  syslogRecvSocket_.bind(udp::endpoint(udp::v4(), SyslogPort));

  if (!syslogRecvSocket_.is_open()) {
    onError_("Couldn't open socket.");
    return;
  }

  StartReceive();
  onMeta_("Started listening...");
}

void Syslog::StartReceive() {
  syslogRecvSocket_.async_receive_from(
    buffer(syslogBuf_.data(), syslogBuf_.size()), syslogRemoteEndpoint_,
    socket_base::message_flags(0),
    [this](auto error, auto bytesRead) { this->HandlePacket(error, bytesRead); });
}

void Syslog::HandlePacket(const boost::system::error_code &error, size_t bytesRead) {
  switch (error.value()) {
  case boost::system::errc::success: {
    syslogBuf_[bytesRead] = '\0';

    const size_t suffixSize = suffix_size(syslogBuf_, bytesRead);

    bool privalSuccess;
    size_t prefixSize;
    Facility facility;
    Severity severity;
    std::tie(privalSuccess, prefixSize, facility, severity) = get_prefix(syslogBuf_, bytesRead - suffixSize);

    const auto msgLength = bytesRead - prefixSize - suffixSize;
    syslogBuf_[bytesRead - suffixSize] = 0;

    onMessage_(facility, severity, syslogRemoteEndpoint_.address(), reinterpret_cast<const char*>(&syslogBuf_[prefixSize]));

    StartReceive();
  } break;

    // Other results indicate failure or we're shutting down.
  case boost::system::errc::operation_canceled: {
    // Timer cancelled
  } break;

  default: { // Happens on normal shutdown
  } break;
  }
}
