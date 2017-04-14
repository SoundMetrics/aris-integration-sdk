#include "ArisRecording.h"
#include "FrameHeader.h"
#include "FileHeader.h"
#include "Reorder.h"
#include <assert.h>
#include <functional>

std::unique_ptr<WriteableArisRecording> WriteableArisRecording::Create(const char * path)
{
  try {
    // Overwrite the file if it exists.
    std::ofstream outfile(path, std::ios::binary | std::ios::out | std::ios::trunc);
    if (outfile) {
      if (WriteFileHeader(outfile)) {
        return std::make_unique<WriteableArisRecording>(path, std::move(outfile));
      }

      // No reason to continue if we can't write the file header.
      outfile.close();
      std::remove(path);

      // Fall through.
    }
  }
  catch (...) {
    // Fall through.
  }

  // Unassociated ptr == failure.
  return std::unique_ptr<WriteableArisRecording>();
}

WriteableArisRecording::WriteableArisRecording(const char * path, std::ofstream && initializedOutfile)
  : path_(path)
  , outfile_(std::move(initializedOutfile))
  , frameCount_(0)
{
}

WriteableArisRecording::~WriteableArisRecording()
{
  if (outfile_.is_open()) {
    outfile_.seekp(0, std::ios_base::end);
    const bool hasFrames = outfile_.tellp() > sizeof ArisFileHeader;
    outfile_.close();

    // If it's an empty file, remove it.
    if (!hasFrames) {
      std::remove(path_.c_str());
    }
  }
}

/* static */
bool WriteableArisRecording::WriteFileHeader(std::ofstream & outfile)
{
  ArisFileHeader header;
  memset(&header, 0, sizeof header);
  header.Version = ARIS_FILE_SIGNATURE;

  assert(outfile.tellp() == 0);

  outfile.write(reinterpret_cast<const char*>(&header), sizeof header);
  return !outfile.fail();
}

//-----------------------------------------------------------------------------
// Helpers for write & restore stream position

struct WriteContext {
  std::ofstream & outfile;
  std::streampos basePos;
};

template <typename T>
void writeAtOffset(const WriteContext & context, std::streamoff offset, const T & value) {
  context.outfile.seekp(context.basePos + offset);
  context.outfile.write(reinterpret_cast<const char*>(&value), sizeof T);
}

static void doAndRestorePos(
  std::ofstream & outfile,
  std::streampos basePos,
  std::function<void(const WriteContext&)> f)
{
  const auto startPos = outfile.tellp();
  const WriteContext context = { outfile, basePos };
  f(context);
  outfile.seekp(startPos); // our own copy of startPos, so no shenanigans
}

//-----------------------------------------------------------------------------

bool WriteableArisRecording::WriteFrame(const Frame & frame)
{
  const auto frameHeaderStartPos = outfile_.tellp();

  const uint32_t frameIndex = frameCount_; // zero-based
  const uint32_t frameCount = ++frameCount_;

  // Write header
  const auto & hdr = frame.Header();
  outfile_.write(reinterpret_cast<const char*>(&hdr), sizeof hdr);

  // Update file header values on the first frame. Doing so before writing the
  // sample data ensures that if an error occurs while updating the file header,
  // the file will be recognized as empty and deleted later.

  if (frameCount == 1) {
    doAndRestorePos(outfile_, 0, // file header, offset from the start of the file
      [hdr](auto & context) {
      writeAtOffset(context, ArisFileHeaderOffset_SamplesPerChannel,
        hdr.SamplesPerBeam);

      writeAtOffset(context, ArisFileHeaderOffset_NumRawBeams,
        Aris::PingModeToNumBeams(hdr.PingMode));

      writeAtOffset(context, ArisFileHeaderOffset_SN,
        hdr.SonarSerialNumber);
    });
  }

  // Write sample data
  if (outfile_) {
    const auto frameDataStartPos = outfile_.tellp();

    const auto & samples = frame.Samples();
    outfile_.write(reinterpret_cast<const char*>(samples.data()), samples.size());

    if (outfile_) {
      doAndRestorePos(outfile_, 0, // file header, offset from the start of the file
        [frameCount](auto & context) {

        // Frame count
        writeAtOffset(context, ArisFileHeaderOffset_FrameCount,
          frameCount);
      });

      // Be sure to write your own frame index (zero-based) to accommodate
      // incomplete/missing frames.
      doAndRestorePos(outfile_, frameDataStartPos,
        [frameIndex](auto & context) {
        writeAtOffset(context, ArisFrameHeaderOffset_FrameIndex,
          frameIndex);
      });

      return true;
    }
  }

  // Rewind on failure.
  outfile_.seekp(frameHeaderStartPos);
  return false;
}
