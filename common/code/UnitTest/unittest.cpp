#include "CppUnitTest.h"
#include "Reorder.h"
#include "Depth.h"
#include <vector>
#include <fstream>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace Aris::AcousticMath;

namespace UnitTest
{	
    bool ReorderTest(uint32_t pingMode, uint32_t samplesPerBeam,
        const char * inputFileName, const char * expectedFileName)
    {
        using namespace Aris;

        const auto dataSize = PingModeToNumBeams(pingMode) * samplesPerBeam;

        // Build input frame header
        ArisFrameHeader header;
        header.PingMode = pingMode;
        header.SamplesPerBeam = samplesPerBeam;
        header.ReorderedSamples = 0;

        // Read unordered image data from input file
        auto samples = std::vector<uint8_t>(dataSize, 0xAA);
        std::ifstream inputFile(inputFileName, std::ifstream::binary);
        inputFile.read((char *)&samples[0], dataSize);

        // Reorder samples 
        Reorder(header, &samples[0]);

        // Read reordered image data from expected results file
        auto reorderedData = std::vector<uint8_t>(dataSize, 0xFF);
        std::ifstream expectedFile(expectedFileName, std::ifstream::binary);
        expectedFile.read((char *)&reorderedData[0], dataSize);

        // Compare result of reordering with expected data
        if (memcmp(&samples[0], &reorderedData[0], dataSize) != 0) {
            return false;
        }

        // Verify that ReorderedSamplse flag is set after reordering
        if (header.ReorderedSamples != 1) {
            return false;
        }

        return true;
    }

    TEST_CLASS(ReorderTests)
    {
    public:

        TEST_METHOD(PingMode1Freq1200ReorderTest)
        {
            bool success = ReorderTest(1, 512, "../../data/input_pingmode_1_1200.dat", "../../data/expected_pingmode_1_1200.dat");
            
            Assert::IsTrue(success);
        }

        TEST_METHOD(PingMode1Freq1800ReorderTest)
        {
            bool success = ReorderTest(1, 544, "../../data/input_pingmode_1_1800.dat", "../../data/expected_pingmode_1_1800.dat");

            Assert::IsTrue(success);
        }

        TEST_METHOD(PingMode3ReorderTest)
        {
            bool success = ReorderTest(3, 512, "../../data/input_pingmode_3.dat", "../../data/expected_pingmode_3.dat");

            Assert::IsTrue(success);
        }

        TEST_METHOD(PingMode6ReorderTest)
        {
            bool success = ReorderTest(6, 410, "../../data/input_pingmode_6.dat", "../../data/expected_pingmode_6.dat");

            Assert::IsTrue(success);
        }

        TEST_METHOD(PingMode9ReorderTest)
        {
            bool success = ReorderTest(9, 512, "../../data/input_pingmode_9.dat", "../../data/expected_pingmode_9.dat");

            Assert::IsTrue(success);
        }

    };

    TEST_CLASS(DepthTests)
    {
    private:

        double kAtmosphere = 14.6959;
        double kMetersPerPSI = 0.702398;

    public:
        
        TEST_METHOD(FreshWaterDepthTest)
        {
            const double pressure = 20.0;
            const uint32_t salinity = 0;
            const double waterTemp = 15;
            const double freshWaterDensity15C = 0.999;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / freshWaterDensity15C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);

            Assert::AreEqual(actual, expected);
        }

        TEST_METHOD(BrackishWaterDepthTest)
        {
            const double pressure = 32.0;
            const uint32_t salinity = 15;
            const double waterTemp = 15;
            const double brackishWaterDensity15C = 1.011;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / brackishWaterDensity15C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);

            Assert::AreEqual(actual, expected);
        }

        TEST_METHOD(SaltWaterDepthTest)
        {
            const double pressure = 65.0;
            const uint32_t salinity = 35;
            const double waterTemp = 15;
            const double saltWaterDensity15C = 1.026;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / saltWaterDensity15C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);
            const bool success = (actual == expected);

            Assert::AreEqual(actual, expected);
        }

        TEST_METHOD(WarmWaterDepthTest)
        {
            const double pressure = 23.0;
            const uint32_t salinity = 35;
            const double waterTemp = 40;
            const double saltWaterDensity30C = 1.022;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / saltWaterDensity30C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);

            Assert::AreEqual(actual, expected);
        }

        // salinity of 50 PPT is invalid so treat as salt water
        TEST_METHOD(WrongWaterDepthTest)
        {
            const double pressure = 37.0;
            const uint32_t salinity = 50;
            const double waterTemp = 10;
            const double saltWaterDensity10C = 1.027;
            const double expected = (pressure - kAtmosphere) * kMetersPerPSI / saltWaterDensity10C;
            const double actual = CalculateDepthM(pressure, salinity, waterTemp);
            const bool success = (actual == expected);

            Assert::AreEqual(actual, expected);
        }

    };
}