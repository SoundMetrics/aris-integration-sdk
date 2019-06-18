// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.HID.Windows
{
    public sealed class MillisecondTimer : IDisposable
    {
        public enum TimerType : uint
        {
            /// <summary>
            /// Timer for single event
            /// </summary>
            OneShot = 0,

            /// <summary>
            /// Timer for continuous periodic event
            /// </summary>
            Periodic = 1,
        }

        public delegate void TimerEventHandler();
        private delegate void TimerEventHandlerCore(
            UInt32 id,
            UInt32 msg,
            UIntPtr userCtx,
            UIntPtr rsv1,
            UIntPtr rsv2);

        public MillisecondTimer(
            int msDelay,
            int msResolution,
            TimerEventHandler callback,
            TimerType timerType)
        {
            _wrappedCallback = (id, msg, userCtx, rsv1, rsv2) =>
                {
                    if (!_killingTimer) { callback(); }
                };

            // NOTE: We aren't using TIME_KILL_SYNCHRONOUS. Sometimes the callback wants to use BeginInvoke
            // to *send* rather than *post* to the same thread that kills the timer; TIME_KILL_SYNCHRONOUS
            // causes a deadlock in this case as timeKillEvent() waits on the timer to get killed while the
            // callback is blocked waiting for access to the same thread: deadlock. So we use _killingTimer
            // to ensure that no more timer ticks are serviced while we asynchronously kill the timer.
            _timerId = NativeMethods.timeSetEvent(
                (uint)msDelay,
                (uint)msResolution,
                _wrappedCallback,
                UIntPtr.Zero,
                TIME_CALLBACK_FUNCTION | (uint)timerType);

            if (_timerId == 0)
                throw new InvalidOperationException(
                    string.Format(
                        "Couldn't create timer; msDelay={0}; msResolution={1}; timerType={2}",
                        msDelay,
                        msResolution,
                        timerType));
        }

        ~MillisecondTimer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _killingTimer = true;

            if (_timerId != 0)
                NativeMethods.timeKillEvent(_timerId);
        }

        /// <summary>
        /// When the timer expires, Windows calls the function pointed to by the lpTimeProc parameter. This is the default.
        /// </summary>
        private const uint TIME_CALLBACK_FUNCTION = 0x0000;

        private static class NativeMethods
        {
            [DllImport("winmm.dll", SetLastError = true)]
            internal static extern UInt32 timeSetEvent(
                UInt32 msDelay,
                UInt32 msResolution,
                TimerEventHandlerCore callback,
                UIntPtr userCtx,
                UInt32 eventType);

            [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
            internal static extern uint timeKillEvent(uint uTimerID);
        }

        private readonly uint _timerId;
        private readonly TimerEventHandlerCore _wrappedCallback;
        private bool _isDisposed;
        private volatile bool _killingTimer;
    }
}
