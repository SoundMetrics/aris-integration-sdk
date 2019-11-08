#ifndef FRAME_ASSEMBLER_LITE_H
#define FRAME_ASSEMBLER_LITE_H

/*
  Note: There is a C++ wrapper for this code in
        ../cpp/FrameAssemblerLite.hpp
*/

/*
  Overview
  ========
  These functions comprise a small state machine that assembles
  "frame parts" into a complete frame. "Frame parts" are defined by
  a google protocol buffer file (\common\protobuf\frame_stream.proto),
  however this code knows nothing of that format. This code is aware
  that frame parts are numbered, and should appear in correct sequence.

  Lifetime
  ========
  The frame assembler is initialized by a call to
  SmcInitFrameAssembler(). The assembler is freed by a call
  to SmcFreeFrameAssembler.

  Adding Frame Parts
  ==================
  For each frame part received from the ARIS, call SmcAddFramePart().
  The callback function you supply for completed frames will be called
  as a frame is completed. The callee (the callback function) is given
  ownership of the allocations, and they should be freed appropriately.

  Missing frame parts cause an entire frame to be lost. At the time of
  this writing ARIS does not have a functioning retry protocol. (Most
  installations--aside from wireless or extremely noisy RF environments--
  will function without losing packets.)
*/

#ifdef __cplusplus
extern "C" {
#endif

#include "type-definitions/c/FrameHeader.h"
#include <stddef.h>

/*
  DO NOT assume knowledge of SmcFrameAssembler.
*/
typedef struct SmcFrameAssembler SmcFrameAssembler;

/*
  Function Types
  ==============
  The user is required to provide three functions:

    SmcAllocateMemory
      The assembler uses this to allocate memory.

    SmcFreeMemory
      The assembler uses this to free memory.
*/

/*
  The assembler uses this to allocate memory. The user is not
  required to pass a useful value for cookie, but has the option
  to do so.
*/
typedef void* (*SmcAllocateMemory)(size_t size, void* cookie);

/*
  The assembler uses this to free memory. The user is not
  required to pass a useful value for cookie, but has the option
  to do so.
*/
typedef void (*SmcFreeMemory)(void* allocation, void* cookie);

/**
  This callback is called when a frame is completed.
  The callee takes ownership of the buffers `header` and `samples`.
  These buffers were allocated by the user-supplied function of
  type SmcAllocateMemory.
*/
typedef void (*SmcFrameComplete)(
  const struct ArisFrameHeader* header,
  uint8_t* samples, /* recipient owns this memory */
  size_t samplesSize,
  void* cookie
);

/*
  Lifetime management functions:
*/

/**
  Allocates and initializes a frame assembler. This returns NULL on NULL
  inputs, though cookie is optional.
*/
SmcFrameAssembler* SmcInitFrameAssembler(
  SmcAllocateMemory allocateMemory,
  SmcFreeMemory freeMemory,
  SmcFrameComplete onFrameComplete,
  void* cookie
);

/** Frees a frame assemblier. */
void SmcFreeFrameAssembler(SmcFrameAssembler*);

/*
  Frame assembly:
*/

/*
  Describes a frame part.
  Field framePartNumber starts with zero.
  Fields data and size should never be NULL or zero.
*/
typedef struct SmcFramePartInfo {
  unsigned framePartNumber; /* from the protobuf */
  const void* data;
  size_t size;
} SmcFramePartInfo;

/*
  Adds the provided frame part to the frame. Upon completion, the
  SmcFrameComplete callback function is called.
*/
void SmcAddFramePart(
  SmcFrameAssembler*,
  const SmcFramePartInfo*
);

#ifdef __cplusplus
}
#endif

#endif /* FRAME_ASSEMBLER_LITE_H */
