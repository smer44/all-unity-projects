using UnityEngine;

public class FlyingTargetedFacingForwardState : AbstractPlayerVisualsRotationState
{
    public FlyingTargetedFacingForwardState(PlayerVisualsRotationController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
    }

    public override void FixedUpdate()
    {
        PlayerController player = Controller.PlayerController;
        if (player == null || player.visualsPivot == null || !player.IsFlying)
        {
            return;
        }

        if (!Controller.IsTargeted)
        {
            Controller.SetState(Controller.GetDefaultState());
            return;
        }

        Vector3 movementDirection = player.IsFlyingMoving ? player.FlyingMoveFacingDirection : player.FlyingVelocity;
        Transform direction = player.Direction;
        Vector3 cameraUp = direction != null ? direction.up : Vector3.up;
        Controller.RotateToFacing3DTargetedForward(movementDirection, Controller.target.transform.position, cameraUp);
    }

    public override void OnExit()
    {
    }
}
