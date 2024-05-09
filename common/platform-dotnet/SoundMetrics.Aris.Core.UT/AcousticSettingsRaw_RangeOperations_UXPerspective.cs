using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoundMetrics.Aris.Core.ApprovalTests;
using SoundMetrics.Aris.Core.Raw;
using System;
using System.Diagnostics;
using System.Linq;

namespace SoundMetrics.Aris.Core
{
    using static AcousticSettingsRawRangeOperations;

    public enum MoveType
    {
        WindowStart,
        WindowEnd,
        EntireWindow,
    }

    public struct StepInputs : IPrettyPrintable
    {
        public StepInputs(in (MoveType moveType, Distance distance) inputs)
        {
            MoveType = inputs.moveType;
            Distance = inputs.distance;
        }

        public MoveType MoveType { get; }
        public Distance Distance { get; }

        public static implicit operator StepInputs(in (MoveType moveType, Distance distance) inputs)
            => new StepInputs(inputs);

        public void Deconstruct(out MoveType moveType, out Distance distance)
        {
            moveType = MoveType;
            distance = Distance;
        }

        public PrettyPrintHelper PrettyPrint(PrettyPrintHelper helper, string label)
        {
            return helper.PrintValue(label, this.ToString());
        }

        public override string ToString()
        {
            return $"{nameof(StepInputs)}({nameof(MoveType)}.{MoveType}, {Distance})";
        }
    }

    public struct MoveActuals : IPrettyPrintable
    {
        public MoveActuals(AcousticSettingsRaw settings, ObservedConditions conditions)
        {
            WindowBounds = settings.WindowBounds(conditions);
            SamplePeriod = settings.SamplePeriod;
            SampleCount = settings.SampleCount;
        }

        public WindowBounds WindowBounds { get; }
        public FineDuration SamplePeriod { get; }
        public int SampleCount { get; }

        public PrettyPrintHelper PrettyPrint(PrettyPrintHelper helper, string label)
        {
            var s = $"{nameof(MoveActuals)}("
                + $"Bounds({WindowBounds}), "
                + $"{nameof(SamplePeriod)}=[{SamplePeriod}], "
                + $"{nameof(SampleCount)}=[{SampleCount}])";
            return helper.PrintValue(label, s);
        }
    }

    // public struct MoveStep
    // {
    //     public MoveStep(in StepInputs inputs)
    //     {
    //         Inputs = inputs;
    //     }

    //     public StepInputs Inputs { get; }

    //     public static implicit operator MoveStep(in StepInputs inputs)
    //         => new MoveStep(inputs);
    // }

    [TestClass]
    [UseApprovalSubdirectory("approval-files"), UseReporter(typeof(DiffReporter))]
    public sealed class AcousticSettingsRaw_RangeOperations_UXPerspective
    {
        [TestMethod]
        public void ValidateConditions()
        {
            var helper = new PrettyPrintHelper(0);
            helper.PrintHeading("Test Conditions");

            using (var _ = helper.PushIndent())
            {
                helper.PrintValue("conditions", conditions);
            }

            Approvals.Verify(helper.ToString());
        }

        [TestMethod]
        public void ValidateStartSettings()
        {
            var helper = new PrettyPrintHelper(0);
            var startSettings = GetStartSettings(helper);

            helper.PrintHeading("Start Settings");
            using (var _ = helper.PushIndent())
            {
                helper.PrintValue("startSettings", startSettings);
            }

            Approvals.Verify(helper.ToString());
        }

        [TestMethod]
        public void MoveWindowStart_NotRecording_GuidedSampleCount()
        {
            var mode = AdjustWindowTerminusGuided.Instance;

            var moveSteps =
                new StepInputs[]
            {
                (MoveType.WindowStart, -(Distance)0.1),
                (MoveType.WindowStart, -(Distance)0.1),
                (MoveType.WindowStart, -(Distance)1),
                (MoveType.WindowStart, (Distance)2),
                (MoveType.WindowStart, -(Distance)2),
                (MoveType.WindowStart, (Distance)1.2),
            };

            var testName = nameof(MoveWindowStart_NotRecording_GuidedSampleCount);
            TestRunner(testName, moveSteps, mode);
        }

        [TestMethod]
        public void MoveWindowEnd_NotRecording_GuidedSampleCount()
        {
            var mode = AdjustWindowTerminusGuided.Instance;

            var moveSteps =
                new StepInputs[]
            {
                (MoveType.WindowEnd, (Distance)0.1),
                (MoveType.WindowEnd, (Distance)0.1),
                (MoveType.WindowEnd, (Distance)1),
                (MoveType.WindowEnd, -(Distance)2),
                (MoveType.WindowEnd, (Distance)2),
                (MoveType.WindowEnd, -(Distance)1.2),
            };

            var testName = nameof(MoveWindowEnd_NotRecording_GuidedSampleCount);
            TestRunner(testName, moveSteps, mode);
        }

        [TestMethod]
        public void MoveEntireWindow_NotRecording_GuidedSampleCount()
        {
            var mode = AdjustWindowTerminusGuided.Instance;

            var moveSteps =
                new StepInputs[]
            {
                (MoveType.EntireWindow, (Distance)0.1),
                (MoveType.EntireWindow, (Distance)0.1),
                (MoveType.EntireWindow, (Distance)1),
                (MoveType.EntireWindow, -(Distance)2),
                (MoveType.EntireWindow, (Distance)2),
                (MoveType.EntireWindow, -(Distance)1.2),
            };

            var testName = nameof(MoveEntireWindow_NotRecording_GuidedSampleCount);
            TestRunner(testName, moveSteps, mode);
        }

        [TestMethod]
        public void MoveEntireWindow_GuidedSampleCount_DoNotShrinkWindow()
        {
            var mode = AdjustWindowTerminusGuided.Instance;
            var windowStart = new WindowBounds(2, 9.531);
            var testName = nameof(MoveEntireWindow_GuidedSampleCount_DoNotShrinkWindow);

            var windowStartFromRealLifeData = new double[]
            {
                // 2.431,
                // 6.753,
                12.471,
                // 14 /* 12.769 */, // Observed 12.769, actual request unknown
            };

            //-----------------------
            // Doesn't quite match TestRunner as this is built from real-world observed data.

            var helper = new PrettyPrintHelper(0);
            helper.PrintHeading(testName);

            var settings = GetStartSettings(helper, windowStart);
            var startingBounds = settings.WindowBounds(conditions);

            PrintStartSettings();

            Debug.WriteLine($"Starting bounds: [{startingBounds}]");

            int idxStep = 0;

            foreach (var requestedWindowStart in windowStartFromRealLifeData.Select(start => (Distance)start))
            {
                Debug.WriteLine($"Test {idxStep} ---------------------");
                Debug.WriteLine($"requestedWindowStart: [{requestedWindowStart}]");
                var newSettings = MoveEntireWindow(settings, requestedWindowStart);

                using (var _ = helper.PushIndent())
                {
                    helper.PrintValue("starting", new MoveActuals(settings, conditions));
                    helper.PrintValue("ending", new MoveActuals(newSettings, conditions));
                    helper.PrintValue("new settings", newSettings);
                }

                settings = newSettings;
                Debug.WriteLine($"New bounds: [{newSettings.WindowBounds(conditions)}]");

                ++idxStep;
            }

            var minSampleCount = new PreferredGuidedSampleCounts()[settings.SystemType].Minimum;
            Assert.IsTrue(
                settings.SampleCount > minSampleCount,
                $"The 12.471 value should allow for >={minSampleCount} samples at a sample period of 11 microseconds. "
                + $"SampleCount=[{settings.SampleCount}]");

            Approvals.Verify(helper.ToString());

            AcousticSettingsRaw MoveEntireWindow(AcousticSettingsRaw settings, Distance requestedStart)
            {
                var bounds = settings.WindowBounds(conditions);
                var newSettings = settings.MoveEntireWindow(
                    requestedStart,
                    conditions,
                    mode,
                    useMaxFrameRate: true,
                    useAutoFrequency: true
                );

                return newSettings;
            }

            void PrintStartSettings()
            {
                helper.PrintHeading("Start Settings");
                using (var _ = helper.PushIndent())
                {
                    helper.PrintValue("startSettings", settings);
                }
            }

        }

        private void TestRunner(string testName, StepInputs[] steps, IAdjustWindowTerminus adjustmentStrategy)
        {
            var helper = new PrettyPrintHelper(0);
            helper.PrintHeading(testName);

            var settings = GetStartSettings(helper);
            var startingBounds = settings.WindowBounds(conditions);

            PrintStartSettings();

            Debug.WriteLine($"Starting bounds: [{startingBounds}]");

            int idxStep = 0;

            foreach (var step in steps)
            {
                Debug.WriteLine($"Step: [{step}]");
                var newSettings = Move(settings, adjustmentStrategy, idxStep, step, helper);

                using (var _ = helper.PushIndent())
                {
                    helper.PrintValue("starting", new MoveActuals(settings, conditions));
                    helper.PrintValue("ending", new MoveActuals(newSettings, conditions));
                    helper.PrintValue("new settings", newSettings);
                }

                settings = newSettings;
                Debug.WriteLine($"New bounds: [{newSettings.WindowBounds(conditions)}]");

                ++idxStep;
            }

            Approvals.Verify(helper.ToString());

            void PrintStartSettings()
            {
                helper.PrintHeading("Start Settings");
                using (var _ = helper.PushIndent())
                {
                    helper.PrintValue("startSettings", settings);
                }
            }
        }

        private static readonly Salinity salinity = Salinity.Brackish;
        private static readonly ObservedConditions conditions =
            new ObservedConditions((Temperature)20.0, (Distance)5.0);
        private static readonly SystemType systemType = SystemType.Aris3000;
        private static readonly PingMode pingMode = PingMode.PingMode9;

        private static AcousticSettingsRaw GetStartSettings(
            PrettyPrintHelper helper,
            WindowBounds? windowBoundsArg = null)
        {
            helper.PrintHeading(nameof(GetStartSettings));

            WindowBounds windowBounds = windowBoundsArg ?? new WindowBounds(2, 9);

            using (var _ = helper.PushIndent())
            {
                helper.PrintValue(nameof(conditions), conditions);
                helper.PrintValue(nameof(salinity), salinity);
                helper.PrintValue(nameof(windowBounds), windowBounds);
            }

            return
                CreateDefaultGuidedSettings(
                    SystemType.Aris3000,
                    salinity,
                    windowBounds,
                    conditions);
        }

        private static AcousticSettingsRaw Move(
            AcousticSettingsRaw settings,
            IAdjustWindowTerminus adjustmentStrategy,
            int idxStep,
            in StepInputs stepInputs,
            PrettyPrintHelper helper)
        {
            PrintStep(stepInputs);

            return Dispatch(stepInputs).WithAutomaticFocusDistance(conditions);

            AcousticSettingsRaw Dispatch(in StepInputs inputs)
            {
                var (moveType, distance) = inputs;

                switch (moveType)
                {
                    case MoveType.WindowStart:
                        {
                            var bounds = settings.WindowBounds(conditions);
                            var requestedStart = bounds.WindowStart + distance;
                            return settings.MoveWindowStart(
                                requestedStart,
                                conditions,
                                adjustmentStrategy,
                                useMaxFrameRate: true,
                                useAutoFrequency: true
                            );
                        }

                    case MoveType.WindowEnd:
                        {
                            var bounds = settings.WindowBounds(conditions);
                            var requestedEnd = bounds.WindowEnd + distance;
                            return settings.MoveWindowEnd(
                                requestedEnd,
                                conditions,
                                adjustmentStrategy,
                                useMaxFrameRate: true,
                                useAutoFrequency: true
                            );
                        }

                    case MoveType.EntireWindow:
                        {
                            var bounds = settings.WindowBounds(conditions);
                            var requestedStart = bounds.WindowStart + distance;
                            return settings.MoveEntireWindow(
                                requestedStart,
                                conditions,
                                adjustmentStrategy,
                                useMaxFrameRate: true,
                                useAutoFrequency: true
                            );
                        }

                    default:
                        throw new ArgumentException($"Value not handled: [{moveType}]");
                }
            }

            void PrintStep(in StepInputs inputs)
            {
                helper.PrintHeading($"Step {idxStep}");

                using (var _ = helper.PushIndent())
                {
                    helper.PrintValue(nameof(StepInputs), inputs);
                }
            }
        }
    }
}
