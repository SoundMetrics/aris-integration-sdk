// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SoundMetrics.HID.Windows
{
    using WORD = UInt16;
    using UINT = UInt32;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct JoyCaps
    {
#pragma warning disable IDE0044 // Add readonly modifier

        public WORD wMid;
        public WORD wPid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32 /*MAXPNAMELEN*/)]
        public string szPname;

        public UINT wXmin;
        public UINT wXmax;
        public UINT wYmin;
        public UINT wYmax;
        public UINT wZmin;
        public UINT wZmax;
        public UINT wNumButtons;
        public UINT wPeriodMin;
        public UINT wPeriodMax;
        public UINT wRmin;
        public UINT wRmax;
        public UINT wUmin;
        public UINT wUmax;
        public UINT wVmin;
        public UINT wVmax;
        public UINT wCaps;
        public UINT wMaxAxes;
        public UINT wNumAxes;
        public UINT wMaxButtons;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32 /*MAXPNAMELEN*/)]
        public string szRegKey;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260 /*MAX_JOYSTICKOEMVXDNAME*/)]
        public string szOEMVxD;

#pragma warning restore IDE0044 // Add readonly modifier
    }

    public static class JoyCapsExtensions
    {
        internal static bool IsSet(uint value, uint mask) => (value & mask) != 0;
        public static bool HasX(this JoyCaps _) => true;
        public static bool HasY(this JoyCaps _) => true;
        public static bool HasZ(this JoyCaps caps) => IsSet(caps.wCaps, Joystick.JOYCAPS_HASZ);
        public static bool HasR(this JoyCaps caps) => IsSet(caps.wCaps, Joystick.JOYCAPS_HASR);
        public static bool HasU(this JoyCaps caps) => IsSet(caps.wCaps, Joystick.JOYCAPS_HASU);
        public static bool HasV(this JoyCaps caps) => IsSet(caps.wCaps, Joystick.JOYCAPS_HASV);

        public static string[] GetAxesLetters(this JoyCaps caps)
        {
            IEnumerable<string> Iterate()
            {
                yield return "X";
                yield return "Y";
                if (caps.HasZ()) yield return "Z";
                if (caps.HasR()) yield return "R";
                if (caps.HasU()) yield return "U";
                if (caps.HasV()) yield return "V";
            }

            return Iterate().ToArray();
        }
    }
}
