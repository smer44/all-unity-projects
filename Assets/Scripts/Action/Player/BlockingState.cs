using UnityEngine;

public class BlockingState : AbstractPlayerState
{
    public BlockingState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("StandingBlock", 0.15f);
        }
    }

    public override void Update()
    {
        Controller.UpdateLook();
    }

    public override void FixedUpdate()
    {
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

        if (!Controller.IsBlocking())
        {
            Controller.SetState(Controller.IdleState);
        }
    }

    public override void OnExit()
    {
    }
}
