#include "stdafx.h"
#include "AvailabilityListener.h"
#include "availability.h"

using namespace boost::asio;
using namespace boost::asio::ip;

constexpr uint16_t Aris2AvailabilityPort = 56124;
constexpr size_t availabilityBufSize = 1024;
constexpr unsigned kInvalidSerialNumber = 0;
const auto kSonarMissingExpiration = boost::posix_time::seconds(5);
const auto kExpirationTimerPeriod = boost::posix_time::seconds(1);

AvailabilityListener::AvailabilityListener(
  boost::asio::io_service & io,
  AvailableSonars::AddCallback onAdd,
  AvailableSonars::UpdateCallback onUpdate,
  OnExpired onExpired,
  OnError onError)
  : io_(io)
  , onAdd_(onAdd)
  , onUpdate_(onUpdate)
  , onExpired_(onExpired)
  , onError_(onError)
  , availabilitySocket_(io)
  , availabilityBuf_(availabilityBufSize)
  , expire_timer_(io, kExpirationTimerPeriod)
{
  Initialize();
}

void AvailabilityListener::Initialize() {
  availabilitySocket_.open(udp::v4());
  availabilitySocket_.set_option(socket_base::reuse_address(true));
  availabilitySocket_.set_option(socket_base::receive_buffer_size(availabilityBuf_.size()));
  availabilitySocket_.bind(udp::endpoint(udp::v4(), Aris2AvailabilityPort));

  if (!availabilitySocket_.is_open()) {
    onError_("Couldn't open availability socket.\n");
    return;
  }

  StartReceive();
  expire_timer_.async_wait([this](auto e) { this->HandleExpiration(e); });
}

void AvailabilityListener::StartReceive() {
  availabilitySocket_.async_receive_from(
    buffer(availabilityBuf_.data(), availabilityBuf_.size()),
    availabilityRemoteEndpoint_,
    socket_base::message_flags(0),
    [this](auto error, auto bytesRead) { this->HandlePacket(error, bytesRead); });
}

void AvailabilityListener::HandlePacket(const boost::system::error_code &error, size_t bytesRead) {
  switch (error.value()) {
  case boost::system::errc::success: {
    availabilityBuf_[bytesRead] = '\0';

    const auto sn = ParseBeacon(reinterpret_cast<const char*>(availabilityBuf_.data()), bytesRead);
    if (sn != kInvalidSerialNumber) {
      availableSonars_.AddOrUpdate(sn, availabilityRemoteEndpoint_.address(), onAdd_, onUpdate_);
    }

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

unsigned AvailabilityListener::ParseBeacon(const char * buffer, size_t bufferSize) {

  aris::Availability message;

  if (message.ParseFromArray(buffer, bufferSize) && message.has_serialnumber()) {
    return message.serialnumber();
  }
  else {
    return kInvalidSerialNumber;
  }
}

void AvailabilityListener::HandleExpiration(const boost::system::error_code & e) {

  if (e.value() == boost::asio::error::operation_aborted) {
    return;
  }

  std::vector<unsigned> sns = std::move(availableSonars_.GetSerialNumbers());
  const auto now = boost::posix_time::microsec_clock::universal_time();

  for (const auto sn : sns) {
    bool found;
    boost::posix_time::ptime latestSighting;
    std::tie(found, latestSighting) = availableSonars_.GetLatestSighting(sn);

    if (now > latestSighting + kSonarMissingExpiration) {
      availableSonars_.Remove(sn);
      onExpired_(sn);
    }
  }

  expire_timer_.expires_from_now(kExpirationTimerPeriod);
  expire_timer_.async_wait([this](auto e) { this->HandleExpiration(e); });
}
