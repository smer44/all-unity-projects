using UnityEngine;

[CreateAssetMenu(fileName = "BasicAttack", menuName = "SO Auto Battler/Basic Attack")]
public class BasicAttack : ActiveTickAction
{
    protected override float GetInterval(ActiveTickActionContext context)
    {
        return context.Owner != null ? context.Owner.GetInterval(context.Owner.AttackSpeed) : 0f;
    }

    public override void Act(ActiveTickActionContext context)
    {
        if (context.Owner == null || context.Target == null || !context.Target.Alive)
            return;

        context.Owner.LogBattleAction(context.Owner.name, $"attacks {context.Target.name}.");
        context.Target.TakeDamage(context.Owner, context.Owner.AttackDamage);
    }
}
