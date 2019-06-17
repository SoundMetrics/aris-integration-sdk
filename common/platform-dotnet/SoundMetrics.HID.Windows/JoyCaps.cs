// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.HID
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
}
