namespace SoundMetrics.Aris.Availability
{
    public sealed class ExplorerBeacon : ArisBeacon
    {
        internal ExplorerBeacon()
            : base(hasDepthReading: true)
        {

        }
    }
}
