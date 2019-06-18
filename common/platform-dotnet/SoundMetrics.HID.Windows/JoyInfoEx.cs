// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace SoundMetrics.HID.Windows
{
    using DWORD = UInt32;
    using Ret = JoystickReturnMasks;

    // Docs for JOYINFOEX:
    // https://docs.microsoft.com/en-us/windows/desktop/api/joystickapi/ns-joystickapi-joyinfoex
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct JoyInfoEx
    {

        public DWORD dwSize;
        public DWORD dwFlags;
        public DWORD dwXpos;
        public DWORD dwYpos;
        public DWORD dwZpos;
        public DWORD dwRpos;
        public DWORD dwUpos;
        public DWORD dwVpos;
        public DWORD dwButtons;
        public DWORD dwButtonNumber;
        public DWORD dwPOV;
        public DWORD dwReserved1;
        public DWORD dwReserved2;

        public bool HasX => (dwFlags & (uint)Ret.JOY_RETURNX) != 0;
        public bool HasY => (dwFlags & (uint)Ret.JOY_RETURNY) != 0;
        public bool HasZ => (dwFlags & (uint)Ret.JOY_RETURNZ) != 0;
        public bool HasR => (dwFlags & (uint)Ret.JOY_RETURNR) != 0;
        public bool HasU => (dwFlags & (uint)Ret.JOY_RETURNU) != 0;
        public bool HasV => (dwFlags & (uint)Ret.JOY_RETURNV) != 0;
    }

    public static class JoyInfoExExtensions
    {
        internal static DWORD GetAxisValue(this JoyInfoEx info, JoyAxis axis)
        {
            if (!Enum.IsDefined(typeof(JoyAxis), axis))
            {
                throw new ArgumentOutOfRangeException(nameof(axis));
            }

            switch (axis)
            {
                case JoyAxis.X: return info.dwXpos;
                case JoyAxis.Y: return info.dwYpos;
                case JoyAxis.Z: return info.dwZpos;
                case JoyAxis.R: return info.dwRpos;
                case JoyAxis.U: return info.dwUpos;
                case JoyAxis.V: return info.dwVpos;
                default:
                    throw new Exception($"Internal error: unhandled axis: {axis}");
            }
        }

        public static float ScaleAxis(this JoyInfoEx info, JoyAxis axis)
        {
            var pos = info.GetAxisValue(axis);
            return 2.0f * pos / 65535.0f - 1.0f;
        }
    }
}
