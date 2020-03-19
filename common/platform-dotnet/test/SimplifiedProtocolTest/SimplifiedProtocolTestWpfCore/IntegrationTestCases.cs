using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;

namespace SimplifiedProtocolTestWpfCore
{
    using IntegrationTestFunc =
        Func<
            string,
            SynchronizationContext,
            ITestOperations,
            IObservable<Frame>,
            Frame,
            CancellationToken,
            IntegrationTestResult>;

    internal static partial class IntegrationTest
    {
        private static IntegrationTestCase[] CreateTestCases()
        {
            return EnumerateTestCases(typeof(TestCases))
                        .Select(ToTestFunction)
                        .ToArray();

            IEnumerable<MethodInfo> EnumerateTestCases(Type type)
            {
                return type
                        .GetMethods()
                        .Where(methodInfo => methodInfo.DeclaringType == type);
            }

            IntegrationTestCase
                ToTestFunction(MethodInfo methodInfo)
            {
                IntegrationTestFunc testFunction =
                    (name, syncContext, testOperations, framesObservable, previousFrame, ct) =>
                    {
                        object? output =
                            methodInfo.Invoke(
                                null,
                                new object?[]
                                {
                                        name,
                                        syncContext,
                                        testOperations,
                                        framesObservable,
                                        previousFrame,
                                        ct,
                                });
                        if (output is IntegrationTestResult result)
                        {
                            return result;
                        }
                        else
                        {
                            throw new Exception(
                                "Test case returned an unexpected result");
                        }
                    };

                var testName = methodInfo.Name;

                return new IntegrationTestCase
                {
                    TestName = testName,
                    TestFunction = testFunction,
                };
            }
        }

        private static class TestCases
        {
            public static IntegrationTestResult DummyTest(
                string name,
                SynchronizationContext syncContext,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                Frame previousFrame,
                CancellationToken ct)
            {
                WaitOnAFrame(syncContext, testOperations, frameObservable, ct);
                throw new Exception("DummyTest");
            }

            public static IntegrationTestResult DummyTest2(
                string name,
                SynchronizationContext syncContext,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                Frame previousFrame,
                CancellationToken ct)
            {
                return new IntegrationTestResult
                {
                    TestName = name,
                    Success = false,
                    Messages = new List<string> { "derp" },
                };
            }
        }
    }
}
