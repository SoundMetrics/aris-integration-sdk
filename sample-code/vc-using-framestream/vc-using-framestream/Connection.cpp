#include "Connection.h"
#include "AcousticSettings.h"
#include "CommandBuilder.h"

using CommandBuilder = Aris::Network::CommandBuilder;

// This is how often we send a Ping command to the sonar.
// This helps ferret out zombie connections.
const auto kPingTimerPeriod = boost::posix_time::seconds(2);

// Buffer size for the network stack. If you are missing packets
// you may need a bigger buffer. During development this sample
// program ran fine with a 16KB buffer, but all it does is wrangle
// packets.
// 
// You probably want a substantial buffer; more if your hardware
// spec is slow or there's a lot going on in your system (context
// switching, for example).
constexpr size_t kNetworkBufferSize = 16 * 1024 * 1024;

/* static */
std::unique_ptr<Connection> Connection::Create(
  boost::asio::io_service & io,
  std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion,
  Aris::Common::SystemType systemType,
  aris::Command::SetSalinity::Salinity salinity,
  boost::asio::ip::tcp::endpoint targetSonar,
  const Aris::Network::optional<boost::asio::ip::udp::endpoint> & multicastEndpoint,
  float initialFocusRange,
  std::string & errorMessage)
{
  assert(onFrameCompletion);

  boost::asio::ip::tcp::socket socket(io);

  errorMessage.clear();
  try {
    socket.connect(targetSonar);
    boost::asio::socket_base::keep_alive option(true);
    socket.set_option(option);

    // In case the command connection goes down during construction,
    // build the Connection here.
    return std::make_unique<Connection>(
      io, std::move(socket), onFrameCompletion, systemType, salinity,
      targetSonar.address(), multicastEndpoint, initialFocusRange);
  }
  catch (const boost::system::system_error & ec) {
    errorMessage = ec.what();
    return std::unique_ptr<Connection>(); // Unassociated unique_ptr
  }
}

Connection::Connection(
  boost::asio::io_service & io,
  boost::asio::ip::tcp::socket && commandSocket,
  std::function<void(Aris::Network::FrameBuilder&)> onFrameCompletion,
  Aris::Common::SystemType systemType,
  aris::Command::SetSalinity::Salinity salinity,
  boost::asio::ip::address targetSonar,
  const Aris::Network::optional<boost::asio::ip::udp::endpoint> & multicastEndpoint,
  float initialFocusRange)
  : commandSocket_(std::move(commandSocket))
  , io_(io)
  , hasConnectionError_(false)
  , ping_timer_(io, kPingTimerPeriod)
  , ping_template_(std::move(CreatePingTemplate()))
  , sendPing_([this](auto e) { this->HandlePingTimer(e); })
  , systemType_(systemType)
  , salinity_(salinity)
  , onFrameCompletion_(onFrameCompletion)
  // Note that framePartListener_ is initialized AFTER onFrameCompletion_ due to declaration
  // order in the class declaration.
  , frameStreamListener_(io, onFrameCompletion_, []() { return kNetworkBufferSize; },
      targetSonar, multicastEndpoint)
{
  assert(onFrameCompletion);

  // Initialize the sonar appropriately.
  SendCommand(CommandBuilder::SetTime());
  SendCommand(
    multicastEndpoint.has_value()
    ? CommandBuilder::SetFrameStreamReceiver(
        multicastEndpoint.value().address().to_string().c_str(),
        multicastEndpoint.value().port())
    : CommandBuilder::SetFrameStreamReceiver(frameStreamListener_.LocalEndpoint().port())
  );
  SendCommand(
    CommandBuilder::RequestAcousticSettings(
      SetCookie(
        DefaultAcousticSettingsForSystem[static_cast<unsigned>(systemType_)])));
  SendCommand(CommandBuilder::SetSalinity(salinity));
  SendCommand(CommandBuilder::SetFocusRange(initialFocusRange));

  // Set up the connection ping timer.
  ping_timer_.async_wait(sendPing_);
}

Connection::~Connection()
{
  ping_timer_.cancel();
}

void Connection::HandlePingTimer(const boost::system::error_code & e) {
  if (e) {
    return;
  }

  const void * buf = &ping_template_[0];
  size_t size = ping_template_.capacity();

  try {
    commandSocket_.send(boost::asio::buffer(buf, size));

    ping_timer_.expires_from_now(kPingTimerPeriod);
    ping_timer_.async_wait(sendPing_);
  }
  catch (const std::exception & e) {
    hasConnectionError_ = true;
    std::cerr << "An error occurred while sending a ping: " << e.what() << '\n';
    std::cerr << "No further pings can be sent.\n";
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
  return buf;
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
  std::cout << "Connection::SendCommand: " << cmd.DebugString() << '\n';

  std::vector<uint8_t> buffer(128);
  SerializeCommand(cmd, buffer);
  commandSocket_.send(boost::asio::buffer(buffer.data(), buffer.size()));
}
