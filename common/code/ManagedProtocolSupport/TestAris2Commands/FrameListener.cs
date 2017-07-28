// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

using Aris.FileTypes;
using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

namespace TestAris2Commands
{
    public class FrameListener
    {
        public FrameListener(string ipAddress)
        {
            _hListener = Native.CreateFrameListener(ipAddress, FrameListener.OnFrame, out _port);
            if (_hListener == IntPtr.Zero)
            {
                throw new Exception("Couldn't open frame listener");
            }

            _listenerMap[_hListener] = this;
        }

        public UInt16 Port => _port;
        public IObservable<ArisFrameHeader> FrameHeaders => _headerSubject;

        private static void OnFrame(IntPtr hListener, IntPtr header, UInt32 headerSize)
        {
            var @this = _listenerMap[hListener];
            @this._headerSubject.OnNext(Marshal.PtrToStructure<ArisFrameHeader>(header));
        }

        private static readonly ConcurrentDictionary<IntPtr, FrameListener> _listenerMap = new ConcurrentDictionary<IntPtr, FrameListener>();

        private readonly UInt16 _port;
        private readonly IntPtr _hListener;
        private readonly Subject<ArisFrameHeader> _headerSubject = new Subject<ArisFrameHeader>();

        private static class Native
        {
            public delegate void FrameCallback(IntPtr hListener, IntPtr header, UInt32 headerSize);

            [DllImport("ArisFramestreamWrapper.dll")]
            internal static extern IntPtr CreateFrameListener(
                [MarshalAs(UnmanagedType.LPStr)] string ipAddress,
                FrameCallback frameCallback,
                out UInt16 listenerPort);
        }
    }
}
