// Copyright 2014-2019 Sound Metrics Corp. All Rights Reserved.

using System;

namespace SoundMetrics.HID.Windows
{
    public struct JoystickInfo
    {
        public uint JoystickId;
        public JoyCaps Caps;

        public static readonly JoystickInfo Empty = new JoystickInfo();
    }
}
