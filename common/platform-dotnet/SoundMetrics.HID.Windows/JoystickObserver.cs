using System;
using System.Reactive.Subjects;
using System.Threading;

namespace SoundMetrics.HID.Windows
{
    public interface IJoystickObserver : IDisposable
    {
        IObservable<JoystickInfo[]> Joysticks { get; }
    }

    internal sealed class JoystickObserver : IJoystickObserver
    {
        public JoystickObserver()
        {
            updateTimer = new Timer(
                callback: OnTimerTick,
                state: null,
                dueTime: TimeSpan.FromSeconds(0),
                period: DefaultPollingPeriod);
        }

        public static TimeSpan DefaultPollingPeriod = TimeSpan.FromSeconds(1);

        public IObservable<JoystickInfo[]> Joysticks => joystickInfoSubject;

        public void Dispose()
        {
            updateTimer.Dispose();
            joystickInfoSubject.OnCompleted();
            joystickInfoSubject.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnTimerTick(object _)
        {
            try
            {
                if (joystickInfoSubject.HasObservers)
                {
                    joystickInfoSubject.OnNext(Joystick.EnumerateJoysticks());
                }
            }
            catch (ObjectDisposedException)
            {
                // Race condition between firing the timer callback
                // and disposing this object.
            }
        }

        private readonly Subject<JoystickInfo[]> joystickInfoSubject =
            new Subject<JoystickInfo[]>();
        private readonly Timer updateTimer;
    }
}
