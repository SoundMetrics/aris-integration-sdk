using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.Raw;

namespace SoundMetrics.Aris.Core.UT
{
    using static AcousticSettingsRaw;

    [TestClass]
    public class AcousticSettingsRaw_RangeOperations_Test
    {
        private static readonly EnvironmentalConditions TestEnvironment = EnvironmentalConditions.Default;
        private static readonly SystemType SystemType = SystemType.Aris3000;
        private static readonly SystemConfiguration sysCfg;

        static AcousticSettingsRaw_RangeOperations_Test()
        {
            sysCfg = SystemConfiguration.GetConfiguration(SystemType);
        }

        private static AcousticSettingsRaw GetClosestRange(int samplesPerBeam)
            => GetNearRange(samplesPerBeam,  addStartDelay: FineDuration.Zero);

        private static AcousticSettingsRaw GetNearRange(int samplesPerBeam, FineDuration addStartDelay)
        {
            var speedOfSound = TestEnvironment.SpeedOfSound;

            var sampleStartDelay = sysCfg.RawConfiguration.SampleStartDelayRange.Minimum + addStartDelay;
            var samplePeriod = sysCfg.RawConfiguration.SamplePeriodRange.Minimum;
            var pulseWidth = sysCfg.RawConfiguration.PulseWidthRange.Minimum;
            var pingMode = sysCfg.DefaultPingMode;
            var enableTransmit = true;
            var frequency = FrequencySelection.High;
            var enable150Volts = true;
            var receiverGain = sysCfg.ReceiverGainRange.Minimum;
            var frameRate = Rate.PerSecond(1);
            var pingsPerFrame = pingMode.PingsPerFrame;
            var framePeriod = 1 / frameRate;
            var cyclePeriod = framePeriod / pingsPerFrame;

            var windowStart = CalculateWindowStart(sampleStartDelay, speedOfSound);
            var windowLength = CalculateWindowLength(samplesPerBeam, samplePeriod, speedOfSound);
            var focusPosition = (windowStart + windowLength) / 2;

            return new AcousticSettingsRaw(
                SystemType,
                Rate.PerSecond(1),
                samplesPerBeam,
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
            const int SamplesPerBeam = 1200;

            var startSettings = GetClosestRange(SamplesPerBeam);
            var result = AdjustRangeOperations.MoveWindowStartIn(startSettings);
            var expectedSampleStartDelay = startSettings.SampleStartDelay;
            var expectedWindowStart = startSettings.WindowStart;
            var expectedWindowEnd = startSettings.WindowEnd;

            Assert.AreEqual(expectedSampleStartDelay, result.SampleStartDelay);
            Assert.AreEqual(expectedWindowStart, result.WindowStart);
            Assert.AreEqual(expectedWindowEnd, result.WindowEnd);
            Assert.AreEqual(startSettings.SamplesPerBeam, result.SamplesPerBeam);
        }

        [TestMethod]
        public void MoveWindowStartIn_FromNearMinDistance()
        {
            const int SamplesPerBeam = 1200;

            var closestRange = GetClosestRange(SamplesPerBeam);
            var startSettings = GetNearRange(SamplesPerBeam, addStartDelay: FineDuration.FromMicroseconds(20));

            Assert.AreNotEqual(closestRange.SampleStartDelay, startSettings.SampleStartDelay);
            Assert.AreNotEqual(closestRange.WindowStart, startSettings.WindowStart);

            var result = AdjustRangeOperations.MoveWindowStartIn(startSettings);
            var expectedSampleStartDelay = sysCfg.RawConfiguration.SampleStartDelayRange.Minimum;
            var expectedWindowStart = closestRange.WindowStart;

            Assert.AreEqual(expectedSampleStartDelay, result.SampleStartDelay, "unexpected sample start delay");
            Assert.AreEqual(expectedWindowStart, result.WindowStart, "unexpected window start");
            Assert.AreEqual(startSettings.SamplesPerBeam, result.SamplesPerBeam, "sample count should not change");
        }
    }
}
