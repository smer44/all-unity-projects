using UnityEngine;

public class FlyingDashState : AbstractPlayerState
{
    public override bool AllowsAiming => true;
    public bool IsDirectionLocked => elapsedTime < lockDuration;
    public Vector3 MovementDirection { get; private set; }

    private Vector3 entryInput = Vector3.back;
    private float elapsedTime;
    private float lockDuration;

    public FlyingDashState(PlayerController controller) : base(controller)
    {
    }

    public void Begin(Vector3 input)
    {
        entryInput = input != Vector3.zero ? input.normalized : Vector3.back;
        Controller.SetState(this);
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        MovementDirection = CameraFacingCalc.RotateFlyingInput(entryInput, Controller.Direction).normalized;
        elapsedTime = 0f;
        lockDuration = Mathf.Max(0f, Controller.FlyingDashLockDuration);
        // Carry speed into the dash, aligned with its captured world direction.
        Controller.SetFlyingVelocity(MovementDirection * Controller.FlyingVelocity.magnitude);
        Controller.ResetAerialJumpCounter();
        Controller.PlayFlyingLocomotionAnimation("FlyingMove");
    }

    public override void Update()
    {
        Controller.UpdateAiming();
    }

    public override void FixedUpdate()
    {
        if (!IsDirectionLocked)
        {
            Controller.UpdateFlyingMoveInput();
            if (Controller.IsEvadeModifierPressed())
            {
                Controller.SetState(Controller.MoveInputRaw3D != Vector3.zero
                    ? (AbstractPlayerState)Controller.AerialEvadeState
                    : Controller.AerialEvadeDownwardsState);
                return;
            }

            if (Controller.ButtonControls == null || !Controller.ButtonControls.IsFlyingDashPressed())
            {
                AbstractPlayerState nextState = Controller.MoveInputRaw3D != Vector3.zero
                    ? (AbstractPlayerState)Controller.FlyingMoveState
                    : Controller.FlyingIdleState;
                Controller.SetState(nextState);
                nextState.FixedUpdate();
                return;
            }

            // With no movement input, keep the last direction (including an idle back-dash).
            if (Controller.MoveInputRaw3D != Vector3.zero)
                MovementDirection = Controller.MoveInputRotated3D.normalized;
        }

        Controller.UpdateFlyingAttackAnimation("FlyingMove");
        Controller.SetFlyingVelocity(CalculateFlyingVelocity());
        elapsedTime += Time.fixedDeltaTime;
    }

    private Vector3 CalculateFlyingVelocity()
    {
        Vector3 velocity = Controller.FlyingVelocity;
        Vector3 thrust = MovementDirection * Controller.FlyingDashAcceleration;
        Vector3 friction = velocity * Controller.FlyingDashFrictionModifier;
        return velocity + (thrust - friction) * Time.fixedDeltaTime;
    }

    public override void OnExit()
    {
    }
}
