
using UnityEngine;
public class IdleState : AbstractPlayerState
{
    public override bool AllowsAiming => true;
    public override bool AllowsUpperBodyAttacks => true;

    public IdleState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.UpdateAiming();
        Controller.ResetAerialJumpCounter();

        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("Idle",0.15f, 0);
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

        if (Controller.IsInteractButtonPressed())
        {
            if (Controller.UpdateInteractObject())
            {
                Controller.SetState(Controller.InteractState);
                return;
            }
            else
            {
                //Debug.Log($"IdleState : no interact object found");
            }
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
        
        if (Controller.IsWASDMoving())
        {
            Controller.SetState(Controller.GroundMovementState);
        }
    }

    public override void OnExit()
    {
    }
}
