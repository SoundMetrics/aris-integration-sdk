// reorder frame
#include "Reorder.h"
#include <fstream>
#include <iostream>

int main() {
    using namespace Aris;

    // Build input frame header
    ArisFrameHeader inHeader;
    inHeader.PingMode = 9;
    inHeader.SamplesPerBeam = 512;
    inHeader.ReorderedSamples = 0;

    std::cout << "Reordering frame with PingMode=" << inHeader.PingMode
              << " and SamplesPerBeam=" << inHeader.SamplesPerBeam << std::endl;

    // Read unordered image data from input file
    const auto dataSize = PingModeToNumBeams(inHeader.PingMode) * inHeader.SamplesPerBeam;
    auto unorderedData = std::vector<uint8_t>(dataSize, 0xAA);
    std::ifstream inputFile("pingmode9samples512.dat", std::ifstream::binary);

    if (!inputFile.is_open()) {
        std::cerr << "ERROR: image data file not found" << std::endl;
        return 1;
    }

    inputFile.read((char *)&unorderedData[0], dataSize);

    // Copy frame header and image data to result frame
    auto buffer = std::vector<uint8_t>(kFrameHeaderSize + dataSize, 0xAA);
    std::copy((uint8_t *)&inHeader, (uint8_t *)&inHeader + kFrameHeaderSize, std::begin(buffer));
    std::copy(std::begin(unorderedData), std::end(unorderedData), &buffer[kFrameHeaderSize]);
    auto result = std::make_shared<Frame>(&buffer[0], buffer.size());

    // Reorder result frame
    Reorder(result);

    std::cout << "Reordering complete" << std::endl;

    return 0;
}
