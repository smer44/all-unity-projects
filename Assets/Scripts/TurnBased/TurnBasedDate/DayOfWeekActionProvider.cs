using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DayOfWeekActionProvider", menuName = "Turn Based/Action Providers/Day Of Week")]
public sealed class DayOfWeekActionProvider : AbstractActionProvider
{
    [Serializable]
    private struct DayActions
    {
        [Tooltip("0 = Monday, 1 = Tuesday, ..., 6 = Sunday.")]
        //[Range(0, 6)]
        public int dayOfWeek;

        public AbstractTurnAction[] actions;
    }

    private static readonly AbstractTurnAction[] EmptyActions = new AbstractTurnAction[0];

    [SerializeField] private DayActions[] actionsByDayOfWeek;

    public override AbstractTurnAction[] GetActionsForDate(StoryMemory memory)
    {
        AbstractDate currentDate = memory != null ? memory.CurrentDate : null;
        if (currentDate == null || actionsByDayOfWeek == null)
            return EmptyActions;

        int currentDayOfWeek = currentDate.DayOfWeek();
        List<AbstractTurnAction> matchingActions = new();

        foreach (DayActions dayActions in actionsByDayOfWeek)
        {
            if (dayActions.dayOfWeek != currentDayOfWeek || dayActions.actions == null)
                continue;

            foreach (AbstractTurnAction action in dayActions.actions)
            {
                if (action != null)
                    matchingActions.Add(action);
            }
        }

        return matchingActions.Count > 0 ? matchingActions.ToArray() : EmptyActions;
    }
}
