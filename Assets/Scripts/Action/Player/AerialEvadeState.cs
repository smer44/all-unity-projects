using UnityEngine;

public class AerialEvadeState : AbstractPlayerState
{
    public const float MoveSpeed = 100f;
    private const float MaxExitSpeed = 20f;
    private const float StateDuration = 0.35f;

    protected virtual float ExitSpeedLimit => MaxExitSpeed;

    private float timeSinceEnter;
    private Vector3 movementDirection;

    public AerialEvadeState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.StopAiming();
        Controller.VisualsRotationController?.EnterFreezeRotationState(StateDuration);
        Controller.UpdateFlyingMoveInput();
        movementDirection = GetMovementDirection();
        // An evade establishes its captured velocity once; it is not acceleration.
        Controller.SetFlyingVelocity(movementDirection * MoveSpeed);
        timeSinceEnter = 0f;
        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.7f;
            Controller.animator.CrossFadeInFixedTime("AerialEvade", 0.07f);
        }
    }

    public override void Update()
    {
    }

    public override void FixedUpdate()
    {
        timeSinceEnter += Time.fixedDeltaTime;
        if (timeSinceEnter >= StateDuration)
        {
            // Refresh the converted input for normal facing on the exit tick.
            Controller.UpdateFlyingMoveInput();
            Controller.SetState(Controller.MoveInputRaw3D != Vector3.zero
                ? (AbstractPlayerState)Controller.FlyingMoveState
                : Controller.FlyingIdleState);
            return;
        }

        Controller.SetFlyingVelocity(movementDirection * MoveSpeed);
    }

    public override void OnExit()
    {
        Controller.VisualsRotationController?.ExitFreezeRotationState();
        Controller.SetFlyingVelocity(Vector3.ClampMagnitude(Controller.FlyingVelocity, ExitSpeedLimit));
    }

    protected virtual Vector3 GetMovementDirection()
    {
        return Controller.MoveInputRotated3D;
    }
}
