using System.Threading;

namespace SoundMetrics.Aris;

internal sealed class CookieFactory
{
    public uint GetNextCookie() => (uint)Interlocked.Increment(ref nextCookie);

    private int nextCookie = 0;
}
