using UnityEngine;

public class AutoBattlingUnit : MonoBehaviour
{
    public const string AttackDamageStat = "attackDamage";
    public const string HitPointsStat = "hitPoints";
    public const string DefenceStat = "defence";
    public const string AttackSpeedStat = "attackSpeed";
    public const string BlocksAmountMaxStat = "blocksAmountMax";
    public const string BlockStrengthStat = "blockStrength";
    public const string BlockAmountRestoreSpeedStat = "blockAmountRestoreSpeed";
    public const string EvadeAmountMaxStat = "evadeAmountMax";
    public const string EvadeAmountRestoreSpeedStat = "evadeAmountRestoreSpeed";
    public const string BlocksAmountStat = "blocksAmount";
    public const string EvadeAmountStat = "evadeAmount";

    [SerializeField] private NamedValues stats;
    [SerializeField] private AutoBattlingUnit target;
    [SerializeField] private UnitTickActions activeActions;
    [SerializeField] private UnitTickActions onAttackActions;
    [SerializeField] private BattleActionLogger logger;
    [SerializeField] private float roundTime = 5f;
    [SerializeField] private bool alive = true;

    private NamedValue hitPoints;
    private NamedValue blocksAmount;
    private NamedValue evadeAmount;
    private UnitTickActions runtimeActiveActions;
    private UnitTickActions runtimeOnAttackActions;

    public NamedValues Stats => stats;
    public BattleActionLogger Logger => logger;
    public bool Alive => alive;
    public float AttackDamage => GetValue(AttackDamageStat);
    public float HitPoints => hitPoints != null ? hitPoints.Value : 0f;
    public float Defence => GetValue(DefenceStat);
    public float AttackSpeed => GetValue(AttackSpeedStat);
    public int BlocksAmountMax => Mathf.RoundToInt(GetValue(BlocksAmountMaxStat));
    public int BlocksAmount => blocksAmount != null ? Mathf.RoundToInt(blocksAmount.Value) : 0;
    public float BlockStrength => GetValue(BlockStrengthStat);
    public float BlockAmountRestoreSpeed => GetValue(BlockAmountRestoreSpeedStat);
    public int EvadeAmountMax => Mathf.RoundToInt(GetValue(EvadeAmountMaxStat));
    public int EvadeAmount => evadeAmount != null ? Mathf.RoundToInt(evadeAmount.Value) : 0;
    public float EvadeAmountRestoreSpeed => GetValue(EvadeAmountRestoreSpeedStat);
    public AutoBattlingUnit Target
    {
        get => target;
        set => target = value;
    }

    private void Awake()
    {
        alive = true;
        stats = stats != null ? stats.CreateRuntimeInstance() : null;
        runtimeActiveActions = activeActions != null ? activeActions.CreateRuntimeInstance() : null;
        runtimeOnAttackActions = onAttackActions != null ? onAttackActions.CreateRuntimeInstance() : null;
        InitializeRuntimeStats();
    }

    private void OnValidate()
    {
        if (stats == null)
            return;

        if (hitPoints != null)
            hitPoints.Value = Mathf.Max(0f, hitPoints.Value);

        if (blocksAmount != null)
            blocksAmount.Value = Mathf.Clamp(BlocksAmount, 0, BlocksAmountMax);

        if (evadeAmount != null)
            evadeAmount.Value = Mathf.Clamp(EvadeAmount, 0, EvadeAmountMax);
    }

    private void InitializeRuntimeStats()
    {
        EnsureRuntimeValues();

        if (stats == null)
        {
            hitPoints.Value = 0f;
            blocksAmount.Value = 0f;
            evadeAmount.Value = 0f;
            return;
        }

        hitPoints.Value = GetValue(HitPointsStat);
        blocksAmount.Value = BlocksAmountMax;
        evadeAmount.Value = EvadeAmountMax;
    }

    private void Update()
    {
        if (!alive)
            return;

        if (stats == null)
        {
            Debug.LogWarning($"{name} has no NamedValues assigned.", this);
            return;
        }

        runtimeOnAttackActions?.TickReactive(Time.deltaTime, this);

        if (target == null)
        {
            return;
        }

        AttackTimeTick(Time.deltaTime);
    }

    private void AttackTimeTick(float delta)
    {
        if (stats == null)
            return;

        runtimeActiveActions?.TickActive(delta, new ActiveTickActionContext(this, target));
    }

    public void RestoreBlock()
    {
        if (stats == null)
            return;

        int oldBlocksAmount = BlocksAmount;
        blocksAmount.Value = Mathf.Min(BlocksAmount + 1, BlocksAmountMax);

        if (BlocksAmount > oldBlocksAmount)
            LogBattleAction(name, $"restored 1 block ({BlocksAmount}/{BlocksAmountMax}).");
    }

    public void RestoreEvade()
    {
        if (stats == null)
            return;

        int oldEvadeAmount = EvadeAmount;
        evadeAmount.Value = Mathf.Min(EvadeAmount + 1, EvadeAmountMax);

        if (EvadeAmount > oldEvadeAmount)
            LogBattleAction(name, $"restored 1 evade ({EvadeAmount}/{EvadeAmountMax}).");
    }

    public void TakeDamage(AutoBattlingUnit attacker, float incomingAttackDamage)
    {
        if (stats == null)
            return;

        string attackerName = GetUnitName(attacker);
        float damage = incomingAttackDamage - Defence;

        if (damage <= 0f)
        {
            LogBattleAction(name, $"ignored {attackerName}'s attack because defence blocked the damage.");
            return;
        }

        OnAttackTickActionContext actionContext = new OnAttackTickActionContext(attacker, this, damage);
        runtimeOnAttackActions?.ActReactive(actionContext);

        if (actionContext.Damage <= 0f)
        {
            LogBattleAction(name, $"avoided all damage from {attackerName}.");
            return;
        }

        hitPoints.Value = Mathf.Max(0f, HitPoints - actionContext.Damage);
        LogBattleAction(name, $"took {actionContext.Damage:0.##} damage from {attackerName} ({HitPoints:0.##} HP left).");

        if (HitPoints <= 0f)
        {
            alive = false;
            LogBattleAction(name, "died.");
        }
    }

    public bool SpendBlock()
    {
        if (BlocksAmount <= 0)
            return false;

        blocksAmount.Value = BlocksAmount - 1;
        LogBattleAction(name, $"spent 1 block ({BlocksAmount}/{BlocksAmountMax}).");
        return true;
    }

    public bool SpendEvade()
    {
        if (EvadeAmount <= 0)
            return false;

        evadeAmount.Value = EvadeAmount - 1;
        LogBattleAction(name, $"spent 1 evade ({EvadeAmount}/{EvadeAmountMax}).");
        return true;
    }

    public float GetInterval(float speed)
    {
        if (speed <= 0f)
            return 0f;

        return roundTime / speed;
    }

    private float GetValue(string valueName)
    {
        return stats != null ? stats.GetValue(valueName) : 0f;
    }

    public void LogBattleAction(string author, string entry)
    {
        logger?.Log(author, entry);
    }

    private static string GetUnitName(AutoBattlingUnit unit)
    {
        return unit != null ? unit.name : "unknown unit";
    }

    private void EnsureRuntimeValues()
    {
        if (hitPoints == null)
            hitPoints = GetRuntimeOrCreateValue(HitPointsStat, 0f);

        if (blocksAmount == null)
            blocksAmount = GetRuntimeOrCreateValue(BlocksAmountStat, 0f);

        if (evadeAmount == null)
            evadeAmount = GetRuntimeOrCreateValue(EvadeAmountStat, 0f);
    }

    private NamedValue GetRuntimeOrCreateValue(string valueName, float fallbackValue)
    {
        NamedValue statValue = stats != null ? stats.GetNamedValue(valueName) : null;
        return statValue != null ? statValue : CreateRuntimeValue(valueName, fallbackValue);
    }

    private static NamedValue CreateRuntimeValue(string valueName, float value)
    {
        NamedValue namedValue = ScriptableObject.CreateInstance<NamedValue>();
        namedValue.Set(valueName, value);
        return namedValue;
    }
}
