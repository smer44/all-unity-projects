using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitActingByNeeds : MonoBehaviour
{
    [Serializable]
    public struct NeedActionPair
    {
        public Need Need;
        public RTSAction Action;
    }

    [SerializeField] private UnitWithNeeds unitWithNeeds;
    [SerializeField] private NeedActionPair[] needActions;

    private readonly Dictionary<string, List<RTSAction>> actionsByNeedName = new();
    private readonly List<UnitActingByNeeds> cooperators = new();

    private NeedsActionContext actionContext;
    private Need currentNeed;
    private RTSAction currentAction;
    private float currentActionTimeLeft;

    private void Awake()
    {
        if (unitWithNeeds == null)
            unitWithNeeds = GetComponent<UnitWithNeeds>();

        actionContext = new NeedsActionContext(this);
        RebuildActionsByNeedName();
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float delta)
    {
        if (currentAction != null)
        {
            currentActionTimeLeft -= delta;
            if (currentActionTimeLeft > 0f)
                return;

            currentAction = null;
        }

        SelectAndStartAction();
    }

    public void RebuildActionsByNeedName()
    {
        actionsByNeedName.Clear();
        if (needActions == null)
            return;

        foreach (NeedActionPair pair in needActions)
        {
            if (pair.Need == null || pair.Action == null)
                continue;

            string needName = pair.Need.Name;
            if (string.IsNullOrWhiteSpace(needName))
                continue;

            if (!actionsByNeedName.TryGetValue(needName, out List<RTSAction> actions))
            {
                actions = new List<RTSAction>();
                actionsByNeedName.Add(needName, actions);
            }

            actions.Add(pair.Action);
        }
    }

    private void SelectAndStartAction()
    {
        if (unitWithNeeds == null)
            return;

        currentNeed = unitWithNeeds.GetNeedWithMaxNecessity();
        if (currentNeed == null)
            return;

        if (!actionsByNeedName.TryGetValue(currentNeed.Name, out List<RTSAction> actions))
            return;

        foreach (RTSAction action in actions)
        {
            if (action == null || !action.CheckAvailable(actionContext))
                continue;

            currentAction = action;
            currentActionTimeLeft = Mathf.Max(0f, action.MinimalTime);
            currentAction.Act(actionContext);
            return;
        }
    }

    private sealed class NeedsActionContext : ActionContext<UnitActingByNeeds, UnitWithNeeds, Need>
    {
        private readonly UnitActingByNeeds owner;

        public NeedsActionContext(UnitActingByNeeds owner)
        {
            this.owner = owner;
        }

        public override UnitActingByNeeds Actor => owner;
        public override UnitWithNeeds Field => owner.unitWithNeeds;
        public override Need Target => owner.currentNeed;
        public override IReadOnlyList<UnitActingByNeeds> Cooperators => owner.cooperators;

        public override float MinimalTime()
        {
            return owner.currentAction == null ? 0f : owner.currentAction.MinimalTime;
        }
    }
}
