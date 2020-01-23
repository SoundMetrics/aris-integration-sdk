using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var packetHeader = FramePacketHeaderExtensions.FromBytes(new byte[0]);
            Assert.IsFalse(packetHeader.HasValue);
        }

        [TestMethod]
        public void RequireValidSignature()
        {
            var buf = new byte[Marshal.SizeOf<FramePacketHeader>()];
            var packetHeader = FramePacketHeaderExtensions.FromBytes(buf);
            Assert.IsFalse(packetHeader.HasValue);
        }

        [TestMethod]
        public void ValidateHeaderSize()
        {
            var signature = Encoding.ASCII.GetBytes("ARIS");
            var remainder = new byte[headerSize - 4];
            var bytes = signature.Concat(remainder).ToArray();
            var packetHeader = FramePacketHeaderExtensions.FromBytes(bytes);
            Assert.IsFalse(packetHeader.HasValue);
        }

        [TestMethod]
        public void SuccessfulCreation()
        {
            var signature = Encoding.ASCII.GetBytes("ARIS");
            var size = new byte[1] { (byte)headerSize };
            var remainder = new byte[headerSize - 5];
            var bytes = signature.Concat(size).Concat(remainder).ToArray();

            var packetHeader = FramePacketHeaderExtensions.FromBytes(bytes);

            Assert.IsTrue(packetHeader.HasValue);
            Assert.AreEqual<uint>((uint)headerSize, packetHeader.Value.HeaderSize);
        }
    }
}
