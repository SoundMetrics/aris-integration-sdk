﻿using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimplifiedProtocolTestWpfCore
{
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
            ITestOperations testOperaions,
            IObservable<Frame> frameObservable,
            IntegrationTestCase testCase)
        {
            // TODO ### implement
            return new IntegrationTestResult
            {
                Success = false,
                TestName = testCase.Name,
                Messages = new List<string> { "No code to run tests yet" },
            };
        }

        private delegate
            IntegrationTestResult IntegrationTestRunner(
                string name,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable);

        private struct IntegrationTestCase
        {
            public string Name;
            public IntegrationTestRunner IntegrationTestRunner;
        }
    }
}
