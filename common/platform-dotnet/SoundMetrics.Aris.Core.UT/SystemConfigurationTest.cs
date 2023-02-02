using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.Raw;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    public sealed class SystemConfigurationTest
    {
        [TestMethod]
        public void TestInitializationOfDeviceLimits()
        {
            // Device limits must be initialized before configurations
            // are initialized. These are static fields, so we need to
            // ensure they are initialized correctly.

            var maxDeviceSamples =
                SystemType.Aris3000
                    .GetConfiguration()
                    .SampleCountDeviceLimits
                    .Maximum;
            Assert.AreNotEqual(default, maxDeviceSamples);
        }

        [TestMethod]
        public void TestGuidedSampleCountsAvailable()
        {
            Assert.AreNotEqual(
                default,
                AdjustWindowTerminusGuided.SampleCountLimits[SystemType.Aris1200]);
            Assert.AreNotEqual(
                default,
                AdjustWindowTerminusGuided.SampleCountLimits[SystemType.Aris1800]);
            Assert.AreNotEqual(
                default,
                AdjustWindowTerminusGuided.SampleCountLimits[SystemType.Aris3000]);
        }
    }
}
