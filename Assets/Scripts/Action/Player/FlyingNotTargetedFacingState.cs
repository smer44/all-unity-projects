using UnityEngine;

public class FlyingNotTargetedFacingState : AbstractPlayerVisualsRotationState
{
    public FlyingNotTargetedFacingState(PlayerVisualsRotationController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
    }

    public override void FixedUpdate()
    {
        PlayerController player = Controller.PlayerController;
        if (player == null || !player.IsFlying)
        {
            return;
        }

        if (Controller.IsTargeted)
        {
            Controller.SetState(Controller.FlyingTargetedState);
            return;
        }

        Vector3 cameraUp = player.Direction != null ? player.Direction.up : Vector3.up;
        // Moving flight faces the input direction even when momentum points elsewhere.
        // Idle continues to follow residual velocity while coasting.
        Vector3 facing = player.IsFlyingMoving ? player.MoveInputRotated3D : player.FlyingVelocity;
        Controller.RotateToFacing3D(facing, cameraUp);
    }

    public override void OnExit()
    {
    }
}
