using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.File
{
    [Flags]
    public enum FileIssue : UInt16
    {
        None = 0,
        EmptyFile = 0b00000000_00000001,
        IncompleteFileHeader = 0b00000000_00000010,
        InvalidFileHeader = 0b00000000_00000100,
        NoFrames = 0b00000000_00001000,
        InvalidFirstFrameHeader = 0b00000000_00010000,
    }

    internal static class FileIssueDescriptions
    {
        internal static string GetFlagDescription(FileIssue issue)
        {
            CheckExactlyOneFlag();
            return issueDescriptions[issue];

            void CheckExactlyOneFlag()
            {
                if (CountSetBits((uint)issue) > 1)
                {
                    throw new ArgumentException("More than one bit flag was set");
                }

                // Hamming Weight, via K&R, via Wegner
                // https://stackoverflow.com/a/37558380/83202
                static int CountSetBits(uint n)
                {
                    // count the number of bits set in n
                    int c; // c accumulates the total bits set in n

                    for (c = 0; n > 0; n = n & (n - 1))
                    {
                        c++;
                    }

                    return c;
                }
            }
        }

        internal static IEnumerable<string> GetFlagDescriptions(FileIssue issues)
        {
            const FileIssue LSB = (FileIssue)1;

            while (issues != 0)
            {
                var oneIssue = issues & LSB;
                yield return issueDescriptions[oneIssue];

                issues = ShiftRight(issues);
            }

            static FileIssue ShiftRight(FileIssue issues) =>
                (FileIssue)((uint)issues >> 1);
        }

        private static readonly Dictionary<FileIssue, string> issueDescriptions =
            new Dictionary<FileIssue, string>
            {
                { FileIssue.EmptyFile, "The file is empty" },
                { FileIssue.IncompleteFileHeader, "The file header is incomplete" },
                { FileIssue.InvalidFileHeader, "The file header is invalid" },
                { FileIssue.NoFrames, "The file contains no frames" },
                { FileIssue.InvalidFirstFrameHeader, "The first frame header is invalid" },
            };
    }
}
