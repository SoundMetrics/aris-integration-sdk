namespace SoundMetrics.Aris.Connection;

internal interface IStateHandler
{
    void OnEnter(StateMachineContext context);

    ConnectionState? DoProcessing(
        StateMachineContext context,
        in MachineEvent ev);

    void OnLeave(StateMachineContext context);

    public static readonly ConnectionState? NoStateChange = default;
}
