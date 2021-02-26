using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.ApprovalTests;
using SoundMetrics.Aris.Core.Raw;

namespace SoundMetrics.Aris.Core.UT
{
    using static AcousticSettingsRaw;
    using static AcousticSettingsRawCalculations;

    [TestClass]
    [UseReporter(typeof(DiffReporter))]
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
            var cyclePeriod = sampleStartDelay + (sampleCount * samplePeriod);
            var focusPosition = Distance.FromMeters(8);

            return new AcousticSettingsRaw(
                SystemType,
                Rate.ToRate(1),
                sampleCount,
                sampleStartDelay,
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
            var result = WindowOperations.MoveWindowStartIn(startSettings, useMaxFrameRate: false);

            var helper = new PrettyPrintHelper(0);
            helper.PrintHeading("Inputs");
            using (var _ = helper.PushIndent())
            {
                helper.PrintValue("startSettings", startSettings);
            }

            helper.PrintHeading("Outputs");
            using (var _ = helper.PushIndent())
            {
                helper.PrintValue("result", result);
            }

            Approvals.Verify(helper.ToString());
        }

        [TestMethod]
        public void MoveWindowStartIn_FromNearMinDistance()
        {
            const int SampleCount = 1200;

            var closestRange = GetClosestRange(SampleCount);
            var startSettings = GetNearRange(SampleCount, addStartDelay: FineDuration.FromMicroseconds(20));

            Assert.AreNotEqual(closestRange.SampleStartDelay, startSettings.SampleStartDelay);
            Assert.AreNotEqual(closestRange.WindowStart, startSettings.WindowStart);

            var result = WindowOperations.MoveWindowStartIn(startSettings, useMaxFrameRate: false);

            var helper = new PrettyPrintHelper(0);
            helper.PrintHeading("Inputs");
            using (var _ = helper.PushIndent())
            {
                helper.PrintValue("startSettings", startSettings);
            }

            helper.PrintHeading("Outputs");
            using (var _ = helper.PushIndent())
            {
                helper.PrintValue("result", result);
            }

            Approvals.Verify(helper.ToString());
        }
    }
}
