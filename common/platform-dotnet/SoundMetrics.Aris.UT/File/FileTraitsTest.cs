﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.Data;
using System;
using System.IO;

namespace SoundMetrics.Aris.File
{
    [TestClass]
    public sealed class FileTraitsTest
    {
        [TestMethod]
        public void NoFile()
        {
            var path = "this file does not exist, anywhere";
            var processedFile = FileTraits.GetFileTraits(path, validateFrameHeaders: false, out var fileTraits);
            Assert.IsFalse(processedFile);
            Assert.IsNull(fileTraits);
        }

        [TestMethod]
        public void EmptyFile()
        {
            var path = CreateEmptyFile();
            var processedFile = FileTraits.GetFileTraits(path, validateFrameHeaders: false, out var fileTraits);
            Assert.IsTrue(processedFile);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssues.EmptyFile));
        }

        [TestMethod]
        public void IncompleteFileHeader()
        {
            var path = CreateFileWithPartialFileHeader();
            var processedFile = FileTraits.GetFileTraits(path, validateFrameHeaders: false, out var fileTraits);
            Assert.IsTrue(processedFile);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssues.IncompleteFileHeader));
        }

        [TestMethod]
        public void InvalidFileHeader()
        {
            var path = CreateFileWithInvalidFileHeader();
            var processedFile = FileTraits.GetFileTraits(path, validateFrameHeaders: false, out var fileTraits);
            Assert.IsTrue(processedFile);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssues.InvalidFileHeader));
        }

        [TestMethod]
        public void NoFrames()
        {
            using var memStream = new MemoryStream();
            memStream.Write(AValidFileHeader.Value);
            memStream.Write(AValidFrameHeader.Value);

            var path = CreateFileWithContents(memStream.ToArray());
            var processedFile = FileTraits.GetFileTraits(path, validateFrameHeaders: false, out var fileTraits);
            Assert.IsTrue(processedFile);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssues.NoFrames));
        }


        [TestMethod]
        public void InvalidFirstFrameHeader()
        {
            using var memStream = new MemoryStream();
            memStream.Write(AValidFileHeader.Value);
            memStream.Write(new byte[] { 1 });

            var path = CreateFileWithContents(memStream.ToArray());
            var processedFile = FileTraits.GetFileTraits(path, validateFrameHeaders: false, out var fileTraits);
            Assert.IsTrue(processedFile);
            Assert.IsTrue(fileTraits.HasIssues);
            Assert.IsTrue(fileTraits.HasIssue(FileIssues.InvalidFirstFrameHeader));
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
                    SonarSerialNumber = 42,
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
