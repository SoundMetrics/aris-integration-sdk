// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Diagnostics;
using System.Reactive.Subjects;

namespace SoundMetrics.HID.Windows
{
    //-------------------------------------------------------------------------
    // Helper types
    //-------------------------------------------------------------------------

    public struct JoystickPositionReport
    {
        public uint JoystickId;
        public JoyInfoEx JoystickInfo;
    }

    /// <summary>
    /// Presents an IDE-friendly way to specify buttons of interest.
    /// </summary>
    public struct ButtonSelection
    {
        public bool EnableButton1;
        public bool EnableButton2;
        public bool EnableButton3;
        public bool EnableButton4;
        public bool EnableButton5;
        public bool EnableButton6;
        public bool EnableButton7;
        public bool EnableButton8;
        public bool EnableButton9;
        public bool EnableButton10;
        public bool EnableButton11;
        public bool EnableButton12;
        public bool EnableButton13;
        public bool EnableButton14;
        public bool EnableButton15;
        public bool EnableButton16;
        public bool EnableButton17;
        public bool EnableButton18;
        public bool EnableButton19;
        public bool EnableButton20;
        public bool EnableButton21;
        public bool EnableButton22;
        public bool EnableButton23;
        public bool EnableButton24;
        public bool EnableButton25;
        public bool EnableButton26;
        public bool EnableButton27;
        public bool EnableButton28;
        public bool EnableButton29;
        public bool EnableButton30;
        public bool EnableButton31;
        public bool EnableButton32;

        private static readonly Lazy<ButtonSelection> allButtons =
            new Lazy<ButtonSelection>(() =>
                new ButtonSelection
                {
                    EnableButton1 = true,
                    EnableButton2 = true,
                    EnableButton3 = true,
                    EnableButton4 = true,
                    EnableButton5 = true,
                    EnableButton6 = true,
                    EnableButton7 = true,
                    EnableButton8 = true,
                    EnableButton9 = true,
                    EnableButton10 = true,
                    EnableButton11 = true,
                    EnableButton12 = true,
                    EnableButton13 = true,
                    EnableButton14 = true,
                    EnableButton15 = true,
                    EnableButton16 = true,
                    EnableButton17 = true,
                    EnableButton18 = true,
                    EnableButton19 = true,
                    EnableButton20 = true,
                    EnableButton21 = true,
                    EnableButton22 = true,
                    EnableButton23 = true,
                    EnableButton24 = true,
                    EnableButton25 = true,
                    EnableButton26 = true,
                    EnableButton27 = true,
                    EnableButton28 = true,
                    EnableButton29 = true,
                    EnableButton30 = true,
                    EnableButton31 = true,
                    EnableButton32 = true,
                });

        internal static ButtonSelection AllButtons = allButtons.Value;

        internal uint ToFlags()
        {
            return
                (EnableButton1 ? Joystick.JOY_BUTTON1 : 0)
                | (EnableButton2 ? Joystick.JOY_BUTTON2 : 0)
                | (EnableButton3 ? Joystick.JOY_BUTTON3 : 0)
                | (EnableButton4 ? Joystick.JOY_BUTTON4 : 0)
                | (EnableButton5 ? Joystick.JOY_BUTTON5 : 0)
                | (EnableButton6 ? Joystick.JOY_BUTTON6 : 0)
                | (EnableButton7 ? Joystick.JOY_BUTTON7 : 0)
                | (EnableButton8 ? Joystick.JOY_BUTTON8 : 0)
                | (EnableButton9 ? Joystick.JOY_BUTTON9 : 0)
                | (EnableButton10 ? Joystick.JOY_BUTTON10 : 0)
                | (EnableButton11 ? Joystick.JOY_BUTTON11 : 0)
                | (EnableButton12 ? Joystick.JOY_BUTTON12 : 0)
                | (EnableButton13 ? Joystick.JOY_BUTTON13 : 0)
                | (EnableButton14 ? Joystick.JOY_BUTTON14 : 0)
                | (EnableButton15 ? Joystick.JOY_BUTTON15 : 0)
                | (EnableButton16 ? Joystick.JOY_BUTTON16 : 0)
                | (EnableButton17 ? Joystick.JOY_BUTTON17 : 0)
                | (EnableButton18 ? Joystick.JOY_BUTTON18 : 0)
                | (EnableButton19 ? Joystick.JOY_BUTTON19 : 0)
                | (EnableButton20 ? Joystick.JOY_BUTTON20 : 0)
                | (EnableButton21 ? Joystick.JOY_BUTTON21 : 0)
                | (EnableButton22 ? Joystick.JOY_BUTTON22 : 0)
                | (EnableButton23 ? Joystick.JOY_BUTTON23 : 0)
                | (EnableButton24 ? Joystick.JOY_BUTTON24 : 0)
                | (EnableButton25 ? Joystick.JOY_BUTTON25 : 0)
                | (EnableButton26 ? Joystick.JOY_BUTTON26 : 0)
                | (EnableButton27 ? Joystick.JOY_BUTTON27 : 0)
                | (EnableButton28 ? Joystick.JOY_BUTTON28 : 0)
                | (EnableButton29 ? Joystick.JOY_BUTTON29 : 0)
                | (EnableButton30 ? Joystick.JOY_BUTTON30 : 0)
                | (EnableButton31 ? Joystick.JOY_BUTTON31 : 0)
                | (EnableButton32 ? Joystick.JOY_BUTTON32 : 0)
                ;
        }
    }

    //-------------------------------------------------------------------------
    // Observable joystick events
    //-------------------------------------------------------------------------

    public sealed class ObservableJoystick : IDisposable
    {
        private readonly uint joystickId;
        private readonly MillisecondTimer timer;
        private readonly Joystick.JoystickInfo joystickInfo;

        private readonly Subject<(JoystickPositionReport, Joystick.JoystickInfo)>
            posSubject = new Subject<(JoystickPositionReport, Joystick.JoystickInfo)>();

        public ObservableJoystick(uint joystickId, int pollingPeriodMs)
        {
            this.joystickId = joystickId;
            if (!Joystick.GetJoystickInfo(joystickId, out this.joystickInfo))
            {
                throw new InvalidOperationException($"Joystic id={joystickId} not found");
            }

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

        public static Func<JoystickPositionReport,bool>
            CreateButtonFilter(ButtonSelection buttons)
        {
            var buttonFlags = buttons.ToFlags();

            var isFirstReading = true;
            uint previousFlags = 0;

            return report =>
            {
                uint newButtonFlags = report.JoystickInfo.dwButtons;

                if (isFirstReading)
                {
                    isFirstReading = false;
                    previousFlags = newButtonFlags;

                    // We prefer to skip the first report, don't make it an event.
                    return false;
                }

                var includeEvent =
                    (previousFlags != newButtonFlags)
                    && (buttonFlags & report.JoystickInfo.dwButtons) != 0;
                previousFlags = newButtonFlags;

                return includeEvent;
            };
        }

        private void OnTimer()
        {
            if (posSubject.HasObservers
                && Joystick.GetJoystickPosition(joystickId, out JoystickPositionReport report))
            {
                Debug.Assert(joystickInfo != null);
                posSubject.OnNext((report, joystickInfo));
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

        public IObservable<(JoystickPositionReport,Joystick.JoystickInfo)>
            JoystickPositionReports => posSubject;
    }
}
