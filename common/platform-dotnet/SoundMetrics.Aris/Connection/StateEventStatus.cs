namespace SoundMetrics.Aris.Connection;

internal record struct StateEventStatus
{
    public bool IsError { get; }

    public readonly bool IsOkay => !IsError;

    public string ErrorText { get; }

    private StateEventStatus(bool isError, string errorText)
    {
        IsError = isError;
        ErrorText = errorText;
    }

    public override string ToString() => IsOkay ? "Okay" : $"Error: {ErrorText}";

    public static StateEventStatus Okay = new(false, "");

    public static StateEventStatus FromError(string errorText) => new(true, errorText);
}
