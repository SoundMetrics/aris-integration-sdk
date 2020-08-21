using System.Diagnostics;

namespace SoundMetrics.Aris.Availability
{
    [DebuggerDisplay("v {Major}.{Minor}.{BuildNumber}")]
    public struct OnboardSoftwareVersion
    {
        public uint Major;
        public uint Minor;
        public uint BuildNumber;

        public override string ToString()
        {
            return $"{Major}.{Minor}.{BuildNumber}";
        }
    }
}
