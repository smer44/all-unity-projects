public abstract class ActiveTickAction : TickAction
{
    public sealed override bool Tick(float delta, TickActionContext context)
    {
        return context is ActiveTickActionContext activeContext && Tick(delta, activeContext);
    }

    public virtual bool Tick(float delta, ActiveTickActionContext context)
    {
        return TickTimer(delta, GetInterval(context));
    }

    public void TickAndAct(float delta, ActiveTickActionContext context)
    {
        if (Tick(delta, context))
            Act(context);
    }

    public abstract void Act(ActiveTickActionContext context);

    protected abstract float GetInterval(ActiveTickActionContext context);
}
