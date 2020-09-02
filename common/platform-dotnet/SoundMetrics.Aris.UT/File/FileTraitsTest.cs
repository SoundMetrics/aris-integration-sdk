using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Data;
using SoundMetrics.Aris.File;
using System;
using System.IO;

namespace SoundMetrics.Aris.UT.File
{
    [TestClass]
    public sealed class FileTraitsTest
    {
        [TestMethod]
        public void EmptyFile()
        {
            var path = CreateEmptyFile();
            var fileTraits = FileTraits.GetFileTraits(path);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssue.EmptyFile));
        }

        [TestMethod]
        public void IncompleteFileHeader()
        {
            var path = CreateFileWithPartialFileHeader();
            var fileTraits = FileTraits.GetFileTraits(path);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssue.IncompleteFileHeader));
        }

        [TestMethod]
        public void InvalidFileHeader()
        {
            var path = CreateFileWithInvalidFileHeader();
            var fileTraits = FileTraits.GetFileTraits(path);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssue.InvalidFileHeader));
        }

        [TestMethod]
        public void NoFrames()
        {
            using var memStream = new MemoryStream();
            memStream.Write(AValidFileHeader.Value);
            memStream.Write(AValidFrameHeader.Value);

            var path = CreateFileWithContents(memStream.ToArray());
            var fileTraits = FileTraits.GetFileTraits(path);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssue.NoFrames));
        }


        [TestMethod]
        public void InvalidFirstFrameHeader()
        {
            using var memStream = new MemoryStream();
            memStream.Write(AValidFileHeader.Value);
            memStream.Write(new byte[] { 1 });

            var path = CreateFileWithContents(memStream.ToArray());
            var fileTraits = FileTraits.GetFileTraits(path);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssue.InvalidFirstFrameHeader));
        }

        private static string CreateEmptyFile()
        {
            return CreateEmptyTempFile();
        }

        private static string CreateFileWithPartialFileHeader()
            => CreateFileWithContents(new byte[] { 1 });

        private static string CreateFileWithInvalidFileHeader()
            => CreateFileWithContents(AnInvalidFileHeader.Value);

        private static string CreateFileWithValidFileHeader()
            => CreateFileWithContents(AValidFileHeader.Value);

        private static readonly Lazy<byte[]> AnInvalidFileHeader =
            new Lazy<byte[]>(() => new byte[1024]);

        private static readonly Lazy<byte[]> AValidFileHeader =
            new Lazy<byte[]>(() =>
            {
                var fileHeader = new FileHeader { Version = FileHeader.ArisFileSignature };
                using var memStream = new MemoryStream();
                memStream.WriteStruct(fileHeader);
                return memStream.ToArray();
            });

        private static readonly Lazy<byte[]> AValidFrameHeader =
            new Lazy<byte[]>(() =>
            {
                var frameHeader = new FrameHeader
                {
                    Version = FrameHeader.ArisFrameSignature,
                    TheSystemType = 1u,
                    PingMode = 9,
                    SamplesPerBeam = 200,
                };

                using var memStream = new MemoryStream();
                memStream.WriteStruct(frameHeader);
                return memStream.ToArray();
            });

        private static string CreateEmptyTempFile()
        {
            var path = Path.GetTempFileName();
            return path;
        }

        private static string CreateFileWithContents(Span<byte> buffer)
        {
            var path = CreateEmptyTempFile();
            using var file = System.IO.File.OpenWrite(path);
            file.Write(buffer);

            return path;
        }
    }
}
