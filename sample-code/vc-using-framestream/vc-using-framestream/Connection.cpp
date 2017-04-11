#include "Connection.h"
#include "AcousticSettings.h"
#include "CommandBuilder.h"

using CommandBuilder = Aris::Network::CommandBuilder;

// This is how often we send a Ping command to the sonar.
// This helps ferret out zombie connections.
const auto kPingTimerPeriod = boost::posix_time::seconds(2);

// ARIS 2 avoids fragmenting UDP packets on Ethernet, so we can use a buffer
// size of  1500 (Ethernet MTU).
constexpr size_t kFramePartBufferSize = 1500;

/* static */
std::unique_ptr<Connection> Connection::Create(
  boost::asio::io_service & io,
  const boost::asio::ip::tcp::endpoint & ep,
  std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion,
  uint32_t systemType,
  aris::Command::SetSalinity::Salinity salinity,
  float initialFocusRange,
  std::string & errorMessage)
{
  boost::asio::ip::tcp::socket socket(io);

  errorMessage.clear();
  try {
    socket.connect(ep);
    boost::asio::socket_base::keep_alive option(true);
    socket.set_option(option);
  }
  catch (boost::system::system_error ec) {
    errorMessage = ec.what();
    return std::unique_ptr<Connection>(); // Unassociated unique_ptr
  }

  return std::make_unique<Connection>(
    io, std::move(socket), ep.address(), onFrameCompletion,
    systemType, salinity, initialFocusRange);
}

Connection::Connection(
  boost::asio::io_service & io,
  boost::asio::ip::tcp::socket && commandSocket,
  boost::asio::ip::address receiveFrom,
  std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion,
  uint32_t systemType,
  aris::Command::SetSalinity::Salinity salinity,
  float initialFocusRange)
  : commandSocket_(std::move(commandSocket))
  , io_(io)
  , ping_timer_(io, kPingTimerPeriod)
  , ping_template_(std::move(CreatePingTemplate()))
  , sendPing_([this](auto e) { this->HandlePingTimer(e); })
  , systemType_(systemType)
  , salinity_(salinity)
  , onFrameCompletion_(onFrameCompletion)
  // Note that framePartListener_ is initialized AFTER onFrameCompletion_ due to declaration
  // order in the class declaration.
  , frameStreamListener_(io, onFrameCompletion_, []() { return kFramePartBufferSize; }, receiveFrom)
  , shutting_down_(false)
{
  // Initialize the sonar appropriately.
  SendCommand(CommandBuilder::SetTime());
  SendCommand(
    CommandBuilder::SetFrameStreamReceiver(
      //frameStreamListener_.LocalEndpoint().address().to_string().c_str(),
      frameStreamListener_.LocalEndpoint().port()));
  SendCommand(
    CommandBuilder::RequestAcousticSettings(
      SetCookie(
        DefaultAcousticSettingsForSystem[systemType_])));
  SendCommand(CommandBuilder::SetSalinity(salinity));
  SendCommand(CommandBuilder::SetFocusRange(initialFocusRange));

  // Set up the connection ping timer.
  ping_timer_.async_wait(sendPing_);
}

Connection::~Connection()
{
  shutting_down_ = true;
}

void Connection::HandlePingTimer(const boost::system::error_code& e) {
  if (e || shutting_down_) {
    return;
  }

  const void * buf = &ping_template_[0];
  size_t size = ping_template_.capacity();
  commandSocket_.send(boost::asio::buffer(buf, size));

  ping_timer_.expires_from_now(kPingTimerPeriod);
  ping_timer_.async_wait(sendPing_);
}

void Connection::HandleCompletedFrame(Aris::Network::FrameBuilder & frameBuilder)
{
  if (frameBuilder.IsComplete()) {
    printf("Received frame %d\n", frameBuilder.FrameIndex());
  }
}

AcousticSettings Connection::SetCookie(const AcousticSettings & settings)
{
  auto adjustedSettings = settings;
  adjustedSettings.cookie = cookie_.Next();
  return adjustedSettings;
}

/* static */
std::vector<uint8_t> Connection::CreatePingTemplate() {
  aris::Command command = std::move(Aris::Network::CommandBuilder::Ping());

  const uint32_t msgLength = command.ByteSize();
  const auto prefixLength = sizeof msgLength;

  // Make enough space to store the message length prefix with the message.
  std::vector<uint8_t> buf(prefixLength + msgLength);

  // The message is prefixed by the message length in network order.
  *reinterpret_cast<uint32_t*>(&buf[0]) = htonl(command.ByteSize());
  command.SerializeToArray(&buf[prefixLength], msgLength);

  // Normally you would just send the command to the ARIS rather than
  // storing it, but the ping doesn't have any content that changes.
  return std::move(buf);
}

void Connection::SerializeCommand(const aris::Command & cmd, std::vector<uint8_t>& buffer)
{
  const uint32_t msgLength = cmd.ByteSize();
  const auto prefixLength = sizeof msgLength;

  // Make enough space to store the message length prefix with the message.
  const auto totalMessageLength = prefixLength + msgLength;
  buffer.resize(totalMessageLength);

  // The message is prefixed by the message length in network order.
  *reinterpret_cast<uint32_t*>(&buffer[0]) = htonl(cmd.ByteSize());
  cmd.SerializeToArray(&buffer[prefixLength], msgLength);
}

void Connection::SendCommand(const aris::Command & cmd)
{
  std::vector<uint8_t> buffer(128);
  SerializeCommand(cmd, buffer);
  commandSocket_.send(boost::asio::buffer(buffer.data(), buffer.size()));
}
