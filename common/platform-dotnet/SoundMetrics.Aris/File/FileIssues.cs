using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.File
{
    [Flags]
#pragma warning disable CA1028 // Enum Storage should be Int32
    public enum FileIssues : UInt16
#pragma warning restore CA1028 // Enum Storage should be Int32
    {
        None = 0,
        EmptyFile =                 0b00000000_00000001,
        IncompleteFileHeader =      0b00000000_00000010,
        InvalidFileHeader =         0b00000000_00000100,
        NoFrames =                  0b00000000_00001000,
        InvalidFirstFrameHeader =   0b00000000_00010000,
        InvalidFrameHeaders =       0b00000000_00100000,
    }

    internal static class FileIssueDescriptions
    {
        internal static string GetFlagDescription(FileIssues issues)
        {
            CheckExactlyOneFlag();
            return issueDescriptions[issues];

            void CheckExactlyOneFlag()
            {
                if (CountSetBits((uint)issues) > 1)
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

        internal static IEnumerable<string> GetFlagDescriptions(FileIssues issues)
        {
            UInt16 bit = 0b10000000_00000000;
            UInt16 uIssues = (UInt16)issues;

            do
            {
                if ((bit & uIssues) != 0)
                {
                    var oneIssue = (FileIssues)bit;
                    yield return issueDescriptions[oneIssue];
                }
            } while ((bit >>= 1) != 0);
        }

        private static readonly Dictionary<FileIssues, string> issueDescriptions =
            new Dictionary<FileIssues, string>
            {
                { FileIssues.EmptyFile, "The file is empty" },
                { FileIssues.IncompleteFileHeader, "The file header is incomplete" },
                { FileIssues.InvalidFileHeader, "The file header is invalid" },
                { FileIssues.NoFrames, "The file contains no frames" },
                { FileIssues.InvalidFirstFrameHeader, "The first frame header is invalid" },
                { FileIssues.InvalidFrameHeaders, "There are one or more invalid frame headers" },
            };
    }
}
