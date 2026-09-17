using UnityEngine;

public class FallState : AbstractPlayerState
{
    public override bool AllowsAiming => true;

    public FallState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("Fall",0.20f);
        }

    }

    public override void Update()
    {
        Controller.UpdateAiming();
        Controller.UpdateLook();
    }

    public override void FixedUpdate()
    {
        Controller.VerticalSpeedInAir();

        if (Controller.IsInWater())
        {
            Controller.ToSwimKinematics();
            Controller.SetState(Controller.SwimIdleState);
            return;
        }

        Controller.UpdateMoveInputRotated();

        if (Controller.CheckIsGrounded())
        {   
            
            Controller.VerticalSpeedOnFloor();
            Controller.SetState(
                Controller.MoveInputRaw == Vector2.zero
                    ? Controller.IdleState
                    : Controller.RunOrWalkState
            );
            return;
        }


        if (Controller.IsJumpPressed() && Controller.TryConsumeAerialJump())
        {
            Controller.SetState(Controller.AerialJumpUpState);
            return;
        }

        if (Controller.IsPunchButtonPresssed() && !Controller.IsGunSelected())
        {
            Controller.SetState(Controller.AirHitState);
            return;
        }

        Controller.MoveWASDKinematic(Controller.MoveInputRotated, Controller.MoveSpeed);
    }

    public override void OnExit()
    {
    }
}
