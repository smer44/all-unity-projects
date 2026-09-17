using UnityEngine;
using System.Collections.Generic;

public class RIQuality<S,A>
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public  Dictionary<(S, A), float> q = new Dictionary<(S, A), float>();
    private Dictionary<S, (A action, float value)> bestActionInState = new Dictionary<S, (A action, float value)>();

    public float Get(S state, A action)
    {
        float value = 0f;
        q.TryGetValue((state,action), out value);
        return value;
    }

    public void Set(S state, A action, float value)
    {
        q[(state, action)] = value;

        if (!bestActionInState.TryGetValue(state, out (A action, float value) best))
        {
            bestActionInState[state] = (action, value);
            return;
        }

        if (EqualityComparer<A>.Default.Equals(action, best.action))
        {
            RecalculateBestInState(state);
            return;
        }

        if (value > best.value)
        {
            bestActionInState[state] = (action, value);
        }
    }

    public A BestActionInState(S state)
    {
        if (bestActionInState.TryGetValue(state, out (A action, float value) best))
        {
            return best.action;
        }

        return default(A);
    }

    public float BestValueInState(S state)
    {
        if (bestActionInState.TryGetValue(state, out (A action, float value) best))
        {
            return best.value;
        }

        return 0f;
    }

    private void RecalculateBestInState(S state)
    {
        bool found = false;
        A bestAction = default(A);
        float bestValue = 0f;

        foreach (KeyValuePair<(S, A), float> entry in q)
        {
            if (!EqualityComparer<S>.Default.Equals(entry.Key.Item1, state))
            {
                continue;
            }

            if (!found || entry.Value > bestValue)
            {
                found = true;
                bestAction = entry.Key.Item2;
                bestValue = entry.Value;
            }
        }

        if (found)
        {
            bestActionInState[state] = (bestAction, bestValue);
        }
        else
        {
            bestActionInState.Remove(state);
        }
    }

}
