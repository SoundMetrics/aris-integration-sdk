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
}
