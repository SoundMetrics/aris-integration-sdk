// Copyright (c) 2010-2021 Sound Metrics Corp.

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
                { WindowOperation.ShortWindow, WindowOperations.ToShortWindow },
                { WindowOperation.MediumWindow, WindowOperations.ToMediumWindow },
                { WindowOperation.LongWindow, WindowOperations.ToLongWindow },

                { WindowOperation.WindowStartIn, WindowOperations.MoveWindowStartIn },
                { WindowOperation.WindowStartOut, WindowOperations.MoveWindowStartOut },
                { WindowOperation.WindowEndIn, WindowOperations.MoveWindowEndIn },
                { WindowOperation.WindowEndOut, WindowOperations.MoveWindowEndOut },
                { WindowOperation.SlideRangeIn, WindowOperations.SlideRangeIn },
                { WindowOperation.SlideRangeOut, WindowOperations.SlideRangeOut },
            };
        }
    }
}
