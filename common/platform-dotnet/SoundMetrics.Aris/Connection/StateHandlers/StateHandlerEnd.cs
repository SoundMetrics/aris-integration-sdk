namespace SoundMetrics.Aris.Connection.StateHandlers
{
    internal sealed class StateHandlerEnd : IStateHandler
    {
        public StateEventStatus OnEnter(StateMachineContext context)
        {
            context.CommandConnection?.Dispose();
            context.CommandConnection = null;
            return StateEventStatus.Okay;
        }

        public StateProcessingResult DoProcessing(
            ConnectionState currentState,
            StateMachineContext context,
            in StateMachineEvent ev)
        {
            // Do nothing.
            return IStateHandler.NoStateChange;
        }

        public StateEventStatus OnLeave(StateMachineContext context)
        {
            // Do nothing.
            return StateEventStatus.Okay;
        }
    }
}
