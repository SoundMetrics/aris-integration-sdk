# Using the frame stream code in Visual Studio.

This Visual Studio  solution builds a program that retrieves images from an ARIS
sonar using the "frame stream" support included in this SDK ([here](../../../common/code/FrameStream)).

The "frame stream" support is C++ code and is reference code used within some of our own applications.

The protobuf .proto files are used to generate messaging code using `generate-protobuf-files.cmd` during a pre-build event. Linking protobuf support requires the libraries found in [protobuflibs](../../../externals/google/protobuf-libs).

As this is sample code, some shortcuts may be taken in order to focus on illustrating how to use the
frame stream code, especially around error handling. Code defensively per your own application's requirements.

This test program can be invoked with the following arguments:

    vc-using-framestream.exe <serial-number> [-m]

The `-m` option causes the program to use a hard-coded multicast group for delivering frames. See [Multicasting](#Multicasting) for more information. Multicasting the frames is not common.

`vc-using-framestream.exe` can be found in the Release subfolder in the solution directory after building.

## Building

This solution may be built with Visual Studio or msbuild. The project uses the script `generate-protobuf-files.cmd` to generate the protocol buffer source compiled into the project. This is run automatically within the Visual Studio solution as a pre-build event.

This project also makes use of the Boost libraries via Nuget packages.

## Specific examples

Pointers to some interesting bits of the sample code:

- There is an example listener for UDP packets in `UdpListener.h/.cpp`. It is used to listen for ARIS beacons. ARIS beacons contain, among other things, the ARIS' serial number; in order to connect to a sonar with a particular serial number, this program listens to the beacons in order to find that ARIS' IP address, which is used to establish a connection.
- There is an example of parsing an ARIS beacon in `ArisBeacons::FindBySerialNumber`. The beacons don't have a length prefix as they occupy a single UDP packet.
- Examples of building commands may be found in [CommandBuilder](../../../common/code/CommandBuilder). Each command sent must be prefixed with a length, as a 4-byte unisigned integer in network byte order, as shown in `Connection::SerializeCommand`.

## Multicasting

This sample also serves as a test program for the multicasting implementation built into the frame stream code. Multicasting of ARIS frames is uncommon, most integrators default to point-to-point delivery of the frames.

For more on using multicasting with ARIS, please see the ARIS Integration SDK documentation.