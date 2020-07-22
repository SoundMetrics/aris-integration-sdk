using SoundMetrics.Aris.Data.Wrappers;
using SoundMetrics.Aris.File;
using System;

namespace CollectRecordingStats
{
    public static class FileInfo
    {
        public static CollectedFile Gather(string filePath)
        {
            long frameCount = 0;
            DateTime? firstFrameSonarTimestamp = null;
            DateTime? lastFrameSonarTimestamp = null;
            ulong? firstGoTime = null;
            ulong? lastGoTime = null;

            var fileLength = ArisFile.GetFileLength(filePath);

            try
            {
                foreach (var frameHeader in ArisFile.EnumerateFrameHeaders(filePath))
                {
                    ++frameCount;

                    var (sonarTimestamp, goTime) =
                        frameHeader.WithParts(
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

            }
            catch (Exception ex)
            {
                return new CollectedFile(filePath, ex.Message);
            }

            return new CollectedFile(
                filePath,
                fileLength,
                frameCount,
                firstFrameSonarTimestamp,
                lastFrameSonarTimestamp,
                firstGoTime,
                lastGoTime);
        }
    }
}
