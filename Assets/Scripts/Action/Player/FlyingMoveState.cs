using UnityEngine;

public class FlyingMoveState : AbstractPlayerState
{
    public override bool AllowsAiming => true;

    public const float BasicAcceleration = 80f;

    public const float MaxFlyingSpeed = 100f;

    public FlyingMoveState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        Controller.SetFlyingVelocity(Vector3.ClampMagnitude(Controller.FlyingVelocity, MaxFlyingSpeed));
        Controller.ResetAerialJumpCounter();
        Controller.PlayFlyingLocomotionAnimation("FlyingMove");
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
            Controller.SetState(Controller.MoveInputRaw != Vector2.zero
                ? (AbstractPlayerState)Controller.AerialEvadeState
                : Controller.AerialEvadeDownwardsState);
            return;
        }

        if (Controller.MoveInputRaw == Vector2.zero)
        {
            Controller.SetState(Controller.FlyingIdleState);
            // Begin coasting in this physics step, without a frame of zero motion.
            Controller.FlyingIdleState.FixedUpdate();
            return;
        }

        Controller.UpdateFlyingAttackAnimation("FlyingMove");

        float acceleration = BasicAcceleration *
            Mathf.Clamp01(1f - Controller.FlyingVelocity.magnitude / MaxFlyingSpeed);
        // Inertia mode interprets input * acceleration as world acceleration.
        Controller.MoveWASDKinematic(Controller.MoveInputRotated3D, acceleration);
    }

    public override void OnExit()
    {
    }
}
