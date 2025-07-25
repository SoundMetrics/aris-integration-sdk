namespace SoundMetrics.Aris.Connection.StateHandlers;

internal interface IStateHandler
{
    StateEventStatus OnEnter(StateMachineContext context);

    StateProcessingResult DoProcessing(
        ConnectionState currentState,
        StateMachineContext context,
        in StateMachineEvent ev);

    StateEventStatus OnLeave(StateMachineContext context);

    public static readonly StateProcessingResult NoStateChange = new(StateEventStatus.Okay, default);
}
