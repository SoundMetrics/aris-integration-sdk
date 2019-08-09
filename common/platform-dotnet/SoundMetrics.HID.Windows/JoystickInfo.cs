// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;

namespace SoundMetrics.HID.Windows
{
    public static partial class Joystick
    {
        public struct JoystickInfo
        {
            public uint JoystickId;
            public JoyCaps Caps;

            private static Lazy<JoystickInfo> empty =
                new Lazy<JoystickInfo>(() => new JoystickInfo());

            public static JoystickInfo Empty = empty.Value;
        }
    }
}
