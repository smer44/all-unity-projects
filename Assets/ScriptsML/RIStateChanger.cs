using UnityEngine;

public abstract class RIStateChanger<S,A>
{
    
    public abstract S NextState(S prev, A action);

    public abstract S StartState();

    public abstract S EndState();

    public abstract float ImmediateReward(S prev, S next);

    public abstract bool IsEndState(S state);

    public abstract A[] AllowedActions(S state);

    public abstract bool IsAllowed(S state);

}
