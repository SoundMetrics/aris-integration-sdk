#pragma once

#include "Frame.h"
#include <memory>
#include <fstream>

//#define DEBUG_RECORDING

class WriteableArisRecording
{
public:

  // Creates a writable recording file. Returns an unassociated unique_ptr on
  // failure. If no frames are written the file is deleted in the destructor.
  // If a file already exists it is overwritten.
  static std::unique_ptr<WriteableArisRecording> Create(const char * path);

  // Use Create() rather than the constructor.
  WriteableArisRecording(const char * path, std::fstream && initializedOutfile);
  ~WriteableArisRecording();

  bool WriteFrame(const Frame & frame);

private:
  const std::string path_;
  std::fstream outfile_;
  uint32_t frameCount_;

#ifdef DEBUG_RECORDING
  unsigned beamCount_, sampleCount_;
#endif

  static bool WriteFileHeader(std::fstream & outfile);
};

