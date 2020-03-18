using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimplifiedProtocolTestWpfCore
{
    using IntegrationTestCase =
        Func<string, ITestOperations, IObservable<Frame>, IntegrationTestResult>;

    internal static partial class IntegrationTest
    {
        public static Task<IntegrationTestResult[]>
            RunAsync(
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                CancellationToken ct)
        {
            var testCases = CreateTestCases();
            return Task<IntegrationTestResult[]>.Run(
                () => RunTestCases(testOperations, frameObservable, testCases, ct));
        }

        private static IntegrationTestResult[] RunTestCases(
            ITestOperations testOperations,
            IObservable<Frame> frameObservable,
            IEnumerable<IntegrationTestCase> localTestCases,
            CancellationToken ct)
        {
            return RunTestCases().ToArray();

            IEnumerable<IntegrationTestResult> RunTestCases()
            {
                foreach (var testCase in localTestCases)
                {
                    if (ct.IsCancellationRequested)
                    {
                        // Return a failed "cancelled" result only if
                        // there were more test cases to work on.
                        yield return new IntegrationTestResult
                        {
                            Success = false,
                            Messages = new List<string> { "Test run cancelled" },
                        };

                        break;
                    }

                    yield return RunTestSafe(testOperations, frameObservable, testCase);
                }
            }
        }

        private static IntegrationTestResult RunTestSafe(
            ITestOperations testOperations,
            IObservable<Frame> frameObservable,
            IntegrationTestCase testCase)
        {
            // TODO ### implement

            // ### Func<string, ITestOperations, IObservable<Frame>, IntegrationTestResult>
            var methodInfo = testCase.Method;
            var testName = methodInfo.Name;

            var callParameters = new object[]
            {   testName,
                testOperations,
                frameObservable
            };

            try
            {
                var result = methodInfo.Invoke(
                    null, // instance object
                    callParameters);

                if (result is IntegrationTestResult testResult)
                {
                    return testResult;
                }
                else
                {
                    return new IntegrationTestResult
                    {
                        Success = false,
                        TestName = testName,
                        Messages = new List<string> { "Test did not return a valid result" },
                    };
                }
            }
            catch (Exception ex)
            {
                return new IntegrationTestResult
                {
                    Success = false,
                    TestName = testName,
                    Messages = new List<string>
                    {
                        "Test threw an exception: " + ex.Message
                    },
                };
            }

        }
    }
}
