namespace SimplifiedProtocolTestWpfCore
{
    public struct CommandRequest
    {
        public string[] Text { get; private set; }

        public CommandRequest(params string[] text)
        {
            Text = text;
        }
    }
}
