namespace SoundMetrics.Aris.Availability
{
    public sealed class VoyagerBeacon : ArisBeacon
    {
        internal VoyagerBeacon()
            : base(hasDepthReading: false)
        {
        }
    }
}
