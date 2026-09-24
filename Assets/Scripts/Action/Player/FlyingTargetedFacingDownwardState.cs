using UnityEngine;

public class FlyingTargetedFacingDownwardState : AbstractPlayerVisualsRotationState
{
    public FlyingTargetedFacingDownwardState(PlayerVisualsRotationController controller) : base(controller)
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

        // Keep the last forward heading while hovering; the target controls roll.
        Vector3 facing = player.IsFlyingIdle ? player.visualsPivot.forward
            : player.IsFlyingMoving ? player.FlyingMoveFacingDirection : player.FlyingVelocity;
        if (facing.sqrMagnitude < 0.0001f)
        {
            facing = player.visualsPivot.forward;
        }

        Vector3 targetPosition = Controller.target.transform.position;
        Transform direction = player.Direction;
        Vector3 cameraUp = direction != null ? direction.up : Vector3.up;
        Controller.RotateToFacing3DTargetedUpwardsChange(facing, targetPosition, cameraUp);
    }

    public override void OnExit()
    {
    }
}
