using System;

namespace SoundMetrics.Aris.Data
{
    public sealed class ArisFormatException : Exception
    {
        public ArisFormatException(string message)
            : base(message)
        {

        }
    }
}
