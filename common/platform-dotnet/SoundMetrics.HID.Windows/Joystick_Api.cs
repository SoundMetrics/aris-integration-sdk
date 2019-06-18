// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoundMetrics.HID.Windows
{
    public static partial class Joystick
    {
        public class JoystickInfo
        {
            public uint JoystickId;
            public JoyCaps Caps;

            private static Lazy<JoystickInfo> empty =
                new Lazy<JoystickInfo>(() => new JoystickInfo());

            public static JoystickInfo Empty = empty.Value;
        }

        public static JoystickInfo[] EnumerateJoysticks()
        {
            var bufSize = Marshal.SizeOf(typeof(JoyCaps));
            var buf = new byte[bufSize];

            var h = GCHandle.Alloc(buf, GCHandleType.Pinned);

            IEnumerable<JoystickInfo> iterate()
            {
                var pinned = h.AddrOfPinnedObject();

                for (uint i = 0; i < 16; ++i)
                {
                    if (GetJoystickInfo(i, pinned, bufSize, out JoystickInfo joystickInfo))
                    {
                        yield return joystickInfo;
                    }

                    //var result = NativeMethods.JoyGetDevCaps(i, pinned, bufSize);
                    //if (result == 0)
                    //{
                    //    var id = i;
                    //    var caps = (JoyCaps)Marshal.PtrToStructure(pinned, typeof(JoyCaps));
                    //    yield return new JoystickInfo
                    //    {
                    //        JoystickId = id,
                    //        Caps = caps,
                    //    };
                    //}
                }
            }

            try
            {
                return iterate().ToArray();
            }
            finally
            {
                h.Free();
            }
        }

        private static bool GetJoystickInfo(
            uint id,
            IntPtr pinned,
            int bufSize,
            out JoystickInfo joystickInfo)
        {
            var result = NativeMethods.JoyGetDevCaps(id, pinned, bufSize);
            if (result == 0)
            {
                var caps = (JoyCaps)Marshal.PtrToStructure(pinned, typeof(JoyCaps));
                joystickInfo = new JoystickInfo
                {
                    JoystickId = id,
                    Caps = caps,
                };

                return true;
            }

            joystickInfo = JoystickInfo.Empty;
            return false;
        }

        internal static bool GetJoystickInfo(uint id, out JoystickInfo joystickInfo)
        {
            var bufSize = Marshal.SizeOf(typeof(JoyCaps));
            var buf = new byte[bufSize];

            var h = GCHandle.Alloc(buf, GCHandleType.Pinned);

            try
            {
                var pinned = h.AddrOfPinnedObject();
                return GetJoystickInfo(id, pinned, bufSize, out joystickInfo);
            }
            finally
            {
                h.Free();
            }
        }

        private const JoystickReturnMasks GetPositionReturnFlags =
            JoystickReturnMasks.JOY_RETURNX
            | JoystickReturnMasks.JOY_RETURNY
            | JoystickReturnMasks.JOY_RETURNZ
            | JoystickReturnMasks.JOY_RETURNR
            | JoystickReturnMasks.JOY_RETURNU
            | JoystickReturnMasks.JOY_RETURNV
            | JoystickReturnMasks.JOY_RETURNBUTTONS
            | JoystickReturnMasks.JOY_USEDEADZONE; // dead zone quiets down the noise at center

        /// Gets a one-time reading of position data from the specified joystick.
        internal static bool GetJoystickPosition(uint joystickId, out JoystickPositionReport report)
        {
            JoyInfoEx InitBuffer() => new JoyInfoEx
            {
                dwSize = (uint)Marshal.SizeOf(typeof(JoyInfoEx)),
                dwFlags = (uint)GetPositionReturnFlags,
            };



            // Initialize the JOYINFOEX structure with size and flags, marshal that to the
            // interop buffer, make the API call, then marshal the structure back to a JOYINFOEX.

            var bufSize = Marshal.SizeOf(typeof(JoyInfoEx));
            var buf = new byte[bufSize];
            var h = GCHandle.Alloc(buf, GCHandleType.Pinned);
            var initialValue = InitBuffer();
            try
            {
                var pinned = h.AddrOfPinnedObject();

                Marshal.StructureToPtr(initialValue, pinned, false);
                var result = NativeMethods.JoyGetPosEx(joystickId, pinned);
                if (result == 0)
                {
                    report = new JoystickPositionReport
                    {
                        JoystickId = joystickId,
                        JoystickInfo = (JoyInfoEx)Marshal.PtrToStructure(pinned, typeof(JoyInfoEx)),
                    };
                    return true;
                }
                else
                {
                    report = new JoystickPositionReport();
                    return false;
                }
            }
            finally
            {
                h.Free();
            }
        }
    }
}
