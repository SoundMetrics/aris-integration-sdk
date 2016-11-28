// reorder frame
#include "Reorder.h"
#include <string.h>
#include <fstream>
#include <iostream>

bool validate_inputs(int argc, char* argv[],
                     const char** inputPath, const char** expectedFile);
void show_usage();
std::vector<uint8_t> read_frame(std::ifstream & file);

int main(int argc, char * argv[]) {
    using namespace Aris;

    const char * inputPath;
    const char * expectedPath;

    if (!validate_inputs(argc, argv, &inputPath, &expectedPath)) {
        show_usage();
        return 1;
    }

    std::ifstream inputFile(inputPath);
    std::ifstream expectedFile(expectedPath);

    if (!inputFile.is_open()) {
        std::cerr << "ERROR: input data file not found" << std::endl;
        return 1;
    }

    if (!expectedFile.is_open()) {
        std::cerr << "ERROR: expected data file not found" << std::endl;
        return 1;
    }

    std::cout << "input data: ";
    auto inputBuf = read_frame(inputFile);
    std::cout << "expected data: ";
    auto expectedBuf= read_frame(expectedFile);

    Frame result(&inputBuf[0], inputBuf.size());
    Reorder(result);

    const auto dataSize = expectedBuf.size() - kFrameHeaderSize;

    // Compare result of reordering with expected data
    if (memcmp(result.GetData(), &expectedBuf[kFrameHeaderSize], dataSize) != 0) {
        std::cerr << "Result of reordering does not match expected data." << std::endl;
        return 1;
    }

    // Copy ArisFrameHeader from result
    const auto outHeader = result.GetHeader();

    // Verify that ReorderedSamplse flag is set after reordering
    if (outHeader.ReorderedSamples != 1) {
        std::cerr << "ReorderedSamples flag not set in frame header for result of reordering." << std::endl;
        return 1;
    }

    std::cout << "Reordering successful." << std::endl;

    return 0;
}

void show_usage() {
    std::cerr << "USAGE:" << std::endl
              << "    reorderframe <input-path> <expected-path>" << std::endl
              << std::endl;
}

bool validate_inputs(int argc, char * argv[],
                     const char** inputPath, const char** expectedPath) {
    if (argc != 3) {
        std::cerr << "Bad number of arguments." << std::endl;
        return false;
    }

    *inputPath = argv[1];
    *expectedPath = argv[2];

    if (strlen(*inputPath) == 0) {
        std::cerr << "No input path." << std::endl;
        return false;
    }

    if (strlen(*expectedPath) == 0) {
        std::cerr << "No expected path." << std::endl;
        return false;
    }

    return true;
}

std::vector<uint8_t> read_frame(std::ifstream & file) {
    using namespace Aris;

    ArisFrameHeader inHeader;

    // Read ArisFrameHeader from file
    file.read((char *)&inHeader, kFrameHeaderSize);

    std::cout << "PingMode=" << inHeader.PingMode
              << " SamplesPerBeam=" << inHeader.SamplesPerBeam << std::endl;

    // Read unordered image data from file    
    const auto dataSize = PingModeToNumBeams(inHeader.PingMode) * inHeader.SamplesPerBeam;
    auto unorderedData = std::vector<uint8_t>(dataSize);
    file.read((char *)&unorderedData[0], dataSize);

    // Copy ArisFrameHeader and image data to in-memory Frame
    auto buffer = std::vector<uint8_t>(kFrameHeaderSize + dataSize);
    std::copy((uint8_t *)&inHeader, (uint8_t *)&inHeader + kFrameHeaderSize, std::begin(buffer));
    std::copy(std::begin(unorderedData), std::end(unorderedData), &buffer[kFrameHeaderSize]);

    return buffer;
}
