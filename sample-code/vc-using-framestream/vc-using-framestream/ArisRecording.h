#pragma once

#include "Frame.h"
#include <memory>
#include <fstream>

class WriteableArisRecording
{
public:
  // Creates a writable recording file. Returns an unassociated unique_ptr on
  // failure. If no frames are written the file is deleted in the destructor.
  // If a file already exists it is overwritten.
  static std::unique_ptr<WriteableArisRecording> Create(const char * path);

  // Use Create() rather than the constructor.
  WriteableArisRecording(const char * path, std::ofstream && initializedOutfile);
  ~WriteableArisRecording();

  bool WriteFrame(const Frame & frame);

private:
  const std::string path_;
  std::ofstream outfile_;
  uint32_t frameCount_;

  static bool WriteFileHeader(std::ofstream & outfile);
};

