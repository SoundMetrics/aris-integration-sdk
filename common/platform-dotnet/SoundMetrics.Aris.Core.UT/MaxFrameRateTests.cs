using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.Raw;

namespace SoundMetrics.Aris.Core.UT
{
    using static System.Math;

    [TestClass]
    public class MaxFrameRateTests
    {
        private static AcousticSettingsRaw AcousticSettings =
            new AcousticSettingsRaw(
            SystemType.Aris3000,
            frameRate: (Rate)9.3,
            sampleCount: 1250,
            sampleStartDelay: (FineDuration)2626,
            samplePeriod: (FineDuration)8,
            pulseWidth: (FineDuration)13,
            PingMode.PingMode9,
            enableTransmit: true,
            Frequency.High,
            enable150Volts: true,
            receiverGain: 12,
            focusDistance: (Distance)((2+7.6)/2),
            antiAliasing: (FineDuration)0,
            InterpacketDelaySettings.Off,
            Salinity.Fresh // Not supplied in issue
            );

        [TestMethod]
        public void GH_Issue_644_Aris_Applications()
        {
            var _ =
                MaxFrameRate.DetermineMaximumFrameRate(
                    AcousticSettings, out var calculatedCyclePeriod);
            var expectedCyclePeriod = 13050.0;
            var toleranceRatio = 0.01;
            var tolerance = expectedCyclePeriod * toleranceRatio;
            var variance = Abs(expectedCyclePeriod - calculatedCyclePeriod.TotalMicroseconds);
            Assert.IsTrue(variance <= tolerance,
                $"Variance [{variance}] exceeds tolerance [{tolerance}];\n"
                + $"toleranceRatio=[{toleranceRatio}];\n"
                + $"expected=[{expectedCyclePeriod}]; actual=[{calculatedCyclePeriod.TotalMicroseconds}]");
        }
    }
}
