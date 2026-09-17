using UnityEngine;

public abstract class RIActionChooser<S,A>
{

    public abstract A ChooseRandomAction(RIStateChanger<S,A> states, S state);

}
