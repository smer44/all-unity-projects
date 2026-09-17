using UnityEngine;

public class SwimMoveState : AbstractPlayerState
{
    public const float MoveSpeed = 5f;

    public SwimMoveState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        Controller.ResetAerialJumpCounter();

        if (Controller.animator != null)
        {
            Controller.animator.speed = 1.0f;
            Controller.animator.CrossFadeInFixedTime("SwimMove", 0.15f);
        }
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

        Controller.UpdateMoveInputRotated3D();
        if (Controller.MoveInputRaw3D == Vector3.zero)
        {
            Controller.SetState(Controller.SwimIdleState);
            return;
        }

        Controller.MoveWASDKinematic(Controller.MoveInputRotated3D, MoveSpeed);
    }

    public override void OnExit()
    {
    }
}
