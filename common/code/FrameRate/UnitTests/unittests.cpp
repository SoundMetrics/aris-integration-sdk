#include "CppUnitTest.h"
#include "FrameRate.h"
#include <algorithm>
#include <string>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace Aris::AcousticMath;

namespace UnitTests
{
    using SystemType = Aris::Common::SystemType;
    using FineDuration = unsigned;
    using uint = unsigned;

    struct TestCase
    {
        SystemType      systemType;
        FineDuration    sampleStartDelay;
        FineDuration    samplePeriod;
        int             samplesPerBeam;
        uint            pingMode;
        FineDuration    antialiasing;
        FineDuration    interpacketDelay;

        double          expectedFrameRate;
    };

    constexpr double kDelta = 0.05;
    constexpr double kMaxArisFrameRate = 15.0;

    // From internal "ARIS Max Frame Rate Test Cases docx"
    static const TestCase testInputs[] = {
        // System, SSD, SP, SPB, pingMode, NumBeams, Frame Rate
        { SystemType::Aris3000, 930, 4, 1750, 9, 0, 0, 13.92 },
        { SystemType::Aris3000, 930, 4, 4000, 9, 0, 0, 6.7 },
        { SystemType::Aris3000, 930, 5, 4000, 9, 0, 0, 5.71 },
        { SystemType::Aris3000, 930, 6, 4000, 9, 0, 0, 4.81 },
        { SystemType::Aris1800, 930, 4, 4000, 3, 0, 0, 9.13 },
        { SystemType::Aris1800, 930, 5, 4000, 3, 0, 0, 7.61 },
        { SystemType::Aris1800, 930, 6, 4000, 3, 0, 0, 6.41 },
        { SystemType::Aris1200, 930, 4, 4000, 1, 0, 0, 15 },
        { SystemType::Aris1200, 930, 5, 4000, 1, 0, 0, 15.46 },
    };

    TEST_CLASS(FrameRateTests)
    {
    public:

        TEST_METHOD(MaxFrameRateTest)
        {
            int idxTestCase = 1;

            std::for_each(
                std::begin(testInputs),
                std::end(testInputs),
                [&idxTestCase](const auto& testCase) {
                    bool enableInterpacketDelay = testCase.interpacketDelay != 0;

                    auto actual =
                        CalculateMaxFrameRate(
                            testCase.systemType,
                            testCase.pingMode,
                            testCase.samplesPerBeam,
                            testCase.sampleStartDelay,
                            testCase.samplePeriod,
                            testCase.antialiasing,
                            enableInterpacketDelay,
                            testCase.interpacketDelay);

                    auto expected =
                        std::min(testCase.expectedFrameRate, kMaxArisFrameRate);

                    wchar_t message[1024];
                    swprintf(message, sizeof message / sizeof message[0],
                        L"Test case %d", idxTestCase);

                    Assert::AreEqual(expected, actual, kDelta, message);

                    ++idxTestCase;
                });
        }
    };
}
