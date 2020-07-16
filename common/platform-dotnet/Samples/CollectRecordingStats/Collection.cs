﻿using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CollectRecordingStats
{

    public class CollectionResult
    {
        public List<CollectedFile> Files { get; private set; }

        public CollectionResult() {}

        public CollectionResult(IEnumerable<CollectedFile> files)
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
            Log.Information("Logging folder {folderPath}", folderPath);

            var startTime = DateTime.Now;

            var filesResult = GatherFiles(folderPath);
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            var fileCount = filesResult.Files.Count;
            var earlyExits = filesResult.Files.Count(f => f.EarlyExit);
            var fileBytes = filesResult.Files.Sum(f => f.FileLength);
            var fileGiBs = (double)fileBytes / (1024 * 1024 * 1024);

            Log.Information(
                "Completed in {duration}. {fileCount} files; {earlyExists} early exists; {fileGiBs} GiB",
                duration, fileCount, earlyExits, fileGiBs);

            if (earlyExits > 0)
            {
                foreach (var errorMessage in
                    filesResult.Files.Where(f => f.EarlyExit).Select(f => f.ErrorMessage))
                {
                    Log.Information("Early exit: \"{errorMessage}\"", errorMessage);
                }
            }

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
