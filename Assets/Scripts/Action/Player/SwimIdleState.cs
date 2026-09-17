using UnityEngine;

public class SwimIdleState : AbstractPlayerState
{
    public SwimIdleState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.ResetAerialJumpCounter();

        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("SwimIdle", 0.15f);
        }
        Controller.StopAllMotion();
    }

    public override void Update()
    {
        Controller.UpdateLook();
    }

    public override void FixedUpdate()
    {
        Controller.StopAllMotion();

        if (!Controller.IsInWater())
        {
            Controller.ToGroundKinematicks();
            Controller.SetState(Controller.IdleState);
            return;
        }

        if (Controller.IsJumpPressed() && Controller.CanJumpOutOfWater())
        {
            Controller.ToGroundKinematicks();
            Controller.SetState(Controller.JumpUpState);
            return;
        }

        if (Controller.IsSwimmingInputPressed())
        {
            Controller.SetState(Controller.SwimMoveState);
        }
    }

    public override void OnExit()
    {
    }
}
