using SoundMetrics.Aris.Headers;
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
        }
    }
}
