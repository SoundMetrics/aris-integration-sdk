#include "sample-code/frame-assembler-lite/cpp/FrameAssemblerLite.hpp"
#include "catch2/catch.hpp"

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

}
