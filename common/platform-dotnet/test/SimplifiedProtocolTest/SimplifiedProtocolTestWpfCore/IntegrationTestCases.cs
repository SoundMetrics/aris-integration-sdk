﻿using SoundMetrics.Aris.Headers;
using SoundMetrics.Aris.SimplifiedProtocol;
using SoundMetrics.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace SimplifiedProtocolTestWpfCore
{
    using static RangeGenerator;

    using IntegrationTestFunc =
        Func<
            SynchronizationContext,
            ITestOperations,
            IObservable<Frame>,
            CancellationToken,
            IntegrationTestResult>;

    internal static class ArisFrameExtensions
    {
        public static uint SettingsCookieInUse(this in ArisFrameHeader header)
        {
            // May return zero at the time this is written, March 20, 2020.
            return Math.Max(header.AppliedSettings, header.ConstrainedSettings);
        }
    }

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
                var name = methodInfo.Name;

                IntegrationTestFunc testFunction =
                    (syncContext, testOperations, framesObservable, ct) =>
                    {
                        object? output =
                            methodInfo.Invoke(
                                null,
                                new object?[]
                                {
                                        syncContext,
                                        testOperations,
                                        framesObservable,
                                        ct,
                                });
                        if (output is IntegrationTestResult result)
                        {
                            result.TestName = name;
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

        private static (bool success, Frame? frame) WaitOnSettingsChange(
            SynchronizationContext syncContext,
            ITestOperations testOperations,
            Action<SynchronizationContext, ITestOperations, CancellationToken> testAction,
            CancellationToken ct
            )
        {
            if (testOperations.WaitOnAFrame(syncContext, anyValidCookie, ct) is Frame passiveFrame)
            {
                var previousSettingsInUse = passiveFrame.Header.SettingsCookieInUse();
                Predicate<Frame> isNewSettings = frame => frame.Header.SettingsCookieInUse() > previousSettingsInUse;

                testAction(syncContext, testOperations, ct);

                Frame? frame = testOperations.WaitOnAFrame(syncContext, isNewSettings, ct) as Frame;
                return (frame != null, frame);
            }
            else
            {
                return (false, null);
            }
        }

        private static readonly Predicate<Frame> anyValidCookie = frame => frame.Header.AppliedSettings > 0;

        private static IntegrationTestResult MakeResult(bool success, params string[] messages)
        {
            return new IntegrationTestResult
            {
                Success = success,
                Messages = messages,
            };
        }

        /// <summary>
        /// Test cases go here and are found via reflection.I believe
        /// </summary>
        private static class TestCases
        {
            public static IntegrationTestResult ToPassiveThenToTestPattern(
                SynchronizationContext syncContext,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                CancellationToken ct)
            {
                testOperations.StartPassiveMode();

                if (testOperations.WaitOnAFrame(syncContext, anyValidCookie, ct) is Frame passiveFrame)
                {
                    Action<SynchronizationContext, ITestOperations, CancellationToken> testAction =
                        (syncContext, testOpoerations, CancellationToken)
                            => { testOperations.StartTestPattern(); };

                    var (success, message) =
                        WaitOnSettingsChange(syncContext, testOperations, testAction, ct)
                        switch
                        {
                            (true, Frame frame) => (true, "Found new settings after switch."),
                            _ => (false, "Couldn't detect settings change.")
                        };

                    return MakeResult(success, message);
                }
                else
                {
                    return MakeResult(false, "Could not get a frame after going passive.");
                }
            }

            public static IntegrationTestResult ToTestPatternThenToPassive(
                SynchronizationContext syncContext,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                CancellationToken ct)
            {
                testOperations.StartTestPattern();

                if (testOperations.WaitOnAFrame(syncContext, anyValidCookie, ct) is Frame passiveFrame)
                {
                    Action<SynchronizationContext, ITestOperations, CancellationToken> testAction =
                        (syncContext, testOpoerations, CancellationToken)
                            => { testOperations.StartPassiveMode(); };

                    var (success, message) =
                        WaitOnSettingsChange(syncContext, testOperations, testAction, ct)
                        switch
                        {
                            (true, Frame frame) => (true, "Found new settings after switch."),
                            _ => (false, "Couldn't detect settings change.")
                        };

                    return MakeResult(success, message);
                }
                else
                {
                    return MakeResult(false, "Could not get a frame after going passive.");
                }
            }

            public static IntegrationTestResult ToTestPatternThenToDefaultAcquire(
                SynchronizationContext syncContext,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                CancellationToken ct)
            {
                testOperations.StartTestPattern();

                if (testOperations.WaitOnAFrame(syncContext, anyValidCookie, ct) is Frame passiveFrame)
                {
                    Action<SynchronizationContext, ITestOperations, CancellationToken> testAction =
                        (syncContext, testOpoerations, CancellationToken)
                            => { testOperations.StartDefaultAcquireMode(); };

                    var (success, message) =
                        WaitOnSettingsChange(syncContext, testOperations, testAction, ct)
                        switch
                        {
                            (true, Frame frame) => (true, "Found new settings after switch."),
                            _ => (false, "Couldn't detect settings change.")
                        };

                    return MakeResult(success, message);
                }
                else
                {
                    return MakeResult(false, "Could not get a frame after going passive.");
                }
            }

            public static IntegrationTestResult TestVariousAcquireValues(
                SynchronizationContext syncContext,
                ITestOperations testOperations,
                IObservable<Frame> frameObservable,
                CancellationToken ct)
            {
                testOperations.StartTestPattern();

                List<string> failures = new List<string>();

                if (testOperations.WaitOnAFrame(syncContext, anyValidCookie, ct) is Frame _)
                {
                    int testNumber = 0;

                    foreach (var acquireSetings in EnumerateAcquireSettings())
                    {
                        var parsedSonarFeedback = testOperations.StartAcquire(acquireSetings);
                        if (parsedSonarFeedback.ResultCode == 200)
                        {
                            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                            {
                                var expectedSettingsCookie = parsedSonarFeedback.SettingsCookie;
                                Predicate<Frame> predicate =
                                    frame =>
                                        expectedSettingsCookie == frame.Header.SettingsCookieInUse();
                                Frame? frame = testOperations.WaitOnAFrame(syncContext, predicate, cts.Token);

                                if (frame is null)
                                {
                                    var failure = $"Failed to get frame on {acquireSetings}";
                                    failures.Add(failure);
                                }
                            }
                        }
                        else
                        {
                            failures.Add(
                                DescribeFailedTestCase(
                                    testNumber, acquireSetings, parsedSonarFeedback));
                        }

                        ++testNumber;
                    }

                    var success = failures.Count == 0;
                    return MakeResult(success, failures.ToArray());
                }
                else
                {
                    return MakeResult(false, "Could not get a frame after going passive.");
                }

                static string DescribeFailedTestCase(
                    int testNumber,
                    AcquireSettings acquireSettings,
                    ParsedFeedbackFromSonar parsedFeedback)
                {
                    var buffer = new StringBuilder();
                    buffer.AppendLine($"testNumber: {testNumber}");
                    buffer.Append("Settings: ");
                    buffer.AppendLine(acquireSettings.ToString());

                    buffer.AppendLine("Feedback:");
                    buffer.Append(parsedFeedback.RawFeedback);

                    return buffer.ToString();
                }

                static IEnumerable<AcquireSettings> EnumerateAcquireSettings()
                {
                    foreach (var startRange in MakeRange(1.0f, 19.0f, AdvanceRange))
                    {
                        foreach (var endRange in MakeRange(startRange + 1.0f, 20.0f, AdvanceRange))
                        {
                            yield return new AcquireSettings
                            {
                                StartRange = startRange,
                                EndRange = endRange,
                            };
                        }
                    }

                    float AdvanceRange(float range)
                    {
                        return range + 1.0f;
                    }
                }
            }
        }
    }
}
