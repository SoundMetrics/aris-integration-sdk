// reorder frame
#include "Reorder.h"
#include "FileHeader.h"
#include <string.h>
#include <fstream>
#include <iostream>

bool validate_inputs(int argc, char* argv[], const char** inputPath);
void show_usage();
void reorder(std::ifstream &inputFile);

int main(int argc, char * argv[]) {
    const char * inputPath;

    if (!validate_inputs(argc, argv, &inputPath)) {
        show_usage();
        return 1;
    }

    std::ifstream inputFile(inputPath);

    if (!inputFile.is_open()) {
        std::cerr << "ERROR: image data file not found" << std::endl;
        return 1;
    }

    reorder(inputFile);

    return 0;
}

void show_usage() {
    std::cerr << "USAGE:" << std::endl
              << "    reorderframe <input-path>" << std::endl
              << std::endl;
}

bool validate_inputs(int argc, char * argv[], const char** inputPath) {
    if (argc != 2) {
        std::cerr << "Bad number of arguments." << std::endl;
        return false;
    }

    *inputPath = argv[1];

    if (strlen(*inputPath) == 0) {
        std::cerr << "No input path." << std::endl;
        return false;
    }

    return true;
}

void reorder(std::ifstream &inputFile) {
    using namespace Aris;

    ArisFrameHeader inHeader;

    // Skip over FileHeader.
    inputFile.seekg(sizeof(ArisFileHeader));

    // Read FrameHeader.
    inputFile.read((char *)&inHeader, kFrameHeaderSize);

    std::cout << "Reordering frame PingMode=" << inHeader.PingMode
              << " SamplesPerBeam=" << inHeader.SamplesPerBeam << std::endl;

    // Read unordered image data from input file    
    const auto dataSize = PingModeToNumBeams(inHeader.PingMode) * inHeader.SamplesPerBeam;
    auto unorderedData = std::vector<uint8_t>(dataSize);
    inputFile.read((char *)&unorderedData[0], dataSize);

    // Copy FrameHeader and image data to result Frame
    auto buffer = std::vector<uint8_t>(kFrameHeaderSize + dataSize);
    std::copy((uint8_t *)&inHeader, (uint8_t *)&inHeader + kFrameHeaderSize, std::begin(buffer));
    std::copy(std::begin(unorderedData), std::end(unorderedData), &buffer[kFrameHeaderSize]);
    auto result = std::make_shared<Frame>(&buffer[0], buffer.size());

    // Reorder result Frame
    Reorder(result);

    std::cout << "Reordering complete." << std::endl;
}