namespace SoundMetrics.Aris.Connection;

internal abstract class StateHandler
{
    public virtual void OnEnter(StateMachineContext context) { }

    public virtual ConnectionState? DoProcessing(
        StateMachineContext context,
        in MachineEvent ev)
    {
        return NoStateChange;
    }

    public virtual void OnLeave(StateMachineContext context) { }

    public static readonly ConnectionState? NoStateChange = default;
}
