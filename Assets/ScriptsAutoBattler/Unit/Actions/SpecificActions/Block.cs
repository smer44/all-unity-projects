using UnityEngine;

[CreateAssetMenu(fileName = "Block", menuName = "SO Auto Battler/Block")]
public class Block : ReactiveTickAction
{
    protected override float GetInterval(ReactiveTickActionContext context)
    {
        return context.Owner != null ? context.Owner.GetInterval(context.Owner.BlockAmountRestoreSpeed) : 0f;
    }

    protected override void OnTick(AutoBattlingUnit owner)
    {
        owner?.RestoreBlock();
    }

    public override void Act(OnAttackTickActionContext context)
    {
        if (context.Defender == null || context.Damage <= 0f)
            return;

        if (!context.Defender.SpendBlock())
            return;

        context.Damage -= context.Defender.BlockStrength;
        context.Defender.LogBattleAction(context.Defender.name, $"blocked {context.Defender.BlockStrength:0.##} damage.");
    }
}
