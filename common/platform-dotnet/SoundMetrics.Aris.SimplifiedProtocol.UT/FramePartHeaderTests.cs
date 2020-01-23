using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SoundMetrics.Aris.SimplifiedProtocol.UT
{
    [TestClass]
    public class FramePartHeaderTests
    {
        private static readonly int headerSize = Marshal.SizeOf<FramePacketHeader>();

        [TestMethod]
        public void FromEmptyArray()
        {
            var success = FramePartHeaderExtensions.FromBytes(new byte[0], out var _);
            Assert.IsFalse(success);
        }

        [TestMethod]
        public void RequireValidSignature()
        {
            var buf = new byte[Marshal.SizeOf<FramePacketHeader>()];
            var success = FramePartHeaderExtensions.FromBytes(buf, out var _);
            Assert.IsFalse(success);
        }

        [TestMethod]
        public void ValidateHeaderSize()
        {
            var signature = Encoding.ASCII.GetBytes("ARIS");
            var remainder = new byte[headerSize - 4];
            var bytes = signature.Concat(remainder).ToArray();
            var success = FramePartHeaderExtensions.FromBytes(bytes, out var _);
            Assert.IsFalse(success);
        }

        [TestMethod]
        public void SuccessfulCreation()
        {
            var signature = Encoding.ASCII.GetBytes("ARIS");
            var size = new byte[1] { (byte)headerSize };
            var remainder = new byte[headerSize - 5];
            var bytes = signature.Concat(size).Concat(remainder).ToArray();

            FramePacketHeader header;
            var success = FramePartHeaderExtensions.FromBytes(bytes, out header);

            Assert.IsTrue(success);
            Assert.AreEqual<uint>((uint)headerSize, header.HeaderSize);
        }
    }
}
