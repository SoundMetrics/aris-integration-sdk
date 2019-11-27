#ifndef FRAME_ASSEMBLER_LITE_HPP
#define FRAME_ASSEMBLER_LITE_HPP

#include "../c/FrmAsmLt.h"

#include <functional>
#include <memory>

/*
  Implements a simple C++ wrapper for the C API in FrmAsmLt.h.

  Overview
  ========
  These functions comprise a small state machine that assembles
  "frame parts" into a complete frame. "Frame parts" are defined by
  a google protocol buffer file (\common\protobuf\frame_stream.proto),
  however this code knows nothing of that format. This code is aware
  that frame parts are numbered, and should appear in correct sequence.

  Adding Frame Parts
  ==================
  For each frame part received from the ARIS, call SmcAddFramePart().
  The callback function you supply for completed frames will be called
  as a frame is completed. The callee (the callback function) is given
  ownership of the allocations, and their lifetime should be managed
  appropriately.

  Missing frame parts cause an entire frame to be lost. At the time of
  this writing ARIS does not have a functioning retry protocol. (Most
  installations--aside from wireless or extremely noisy RF environments--
  will function without losing packets.)
*/
class FrameAssemblerLite final {
public:
  using SampleAllocation = std::unique_ptr<uint8_t, decltype(&std::free)>;

  struct SampleBuffer {
    SampleAllocation samples;
    size_t size;
  };

  // This callback function receives ownership of the sample buffer.
  using OnFrameCompleteFn =
   std::function<void(const ArisFrameHeader&, SampleBuffer&&)>;

  // Contructs the frame assembler, maintaining a refrence to a callback
  // function that is called on completion of a frame. That callback
  // takes ownership of the buffers.
  FrameAssemblerLite(OnFrameCompleteFn onFrameComplete)
    : onFrameComplete(onFrameComplete),
      assembler{
        std::unique_ptr<SmcFrameAssembler, decltype(&free_assembler)>(
          SmcInitFrameAssembler(&allocate, free, onComplete, this),
          &free_assembler)
      }
  {
    if (!onFrameComplete) {
      throw "onFrameComplete must not be empty";
    }
  }

  FrameAssemblerLite(const FrameAssemblerLite&) = delete;
  FrameAssemblerLite(FrameAssemblerLite&&) = delete;

  FrameAssemblerLite& operator = (const FrameAssemblerLite&) = delete;
  FrameAssemblerLite& operator = (FrameAssemblerLite&&) = delete;

  // Assembles the frame part to the frame. When the frame is complete,
  // thye callback function is called.
  void AddFramePart(const SmcFramePartInfo& partInfo) noexcept {
    SmcAddFramePart(assembler.get(), &partInfo);
  }

private:
  // Owns the required function signature, in case SmcFreeFrameAssembler
  // changes.
  static void free_assembler(SmcFrameAssembler* frameAssembler) {
    SmcFreeFrameAssembler(frameAssembler);
  }

  using AssemblerPtr =
   std::unique_ptr<SmcFrameAssembler, decltype(&free_assembler)>;

  OnFrameCompleteFn onFrameComplete;
  AssemblerPtr assembler;

  static void* allocate(size_t size, void*) { return std::malloc(size); }
  static void free(void* pv, void*) { std::free(pv); }

  static void onComplete(
    const ArisFrameHeader* pHeader,
    uint8_t* pSamples, /* recipient owns this memory */
    size_t samplesSize,
    void* cookie
  )
  {
    auto me = reinterpret_cast<FrameAssemblerLite*>(cookie);
    me->onFrameComplete(
      *pHeader,
      SampleBuffer{
        SampleAllocation{
          static_cast<uint8_t*>(pSamples),
          &std::free
        },
        samplesSize
      });
  }
};

#endif // FRAME_ASSEMBLER_LITE_HPP
