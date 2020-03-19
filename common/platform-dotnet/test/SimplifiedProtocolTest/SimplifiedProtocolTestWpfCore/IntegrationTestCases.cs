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
                    (name, syncContext, testOperations, framesObservable, earlierFrame, ct) =>
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
                                        earlierFrame,
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
            public static IntegrationTestResult ToPassiveThenToTestPattern(
                string name,
                SynchronizationContext syncContext,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                Frame earlierFrame,
                CancellationToken ct)
            {
                testOperations.StartPassiveMode();

                Predicate<Frame> anyValidCookie = frame => frame.Header.AppliedSettings > 0;

                if (testOperations.WaitOnAFrame(syncContext, anyValidCookie, ct) is Frame passiveFrame)
                {
                    var passiveSettingsCookie = passiveFrame.Header.AppliedSettings;

                    testOperations.StartTestPattern();

                    Predicate<Frame> isNewCookie = frame => frame.Header.AppliedSettings > passiveSettingsCookie;
                    if (testOperations.WaitOnAFrame(syncContext, isNewCookie, ct) is Frame frame)
                    {
                        return new IntegrationTestResult
                        {
                            Success = true,
                            TestName = name,
                            Messages = new List<string> { "Found new settings after switch to test pattern." },
                        };
                    }
                    else
                    {
                        return new IntegrationTestResult
                        {
                            Success = false,
                            TestName = name,
                            Messages = new List<string>
                            {
                                "Couldn't detect settings change on change to test pattern.",
                            },
                        };
                    }
                }
                else
                {
                    throw new Exception("Could not get a frame after going passive.");
                }
            }
        }
    }
}
