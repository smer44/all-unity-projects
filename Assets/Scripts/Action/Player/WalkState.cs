using UnityEngine;

public class WalkState : AbstractPlayerState
{
    public override bool AllowsAiming => true;
    public override bool AllowsUpperBodyAttacks => true;

    public const float MoveSpeed = 5f;

    public WalkState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        Controller.ResetAerialJumpCounter();

        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("Walk", 0.15f, 0);
        }
    }

    public override void Update()
    {
        Controller.UpdateAiming();
        Controller.UpdateLook();
    }

    public override void FixedUpdate()
    {
        if (Controller.IsInWater())
        {
            Controller.ToSwimKinematics();
            Controller.SetState(Controller.SwimIdleState);
            return;
        }

        if (Controller.CheckIsGrounded())
        {
            Controller.VerticalSpeedOnFloor();
        }
        else
        {
            Controller.KeepFloorSpeed();
            Controller.VerticalSpeedInAir();
            Controller.ClearGroundParent();
            Controller.SetState(Controller.FallState);
            return;
        }

        if (Controller.IsBlocking())
        {
            Controller.SetState(Controller.BlockingState);
            return;
        }

        if (Controller.IsPunchButtonPresssed())
        {
            Controller.SwitchToAttackState();
        }

        /*/
        if (Controller.IsForwardEvade)
        {
            Controller.SetState(Controller.ForwardEvadeState);
            return;
        }

        if (Controller.IsBackFlip)
        {
            Controller.SetState(Controller.BackFlipState);
            return;
        }

        if (Controller.IsSideEvade)
        {
            Controller.SetState(Controller.SideEvadeState);
            return;
        }
        /*/
        if (Controller.IsJumpPressed())
        {
            Controller.KeepFloorSpeed();
            Controller.VerticalSpeedInAir();
            Controller.ClearGroundParent();
            Controller.SetState(Controller.JumpUpState);
            return;
        }

        if (Controller.IsWASDStaying())
        {
            Controller.SetState(Controller.IdleState);
            return;
        }

        if (Controller.GroundMovementState != this)
        {
            Controller.SetState(Controller.GroundMovementState);
            return;
        }

        Controller.UpdateMoveInputRotated();
        Controller.MoveWASDKinematic(Controller.MoveInputRotated, MoveSpeed);


    }

    public override void OnExit()
    {
    }
}
