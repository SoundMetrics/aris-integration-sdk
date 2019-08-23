// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Reactive.Subjects;

namespace SoundMetrics.HID.Windows
{
    //-------------------------------------------------------------------------
    // Helper types
    //-------------------------------------------------------------------------

    public struct JoystickPositionReport
    {
        public uint JoystickId;
        public JoyInfoEx JoyInfoEx;
        public JoystickInfo JoystickInfo;
    }

    /// <summary>
    /// Specifies buttons of interest.
    /// </summary>
    public struct ButtonSelection
    {
        public readonly UInt32 Flags;

        public ButtonSelection(int button, params int[] more)
        {
            uint ButtonToBit(int b)
            {
                return 1u << (b - 1);
            }

            ValidateButtonNumber(button);

            var flags = ButtonToBit(button);
            if (more != null)
            {
                foreach (var btn in more)
                {
                    ValidateButtonNumber(btn);
                    flags |= ButtonToBit(btn);
                }
            }

            Flags = flags;
        }

        public static ButtonSelection FromJoyCaps(JoyCaps caps)
        {
            var buttonCount = caps.wNumButtons;
            UInt32 flags;

            if (buttonCount == 0)
            {
                flags = 0;
            }
            else if (buttonCount > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(caps.wNumButtons));
            }
            else
            {
                flags = (uint)(
                            (1UL << (int)buttonCount) - 1);
            }

            return new ButtonSelection(true, flags);
        }

        private static void ValidateButtonNumber(int btn)
        {
            if (btn < 1 || 32 < btn)
            {
                throw new ArgumentOutOfRangeException(nameof(btn));
            }
        }

        private ButtonSelection(bool _, UInt32 flags)
        {
            Flags = flags;
        }

        private static readonly Lazy<ButtonSelection> allButtons =
            new Lazy<ButtonSelection>(() => new ButtonSelection(true, 0xFFFFFFFF));

        internal static ButtonSelection AllButtons = allButtons.Value;
    }

    //-------------------------------------------------------------------------
    // Observable joystick events
    //-------------------------------------------------------------------------

    public delegate bool ButtonFilter(JoystickPositionReport report);

    public sealed class ObservableJoystick : IDisposable
    {
        private readonly uint joystickId;
        private readonly MillisecondTimer timer;
        private readonly JoystickInfo joystickInfo;

        private readonly Subject<JoystickPositionReport> posSubject = new Subject<JoystickPositionReport>();

        public ObservableJoystick(uint joystickId, int pollingPeriodMs)
        {
            if (!Joystick.GetJoystickInfo(joystickId, out joystickInfo))
            {
                throw new InvalidOperationException($"Couldn't find joystick id={joystickId}");
            }

            this.joystickId = joystickId;

            var pollingPeriod = pollingPeriodMs;
            try
            {
                timer = new MillisecondTimer(
                    pollingPeriod,
                    pollingPeriod,
                    OnTimer,
                    MillisecondTimer.TimerType.Periodic);
            }
            catch (Exception)
            {
                posSubject?.Dispose();
                throw;
            }
        }

        public static ButtonFilter
            CreateButtonFilter(ButtonSelection buttons)
        {
            bool IncludeEvent(uint oldFlags, uint newFlags, uint interestingFlags)
            {
                if (oldFlags != newFlags)
                {
                    var alteredFlags = oldFlags ^ newFlags;
                    return (alteredFlags & interestingFlags) != 0;
                }
                else
                {
                    return false;
                }
            }

            var flagsOfInterest = buttons.Flags;
            var isFirstReading = true;
            uint previousFlags = 0;

            return report =>
            {
                uint newButtonFlags = report.JoyInfoEx.dwButtons;

                if (isFirstReading)
                {
                    isFirstReading = false;
                    previousFlags = newButtonFlags;

                    // We prefer to skip the first report, don't make it an event.
                    return false;
                }

                var include =
                    IncludeEvent(
                        previousFlags,
                        newButtonFlags,
                        flagsOfInterest);
                previousFlags = newButtonFlags;
                return include;
            };
        }

        private void OnTimer()
        {
            if (posSubject.HasObservers)
            {
                if (Joystick.GetJoystickPosition(joystickId, out var posInfo))
                {
                    var (joystickId, joyInfoEx) = posInfo;
                    var report = new JoystickPositionReport
                    {
                        JoystickId = joystickId,
                        JoyInfoEx = joyInfoEx,
                        JoystickInfo = joystickInfo,
                    };
                    posSubject.OnNext(report);
                }
            }
        }

        ~ObservableJoystick()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free up managed resousrces
                timer?.Dispose();
                posSubject?.OnCompleted();
                posSubject?.Dispose();
            }

            // Free up unmanaged resources
        }

        public IObservable<JoystickPositionReport> JoystickPositionReports => posSubject;
    }
}
