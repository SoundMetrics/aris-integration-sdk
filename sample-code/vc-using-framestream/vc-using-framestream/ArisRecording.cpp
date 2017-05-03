#include "ArisRecording.h"
#include "FrameHeader.h"
#include "FileHeader.h"
#include "Reorder.h"
#include <assert.h>
#include <functional>

#ifdef DEBUG_RECORDING
#include <iostream>
#endif

//-----------------------------------------------------------------------------
// Helpers for write & restore stream position

struct FileOpContext {
  std::fstream & outfile;
  std::streampos basePos;
};

template <typename T>
void writeAtOffset(const FileOpContext & context, std::streamoff offset, const T & value) {
  context.outfile.seekp(context.basePos + offset);
  context.outfile.write(reinterpret_cast<const char*>(&value), sizeof T);
}

template <typename T>
T readAtOffset(const FileOpContext & context, std::streamoff offset) {
  context.outfile.seekp(context.basePos + offset);
  T t;
  context.outfile.read(reinterpret_cast<char*>(&t), sizeof t);
  return t;
}

static void doAndRestorePos(
  std::fstream & outfile,
  std::streampos basePos,
  std::function<void(const FileOpContext&)> f)
{
  const auto startPos = outfile.tellp();
  const FileOpContext context = { outfile, basePos };
  f(context);
  outfile.seekp(startPos); // our own copy of startPos, so no shenanigans
}


std::unique_ptr<WriteableArisRecording> WriteableArisRecording::Create(const char * path)
{
  try {
    // Overwrite the file if it exists.
    std::fstream outfile(path, 
      std::ios::binary | std::ios::in | std::ios::out | std::ios::trunc);
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

WriteableArisRecording::WriteableArisRecording(const char * path, std::fstream && initializedOutfile)
  : path_(path)
  , outfile_(std::move(initializedOutfile))
  , frameCount_(0)
#ifdef DEBUG_RECORDING
  , beamCount_(0)
  , sampleCount_(0)
#endif
{
}

#ifdef DEBUG_RECORDING

template <class _InIt, class _Fn1> inline
void pairwise(_InIt first, _InIt last, _Fn1 func) {
  for (; first != last; ++first) {
    auto right = first;

    if (++right == last)
      break;

    func(*first, *right);
  }

}

static inline void DumpFileInfo(std::fstream & file, unsigned beamCount, unsigned sampleCount) {
  const auto pos = file.tellp();

  std::cout << "beamCount=" << beamCount << "; sampleCount=" << sampleCount << '\n';

  file.seekp(0, std::ios_base::end);
  if (file.tellp() == std::streampos(0)) {
    std::cout << "File is empty.\n";
    return;
  }

  const uint64_t fileLength = file.tellp();
  const uint64_t framesLength = fileLength - 1024;

  const unsigned frameSize = 1024 + beamCount * sampleCount;
  const double calculatedFrameCount = (double)framesLength / frameSize;
  const auto lastFramePos = 1024 + (((uint64_t)calculatedFrameCount - 1) * frameSize);
  std::cout << "There are " << calculatedFrameCount << " frames.\n";
  std::cout << "File length is " << fileLength << '\n';

  doAndRestorePos(file, 0, [&](auto & context) {
    const auto frameCount = readAtOffset<uint32_t>(context, ArisFileHeaderOffset_FrameCount);
    std::cout << "Recorded frame count: " << frameCount << '\n';

    const auto frameIndexInFirstFrame =
      readAtOffset<uint32_t>(context, 1024 + ArisFrameHeaderOffset_FrameIndex);
    const auto frameIndexInLastFrame =
      readAtOffset<uint32_t>(context, lastFramePos + ArisFrameHeaderOffset_FrameIndex);
    std::cout << "Frame index in first frame: " << frameIndexInFirstFrame << '\n';
    std::cout << "Frame index in last frame: " << frameIndexInLastFrame << '\n';

    auto loadFrameIndexes = [&]() {
      // Very clunky to collect all the frame indexes at once, but
      // quick enough for debugging.
      std::vector<uint32_t> frameIndexes;
      for (auto framePos = 1024; framePos <= lastFramePos; framePos += frameSize) {
        const auto fi = readAtOffset<uint32_t>(context, framePos + ArisFrameHeaderOffset_FrameIndex);
        frameIndexes.push_back(fi);
      }

      return frameIndexes;
    };

    auto frameIndexes = std::move(loadFrameIndexes());
    pairwise(frameIndexes.cbegin(), frameIndexes.cend(), [](auto left, auto right) {
      if ((right - left) != 1) {
        std::cout << "*** Discontinuity at [" << left << "," << right << "]\n";
      }
    });
  });

  file.seekp(pos);
}
#endif

WriteableArisRecording::~WriteableArisRecording()
{
#ifdef DEBUG_RECORDING
  DumpFileInfo(outfile_, beamCount_, sampleCount_);
#endif

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
bool WriteableArisRecording::WriteFileHeader(std::fstream & outfile)
{
  ArisFileHeader header;
  memset(&header, 0, sizeof header);
  header.Version = ARIS_FILE_SIGNATURE;

  assert(outfile.tellp() == std::streampos(0));

  outfile.write(reinterpret_cast<const char*>(&header), sizeof header);
  return !outfile.fail();
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

  if (frameIndex == 0) {
    doAndRestorePos(outfile_, 0, // file header, offset from the start of the file
      [this, hdr, &frame](auto & context) {
      writeAtOffset(context, ArisFileHeaderOffset_SamplesPerChannel,
        hdr.SamplesPerBeam);

      writeAtOffset(context, ArisFileHeaderOffset_NumRawBeams,
        Aris::PingModeToNumBeams(hdr.PingMode));

      writeAtOffset(context, ArisFileHeaderOffset_SN,
        hdr.SonarSerialNumber);

#ifdef DEBUG_RECORDING
      sampleCount_ = frame.Header().SamplesPerBeam;
      beamCount_ = frame.Samples().size() / sampleCount_;
#endif
    });
  }

  // Write sample data
  if (outfile_) {
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
      doAndRestorePos(outfile_, frameHeaderStartPos,
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
