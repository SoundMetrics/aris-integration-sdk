using System;

namespace CollectRecordingStats
{
    public class CollectedFile
    {
        public string Path { get; private set; }
        public long FileLength { get; private set; }
        public long FrameCount { get; private set; }
        public bool EarlyExit { get; private set; }

        public DateTime? FirstFrameSonarTimestamp { get; private set; }
        public DateTime? LastFrameSonarTimestamp { get; private set; }
        public TimeSpan? RecordingSpan
        {
            get => LastFrameSonarTimestamp - FirstFrameSonarTimestamp;
        }

        public TimeSpan? AverageFramePeriod { get; private set; }

        public ulong? FirstGoTime { get; private set; }
        public ulong? LastGoTime { get; private set; }
        public double? AverageGoTimePeriodMicros { get; private set; }
        public string ErrorMessage { get; private set; }

        public CollectedFile(
            string path,
            long fileLength,
            long frameCount,
            DateTime? firstFrameSonarTimestamp,
            DateTime? lastFrameSonarTimestamp,
            ulong? firstGoTime,
            ulong? lastGoTime)
            : this(path)
        {
            if (frameCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameCount));
            }

            Path = path;
            FileLength = fileLength;
            FrameCount = frameCount;
            FirstFrameSonarTimestamp = firstFrameSonarTimestamp;
            LastFrameSonarTimestamp = lastFrameSonarTimestamp;
            FirstGoTime = firstGoTime;
            LastGoTime = lastGoTime;
            ErrorMessage = "";

            // Calculate averages when it makes sense.
            if (frameCount > 1)
            {
                var intervals = frameCount - 1;

                if (firstFrameSonarTimestamp is DateTime firstSonarTS
                    && lastFrameSonarTimestamp is DateTime lastSonarTS)
                {
                    AverageFramePeriod = (lastSonarTS - firstSonarTS) / intervals;
                }

                if (firstGoTime is ulong firstGo && lastGoTime is ulong lastGo)
                {
                    AverageGoTimePeriodMicros = (double)(lastGo - firstGo) / intervals;
                }
            }
        }

        public CollectedFile(string path, string errorMessage)
            : this(path)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Missing error message");
            }

            ErrorMessage = errorMessage;
            EarlyExit = true;
        }

        private CollectedFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path;
        }
    }
}
