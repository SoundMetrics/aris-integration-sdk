namespace SimplifiedProtocolTestWpfCore
{
    public struct CommandResponse
    {
        public string Text { get; private set; }

        public static explicit operator CommandResponse(string text)
        {
            return new CommandResponse { Text = text };
        }
    }
}
