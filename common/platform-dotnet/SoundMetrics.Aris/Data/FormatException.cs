using System;

namespace SoundMetrics.Aris.Data
{
    public sealed class FormatException : Exception
    {
        public FormatException(string message)
            : base(message)
        {

        }
    }
}
