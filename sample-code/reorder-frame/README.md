## Reorder C++ Program

This sample program reorders the first frame of a `.aris` file
(a '`.aris` file' being a recording of consecutive ARIS images).

### Program Usage

    reorderframe <input-path>

### C Type Definitions

There are two C header files, `FileHeader.h` and `FrameHeader.h`, which can be found in
[type_definitions](https://github.com/SoundMetrics/aris-file-sdk/tree/master/type-definitions)
--these files
are pre-generated for ease of use. These headers define structs for use in interpreting the data
in a `.aris` file.

The easiest way to ensure that the C type definitions are available to the MinGW makefile is to git clone the `aris-file-sdk` repo into the same parent directory where your working `aris-integration-sdk` repo is located. 

### C++ Headers

There are two C++ header files, `Frame.h` and `Reorder.h`, which can be found in [Reordering](https://github.com/SoundMetrics/aris-integration-sdk/tree/master/common/code/Reordering). The `Frame.h` header file provides a `Frame` class for easy access to the `FrameHeader` and sonar image data. The `Reorder.h` header file provides a `Reorder` function for reordering the sonar image data in a `Frame`.

### C++ Code

The code in this sample is standard C++11 that should build with any modern C++ compiler.

### ARIS Sample Recording
A sample recording is provided in
[sample-code](https://github.com/SoundMetrics/aris-file-sdk/tree/master/sample-code).

### gcc

A makefile is provided in `.\mingw`. MinGW was used to compile it (specifically,
[this distibution of MinGW](https://sourceforge.net/projects/mingw-w64)).
