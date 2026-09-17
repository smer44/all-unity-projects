public sealed class GroundMeleeMoveState : RunState
{
    public new const float MoveSpeed = 5f;
    protected override float MovementSpeed => MoveSpeed;

    public GroundMeleeMoveState(PlayerController controller) : base(controller)
    {
    }
}
