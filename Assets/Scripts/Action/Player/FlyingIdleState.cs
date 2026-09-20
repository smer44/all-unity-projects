using UnityEngine;

public class FlyingIdleState : AbstractPlayerState
{
    public override bool AllowsAiming => true;

    private const float StopSpeedThreshold = 0.01f;
    public const float BrakingAcceleration = 80f;

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
        // Stop before braking could reverse direction, including the tiny-speed tail.
        if (velocity.magnitude - BrakingAcceleration * Time.fixedDeltaTime <= StopSpeedThreshold)
        {
            Controller.StopAllMotion();
            return;
        }

        Controller.MoveWASDKinematic(-velocity.normalized, BrakingAcceleration);
    }

    public override void OnExit()
    {
    }
}
