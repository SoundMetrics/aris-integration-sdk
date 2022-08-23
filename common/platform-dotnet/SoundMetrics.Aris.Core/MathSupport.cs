// Copyright (c) 2022 Sound Metrics Corp. All Rights Reserved.

using System;
using static System.Math;

namespace SoundMetrics.Aris.Core
{
    public static class MathSupport
    {
        /// <summary>
        /// Rounds using the 'round midpoint away from zero' convention for use
        /// when statistical fairness is not important. The .net framework
        /// System.Math.Round() defaults to 'round midpoint to even' which is
        /// more appropriate for financial and statistical work.
        /// </summary>
        /// <param name="value">The value to be rounded.</param>
        /// <returns>The rounded value.</returns>
        public static double RoundAway(double value)
            => Round(value, MidpointRounding.AwayFromZero);
    }
}
