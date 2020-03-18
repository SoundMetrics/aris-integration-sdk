using SoundMetrics.Aris.SimplifiedProtocol;
using System;

namespace SimplifiedProtocolTestWpfCore
{
    using IntegrationTestCase =
        Func<string, ITestOperations, IObservable<Frame>, Frame, IntegrationTestResult>;

    internal static partial class IntegrationTest
    {
        private static IntegrationTestCase[] CreateTestCases()
        {
            return new IntegrationTestCase[]
            {
                DummyTest,
            };
        }

        private static IntegrationTestResult DummyTest(
            string name,
            ITestOperations testOperations,
            IObservable<Frame> frameObservable,
            Frame previousFrame)
        {
            throw new Exception("DummyTest");
        }
    }
}
