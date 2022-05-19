// Copyright (c) 2010-2021 Sound Metrics Corp.

namespace SoundMetrics.Aris.Core.Raw
{
    public enum WindowOperation
    {
        /// <summary>
        /// Sets the image window to a short view, similar to the 'z'
        /// keyboard shortcut behavior in ARIScope.
        /// </summary>
        SetShortWindow,

        /// <summary>
        /// Sets the image window to a medium view, similar to the 'a'
        /// keyboard shortcut behavior in ARIScope.
        /// </summary>
        SetMediumWindow,

        /// <summary>
        /// Sets the image window to a long view, similar to the 'q'
        /// keyboard shortcut behavior in ARIScope.
        /// </summary>
        SetLongWindow,

        /// <summary>Moves the image window start closer.</summary>
        MoveWindowStartCloser,

        /// <summary>Moves the image window start farther.</summary>
        MoveWindowStartFarther,

        /// <summary>Moves the image window end closer.</summary>
        MoveWindowEndCloser,

        /// <summary>Moves the image window end farther.</summary>
        MoveWindowEndFarther,

        /// <summary>Slides the whole window closer.</summary>
        SlideWindowCloser,

        /// <summary>Slides the whole window farther.</summary>
        SlideWindowFarther,
    }
}
