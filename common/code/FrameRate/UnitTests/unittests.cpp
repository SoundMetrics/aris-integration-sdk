#include "CppUnitTest.h"
#include "FrameRate.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace Aris::AcousticMath;

namespace UnitTests
{		
    TEST_CLASS(FrameRateTests)
    {
    private:

        const double kDelta = 0.01;

    public:

        // lower slope cycle period adjustment
        TEST_METHOD(SamplesPerBeamLessThan2000FrameRateTest)
        {
            const double expected = 6.04;
            const double actual = CalculateMaxFrameRate(SystemType::Aris3000,
                9,      // samplePeriod
                1999,   // samplesPerBeam
                19281,  // cyclePeriod
                8);     // pingsPerFrame

            Assert::AreEqual(expected, actual, kDelta);
        }

        // higher slope cycle period adjustment
        TEST_METHOD(SamplesPerBeamGreaterThan2000FrameRateTest)
        {
            const double expected = 5.03;
            const double actual = CalculateMaxFrameRate(SystemType::Aris3000,
                9,      // samplePeriod
                2400,   // samplesPerBeam
                22890,  // cyclePeriod
                8);     // pingsPerFrame

            Assert::AreEqual(expected, actual, kDelta);
        }

        // no cycle period delay
        TEST_METHOD(SamplePeriod7FrameRateTest)
        {
            const double expected = 14.84;
            const double actual = CalculateMaxFrameRate(SystemType::Aris3000,
                7,     // samplePeriod
                1518,  // samplesPerBeam
                7362,  // cyclePeriod
                8);    // pingsPerFrame

            Assert::AreEqual(expected, actual, kDelta);
        }

        // some cycle period delay
        TEST_METHOD(SamplePeriod6FrameRateTest)
        {
            const double expected = 14.01;
            const double actual = CalculateMaxFrameRate(SystemType::Aris3000,
                6,     // samplePeriod
                1518,  // samplesPerBeam
                7362,  // cyclePeriod
                8);    // pingsPerFrame

            Assert::AreEqual(expected, actual, kDelta);
        }

        // most cycle period delay
        TEST_METHOD(SamplePeriod4FrameRateTest)
        {
            const double expected = 13.07;
            const double actual = CalculateMaxFrameRate(SystemType::Aris3000,
                4,     // samplePeriod
                1518,  // samplesPerBeam
                7362,  // cyclePeriod
                8);    // pingsPerFrame

            Assert::AreEqual(expected, actual, kDelta);
        }
    };
}
