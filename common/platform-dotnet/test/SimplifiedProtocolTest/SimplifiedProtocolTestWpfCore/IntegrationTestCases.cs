using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace SimplifiedProtocolTestWpfCore
{
    using IntegrationTestFunc =
        Func<string, ITestOperations, IObservable<Frame>, Frame, IntegrationTestResult>;

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
                    (name, testOperations, framesObservable, previousFrame) =>
                    {
                        object? output =
                            methodInfo.Invoke(
                                null,
                                new object?[]
                                {
                                        name,
                                        testOperations,
                                        framesObservable,
                                        previousFrame,
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
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                Frame previousFrame)
            {
                throw new Exception("DummyTest");
            }

            public static IntegrationTestResult DummyTest2(
                string name,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                Frame previousFrame)
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
