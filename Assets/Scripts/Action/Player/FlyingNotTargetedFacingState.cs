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

        if (Controller.IsTargeted && !player.IsFlyingDashing)
        {
            Controller.SetState(Controller.FlyingTargetedState);
            return;
        }

        // Idle flight keeps its last visual facing even while coasting.
        if (player.IsFlyingIdle)
        {
            return;
        }

        Vector3 cameraUp = player.Direction != null ? player.Direction.up : Vector3.up;
        // Moving flight faces WASD input, ignoring Space's camera-relative ascent.
        Vector3 facing = player.IsFlyingDashing ? player.FlyingDashState.MovementDirection
            : player.IsFlyingMoving ? player.FlyingMoveFacingDirection : player.FlyingVelocity;
        Controller.RotateToFacing3D(facing, cameraUp);
    }

    public override void OnExit()
    {
    }
}
