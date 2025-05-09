namespace SoundMetrics.Aris.Connection.StateHandlers
{
    internal sealed class StateHandlerEnd : IStateHandler
    {
        public void OnEnter(StateMachineContext context)
        {
            context.CommandConnection?.Dispose();
            context.CommandConnection = null;
        }

        public ConnectionState? DoProcessing(
            ConnectionState currentState,
            StateMachineContext context,
            in StateMachineEvent ev)
        {
            // Do nothing.
            return IStateHandler.NoStateChange;
        }

        public void OnLeave(StateMachineContext context)
        {
            // Do nothing.
        }
    }
}
