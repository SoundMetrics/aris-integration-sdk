using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CollectRecordingStats
{

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
            var results = existingFolders
                .Select(path => GatherFolderStats(path))
                .ToArray();

            return CollectionResult.Merge(results);
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
