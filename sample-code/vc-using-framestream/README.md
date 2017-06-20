# vc-using-framestream

This sample project implements a frame stream client that

* finds a sonar by serial number
* connects to the sonar
* sends commands to the sonar
* receives frames from the sonar
* records frames to a file (`output.aris`)
* optionally uses multicast to receive the frames (see **Multicasting ARIS Frames** in the SDK documentation)

The code in this project is a Visual Studio solution that relies heavily on Boost ASIO for its networking code.
