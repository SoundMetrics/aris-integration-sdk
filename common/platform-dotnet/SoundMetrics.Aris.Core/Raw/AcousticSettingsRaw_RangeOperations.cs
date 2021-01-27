// Copyright (c) 2010-2021 Sound Metrics Corp.

using System;
using System.Collections.Generic;

namespace SoundMetrics.Aris.Core.Raw
{
    public sealed partial class AcousticSettingsRaw
    {
        public AcousticSettingsRaw AdjustRange(AcousticSettingsRaw currentSettings, AdjustRangeOperation operation)
        {
            if (rangeOperationMap.TryGetValue(operation, out var adjustFn))
            {
                return adjustFn(currentSettings);
            }
            else
            {
                throw new NotImplementedException($"Operation '{operation}' is not implemented");
            }
        }

        private static readonly Dictionary<AdjustRangeOperation, AdjustRangeFn>
            rangeOperationMap = BuildRangeOperationMap();

        private static Dictionary<AdjustRangeOperation, AdjustRangeFn> BuildRangeOperationMap()
        {
            return new Dictionary<AdjustRangeOperation, AdjustRangeFn>
            {
                { AdjustRangeOperation.ShortWindow, AdjustRangeOperations.ToShortWindow },
                { AdjustRangeOperation.MediumWindow, AdjustRangeOperations.ToMediumWindow },
                { AdjustRangeOperation.LongWindow, AdjustRangeOperations.ToLongWindow },

                { AdjustRangeOperation.WindowStartIn, AdjustRangeOperations.MoveWindowStartIn },
                { AdjustRangeOperation.WindowStartOut, AdjustRangeOperations.MoveWindowStartOut },
                { AdjustRangeOperation.WindowEndIn, AdjustRangeOperations.MoveWindowEndIn },
                { AdjustRangeOperation.WindowEndOut, AdjustRangeOperations.MoveWindowEndOut },
                { AdjustRangeOperation.SlideRangeIn, AdjustRangeOperations.SlideRangeIn },
                { AdjustRangeOperation.SlideRangeOut, AdjustRangeOperations.SlideRangeOut },
            };
        }
    }
}
