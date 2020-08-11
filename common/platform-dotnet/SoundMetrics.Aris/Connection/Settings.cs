namespace SoundMetrics.Aris.Connection
{
    public interface ISettings
    {
        string[] GenerateCommand();
    }

    public sealed class PassiveSettings : ISettings
    {
        public string[] GenerateCommand()
        {
            return new[] { "passive" };
        }
    }
}
