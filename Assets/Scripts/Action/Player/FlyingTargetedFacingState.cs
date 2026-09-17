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

        if (Controller.TryGetTargetFacing3D(out Vector3 facing))
        {
            Vector3 cameraUp = player.Direction != null ? player.Direction.up : Vector3.up;
            Controller.RotateToFacing3D(facing, cameraUp);
        }
    }

    public override void OnExit()
    {
    }
}
