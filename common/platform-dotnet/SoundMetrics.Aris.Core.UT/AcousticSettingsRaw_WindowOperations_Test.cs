using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.Raw;

namespace SoundMetrics.Aris.Core.UT
{
    using static AcousticSettingsRaw;
    using static AcousticSettingsRawCalculations;

    [TestClass]
    public class AcousticSettingsRaw_WindowOperations_Test
    {
        private static readonly EnvironmentalContext TestEnvironment = EnvironmentalContext.Default;
        private static readonly SystemType SystemType = SystemType.Aris3000;
        private static readonly SystemConfiguration sysCfg;

        static AcousticSettingsRaw_WindowOperations_Test()
        {
            sysCfg = SystemConfiguration.GetConfiguration(SystemType);
        }

        private static AcousticSettingsRaw GetClosestRange(int sampleCount)
            => GetNearRange(sampleCount,  addStartDelay: FineDuration.Zero);

        private static AcousticSettingsRaw GetNearRange(int sampleCount, FineDuration addStartDelay)
        {
            var sampleStartDelay = sysCfg.RawConfiguration.SampleStartDelayRange.Minimum + addStartDelay;
            var samplePeriod = sysCfg.RawConfiguration.SamplePeriodRange.Minimum;
            var pulseWidth = sysCfg.RawConfiguration.PulseWidthRange.Minimum;
            var pingMode = sysCfg.DefaultPingMode;
            var enableTransmit = true;
            var frequency = Frequency.High;
            var enable150Volts = true;
            var receiverGain = sysCfg.ReceiverGainRange.Minimum;
            var frameRate = Rate.ToRate(1);
            var pingsPerFrame = pingMode.PingsPerFrame;
            var framePeriod = 1 / frameRate;
            var cyclePeriod = framePeriod / pingsPerFrame;
            var focusPosition = Distance.FromMeters(8);

            return new AcousticSettingsRaw(
                SystemType,
                Rate.ToRate(1),
                sampleCount,
                sampleStartDelay,
                cyclePeriod,
                samplePeriod,
                pulseWidth,
                pingMode,
                enableTransmit,
                frequency,
                enable150Volts,
                receiverGain,
                focusPosition,
                antiAliasing: FineDuration.Zero,
                new InterpacketDelaySettings { },
                sonarEnvironment: TestEnvironment);
        }

        [TestMethod]
        public void MoveWindowStartIn_FromMinDistance()
        {
            const int SampleCount = 1200;

            var startSettings = GetClosestRange(SampleCount);
            var result = WindowOperations.MoveWindowStartIn(startSettings);
            var expectedSampleStartDelay = startSettings.SampleStartDelay;
            var expectedWindowStart = startSettings.WindowStart;
            var expectedWindowEnd = startSettings.WindowEnd;

            Assert.AreEqual(expectedSampleStartDelay, result.SampleStartDelay);
            Assert.AreEqual(expectedWindowStart, result.WindowStart);
            Assert.AreEqual(expectedWindowEnd, result.WindowEnd);
            Assert.AreEqual(startSettings.SampleCount, result.SampleCount);
        }

        [TestMethod]
        public void MoveWindowStartIn_FromNearMinDistance()
        {
            const int SampleCount = 1200;

            var closestRange = GetClosestRange(SampleCount);
            var startSettings = GetNearRange(SampleCount, addStartDelay: FineDuration.FromMicroseconds(20));

            Assert.AreNotEqual(closestRange.SampleStartDelay, startSettings.SampleStartDelay);
            Assert.AreNotEqual(closestRange.WindowStart, startSettings.WindowStart);

            var result = WindowOperations.MoveWindowStartIn(startSettings);
            var expectedSampleStartDelay = sysCfg.RawConfiguration.SampleStartDelayRange.Minimum;
            var expectedWindowStart = closestRange.WindowStart;

            Assert.AreEqual(expectedSampleStartDelay, result.SampleStartDelay, "unexpected sample start delay");
            Assert.AreEqual(expectedWindowStart, result.WindowStart, "unexpected window start");
            Assert.AreEqual(startSettings.SampleCount, result.SampleCount, "sample count should not change");
        }
    }
}
