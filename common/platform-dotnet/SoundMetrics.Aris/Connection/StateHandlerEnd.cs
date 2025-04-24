namespace SoundMetrics.Aris.Connection
{
    internal sealed class StateHandlerEnd : StateHandler
    {
        public override void OnEnter(StateMachineContext context)
        {
            context.CommandConnection?.Dispose();
            context.CommandConnection = null;
        }
    }
}
