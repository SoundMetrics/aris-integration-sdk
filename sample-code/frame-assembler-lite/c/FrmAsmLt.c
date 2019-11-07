#include "FrmAsmLt.h"
#include "type-definitions/c/FrameHeader.h"
#include "common-code/FrameFuncs.h"
#include <string.h>

typedef enum SmcAssemblerState {
  FrameAssembler_Start,
  FrameAssembler_Assembling,
} SmcAssemblerState;

struct SmcFrameAssembler {
  SmcAllocateMemory allocateMemory;
  SmcFreeMemory freeMemory;
  SmcFrameComplete onFrameComplete;
  void* cookie;
  SmcAssemblerState state;
  unsigned nextFrameNumber;
  unsigned nextExpectedPartNumber;
  size_t samplesExpected;
  struct ArisFrameHeader* pHeader;
  uint8_t* pSamples;
  uint8_t* pSampleInsert;
};

SmcFrameAssembler* SmcInitFrameAssembler(
  SmcAllocateMemory allocateMemory,
  SmcFreeMemory freeMemory,
  SmcFrameComplete onFrameComplete,
  void* cookie
)
{
  if (allocateMemory == NULL
    || freeMemory == NULL
    || onFrameComplete == NULL)
  {
    return NULL;
  }

  SmcFrameAssembler* frameAssembler =
    allocateMemory(sizeof(SmcFrameAssembler), cookie);

  if (frameAssembler) {
    memset(frameAssembler, 0, sizeof(*frameAssembler));

    frameAssembler->cookie = cookie;
    frameAssembler->allocateMemory = allocateMemory;
    frameAssembler->freeMemory = freeMemory;
    frameAssembler->onFrameComplete = onFrameComplete;
    frameAssembler->state = FrameAssembler_Start;
  }

  return frameAssembler;
}

static void SmcFree(
  SmcFreeMemory freeMemory,
  void* p,
  void* cookie
)
{
  /* We don't know if the provided free function will check for NULL. */
  if (p) {
    freeMemory(p, cookie);
  }
}

void SmcFreeFrameAssembler(
  SmcFrameAssembler *frameAssembler
)
{
  SmcFreeMemory freeMemory = frameAssembler->freeMemory;

  SmcFree(freeMemory, frameAssembler->pHeader, frameAssembler->cookie);
  SmcFree(freeMemory, frameAssembler->pSamples, frameAssembler->cookie);
  SmcFree(freeMemory, frameAssembler, frameAssembler->cookie);
}

typedef int (*SmcAssemblerSMHandler)(
  SmcFrameAssembler*,
  const SmcFramePartInfo*
);

int SmcFrameAssembler_HandleStart(
  SmcFrameAssembler*,
  const SmcFramePartInfo*
);

int SmcFrameAssembler_HandleAssembling(
  SmcFrameAssembler*,
  const SmcFramePartInfo*
);

const SmcAssemblerSMHandler smc_assembler_state_macine[] = {
  SmcFrameAssembler_HandleStart,
  SmcFrameAssembler_HandleAssembling,
};

void SmcAddFramePart(
  SmcFrameAssembler* frameAssembler,
  const SmcFramePartInfo* framePartInfo
)
{
  /* Advance the state machine */
  int done = 0;

  if (framePartInfo->data == NULL || framePartInfo->size == 0) {
    return;
  }

  do {
    SmcAssemblerSMHandler handler = smc_assembler_state_macine[frameAssembler->state];
    done = handler(frameAssembler, framePartInfo);
  } while (!done);
}

void SmcFrameAssembler_Reset(SmcFrameAssembler* frameAssembler)
{
  frameAssembler->state = FrameAssembler_Start;

  frameAssembler->nextExpectedPartNumber = 0;

  SmcFree(
    frameAssembler->freeMemory,
    frameAssembler->pHeader,
    frameAssembler->cookie);
  SmcFree(
    frameAssembler->freeMemory,
    frameAssembler->pSamples,
    frameAssembler->cookie);

  frameAssembler->pHeader = NULL;
  frameAssembler->pSamples = NULL;

  frameAssembler->pSampleInsert = NULL; /* Doesn't own memory */
  frameAssembler->samplesExpected = ~0u;
}

size_t SmcGetTotalSampleCount(unsigned pingMode, size_t samplesPerBeam)
{
  const size_t beamCount = get_beams_from_pingmode(pingMode);
  return beamCount * samplesPerBeam;
}

int SmcFrameAssembler_HandleStart(
  SmcFrameAssembler* frameAssembler,
  const SmcFramePartInfo* framePartInfo
)
{
  if (framePartInfo->framePartNumber == 0
    && framePartInfo->size == sizeof(struct ArisFrameHeader)) {

    struct ArisFrameHeader* pHeader =
      frameAssembler->allocateMemory(
        sizeof(struct ArisFrameHeader), frameAssembler->cookie);
    memset(pHeader, 0, sizeof(*pHeader));
    memcpy(pHeader, framePartInfo->data, framePartInfo->size);
    frameAssembler->pHeader = pHeader;

    frameAssembler->samplesExpected =
      SmcGetTotalSampleCount(pHeader->PingMode, pHeader->SamplesPerBeam);
    frameAssembler->pSamples =
      frameAssembler->allocateMemory(
        frameAssembler->samplesExpected, frameAssembler->cookie);
    frameAssembler->pSampleInsert = frameAssembler->pSamples;

    frameAssembler->state = FrameAssembler_Assembling;
    frameAssembler->nextExpectedPartNumber = 1;
  }
  else {
    /* Ignore the frame part */
  }

  return 1; /* success */
}

int SmcFrameAssembler_HandleAssembling(
  SmcFrameAssembler* frameAssembler,
  const SmcFramePartInfo* framePartInfo
)
{
  int success;

  if (framePartInfo->data == NULL || framePartInfo->size == 0) {
    return 1;
  }

  if (framePartInfo->framePartNumber == frameAssembler->nextExpectedPartNumber) {
    memcpy(frameAssembler->pSampleInsert, framePartInfo->data, framePartInfo->size);

    frameAssembler->pSampleInsert += framePartInfo->size;

    if ((size_t)(frameAssembler->pSampleInsert - frameAssembler->pSamples)
      >= frameAssembler->samplesExpected)
    {
      frameAssembler->pHeader->FrameIndex = frameAssembler->nextFrameNumber++;
      frameAssembler->onFrameComplete(
        frameAssembler->pHeader,
        sizeof(*frameAssembler->pHeader),
        frameAssembler->pSamples,
        frameAssembler->samplesExpected,
        frameAssembler->cookie
      );

      /* we release ownership of the allocations to the client */
      frameAssembler->pHeader = NULL;
      frameAssembler->pSamples = NULL;
      frameAssembler->pSampleInsert = NULL; /* doesn't own memory */

      SmcFrameAssembler_Reset(frameAssembler);
    }
    else {
      ++frameAssembler->nextExpectedPartNumber;
    }

    success = 1;
  }
  else if (framePartInfo->framePartNumber == 0) {
    /* Unexpected restart; normal part 0 is handled in the block above */
    SmcFrameAssembler_Reset(frameAssembler);
    success = 0;
  }
  else {
    /* ignore unexpected part */
    success = 1;
  }

  return success;
}
