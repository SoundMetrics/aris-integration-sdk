namespace SoundMetrics.Aris.Connection
{
    public interface ISettings
    {
    }

    public sealed class PassiveSettings : ISettings, ICommand
    {
        public string[] GenerateCommand()
        {
            return new[] { "passive" };
        }
    }

    public sealed class TestPatternSettings : ISettings, ICommand
    {
        public string[] GenerateCommand()
        {
            return new[] { "testpattern" };
        }
    }
}
