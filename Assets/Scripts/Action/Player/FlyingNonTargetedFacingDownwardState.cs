using UnityEngine;

public class FlyingNonTargetedFacingDownwardState : AbstractPlayerVisualsRotationState
{
    public FlyingNonTargetedFacingDownwardState(PlayerVisualsRotationController controller) : base(controller)
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
            Controller.SetState(Controller.GetDefaultState());
            return;
        }

        // Idle flight keeps its last visual facing even while coasting.
        if (player.IsFlyingIdle)
        {
            return;
        }

        // Moving flight uses the movement direction cached for this physics step.
        Vector3 facing = player.IsFlyingDashing ? player.FlyingDashState.MovementDirection
            : player.IsFlyingMoving ? player.FlyingMoveFacingDirection : player.FlyingVelocity;

        Transform cameraTransform = player.PlayerCameraController.GetDirection();
        Controller.RotateToFacing3Dv2(facing, cameraTransform.forward, cameraTransform.up);
    }

    public override void OnExit()
    {
    }
}
