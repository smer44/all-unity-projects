public sealed class StabUpperBodyState : AbstractUpperBodyState
{
    private const float ComboIntervalStart = 0.75f;
    private const float ComboIntervalEnd = 1.25f;
    private bool followUp;

    public override bool IsMelee => true;

    public StabUpperBodyState(UpperBodyVisualsController controller) : base(controller) { }

    public override void OnEnter()
    {
        followUp = false;
        BeginAttack("ForwardSwordAttack", Controller.StabDuration);
    }

    public override void FixedUpdate() => UpdateAttackTimer();

    public override void OnAttackRequested()
    {
        if (!followUp && TimeSinceEnter >= ComboIntervalStart && TimeSinceEnter <= ComboIntervalEnd)
        {
            followUp = true;
            BeginAttack("Stab2", Controller.StabFollowUpDuration);
        }
    }
}
