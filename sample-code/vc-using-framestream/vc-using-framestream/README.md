# Using the frame stream code in Visual Studio.

This Visual Studio 2017 solution builds a program that retrieves images from an ARIS
sonar using the "frame stream" support included in this SDK ([here](../../../common/code/FrameStream)).

The "frame stream" support is C++ code and is reference code used within some of our own applications.

The protobuf .proto files are used to generate code using `generate-protobuf-files.cmd` during a pre-build event. Linking protobuf support requries the libraries found in [protobuflibs](../../../externals/google/protobuf-libs).

As this is sample code, some shortcuts are taken in order to focus on illustrating how to use the
frame stream code.

This test program can be invoked with the following arguments:

    vc-using-framestream.exe <serial-number>

`vc-using-framestream.exe` can be found in the Release subfolder in the solution directory after building.

### Building

This project uses the script `generate-protobuf-files.cmd` to genereate the protocol buffer source compiled into the project. This is run as a pre-build event.

This project also makes use of the Boost libraries via Nuget packages.

### This release brings less reliance on boost::function, more on std::function.

### Specific examples

- There is an example listener for UDP packets in `UdpListener.h/.cpp`. It is used to listen for ARIS beacons.
- There is an example of building a command in `Connection::CreatePingTemplate()`. Each command sent must be prefixed with a length, as a 4-byte unisigned integer in network byte order.
- There is an example of parsing an ARIS beacon in `ArisBeacons::FindBySerialNumber`. The beacons don't have a length prefix is they occupy a single UDP packet.
- There is an example of parsing a `FramePart` in `TBD`.
