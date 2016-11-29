## Reorder C++ Program

This sample program reorders the first frame read from a `.dat` file
(a `.dat` file being an `ArisFrameHeader` followed by the corresponding image data) and compares the result with the contents of an expected `.exp` file. Unlike a `.aris` recording file which starts with an `ArisFileHeader` and is already reordered, a `.dat` file omits the leading `ArisFileHeader` and is not yet reordered. Reading and reordering frames from a `.dat` file is atypical since reordering mostly occurs when receiving frames via a `FrameStream`.

### Program Usage

    reorder <input-path> <expected-path>

### C Type Definitions

The `FrameHeader.h` C header file can be found in
[type_definitions](https://github.com/SoundMetrics/aris-file-sdk/tree/master/type-definitions/C)
--this header file is pre-generated for ease of use. `FrameHeader.h` defines the `ArisFrameHeader` struct for use in interpreting the data in a `.dat` file.

The easiest way to ensure that the C type definitions are available to the MinGW makefile is to git clone the `aris-file-sdk` repo into the same parent directory where your working `aris-integration-sdk` repo is located. 

### C++ Headers

There are two C++ header files, `Frame.h` and `Reorder.h`, which can be found in [Reordering](https://github.com/SoundMetrics/aris-integration-sdk/tree/master/common/code/Reordering). The `Frame.h` header file provides a `Frame` class for easy access to the `ArisFrameHeader` and sonar image data. The `Reorder.h` header file provides a `Reorder` function for reordering the sonar image data in a `Frame`.

### C++ Code

The code in this sample is standard C++11 that should build with any modern C++ compiler.

### ARIS Sample and Expected Data
A sample `.dat` file containing a single frame for reordering is provided in
[sample-code](https://github.com/SoundMetrics/aris-integration-sdk/tree/master/sample-code/reorder-frame) along with an expected `.exp` file containing the same sample data reordered for verification purposes.

### gcc

A makefile is provided in `.\mingw`. MinGW was used to compile it (specifically,
[this distibution of MinGW](https://sourceforge.net/projects/mingw-w64)).
