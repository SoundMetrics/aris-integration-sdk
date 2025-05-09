namespace SoundMetrics.Aris.Connection.StateHandlers;

internal interface IStateHandler
{
    void OnEnter(StateMachineContext context);

    ConnectionState? DoProcessing(
        ConnectionState currentState,
        StateMachineContext context,
        in StateMachineEvent ev);

    void OnLeave(StateMachineContext context);

    public static readonly ConnectionState? NoStateChange = default;
}
