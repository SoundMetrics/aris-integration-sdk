#include "FrmAsmLt.h"
#include "common-code/FrameFuncs.h"
#include <string.h>

typedef enum SmcFrameAssemblerState {
  SmcFrameAssembler_Start,
  SmcFrameAssembler_Assembling,
} SmcFrameAssemblerState;

struct SmcFrameAssembler {
  struct ArisFrameHeader frameHeader;
  SmcAllocateMemory allocateMemory;
  SmcFreeMemory freeMemory;
  SmcFrameComplete onFrameComplete;
  void* cookie;
  SmcFrameAssemblerState state;
  unsigned nextFrameNumber;
  unsigned nextExpectedPartNumber;
  size_t samplesExpected;
  uint8_t* pSamples;
  uint8_t* pSampleInsert; /* This never owns memory */
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
    frameAssembler->state = SmcFrameAssembler_Start;
  }

  return frameAssembler;
}

static void SmcFree(
  SmcFreeMemory freeMemory,
  void* p,
  void* cookie
)
{
  /*
    We don't know if the provided free function will check for NULL.
    E.g., free() operates fine if you pass it NULL. But the user's
    provided function is an unknown.
  */
  if (p) {
    freeMemory(p, cookie);
  }
}

void SmcFreeFrameAssembler(
  SmcFrameAssembler *frameAssembler
)
{
  SmcFreeMemory freeMemory = frameAssembler->freeMemory;

  SmcFree(freeMemory, frameAssembler->pSamples, frameAssembler->cookie);
  SmcFree(freeMemory, frameAssembler, frameAssembler->cookie);
}

typedef int (*SmcAssemblerSMHandler)(
  SmcFrameAssembler*,
  const SmcFramePartInfo*
);

static int SmcFrameAssembler_HandleStart(
  SmcFrameAssembler*,
  const SmcFramePartInfo*
);

static int SmcFrameAssembler_HandleAssembling(
  SmcFrameAssembler*,
  const SmcFramePartInfo*
);

/*
  This is the dispatch vector for the state machine.
*/
static const SmcAssemblerSMHandler smc_assembler_state_macine[] = {
  SmcFrameAssembler_HandleStart,
  SmcFrameAssembler_HandleAssembling,
};

void SmcAddFramePart(
  SmcFrameAssembler* frameAssembler,
  const SmcFramePartInfo* framePartInfo
)
{
  int done = 0;

  if (framePartInfo->data == NULL || framePartInfo->size == 0) {
    return;
  }

  /* Advance the state machine */
  do {
    SmcAssemblerSMHandler handler = smc_assembler_state_macine[frameAssembler->state];
    done = handler(frameAssembler, framePartInfo);
  } while (!done);
}

static void SmcFrameAssembler_Reset(SmcFrameAssembler* frameAssembler)
{
  frameAssembler->state = SmcFrameAssembler_Start;

  frameAssembler->nextExpectedPartNumber = 0;

  SmcFree(
    frameAssembler->freeMemory,
    frameAssembler->pSamples,
    frameAssembler->cookie);
  frameAssembler->pSamples = NULL;

  frameAssembler->pSampleInsert = NULL; /* This never owns memory */
  frameAssembler->samplesExpected = ~0u;
}

static size_t SmcGetTotalSampleCount(unsigned pingMode, size_t samplesPerBeam)
{
  const size_t beamCount = get_beams_from_pingmode(pingMode);
  return beamCount * samplesPerBeam;
}

static int SmcFrameAssembler_HandleStart(
  SmcFrameAssembler* frameAssembler,
  const SmcFramePartInfo* framePartInfo
)
{
  if (framePartInfo->framePartNumber == 0
    && framePartInfo->size == sizeof(struct ArisFrameHeader)) {

    struct ArisFrameHeader* pHeader = &frameAssembler->frameHeader;
    memset(pHeader, 0, sizeof(*pHeader));
    memcpy(pHeader, framePartInfo->data, framePartInfo->size);

    frameAssembler->samplesExpected =
      SmcGetTotalSampleCount(pHeader->PingMode, pHeader->SamplesPerBeam);
    frameAssembler->pSamples =
      frameAssembler->allocateMemory(
        frameAssembler->samplesExpected, frameAssembler->cookie);
    frameAssembler->pSampleInsert = frameAssembler->pSamples;

    frameAssembler->state = SmcFrameAssembler_Assembling;
    frameAssembler->nextExpectedPartNumber = 1;
  }
  else {
    /* Ignore the frame part */
  }

  return 1; /* success */
}

static int SmcFrameAssembler_HandleAssembling(
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
      /* Complete! */
      frameAssembler->frameHeader.FrameIndex = frameAssembler->nextFrameNumber++;
      frameAssembler->onFrameComplete(
        &frameAssembler->frameHeader,
        frameAssembler->pSamples,
        frameAssembler->samplesExpected,
        frameAssembler->cookie
      );

      /* we release ownership of the allocations to the client */
      frameAssembler->pSamples = NULL;
      frameAssembler->pSampleInsert = NULL; /* This never owns memory */

      SmcFrameAssembler_Reset(frameAssembler);
    }
    else {
      ++frameAssembler->nextExpectedPartNumber;
    }

    success = 1;
  }
  else if (framePartInfo->framePartNumber == 0) {
    /*
      An unexpected restart occurred. Normal handling of
      frame part 0 is handled in SmcFrameAssembler_HandleStart().
      Reset and continue processing.
    */
    SmcFrameAssembler_Reset(frameAssembler);
    success = 0;
  }
  else {
    /* ignore unexpected part */
    success = 1;
  }

  return success;
}
