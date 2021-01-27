// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    public enum AdjustRangeOperation
    {
        /// <summary>Invokes the 'z' keyboard shortcut behavior.</summary>
        ShortWindow,

        /// <summary>Invokes the 'a' keyboard shortcut behavior.</summary>
        MediumWindow,

        /// <summary>Invokes the 'q' keyboard shortcut behavior.</summary>
        LongWindow,

        /// <summary>Moves the image window start closer.</summary>
        WindowStartIn,

        /// <summary>Moves the image window start farther.</summary>
        WindowStartOut,

        /// <summary>Moves the image window end closer.</summary>
        WindowEndIn,

        /// <summary>Moves the image window end farther.</summary>
        WindowEndOut,

        /// <summary>Slides the whole window closer.</summary>
        SlideRangeIn,

        /// <summary>Slides the whole window farther.</summary>
        SlideRangeOut,
    }
}
