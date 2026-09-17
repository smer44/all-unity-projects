using UnityEngine;

[CreateAssetMenu(fileName = "UnitTickActions", menuName = "SO Auto Battler/Unit Tick Actions")]
public class UnitTickActions : ScriptableObject
{
    [SerializeField] private TickAction[] actions;

    public TickAction[] Actions => actions;

    public void TickActive(float delta, ActiveTickActionContext context)
    {
        if (actions == null)
            return;

        foreach (TickAction action in actions)
        {
            if (action is ActiveTickAction activeAction)
                activeAction.TickAndAct(delta, context);
        }
    }

    public void TickReactive(float delta, AutoBattlingUnit owner)
    {
        if (actions == null)
            return;

        foreach (TickAction action in actions)
        {
            if (action is ReactiveTickAction reactiveAction)
                reactiveAction.TickRestore(delta, new ReactiveTickActionContext(owner));
        }
    }

    public void ActReactive(OnAttackTickActionContext context)
    {
        if (actions == null)
            return;

        foreach (TickAction action in actions)
        {
            if (context.Damage <= 0f)
                return;

            if (action is ReactiveTickAction reactiveAction)
                reactiveAction.Act(context);
        }
    }

    public UnitTickActions CreateRuntimeInstance()
    {
        UnitTickActions runtimeActions = Instantiate(this);

        if (runtimeActions.actions == null)
            return runtimeActions;

        for (int i = 0; i < runtimeActions.actions.Length; i++)
        {
            if (runtimeActions.actions[i] != null)
                runtimeActions.actions[i] = Instantiate(runtimeActions.actions[i]);
        }

        return runtimeActions;
    }
}
