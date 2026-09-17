public abstract class ReactiveTickAction : TickAction
{
    public sealed override bool Tick(float delta, TickActionContext context)
    {
        return context is ReactiveTickActionContext reactiveContext && Tick(delta, reactiveContext);
    }

    public virtual bool Tick(float delta, ReactiveTickActionContext context)
    {
        return TickTimer(delta, GetInterval(context));
    }

    public void TickRestore(float delta, ReactiveTickActionContext context)
    {
        if (Tick(delta, context))
            OnTick(context.Owner);
    }

    protected virtual void OnTick(AutoBattlingUnit owner)
    {
    }

    public abstract void Act(OnAttackTickActionContext context);

    protected abstract float GetInterval(ReactiveTickActionContext context);
}
