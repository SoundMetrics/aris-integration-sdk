#include "sample-code/frame-assembler-lite/cpp/FrameAssemblerLite.hpp"
#include "catch2/catch.hpp"

#include "type-definitions/C/FrameHeader.h"

#include <array>
#include <numeric>
#include <vector>

using Buffer = FrameAssemblerLite::Buffer;
using Samples = FrameAssemblerLite::Samples;

SCENARIO("Bad ctor inputs") {
  GIVEN("an empty frame completion function") {

    REQUIRE_THROWS(
      FrameAssemblerLite{ FrameAssemblerLite::OnFrameCompleteFn{} },
      "Should throw on empty func");
  }

  GIVEN("A proper frame completion function doesn't throw") {

    REQUIRE_NOTHROW(
      FrameAssemblerLite{
      [](Buffer&&, size_t, Samples&&, size_t) {}
    });
  }
}

SCENARIO("Add frame part") {
  GIVEN("A complete small frame") {
    ArisFrameHeader hdr{ 0, 0, ARIS_FRAME_SIGNATURE };
    hdr.PingMode = 1; // 48 beams
    hdr.SamplesPerBeam = 2; // a total of 96 samples ("unrealistic")

    std::array<uint8_t, 24> a1, a2, a3, a4;

    std::iota(a1.begin(), a1.end(), 0);
    std::iota(a2.begin(), a2.end(), 24);
    std::iota(a3.begin(), a3.end(), 48);
    std::iota(a4.begin(), a4.end(), 72);

    bool completedFrame = false;
    unsigned headerSize = 0;
    unsigned samplesCount = 0;
    void* pHeader;
    uint8_t* pSamples;
    ArisFrameHeader hdrReceived;
    std::vector<uint8_t> samplesReceived;

    FrameAssemblerLite fa{
      [&](Buffer&& h, size_t headerSize, Samples&& s, size_t samplesSize) {
        auto hdr = std::move(h);
        auto samples = std::move(s);
        completedFrame = true;
        samplesCount = samplesSize;

        // These are invalid on return, but can be used for REQUIRE testing.
        pHeader = hdr.get();
        pSamples = samples.get();

        hdrReceived = *reinterpret_cast<ArisFrameHeader*>(pHeader);
        samplesReceived =
          std::move(std::vector<uint8_t>(pSamples, pSamples + samplesCount));
      }
    };

    auto addPart = [&fa](const auto& buffer) {
      static unsigned partNumber = 1;

      const SmcFramePartInfo fp{ partNumber++, buffer.data(), buffer.size() };
      fa.AddFramePart(fp);
    };

    fa.AddFramePart({ 0, &hdr, sizeof hdr });
    addPart(a1);
    addPart(a2);
    addPart(a3);
    addPart(a4);

    REQUIRE(completedFrame == true);
    REQUIRE(samplesCount == 96);
    REQUIRE(pHeader != nullptr); // No longer a valid pointer, just check for non-null
    REQUIRE(pSamples != nullptr); // No longer a valid pointer, just check for non-null

    REQUIRE(hdrReceived.FrameIndex == 0);

    for (unsigned i = 0; i < samplesReceived.size(); ++i) {
      REQUIRE(samplesReceived[i] == i);
    }
  }

  GIVEN("Two complete small frames") {
    ArisFrameHeader hdr{ 0, 0, ARIS_FRAME_SIGNATURE };
    hdr.PingMode = 1; // 48 beams
    hdr.SamplesPerBeam = 2; // a total of 96 samples ("unrealistic")

    std::array<uint8_t, 24> a1, a2, a3, a4;

    std::iota(a1.begin(), a1.end(), 0);
    std::iota(a2.begin(), a2.end(), 24);
    std::iota(a3.begin(), a3.end(), 48);
    std::iota(a4.begin(), a4.end(), 72);

    bool completedFrame = false;
    unsigned headerSize = 0;
    unsigned samplesCount = 0;
    void* pHeader;
    uint8_t* pSamples;
    ArisFrameHeader hdrReceived;
    std::vector<uint8_t> samplesReceived;

    FrameAssemblerLite fa{
      [&](Buffer&& h, size_t headerSize, Samples&& s, size_t samplesSize) {
        auto hdr = std::move(h);
        auto samples = std::move(s);
        completedFrame = true;
        samplesCount = samplesSize;

        // These are invalid on return, but can be used for REQUIRE testing.
        pHeader = hdr.get();
        pSamples = samples.get();

        hdrReceived = *reinterpret_cast<ArisFrameHeader*>(pHeader);
        samplesReceived =
          std::move(std::vector<uint8_t>(pSamples, pSamples + samplesCount));
      }
    };

    for (unsigned frameIndex = 0; frameIndex < 2; ++frameIndex) {

      unsigned partNumber = 1;
      hdrReceived.FrameIndex = ~0;

      auto addPart = [&](const auto& buffer) {
        const SmcFramePartInfo fp{ partNumber++, buffer.data(), buffer.size() };
        fa.AddFramePart(fp);
      };

      fa.AddFramePart({ 0, &hdr, sizeof hdr });
      addPart(a1);
      addPart(a2);
      addPart(a3);
      addPart(a4);

      REQUIRE(completedFrame == true);
      REQUIRE(samplesCount == 96);
      REQUIRE(pHeader != nullptr); // No longer a valid pointer, just check for non-null
      REQUIRE(pSamples != nullptr); // No longer a valid pointer, just check for non-null

      REQUIRE(hdrReceived.FrameIndex == frameIndex);

      for (unsigned i = 0; i < samplesReceived.size(); ++i) {
        REQUIRE(samplesReceived[i] == i);
      }
    }
  }
}
