// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

using Aris.FileTypes;
using SoundMetrics.Aris2.Protocols;
using SoundMetrics.Aris2.Protocols.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using static SoundMetrics.Aris2.Protocols.ArisCommands;

namespace TestAris2Commands
{
    // Types for test cases:
    using TestResult = ValueTuple<bool, string>;
    delegate void SendCommand(Command cmd);

    // Delegate allows for ref/out arguments (Func<_> does not).
    delegate TestResult TestCase(out string testCaseName, string systemType, SendCommand sendCmd, FrameListener frameListener);

    /// <summary>
    /// Integration tests for the MangedProtocolSupport assembly. This tests only that the command
    /// builder functions build a successful command.
    /// </summary>
    public static class Tests
    {
        public static void Run(string systemType, ArisConnection cxn, FrameListener frameListener)
        {
            SendCommand sendCmd = cmd =>
            {
                Console.WriteLine("Sending command:");
                Console.WriteLine(cmd.ToString());
                cxn.SendCommand(cmd);
            };

            (int, int, int, List<ValueTuple<string, string>>) RunTestCases()
            {
                var failures = new List<ValueTuple<string, string>>();
                int testCaseCount = 0, passCount = 0;
                foreach (var testCase in testCases)
                {
                    ++testCaseCount;
                    string testName = null;

                    var (passed, message) = testCase.Invoke(out testName, systemType, sendCmd, frameListener);
                    if (testName == null)
                    {
                        throw new Exception("Name was not set in the " + testCaseCount + "th test case");
                    }

                    passCount += passed ? 1 : 0;
                    var prefix = passed ? "+++ PASS: " : "*** FAIL: ";
                    Console.WriteLine("Test: " + testName);
                    Console.WriteLine(prefix + message);

                    if (!passed)
                    {
                        failures.Add((testName, message));
                    }
                }

                var failCount = testCaseCount - passCount;
                return (testCaseCount, passCount, failCount, failures);
            }

            var (total, passes, fails, failureInfo) = RunTestCases();

            Console.WriteLine();
            Console.WriteLine($"Tests completed. {total} test cases run, {passes} succeeded.");

            if (fails > 0)
            {
                Console.WriteLine($"*** {fails} test cases failed. ***");

                foreach (var (testName, message) in failureInfo)
                {
                    Console.WriteLine($"    Failed: {testName}; {message}");
                }
            }
        }

        static Tests()
        {
            UInt32 nextCookie = 1; // 0 is an invalid cookie
            getCookie = () => { return nextCookie++; };
        }

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
        private static readonly Func<UInt32> getCookie;


        // -- Tests -----------------------------------------------------------

        private static readonly TestCase[] testCases = new TestCase[]
            {
                // Unfortunately the 'out' param requires that everything in the arg list be fully specified.
                (out string testCaseName, string systemType, SendCommand sendCmd, FrameListener frameListener) =>
                {
                    testCaseName = "acoustic settings, frame stream receiver";

                    var acousticSettings = ApplyCookie(TestAcousticSettings.GetAcousticSettings(systemType));
                    var acousticSettingsCmd = ArisCommands.BuildAcousticSettingsCmd(acousticSettings);
                    sendCmd(acousticSettingsCmd);

                    var rcvCmd = ArisCommands.BuildSetFrameStreamReceiverCmd(frameListener.Port);
                    sendCmd(rcvCmd);

                    return VerifyFirst(frameListener,
                        hdr => hdr.AppliedSettings == acousticSettings.Cookie,
                        "Check that acoustic settings took hold",
                        DefaultTimeout);
                },

                (out string testCaseName, string systemType, SendCommand sendCmd, FrameListener frameListener) =>
                {
                    testCaseName = "more acoustic settings";

                    var acousticSettings = ApplyCookie(TestAcousticSettings.GetAcousticSettings(systemType));
                    var acousticSettingsCmd = ArisCommands.BuildAcousticSettingsCmd(acousticSettings);
                    sendCmd(acousticSettingsCmd);

                    return VerifyFirst(frameListener,
                        hdr => hdr.AppliedSettings == acousticSettings.Cookie,
                        "Check that 2nd acoustic settings took hold",
                        DefaultTimeout);
                },

                (out string testCaseName, string systemType, SendCommand sendCmd, FrameListener frameListener) =>
                {
                    testCaseName = "focus";

                    var focusCmd = ArisCommands.BuildFocusCmd(4);
                    sendCmd(focusCmd);
                    return VerifyChanging(frameListener,
                        hdr => hdr.FocusCurrentPosition,
                        "Focus should change",
                        DefaultTimeout);
                },

                (out string testCaseName, string systemType, SendCommand sendCmd, FrameListener frameListener) =>
                {
                    testCaseName = "time";

                    (bool, string) SetAndCheck(DateTime requestedTime)
                    {
                        var cmd = ArisCommands.BuildTimeCmd(requestedTime);
                        sendCmd(cmd);

                        var (success, frameTime) = GetAValue<ulong>(frameListener, hdr => hdr.FrameTime, DefaultTimeout);

                        if (success)
                        {
                            // In Aris FrameTime is timestamp recorded in the sonar:
                            // microseconds since epoch (Jan 1st 1970)

                            var timestampEpoch = new DateTime(1970, 1, 1);
                            var ts = new TimeSpan((long)frameTime * 10);
                            var reconstructed = timestampEpoch + ts;
                            var diff = requestedTime - reconstructed;
                            Console.WriteLine($"requestedTime={requestedTime}");
                            Console.WriteLine($"reconstructed frame time={reconstructed}");

                            // ARIS 2 time cmd is rounded to the second, so allow for that, more or less.
                            return (diff < TimeSpan.FromSeconds(1.1), $"FrameTime should be reflected; diff is {diff}");
                        }

                        return (false, "Couldn't retrieve a frame");
                    }

                    var now = DateTime.Now;
                    foreach (var time in new [] { now.AddMonths(-18), now })
                    {
                        var (success, message) = SetAndCheck(time);
                        if (!success)
                        {
                            return Fail(message);
                        }
                    }

                    return Pass("Time was set within variance");
                },

                (out string testCaseName, string systemType, SendCommand sendCmd, FrameListener frameListener) =>
                {
                    testCaseName = "salinity";

                    (bool, string) SetAndCheck(Salinity requestedSalinity)
                    {
                        var cmd = ArisCommands.BuildSalinityCmd(requestedSalinity);
                        sendCmd(cmd);
                        var (success, salinity) = GetAValue<uint>(frameListener, hdr => hdr.Salinity, DefaultTimeout);

                        if (success)
                        {
                            return (salinity == (uint)requestedSalinity, "Salinity should be " + requestedSalinity);
                        }

                        return (false, "Couldn't retrieve salinity, timed out");
                    }

                    foreach (var salinity in new [] { Salinity.Fresh, Salinity.Brackish, Salinity.Seawater, Salinity.Fresh })
                    {
                        var (success, message) = SetAndCheck(salinity);
                        if (!success)
                        {
                            return Fail(message);
                        }
                    }

                    return Pass("Salinity was set as expected");
                },

                (out string testCaseName, string systemType, SendCommand sendCmd, FrameListener frameListener) =>
                {
                    testCaseName = "frame stream settings";

                    // Interpacket delay is the only functional part of this message.
                    (bool, string) SetAndCheck(uint interpacketDelay)
                    {
                        var cmd = ArisCommands.BuildFrameStreamSettingsCmd(interpacketDelay);
                        sendCmd(cmd);

                        var (success, observedDelay) = GetAValue<uint>(frameListener, hdr => hdr.InterpacketDelayPeriod, DefaultTimeout);
                        if (success)
                        {
                            return (interpacketDelay == observedDelay, "Interpacket delay should change");
                        }

                        return (false, "Couldn't retrive salinity, timed out");
                    }

                    foreach (var delay in new [] { 0u, 10u, 20u, 0u })
                    {
                        var (success, message) = SetAndCheck(delay);
                        if (!success)
                        {
                            return Fail(message);
                        }
                    }

                    return Pass("Interpacket delay updates as expected");
                },

                (out string testCaseName, string systemType, SendCommand sendCmd, FrameListener frameListener) =>
                {
                    testCaseName = "ping";

                    var cmd = ArisCommands.BuildPingCmd();
                    sendCmd(cmd);

                    return Pass("requires visual verification in log of no failure");
                },
            };

        // -- Helpers ---------------------------------------------------------

        private static AcousticSettingsWithCookie ApplyCookie(AcousticSettingsWithCookie settings)
        {
            var updated = settings;
            updated.Cookie = getCookie();
            return updated;
        }

        private static (bool, string) Pass(string message)
        {
            return (true, message);
        }

        private static (bool, string) Fail(string message)
        {
            return (false, message);
        }

        private static TestResult VerifyFirst(FrameListener frameListener, Func<ArisFrameHeader, bool> evaluateTest,
            string message, TimeSpan timeout)
        {
            bool evaluatedTrue = false;

            using (var signal = new ManualResetEventSlim(false))
            using (var sub = frameListener.FrameHeaders.Take(1).Subscribe(hdr =>
            {
                evaluatedTrue = evaluateTest(hdr);
                signal.Set();
            }))
            {
                if (signal.Wait(timeout))
                {
                    if (evaluatedTrue)
                    {
                        return Pass(message);
                    }
                    else
                    {
                        return Fail(message);
                    }
                }
                else
                {
                    return Fail("Timed out: " + message);
                }
            }
        }

        private enum VerifyResult { Indeterminate, Success, Failure };

        private static TestResult VerifySeries(
            FrameListener frameListener,
            Func<ArisFrameHeader, (bool continueTest, VerifyResult result)> evaluateTest,
            string message, TimeSpan timeout)
        {
            var result = VerifyResult.Indeterminate;

            using (var signal = new ManualResetEventSlim(false))
            using (var sub = frameListener.FrameHeaders.Subscribe(hdr =>
            {
                // success || eval because we might get frames past the first success.
                bool continueTest;
                (continueTest, result) = evaluateTest(hdr);

                if (!continueTest)
                {
                    Debug.Assert(result == VerifyResult.Success || result == VerifyResult.Failure);
                    signal.Set();
                }
            }))
            {
                if (signal.Wait(timeout))
                {
                    if (result == VerifyResult.Success)
                    {
                        return Pass(message);
                    }
                    else
                    {
                        return Fail(message);
                    }
                }
                else
                {
                    return Fail("Timed out: " + message);
                }
            }
        }

        private static (bool, T) GetAValue<T>(FrameListener frameListener, Func<ArisFrameHeader, T> map, TimeSpan timeout,
            bool addSlightDelay = true)
        {
            // This assumes that the first packet received will be updated.
            // This in fact relies on the update being applied very quickly,
            // which may not always happen. So, give it a slight delay.

            if (addSlightDelay)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.2));
            }

            bool success = false;
            T value = default(T);

            using (var signal = new ManualResetEventSlim(false))
            using (var sub = frameListener.FrameHeaders.Take(1).Subscribe(hdr =>
            {
                value = map(hdr);
                signal.Set();
            }))
            {
                success = signal.Wait(timeout);
                return (success, value);
            }
        }

        private static TestResult VerifyChanging<T>(FrameListener frameListener, Func<ArisFrameHeader, T> map, string message,
            TimeSpan timeout)
        {
            // Don't delay getting the initial value.
            var (gotInitial, initialValue) = GetAValue(frameListener, map, timeout, addSlightDelay: false);
            if (gotInitial)
            {
                return VerifySeries(frameListener,
                    hdr =>
                    {
                        var areEqual = Object.Equals(map(hdr), initialValue);
                        if (areEqual)
                        {
                            return (true, VerifyResult.Indeterminate);
                        }


                        return (false, VerifyResult.Success);
                    },
                    message,
                    timeout);
            }
            else
            {
                return Fail("Couldn't get initial value: " + message);
            }
        }
    }
}
