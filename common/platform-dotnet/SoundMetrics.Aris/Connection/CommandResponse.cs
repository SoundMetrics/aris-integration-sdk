using System.Collections.Generic;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class CommandResponse
    {
        public CommandResponse(bool isSuccessful, IEnumerable<string> response)
        {
            IsSuccessful = isSuccessful;
            ResponseText = response;
        }

        public bool IsSuccessful { get; }
        public IEnumerable<string> ResponseText { get; }
    }
}
