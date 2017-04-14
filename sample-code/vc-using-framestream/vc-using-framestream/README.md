# Using the frame stream code in Visual Studio.

This Visual Studio 2017 solution builds a program that retrieves images from an ARIS
sonar using the "frame stream" support included in this SDK ([here](../../../common/code/FrameStream)).

The "frame stream" support is C++ code and is reference code used within some of our own applications.

The protobuf .proto files are used to generate code using `generate-protobuf-files.cmd` during a pre-build event. Linking protobuf support requires the libraries found in [protobuflibs](../../../externals/google/protobuf-libs).

As this is sample code, some shortcuts may be taken in order to focus on illustrating how to use the
frame stream code, especially around error handling. Code defensively per your own application's requirements.

This test program can be invoked with the following arguments:

    vc-using-framestream.exe <serial-number>

`vc-using-framestream.exe` can be found in the Release subfolder in the solution directory after building.

### Building

This project uses the script `generate-protobuf-files.cmd` to generate the protocol buffer source compiled into the project. This is run as a pre-build event.

This project also makes use of the Boost libraries via Nuget packages.

### Specific examples

- There is an example listener for UDP packets in `UdpListener.h/.cpp`. It is used to listen for ARIS beacons. ARIS beacons contain, among other things, the ARIS' serial number; in order to connect to a sonar with a particular serial number, this program listens to the beacons in order to find that ARIS' IP address, which is used to establish a connection.
- There is an example of parsing an ARIS beacon in `ArisBeacons::FindBySerialNumber`. The beacons don't have a length prefix as they occupy a single UDP packet.
- Examples of building commands may be found in [CommandBuilder](../../../common/code/CommandBuilder). Each command sent must be prefixed with a length, as a 4-byte unisigned integer in network byte order, as shown in `Connection::SerializeCommand`.

