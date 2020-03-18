using SoundMetrics.Aris.SimplifiedProtocol;
using System;

namespace SimplifiedProtocolTestWpfCore
{
    internal static partial class IntegrationTest
    {
        private static IntegrationTestCase[] CreateTestCases()
        {
            return new[]
            {
                MakeTestCase("Dummy test", DummyTest),
            };
        }

        private static IntegrationTestCase
            MakeTestCase(
                string name,
                IntegrationTestRunner testRunner)
        {
            return new IntegrationTestCase
            {
                Name = name,
                IntegrationTestRunner = testRunner,
            };
        }

        private static IntegrationTestResult DummyTest(
            string name,
            ITestOperations testOperations,
            IObservable<Frame> frameObservable)
        {
            throw new Exception("DummyTest");
        }
    }
}
