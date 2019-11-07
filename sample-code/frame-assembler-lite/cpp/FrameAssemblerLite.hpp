#ifndef FRAME_ASSEMBLER_LITE_HPP
#define FRAME_ASSEMBLER_LITE_HPP

extern "C" {
#include "../c/FrmAsmLt.h"
}

#include <functional>
#include <memory>

class FrameAssemblerLite final {
public:
  using Buffer = std::unique_ptr<void, decltype(&std::free)>;
  using Samples = std::unique_ptr<uint8_t, decltype(&std::free)>;

  // This callback function receives ownership of the memory.
  using OnFrameCompleteFn =
   std::function<void(Buffer&&, size_t, Samples&&, size_t)>;

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

  void AddFramePart(const SmcFramePartInfo& partInfo) noexcept {
    SmcAddFramePart(assembler.get(), &partInfo);
  }

private:
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
    void* pHeader, /* recipient owns this memory */
    size_t headerSize,
    void* pSamples, /* recipient owns this memory */
    size_t samplesSize,
    void* cookie
  )
  {
    auto me = reinterpret_cast<FrameAssemblerLite*>(cookie);
    me->onFrameComplete(
      std::unique_ptr<void, decltype(&std::free)>(
        static_cast<void*>(pHeader), &std::free),
      headerSize,
      std::unique_ptr<uint8_t, decltype(&std::free)>(
        static_cast<uint8_t*>(pSamples), &std::free),
      samplesSize);
  }
};

#endif // FRAME_ASSEMBLER_LITE_HPP
