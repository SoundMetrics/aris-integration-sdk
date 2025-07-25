namespace SoundMetrics.Aris.Connection.StateHandlers
{
    internal sealed class FastTransitionTo(ConnectionState nextState)
        : IStateHandler
    {
        public StateEventStatus OnEnter(StateMachineContext context)
        {
            return StateEventStatus.Okay;
        }

        public StateProcessingResult DoProcessing(
            ConnectionState currentState,
            StateMachineContext context,
            in StateMachineEvent ev)
        {
            return new(StateEventStatus.Okay, nextState);
        }

        public StateEventStatus OnLeave(StateMachineContext context)
        {
            return StateEventStatus.Okay;
        }
    }
}
