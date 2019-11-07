#ifndef FRAME_ASSEMBLER_LITE_H
#define FRAME_ASSEMBLER_LITE_H

#include <stddef.h>

/*
  DO NOT assume knowledge of this structure.
*/

typedef struct SmcFrameAssembler SmcFrameAssembler;

/*
  Function types:
*/

typedef void* (*SmcAllocateMemory)(size_t size, void* cookie);
typedef void (*SmcFreeMemory)(void* allocation, void* cookie);

/** Callback used when a frame is completed. */
typedef void (*SmcFrameComplete)(
  void* header, /* recipient owns this memory */
  size_t headerSize,
  void* samples, /* recipient owns this memory */
  size_t samplesSize,
  void* cookie
);

/*
  Lifetime management functions:
*/

/** Allocates and initializes a frame assembler; returns null on null inputs. */
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

typedef struct SmcFramePartInfo {
  const unsigned framePartNumber; /* from the protobuf */
  const void* const data;
  const size_t size;
} SmcFramePartInfo;

void SmcAddFramePart(
  SmcFrameAssembler*,
  const SmcFramePartInfo*
);

#endif // FRAME_ASSEMBLER_LITE_H
