using UnityEngine;

public class FlyingMoveState : AbstractPlayerState
{
    public override bool AllowsAiming => true;

    public const string AnimationName = "FlyingMovev2";

    public FlyingMoveState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        Controller.ResetAerialJumpCounter();
        Controller.PlayFlyingLocomotionAnimation(AnimationName);
    }

    public override void Update()
    {
        Controller.UpdateAiming();
    }

    public override void FixedUpdate()
    {
        Controller.UpdateFlyingMoveInput();
        if (Controller.IsEvadeModifierPressed())
        {
            Controller.SetState(Controller.MoveInputRaw3D != Vector3.zero
                ? (AbstractPlayerState)Controller.AerialEvadeState
                : Controller.AerialEvadeDownwardsState);
            return;
        }

        if (Controller.MoveInputRaw3D == Vector3.zero)
        {
            Controller.SetState(Controller.FlyingIdleState);
            // Begin coasting in this physics step, without a frame of zero motion.
            Controller.FlyingIdleState.FixedUpdate();
            return;
        }

        Controller.UpdateFlyingAttackAnimation("FlyingMove");

        Controller.SetFlyingVelocity(CalculateFlyingVelocity());
    }

    private Vector3 CalculateFlyingVelocity()
    {
        Vector3 velocity = Controller.FlyingVelocity;
        Vector3 thrust = Controller.MoveInputRotated3D * Controller.FlyingMoveAcceleration;
        Vector3 friction = velocity * Controller.FlyingMoveFrictionModifier;
        // At full input, thrust balances friction at acceleration / friction modifier.
        return velocity + (thrust - friction) * Time.fixedDeltaTime;
    }

    public override void OnExit()
    {
    }
}
