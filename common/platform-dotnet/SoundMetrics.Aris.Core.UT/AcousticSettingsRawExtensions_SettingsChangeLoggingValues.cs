using Microsoft.VisualStudio.TestTools.UnitTesting;

using static SoundMetrics.Aris.Core.Raw.AcousticSettingsRawExtensions;

namespace SoundMetrics.Aris.Core
{

    [TestClass]
    public sealed class AcousticSettingsRawExtensions_SettingsChangeLoggingValues
    {
        [TestMethod]
        public void TestDoubleFormat()
        {
            var input = 1.125;
            var expected = "1.125";
            var actual = GetInvariantFormatttedString(input);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSingleFormat()
        {
            var input = 1.125f;
            var expected = "1.125";
            var actual = GetInvariantFormatttedString(input);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestDecimalFormat()
        {
            var input = 1.125m;
            var expected = "1.125";
            var actual = GetInvariantFormatttedString(input);
            Assert.AreEqual(expected, actual);
        }
    }
}
