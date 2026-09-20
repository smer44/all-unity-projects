using UnityEngine;

public class FlyingMoveState : AbstractPlayerState
{
    public override bool AllowsAiming => true;

    public const float MaxFlyingSpeed = 50f;

    public const string AnimationName = "FlyingMovev2";

    public FlyingMoveState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        Controller.SetFlyingVelocity(Vector3.ClampMagnitude(Controller.FlyingVelocity, MaxFlyingSpeed));
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

        float acceleration = Controller.FlyingMoveAcceleration *
            Mathf.Clamp01(1f - Controller.FlyingVelocity.magnitude / MaxFlyingSpeed);
        // Inertia mode interprets input * acceleration as world acceleration.
        Controller.MoveWASDKinematic(Controller.MoveInputRotated3D, acceleration);
    }

    public override void OnExit()
    {
    }
}
