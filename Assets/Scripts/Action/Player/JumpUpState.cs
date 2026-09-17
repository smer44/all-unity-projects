using UnityEngine;

public class JumpUpState : AbstractPlayerState
{
    public override bool AllowsAiming => true;

    private const float LandingCheckDelay = 0.1f;
    private const float ToFallDelay = 0.4f;
    private float timeSinceEnter;

    public JumpUpState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.Play("JumpUp");
        }
        Controller.DoJumpImpulse();
        timeSinceEnter = 0f;

    }

    public override void Update()
    {
        Controller.UpdateAiming();
        Controller.UpdateLook();
    }

    public override void FixedUpdate()
    {
        Controller.VerticalSpeedInAir();

        //if (Controller.IsInWater())
        //{
        //    Controller.ToSwimKinematics();
        //    Controller.SetState(Controller.SwimIdleState);
        //    return;
        //}

        Controller.UpdateMoveInputRotated();
        Controller.MoveWASDKinematic(Controller.MoveInputRotated, Controller.MoveSpeed);


        if (Controller.IsPunchButtonPresssed() && !Controller.IsGunSelected())
        {
            Controller.SetState(Controller.AirHitState);
            return;
        }

        timeSinceEnter += Time.fixedDeltaTime;
        if (timeSinceEnter >= LandingCheckDelay
            && Controller.localVerticalSpeedAccumulator <= 0f
            && Controller.CheckIsGrounded())
        {
            Controller.VerticalSpeedOnFloor();
            Controller.SetState(
                Controller.MoveInputRaw == Vector2.zero
                    ? Controller.IdleState
                    : Controller.RunOrWalkState
            );
            return;
        }

        if (timeSinceEnter >= ToFallDelay)
        {
            Controller.ClearGroundParent();
            Controller.SetState(Controller.FallState);
            return;
        }
    }

    public override void OnExit()
    {
    }
}
