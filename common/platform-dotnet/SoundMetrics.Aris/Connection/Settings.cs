using System.Collections.Generic;
using System.Linq;

namespace SoundMetrics.Aris.Connection
{
    public interface ISettings
    {
    }

    public sealed class TestPatternSettings : ISettings, ICommand
    {
        public string[] GenerateCommand()
        {
            return new[] { "testpattern" };
        }
    }

    public sealed class PassthroughSettings : ISettings, ICommand
    {
        public PassthroughSettings(IEnumerable<string> passthroughValues)
        {
            this.passthroughValues = passthroughValues.ToArray();
        }

        public string[] GenerateCommand()
        {
            return passthroughValues;
        }

        private readonly string[] passthroughValues;
    }
}
