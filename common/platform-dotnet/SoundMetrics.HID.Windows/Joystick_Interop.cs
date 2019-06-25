// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.HID.Windows
{
    using JOYCAPS_PTR = IntPtr;
    using JOYINFOEX_PTR = IntPtr;
    using MMRESULT = UInt32;
    using UINT = UInt32;

    public static partial class Joystick
    {
        private static class NativeMethods
        {
            [DllImport("Winmm.dll", EntryPoint = "joyGetDevCapsW")]
            public static extern MMRESULT JoyGetDevCaps(
                UINT uJoyID,
                JOYCAPS_PTR pcaps,
                int cb);

            [DllImport("Winmm.dll", EntryPoint = "joyGetPosEx")]
                public static  extern MMRESULT JoyGetPosEx(
                    UINT uJoyID,
                    JOYINFOEX_PTR p);
        }
    }
}
