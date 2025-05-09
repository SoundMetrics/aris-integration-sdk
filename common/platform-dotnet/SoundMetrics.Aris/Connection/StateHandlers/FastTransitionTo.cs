namespace SoundMetrics.Aris.Connection.StateHandlers
{
    internal sealed class FastTransitionTo(ConnectionState nextState)
        : IStateHandler
    {
        public void OnEnter(StateMachineContext context)
        {
        }

        public ConnectionState? DoProcessing(
            ConnectionState currentState,
            StateMachineContext context,
            in StateMachineEvent ev)
            => nextState;

        public void OnLeave(StateMachineContext context)
        {
        }
    }
}
