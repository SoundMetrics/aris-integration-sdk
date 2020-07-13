using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.File;
using System;

namespace CollectRecordingStats
{
    public static class FileInfo
    {
        public static CollectionFile Gather(string filePath)
        {
            string errorMessage = null;
            long frameCount = 0;
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

                    var sonarTimestamp = frame.FrameHeader.GetSonarTimestamp();
                    var goTime = frame.FrameHeader.GoTime;

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
                    break;
                }
            }

            if (errorMessage is null)
            {
                return new CollectionFile(
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
                return new CollectionFile(filePath, errorMessage);
            }
        }
    }
}
