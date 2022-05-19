// Copyright (c) 2010-2022 Sound Metrics Corp.

using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.Core.Raw
{
    public sealed partial class AcousticSettingsRaw
    {
        public static AcousticSettingsRaw AdjustRange(
            AcousticSettingsRaw currentSettings,
            ObservedConditions observedConditions,
            WindowOperation operation,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (rangeOperationMap.TryGetValue(operation, out var adjustFn))
            {
                return adjustFn(
                    currentSettings,
                    observedConditions,
                    useMaxFrameRate,
                    useAutoFrequency);
            }
            else
            {
                throw new NotImplementedException($"Operation '{operation}' is not implemented");
            }
        }

        private static readonly Dictionary<WindowOperation, AdjustRangeFn>
            rangeOperationMap = BuildRangeOperationMap();

        private static Dictionary<WindowOperation, AdjustRangeFn> BuildRangeOperationMap()
        {
            return new Dictionary<WindowOperation, AdjustRangeFn>
            {
                { WindowOperation.SetShortWindow, WindowOperations.ToShortWindow },
                { WindowOperation.SetMediumWindow, WindowOperations.ToMediumWindow },
                { WindowOperation.SetLongWindow, WindowOperations.ToLongWindow },

                { WindowOperation.MoveWindowStartCloser, WindowOperations.MoveWindowStartCloser },
                { WindowOperation.MoveWindowStartFarther, WindowOperations.MoveWindowStartFarther },
                { WindowOperation.MoveWindowEndCloser, WindowOperations.MoveWindowEndCloser },
                { WindowOperation.MoveWindowEndFarther, WindowOperations.MoveWindowEndFarther },
                { WindowOperation.SlideWindowCloser, WindowOperations.SlideWindowCloser },
                { WindowOperation.SlideWindowFarther, WindowOperations.SlideWindowFarther },
            };
        }
    }
}
