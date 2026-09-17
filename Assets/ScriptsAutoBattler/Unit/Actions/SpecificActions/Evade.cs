using UnityEngine;

[CreateAssetMenu(fileName = "Evade", menuName = "SO Auto Battler/Evade")]
public class Evade : ReactiveTickAction
{
    protected override float GetInterval(ReactiveTickActionContext context)
    {
        return context.Owner != null ? context.Owner.GetInterval(context.Owner.EvadeAmountRestoreSpeed) : 0f;
    }

    protected override void OnTick(AutoBattlingUnit owner)
    {
        owner?.RestoreEvade();
    }

    public override void Act(OnAttackTickActionContext context)
    {
        if (context.Defender == null || context.Damage <= 0f)
            return;

        if (!context.Defender.SpendEvade())
            return;

        context.CancelDamage();
        context.Defender.LogBattleAction(context.Defender.name, "evaded the attack.");
    }
}
