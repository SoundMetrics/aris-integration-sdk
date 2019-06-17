// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;

namespace SoundMetrics.HID.Windows
{
    public static partial class Joystick
    {
        // Buttons

        internal const uint JOY_BUTTON1CHG = 0x0100u;
        internal const uint JOY_BUTTON2CHG = 0x0200u;
        internal const uint JOY_BUTTON3CHG = 0x0400u;
        internal const uint JOY_BUTTON4CHG = 0x0800u;

        internal const uint JOY_BUTTON1 = 0x0001u;
        internal const uint JOY_BUTTON2 = 0x0002u;
        internal const uint JOY_BUTTON3 = 0x0004u;
        internal const uint JOY_BUTTON4 = 0x0008u;
        internal const uint JOY_BUTTON5 = 0x00000010u;
        internal const uint JOY_BUTTON6 = 0x00000020u;
        internal const uint JOY_BUTTON7 = 0x00000040u;
        internal const uint JOY_BUTTON8 = 0x00000080u;
        internal const uint JOY_BUTTON9 = 0x00000100u;
        internal const uint JOY_BUTTON10 = 0x00000200u;
        internal const uint JOY_BUTTON11 = 0x00000400u;
        internal const uint JOY_BUTTON12 = 0x00000800u;
        internal const uint JOY_BUTTON13 = 0x00001000u;
        internal const uint JOY_BUTTON14 = 0x00002000u;
        internal const uint JOY_BUTTON15 = 0x00004000u;
        internal const uint JOY_BUTTON16 = 0x00008000u;
        internal const uint JOY_BUTTON17 = 0x00010000u;
        internal const uint JOY_BUTTON18 = 0x00020000u;
        internal const uint JOY_BUTTON19 = 0x00040000u;
        internal const uint JOY_BUTTON20 = 0x00080000u;
        internal const uint JOY_BUTTON21 = 0x00100000u;
        internal const uint JOY_BUTTON22 = 0x00200000u;
        internal const uint JOY_BUTTON23 = 0x00400000u;
        internal const uint JOY_BUTTON24 = 0x00800000u;
        internal const uint JOY_BUTTON25 = 0x01000000u;
        internal const uint JOY_BUTTON26 = 0x02000000u;
        internal const uint JOY_BUTTON27 = 0x04000000u;
        internal const uint JOY_BUTTON28 = 0x08000000u;
        internal const uint JOY_BUTTON29 = 0x10000000u;
        internal const uint JOY_BUTTON30 = 0x20000000u;
        internal const uint JOY_BUTTON31 = 0x40000000u;
        internal const uint JOY_BUTTON32 = 0x80000000u;

        // Return flags

        [Flags]
        internal enum JoystickReturnMasks : uint
        {
            JOY_RETURNX = 0x00000001u,
            JOY_RETURNY = 0x00000002u,
            JOY_RETURNZ = 0x00000004u,
            JOY_RETURNR = 0x00000008u,
            JOY_RETURNU = 0x00000010u,     //* axis 5 */
            JOY_RETURNV = 0x00000020u,     //* axis 6 */
            JOY_RETURNPOV = 0x00000040u,
            JOY_RETURNBUTTONS = 0x00000080u,
            JOY_RETURNRAWDATA = 0x00000100u,
            JOY_RETURNPOVCTS = 0x00000200u,
            JOY_RETURNCENTERED = 0x00000400u,
            JOY_USEDEADZONE = 0x00000800u,
        }
    }
}
