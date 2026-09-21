using UnityEngine;

public class FlyingIdleState : AbstractPlayerState
{
    public override bool AllowsAiming => true;

    private const float StopSpeedThresholdSquare = 0.01f;

    public FlyingIdleState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        Controller.ResetAerialJumpCounter();
        Controller.PlayFlyingLocomotionAnimation("SwimIdle");
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

        if (Controller.MoveInputRaw3D != Vector3.zero)
        {
            Controller.SetState(Controller.FlyingMoveState);
            Controller.FlyingMoveState.FixedUpdate();
            return;
        }

        Controller.UpdateFlyingAttackAnimation("SwimIdle");

        Vector3 velocity = Controller.FlyingVelocity;
        Vector3 friction = velocity * Controller.FlyingIdleFrictionModifier;
        float minimumFrictionMagnitude = Controller.FlyingIdleMinimumFrictionMagnitude;
        // Keep braking firm at low speed, then integrate the friction once per step.
        if (friction.sqrMagnitude < minimumFrictionMagnitude * minimumFrictionMagnitude)
        {
            friction = velocity.normalized * minimumFrictionMagnitude;
        }
        friction *= Time.fixedDeltaTime;

        // Friction may stop motion, but must never accelerate it in reverse.
        velocity = friction.sqrMagnitude >= velocity.sqrMagnitude ? Vector3.zero : velocity - friction;
        if (velocity.sqrMagnitude <= StopSpeedThresholdSquare)
        {
            Controller.StopAllMotion();
            return;
        }

        Controller.SetFlyingVelocity(velocity);
    }

    public override void OnExit()
    {
    }
}
