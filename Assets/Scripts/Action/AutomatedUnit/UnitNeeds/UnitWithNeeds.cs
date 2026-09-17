using System.Collections.Generic;
using UnityEngine;

public class UnitWithNeeds : MonoBehaviour
{
    [SerializeField] private List<Need> needs = new();

    public IReadOnlyList<Need> Needs => needs;

    private void Update()
    {
        TickNeeds(Time.deltaTime);
    }

    public void TickNeeds(float delta)
    {
        foreach (Need need in needs)
        {
            if (need == null)
                continue;

            need.Tick(delta);
        }
    }

    public Need GetNeedWithMaxNecessity()
    {
        Need maxNeed = null;
        float maxNecessity = float.NegativeInfinity;

        foreach (Need need in needs)
        {
            if (need == null)
                continue;

            float necessity = need.GetNecessity();
            if (necessity <= maxNecessity)
                continue;

            maxNeed = need;
            maxNecessity = necessity;
        }

        return maxNeed;
    }
}
