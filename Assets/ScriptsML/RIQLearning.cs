using UnityEngine;

public class RIQLearning<S,A>
{
    
    public RIStateChanger<S,A> states;
    public RIActionChooser<S,A> actions;

    public RIQuality<S,A> quality;

    public float epsilon;

    public float gammaDecay;

    public float alfa;

    public int maxChain = 100;

    public A NextAction(S state)
    {
        if (Random.value < epsilon)
        {
            return actions.ChooseRandomAction(states, state);
        }
        return quality.BestActionInState(state);
    }

    public S UpdateStep(S state)
    {
        
        A chosenAction = NextAction(state);

        S nextState = states.NextState(state, chosenAction);

        float r = states.ImmediateReward(state,nextState);

        float target = 0f;
        if (states.IsEndState(nextState))
        {
            target = states.ImmediateReward(state,nextState);

        }
        else
        {
            target = states.ImmediateReward(state,nextState) + gammaDecay * quality.BestValueInState(nextState);
        }

        float oldQuality = quality.Get(state, chosenAction);

        float newQuality = oldQuality + alfa * (target -oldQuality);

        quality.Set(state,chosenAction,newQuality);
        if (states.IsAllowed(nextState))
        {
            
            return nextState;
        }
        else
        {
            return states.EndState();
        }
        

        
    }

    public void UpdateChain()
    {
        //Debug.Log("called UpdateChain");
        S state = states.StartState();
        int n = 0;
        while (n < maxChain && !states.IsEndState(state))
        {
            state = UpdateStep(state);
            n++;
        }
    }

    public void UpdateLoop(int times)
    {
        for (int i = 0; i < times; i++)
        {
            UpdateChain();
        }
    }

}
