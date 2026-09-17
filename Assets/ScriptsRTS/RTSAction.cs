using UnityEngine;
using System.Collections.Generic;


public interface IActionContext
{
    float MinimalTime();
}


public abstract class ActionContext<A, F, T>
    : IActionContext
{
    public abstract A Actor { get; }
    public abstract F Field { get; }
    public abstract T Target { get; } // can be unit, item or location

    public abstract float MinimalTime();
    public abstract IReadOnlyList<A> Cooperators { get; }
}


public abstract class RTSAction : ScriptableObject
{
    [SerializeField] private float minimalTime;

    public float MinimalTime => minimalTime;

    public abstract bool CheckAvailable(IActionContext ctx);
    public abstract void Act(IActionContext ctx);
}


public abstract class RTSAction<A, F, T> : RTSAction
{
    public sealed override bool CheckAvailable(IActionContext ctx)
    {
        return ctx is ActionContext<A, F, T> typedCtx && CheckAvailable(typedCtx);
    }

    public sealed override void Act(IActionContext ctx)
    {
        if (ctx is ActionContext<A, F, T> typedCtx)
            Act(typedCtx);
    }

    public bool CheckAvailable(ActionContext<A, F, T> ctx)
    {
        return CheckTools(ctx) && CheckLocation(ctx) && CheckCooperation(ctx);
    }

    public abstract void Act(ActionContext<A, F, T> ctx);

    protected virtual bool CheckTools(ActionContext<A, F, T> ctx)
    {
        return true;
    }

    protected virtual bool CheckLocation(ActionContext<A, F, T> ctx)
    {
        return true;
    }

    protected virtual bool CheckCooperation(ActionContext<A, F, T> ctx)
    {
        return true;
    }    

}
