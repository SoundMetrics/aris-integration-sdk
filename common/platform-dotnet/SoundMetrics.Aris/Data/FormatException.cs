﻿using System;

namespace SoundMetrics.Aris.Data
{
    public sealed class FormatException : Exception
    {
        public FormatException()
        {
        }

        public FormatException(string message)
            : base(message)
        {

        }

        public FormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
