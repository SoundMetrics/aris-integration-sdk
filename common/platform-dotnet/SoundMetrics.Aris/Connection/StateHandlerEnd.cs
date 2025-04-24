namespace SoundMetrics.Aris.Connection
{
    internal sealed class StateHandlerEnd : IStateHandler
    {
        public void OnEnter(StateMachineContext context)
        {
            context.CommandConnection?.Dispose();
            context.CommandConnection = null;
        }

        public ConnectionState? DoProcessing(StateMachineContext context, in MachineEvent ev)
        {
            throw new System.NotImplementedException();
        }

        public void OnLeave(StateMachineContext context)
        {
            // Do nothing.
        }
    }
}
