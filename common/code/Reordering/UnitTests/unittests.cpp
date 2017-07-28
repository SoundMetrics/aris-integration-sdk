#include "CppUnitTest.h"
#include "Reorder.h"
#include <vector>
#include <fstream>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace UnitTests
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
        if (memcmp(&samples[0], &reorderedData[0], dataSize) != 0)
        {
            return false;
        }

        // Verify that ReorderedSamplse flag is set after reordering
        if (header.ReorderedSamples != 1)
        {
            return false;
        }

        return true;
    }

    TEST_CLASS(ReorderTests)
    {
    private:

        // x64 DLL is one level deeper
        const std::string dataDir = (sizeof(int32_t *) == 8) ? "../../data/" : "../data/";

    public:

        TEST_METHOD(PingMode1Freq1200ReorderTest)
        {
            const auto inputPath = dataDir + "input_pingmode_1_1200.dat";
            const auto expectedPath = dataDir + "expected_pingmode_1_1200.dat";
            bool success = ReorderTest(1, 512, inputPath.c_str(), expectedPath.c_str());

            Assert::IsTrue(success);
        }

        TEST_METHOD(PingMode1Freq1800ReorderTest)
        {
            const auto inputPath = dataDir + "input_pingmode_1_1800.dat";
            const auto expectedPath = dataDir + "expected_pingmode_1_1800.dat";
            bool success = ReorderTest(1, 544, inputPath.c_str(), expectedPath.c_str());

            Assert::IsTrue(success);
        }

        TEST_METHOD(PingMode3ReorderTest)
        {
            const auto inputPath = dataDir + "input_pingmode_3.dat";
            const auto expectedPath = dataDir + "expected_pingmode_3.dat";
            bool success = ReorderTest(3, 512, inputPath.c_str(), expectedPath.c_str());

            Assert::IsTrue(success);
        }

        TEST_METHOD(PingMode6ReorderTest)
        {
            const auto inputPath = dataDir + "input_pingmode_6.dat";
            const auto expectedPath = dataDir + "expected_pingmode_6.dat";
            bool success = ReorderTest(6, 410, inputPath.c_str(), expectedPath.c_str());

            Assert::IsTrue(success);
        }

        TEST_METHOD(PingMode9ReorderTest)
        {
            const auto inputPath = dataDir + "input_pingmode_9.dat";
            const auto expectedPath = dataDir + "expected_pingmode_9.dat";
            bool success = ReorderTest(9, 512, inputPath.c_str(), expectedPath.c_str());

            Assert::IsTrue(success);
        }
    };
}
