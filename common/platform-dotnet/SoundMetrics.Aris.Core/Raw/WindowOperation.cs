// Copyright (c) 2010-2021 Sound Metrics Corp.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace SoundMetrics.Aris.Core.Raw
{
    /*
     * NOTE
     *
     * These enum values are spelled in the batch files located in
     * ARIScope\automation-streamdeck\. The compiler will not catch
     * the error if you rename them here without changing the batch
     * files.
     */

    /// <summary>
    /// Simple window operations.
    /// </summary>
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

    public static class WindowOpeartionOps
    {
        public static AcousticSettingsRaw ApplyWindowOperation(
            this AcousticSettingsRaw settings,
            WindowOperation operation,
            ObservedConditions observedConditions,
            IAdjustWindowTerminus adjustmentStrategy,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (rangeOperationMap.TryGetValue(operation, out var op))
            {
                return op(
                    settings,
                    observedConditions,
                    adjustmentStrategy,
                    useMaxFrameRate,
                    useAutoFrequency);
            }
            else
            {
                throw new NotImplementedException($"Operation '{operation}' is not implemented");
            }
        }

        private static readonly ReadOnlyDictionary<WindowOperation, AdjustRangeFn>
            rangeOperationMap = new ReadOnlyDictionary<WindowOperation, AdjustRangeFn>(
                new Dictionary<WindowOperation, AdjustRangeFn>
                {
                    { WindowOperation.SetShortWindow, PredefinedWindowSizes.ToShortWindow },
                    { WindowOperation.SetMediumWindow, PredefinedWindowSizes.ToMediumWindow },
                    { WindowOperation.SetLongWindow, PredefinedWindowSizes.ToLongWindow },

                    { WindowOperation.MoveWindowStartCloser, PredefinedWindowSizes.MoveWindowStartCloser},
                    { WindowOperation.MoveWindowStartFarther, PredefinedWindowSizes.MoveWindowStartFarther },
                    { WindowOperation.MoveWindowEndCloser, PredefinedWindowSizes.MoveWindowEndCloser },
                    { WindowOperation.MoveWindowEndFarther, PredefinedWindowSizes.MoveWindowEndFarther },
                    { WindowOperation.SlideWindowCloser, PredefinedWindowSizes.SlideWindowCloser },
                    { WindowOperation.SlideWindowFarther, PredefinedWindowSizes.SlideWindowFarther },
                });
    }
}
