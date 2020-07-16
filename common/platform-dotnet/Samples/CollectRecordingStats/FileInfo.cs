using SoundMetrics.Aris.Data.Wrappers;
using SoundMetrics.Aris.File;
using System;

namespace CollectRecordingStats
{
    public static class FileInfo
    {
        public static CollectedFile Gather(string filePath)
        {
            string errorMessage = null;
            long frameCount = 0;
            bool earlyExit = false;
            DateTime? firstFrameSonarTimestamp = null;
            DateTime? lastFrameSonarTimestamp = null;
            ulong? firstGoTime = null;
            ulong? lastGoTime = null;

            var fileLength = ArisFile.GetFileLength(filePath);

            foreach (var frame in ArisFile.EnumerateFrameHeaders(filePath))
            {
                if (frame.Success)
                {
                    ++frameCount;

                    var (sonarTimestamp, goTime) =
                        frame.FrameHeader.WithParts(
                            (in ArisFrameHeaderParts hdr) =>
                            {
                                var timing = hdr.Time;
                                return (timing.SonarTimestamp, timing.GoTime);
                            });

                    if (!firstFrameSonarTimestamp.HasValue)
                    {
                        firstFrameSonarTimestamp = sonarTimestamp;
                        firstGoTime = goTime;
                    }

                    lastFrameSonarTimestamp = sonarTimestamp;
                    lastGoTime = goTime;
                }
                else
                {
                    errorMessage = frame.ErrorMessage;
                    earlyExit = true;
                    break;
                }
            }

            if (errorMessage is null)
            {
                return new CollectedFile(
                    filePath,
                    fileLength,
                    frameCount,
                    firstFrameSonarTimestamp,
                    lastFrameSonarTimestamp,
                    firstGoTime,
                    lastGoTime);
            }
            else
            {
                return new CollectedFile(filePath, errorMessage);
            }
        }
    }
}
