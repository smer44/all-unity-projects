public abstract class TickActionContext
{
}

public class ActiveTickActionContext : TickActionContext
{
    public ActiveTickActionContext(AutoBattlingUnit owner, AutoBattlingUnit target)
    {
        Owner = owner;
        Target = target;
    }

    public AutoBattlingUnit Owner { get; }
    public AutoBattlingUnit Target { get; }
}

public class ReactiveTickActionContext : TickActionContext
{
    public ReactiveTickActionContext(AutoBattlingUnit owner)
    {
        Owner = owner;
    }

    public AutoBattlingUnit Owner { get; }
}

public class OnAttackTickActionContext : TickActionContext
{
    public OnAttackTickActionContext(AutoBattlingUnit attacker, AutoBattlingUnit defender, float damage)
    {
        Attacker = attacker;
        Defender = defender;
        Damage = damage;
    }

    public AutoBattlingUnit Attacker { get; }
    public AutoBattlingUnit Defender { get; }
    public float Damage { get; set; }

    public void CancelDamage()
    {
        Damage = 0f;
    }
}
