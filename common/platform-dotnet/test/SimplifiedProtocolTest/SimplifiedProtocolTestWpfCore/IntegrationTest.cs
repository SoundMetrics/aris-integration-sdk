using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimplifiedProtocolTestWpfCore
{
    using IntegrationTestFunc =
        Func<
            SynchronizationContext,
            ITestOperations,
            IObservable<Frame>,
            CancellationToken,
            IntegrationTestResult>;

    struct IntegrationTestCase
    {
        public string TestName;
        public IntegrationTestFunc TestFunction;
    }

    internal static partial class IntegrationTest
    {
        public static Task<IntegrationTestResult[]>
            RunAsync(
                SynchronizationContext uiSyncContext,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                CancellationToken ct)
        {
            var testCases = CreateTestCases();
            return Task<IntegrationTestResult[]>.Run(
                () => RunTestCases(uiSyncContext, testOperations, frameObservable, testCases, ct));
        }

        private static IntegrationTestResult[] RunTestCases(
            SynchronizationContext syncContext,
            ITestOperations testOperations,
            IObservable<Frame> frameObservable,
            IEnumerable<IntegrationTestCase> localTestCases,
            CancellationToken ct)
        {
            var cachedResults = RunTestCases().ToArray();
            var failedResults = cachedResults.Where(result => !result.Success).ToArray();
            Serilog.Log.Information($"RunTestCases: total results: {cachedResults.Length}; failed: {failedResults.Length}");

            return failedResults;

            IEnumerable<IntegrationTestResult> RunTestCases()
            {
                foreach (var testCase in localTestCases)
                {
                    if (ct.IsCancellationRequested)
                    {
                        // Return a "cancelled" result only if
                        // there were more test cases to work on.
                        yield return new IntegrationTestResult
                        {
                            Success = false,
                            Messages = new List<string> { "Test run cancelled" },
                        };

                        break;
                    }

                    // Get frames flowing if they haven't already started.
                    testOperations.StartTestPattern();

                    yield return RunTestSafe(
                                    syncContext,
                                    testOperations,
                                    frameObservable,
                                    testCase,
                                    ct);
                }
            }
        }

        private static IntegrationTestResult RunTestSafe(
            SynchronizationContext syncContext,
            ITestOperations testOperations,
            IObservable<Frame> frameObservable,
            IntegrationTestCase testCase,
            CancellationToken ct)
        {
            var testName = testCase.TestName;

            try
            {
                if (testCase.TestFunction(
                                syncContext,
                                testOperations,
                                frameObservable,
                                ct)
                    is IntegrationTestResult testResult)
                {
                    return testResult;
                }
                else
                {
                    return new IntegrationTestResult
                    {
                        Success = false,
                        TestName = testName,
                        Messages = FormatMessages("Test did not return a valid result"),
                    };
                }
            }
            catch (Exception ex)
            {
                return new IntegrationTestResult
                {
                    Success = false,
                    TestName = testName,
                    Messages = FormatMessages(FormatTestException(ex)),
                };
            }

            List<string> FormatMessages(params string[] messages)
            {
                return new List<string>(messages);
            }

            string FormatTestException(Exception ex)
            {
                var exceptionMessage = new StringBuilder();
                exceptionMessage.AppendLine(
                    $"Ttest threw a {ex.GetType().FullName} exception: {ex.Message}");

                if (ex.InnerException != null)
                {
                    exceptionMessage.AppendLine($"Inner exception: {ex.InnerException.Message}");
                }

                exceptionMessage.AppendLine("Stack trace:");
                exceptionMessage.AppendLine(ex.StackTrace?.ToString());

                return exceptionMessage.ToString();
            }
        }
    }
}
