using UnityEngine;

public class FlyingNonTargetedFacingForwardState : AbstractPlayerVisualsRotationState
{
    public FlyingNonTargetedFacingForwardState(PlayerVisualsRotationController controller) : base(controller)
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

        if (Controller.IsTargeted)
        {
            Controller.SetState(Controller.GetDefaultState());
            return;
        }

        Vector3 movementDirection = player.IsFlyingMoving ? player.FlyingMoveFacingDirection : player.FlyingVelocity;
        Transform cameraTransform = player.PlayerCameraController.GetDirection();
        Controller.RotateToFacing3Dv2(movementDirection, cameraTransform.forward, cameraTransform.up, faceForward: true);
    }

    public override void OnExit()
    {
    }
}
