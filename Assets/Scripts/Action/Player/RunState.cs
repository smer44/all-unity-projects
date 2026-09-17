using UnityEngine;

public class RunState : AbstractPlayerState
{
    public override bool AllowsAiming => true;
    public override bool AllowsUpperBodyAttacks => true;

    public const float MoveSpeed = 10f;
    protected virtual float MovementSpeed => MoveSpeed;

    public RunState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        Controller.ResetAerialJumpCounter();

        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("Run",0.15f, 0);
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

        Vector2 moveInput = Controller.GetMove2D();
        if (Controller.IsEvadeModifierPressed()
            && (moveInput.y != 0f || moveInput.x != 0f))
        {
            Controller.SetState(Controller.SlideState);
            return;
        }

        if (Controller.IsPunchButtonPresssed())
        {
            Controller.SwitchToAttackState();
        }

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
        Controller.MoveWASDKinematic(Controller.MoveInputRotated, MovementSpeed);


    }

    public override void OnExit()
    {
    }
}
