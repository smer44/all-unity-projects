using UnityEngine;

public class DailyActionsProvider : MonoBehaviour
{

/*
Date availability
    Is this action valid on this date?

Location availability
    Is the player in the right place?

Quest availability
    Has the player unlocked this action?

NPC availability
    Is the relevant character present?

World-state availability
    Is the town destroyed, locked down, celebrating, under attack, etc.?

Player-state availability
    Is the player injured, exhausted, cursed, wanted, or restricted?

Resource availability
    Does the player have required money/items/tools?

Energy performability
    Can the player pay the energy cost right now?

    Known action
    → passes date rules?
    → passes location rules?
    → passes quest/world/player rules?
    → becomes available today
    → has enough energy?
    → becomes performable now


    Known action
    → passes date rules?
    → passes location rules?
    → passes quest/world/player rules?
    → becomes available today
    → has enough energy?
    → becomes performable now


    Month 1, Day 15
    Event: Spring Festival
    Adds actions:
        - attend_festival
        - buy_festival_food
        - meet_traveling_merchant

Month 2, Day 1 to Day 5
    Event: Tax Period
    Adds actions:
        - pay_tax
        - request_tax_delay



    What calendar events are active today?
    What actions do those events unlock?


TurnTimeManager asks each action provider:
    What actions are available today?

Providers:
    BaseActionProvider
    TownActionProvider
    QuestActionProvider
    NPCScheduleActionProvider
    CalendarEventActionProvider
    InjuryActionProvider
    LocationActionProvider

    (different provider types, having list of providers)


 Keep “hidden” and “disabled” separate

   Hidden:
    Secret ritual before discovery.
    Quest action before quest starts.
    Spoiler action before story trigger.

Disabled:
    Market is closed today.
    Not enough energy.
    Need shovel.
    NPC is unavailable.
    Requires Month 3.


     Visible Available Action
    Can perform now.

Visible Disabled Action
    Show reason why unavailable.

Hidden Action
    Do not show to player.


Store failure reasons
Visit Market
    Unavailable reason:
        "The market is closed on Sundays."

Train With Guard
    Unavailable reason:
        "The guard trains recruits only on Monday, Wednesday, and Friday."

Attend Festival
    Unavailable reason:
        "The festival is held on Day 15 of each month."

Gather Supplies
    Unavailable reason:
        "You are too injured to gather supplies today."

StoryKnowledge
 ├── current date
 ├── current energy
 ├── story flags


*/

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
