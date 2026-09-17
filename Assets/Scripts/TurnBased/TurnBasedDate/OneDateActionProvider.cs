using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OneDateActionProvider", menuName = "Turn Based/Action Providers/One Date")]
public sealed class OneDateActionProvider : AbstractActionProvider
{
    [Serializable]
    private struct DateActions
    {
        [Tooltip("Absolute date")]
        public AbstractDate absoluteDay;

        [Tooltip("Actions in this day")]
        public AbstractTurnAction[] actions;
    }

    private static readonly AbstractTurnAction[] EmptyActions = new AbstractTurnAction[0];

    [SerializeField] private DateActions[] actionsByDate;

    public override AbstractTurnAction[] GetActionsForDate(StoryMemory memory)
    {
        AbstractDate currentDate = memory != null ? memory.CurrentDate : null;
        if (currentDate == null || actionsByDate == null)
            return EmptyActions;

        int currentDay = currentDate.Days();

        List<AbstractTurnAction> matchingActions = new();

        foreach (DateActions dateActions in actionsByDate)
        {   

            if (dateActions.absoluteDay == null || dateActions.absoluteDay.Days() != currentDay || dateActions.actions == null)
                continue;

            foreach (AbstractTurnAction action in dateActions.actions)
            {
                if (action != null)
                    matchingActions.Add(action);
            }
        }

        return matchingActions.Count > 0 ? matchingActions.ToArray() : EmptyActions;
    }
}
