// Copyright (c) 2010-2022 Sound Metrics Corp.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SoundMetrics.Aris.Core.Raw
{
    public sealed partial class AcousticSettingsRaw
    {
        public AcousticSettingsRaw AdjustRange(
            WindowOperation operation,
            ObservedConditions observedConditions,
            bool useMaxFrameRate,
            bool useAutoFrequency)
        {
            if (rangeOperationMap.TryGetValue(operation, out var op))
            {
                return op(this, observedConditions, useMaxFrameRate, useAutoFrequency);
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
                    { WindowOperation.SetShortWindow, WindowOperations.ToShortWindow },
                    { WindowOperation.SetMediumWindow, WindowOperations.ToMediumWindow },
                    { WindowOperation.SetLongWindow, WindowOperations.ToLongWindow },

                    { WindowOperation.MoveWindowStartCloser, WindowOperations.MoveWindowStartCloser },
                    { WindowOperation.MoveWindowStartFarther, WindowOperations.MoveWindowStartFarther },
                    { WindowOperation.MoveWindowEndCloser, WindowOperations.MoveWindowEndCloser },
                    { WindowOperation.MoveWindowEndFarther, WindowOperations.MoveWindowEndFarther },
                    { WindowOperation.SlideWindowCloser, WindowOperations.SlideWindowCloser },
                    { WindowOperation.SlideWindowFarther, WindowOperations.SlideWindowFarther },
                });
    }
}
