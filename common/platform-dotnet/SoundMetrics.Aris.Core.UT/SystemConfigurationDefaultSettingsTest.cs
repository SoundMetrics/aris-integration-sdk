using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.ApprovalTests;

namespace SoundMetrics.Aris.Core
{
    [TestClass]
    [UseApprovalSubdirectory("approval-files"), UseReporter(typeof(DiffReporter))]
    public sealed class SystemConfigurationDefaultSettingsTest
    {
        private static readonly ObservedConditions observedConditions = new((Temperature)15, (Distance)5);
        private static readonly Salinity salinity = Salinity.Fresh;

        [TestMethod]
        public void Aris3000DefaultSettings()
        {
            RunTest(nameof(Aris3000DefaultSettings), SystemType.Aris3000);
        }

        [TestMethod]
        public void Aris1800DefaultSettings()
        {
            RunTest(nameof(Aris1800DefaultSettings), SystemType.Aris1800);
        }

        [TestMethod]
        public void Aris1200DefaultSettings()
        {
            RunTest(nameof(Aris1200DefaultSettings), SystemType.Aris1200);
        }

        private static void RunTest(string testName, SystemType systemType)
        {
            var helper = new PrettyPrintHelper(0);
            helper.PrintHeading(testName);

            var systemCfg = SystemConfiguration.GetConfiguration(systemType);

            helper.PrintHeading("Inputs");

            using (var _ = helper.PushIndent())
            {
                helper.PrintValue(nameof(observedConditions), observedConditions);
                helper.PrintValue(nameof(salinity), salinity);
            }

            var defaultSettings =
                systemCfg.GetDefaultSettings(observedConditions, salinity, out var windowBounds);

            helper.PrintHeading($"Default settings for [{systemType}]");
            using (var _ = helper.PushIndent())
            {
                helper.PrintValue(nameof(windowBounds), windowBounds);
                helper.PrintValue(nameof(defaultSettings), defaultSettings);
            }

            Approvals.Verify(helper.ToString());
        }
    }
}
