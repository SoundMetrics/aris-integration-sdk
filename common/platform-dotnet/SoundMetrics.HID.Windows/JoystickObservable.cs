// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Reactive.Subjects;

namespace SoundMetrics.HID.Windows
{
    public struct JoystickPositionReport
    {
        public uint JoystickId;
        public JoyInfoEx JoystickInfo;
    }

    public sealed class JoystickObservable : IDisposable
    {
        private readonly uint joystickId;
        private readonly MillisecondTimer timer;

        private Subject<JoystickPositionReport> posSubject = new Subject<JoystickPositionReport>();

        public JoystickObservable(uint joystickId, int pollingPeriodMs)
        {
            this.joystickId = joystickId;

            var pollingPeriod = 55;
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

        private void OnTimer()
        {
            if (posSubject.HasObservers
                && Joystick.GetJoystickPosition(joystickId, out JoystickPositionReport report))
            {
                posSubject.OnNext(report);
            }
        }

        ~JoystickObservable()
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
                posSubject.OnCompleted();
                posSubject?.Dispose();
            }

            // Free up unmanaged resources
        }

        public IObservable<JoystickPositionReport> JoystickPositionReports
        {
            get => posSubject;
        }

    }
}
