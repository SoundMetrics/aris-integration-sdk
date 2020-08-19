using System.Collections.Generic;

namespace SoundMetrics.Aris.Connection
{
    public class CommandResponse
    {
        internal CommandResponse(bool isSuccessful, List<string> response)
        {
            IsSuccessful = isSuccessful;
            this.response = response;
        }

        public bool IsSuccessful { get; }
        public IEnumerable<string> ResponseText => response;

        private readonly List<string> response;
    }
}
