public sealed class PunchUpperBodyState : AbstractUpperBodyState
{
    public override bool IsMelee => true;

    public PunchUpperBodyState(UpperBodyVisualsController controller) : base(controller) { }

    public override void OnEnter() => BeginAttack("Punch", Controller.PunchDuration);
    public override void FixedUpdate() => UpdateAttackTimer();
}
