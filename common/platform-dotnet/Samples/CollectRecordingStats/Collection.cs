using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CollectRecordingStats
{
    public class CollectionFile
    {
        public string Path { get; private set; }
        public long FileLength { get; private set; }
        public long FrameCount { get; private set; }
        public DateTime? FirstFrameSonarTimestamp { get; private set; }
        public DateTime? LastFrameSonarTimestamp { get; private set; }
        public TimeSpan? AverageFramePeriod { get; private set; }
        public ulong? FirstGoTime { get; private set; }
        public ulong? LastGoTime { get; private set; }
        public double? AverageGoTimePeriodMicros { get; private set; }
        public string ErrorMessage { get; private set; }

        public CollectionFile(
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

        public CollectionFile(string path, string errorMessage)
            : this(path)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Missing error message");
            }

            ErrorMessage = errorMessage;
        }

        private CollectionFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path;
        }
    }

    public class CollectionResult
    {
        public List<CollectionFile> Files { get; private set; }

        public CollectionResult() {}

        public CollectionResult(IEnumerable<CollectionFile> files)
        {
            Files = files.ToList();
        }

        internal static CollectionResult Merge(params CollectionResult[] results)
            => Merge((IEnumerable<CollectionResult>)results);

        internal static CollectionResult Merge(IEnumerable<CollectionResult> results)
            => new CollectionResult
                {
                    Files = results.SelectMany(r => r.Files).ToList()
                };
    }

    public static class Collection
    {
        public static CollectionResult GatherAllStats(IEnumerable<string> folderPaths)
        {
            var existingFolders = folderPaths.Where(path => Directory.Exists(path));
            var tasks = existingFolders
                .Select(path => Task.Run(() => GatherFolderStats(path)))
                .ToArray();

            Task.WaitAll(tasks);
            return CollectionResult.Merge(tasks.Select(t => t.Result));
        }

        private static CollectionResult GatherFolderStats(string folderPath)
        {
            var filesResult = GatherFiles(folderPath);
            var folders = Directory.EnumerateDirectories(folderPath);
            var foldersResults = GatherAllStats(folders);

            return CollectionResult.Merge(filesResult, foldersResults);
        }

        private static CollectionResult GatherFiles(string folderPath)
        {
            return
                new CollectionResult(
                    Directory.GetFiles(folderPath, "*.aris")
                        .Select(FileInfo.Gather)
                        .ToList());
        }
    }
}
