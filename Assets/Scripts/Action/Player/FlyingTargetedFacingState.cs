using UnityEngine;

public class FlyingTargetedFacingState : AbstractPlayerVisualsRotationState
{
    public FlyingTargetedFacingState(PlayerVisualsRotationController controller) : base(controller)
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

        if (!Controller.IsTargeted)
        {
            Controller.SetState(Controller.FlyingNotTargetedState);
            return;
        }

        if (player.IsFlyingIdle)
        {
            return;
        }

        if (Controller.TryGetTargetFacing3D(out Vector3 facing))
        {
            //Vector3 cameraUp = player.Direction != null ? player.Direction.up : Vector3.up;
            //Controller.RotateToFacing3D(facing, cameraUp);
            Vector3 targetPosition = Vector3.zero;
            Transform cameraTransform = player.PlayerCameraController.GetDirection();

            Controller.RotateToFacing3DTargetedUpwardsChange(facing,targetPosition, cameraTransform.up );

        }
    }

    public override void OnExit()
    {
    }
}
