using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimplifiedProtocolTestWpfCore
{
    using IntegrationTestFunc =
        Func<string, ITestOperations, IObservable<Frame>, Frame, IntegrationTestResult>;

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
            SynchronizationContext uiSyncContext,
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
                        // Return a "cancelled" result only if
                        // there were more test cases to work on.
                        yield return new IntegrationTestResult
                        {
                            Success = false,
                            Messages = new List<string> { "Test run cancelled" },
                        };

                        break;
                    }

                    if (WaitOnAFrame() is Frame previousFrame)
                    {
                        yield return RunTestSafe(testOperations, frameObservable, testCase, previousFrame);
                    }
                    else
                    {
                        yield return new IntegrationTestResult
                        {
                            TestName = testCase.TestName,
                            Success = false,
                            Messages = new List<string>
                            {
                                "Couldn't receive a frame; please start acquisition before running tests"
                            },
                        };
                    }
                }
            }

            Frame? WaitOnAFrame()
            {
                Frame? receivedFrame = null;

                var timeout = TimeSpan.FromSeconds(2);
                var observation =
                    frameObservable
                        .FirstOrDefaultAsync()
                        .ObserveOn(uiSyncContext);

                using (var timeoutCancellation = new CancellationTokenSource(timeout))
                using (var doneSignal = new ManualResetEventSlim())
                {
                    observation.Subscribe(
                        frame =>
                        {
                            Interlocked.Exchange(ref receivedFrame, frame);
                            doneSignal.Set();
                        },
                        timeoutCancellation.Token);

                    doneSignal.Wait(timeout);
                }

                return receivedFrame;
            }
        }

        private static IntegrationTestResult RunTestSafe(
            ITestOperations testOperations,
            IObservable<Frame> frameObservable,
            IntegrationTestCase testCase,
            Frame previousFrame)
        {
            var testName = testCase.TestName;

            try
            {
                if (testCase.TestFunction(testName, testOperations, frameObservable, previousFrame)
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
                        Messages = new List<string> { "Test did not return a valid result" },
                    };
                }
            }
            catch (Exception ex)
            {
                var exceptionMessage = new StringBuilder();
                exceptionMessage.AppendLine($"Test threw an exception: {ex.Message}");
                if (ex.InnerException != null)
                {
                    exceptionMessage.AppendLine($"Inner exception: {ex.InnerException.Message}");
                }

                return new IntegrationTestResult
                {
                    Success = false,
                    TestName = testName,
                    Messages = new List<string>
                    {
                        exceptionMessage.ToString()
                    },
                };
            }

        }
    }
}
