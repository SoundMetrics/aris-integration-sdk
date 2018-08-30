# Managed Protocol Support

> **Note:** this folder will be made obsolete by `SoundMetrics.Platform`, elsewhere in this repository.

## `SoundMetrics.Aris2.Protocols`

Folder [SoundMetrics.Aris2.Protocols](SoundMetrics.Aris2.Protocols) contains a C# assembly project that implements helper functions for building the Google Protocol Buffers-based commands that you send to an ARIS to control it. This assembly covers only the commonly-used commands.

You may wish to use this assembly in your own project, or examine the code in [SoundMetrics.Aris2.Protocols/ArisCommands.cs](SoundMetrics.Aris2.Protocols/ArisCommands.cs) to learn:

* how to successfully compose a command
* how to convert it to a byte array, complete with required network-order length prefix, as described in the [documentation](../../../../documents).

> Note that this project depends on NuGet package Google.ProtocolBuffers, which supports the version 2 syntax used in our `.proto` files, and makes use of some C# 7 syntax.

## Build Notes

The C++ project (`ArisFrameWrapper`) depends on a protobuf library in the `vcpkg` submodule. You'll need to run `./submodules/vcpkg/bootstrap-vcpkg.bat` to use it. (If you don't have a `vcpkg` submodule, you probably forgot to clone recursively, as noted in the main repo [README](../../../README.md).)

## Warnings

Parts which you may not find useful:

* `TestAris2Commands` is an integration test used by Sound Metrics to verify the functionality of the `SoundMetrics.Aris2.Protocols` assembly, and has no production value.
* `ArisFramestreamWrapper` is a crude adaptation of our [C++ FrameStream code](../../code/FrameStream/) intended only to support the integration tests in `TestAris2Commands`; to be clear, it is not production code and it's strategy to release resources is "process termination."
