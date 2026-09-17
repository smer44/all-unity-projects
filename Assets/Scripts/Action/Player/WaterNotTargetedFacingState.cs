using UnityEngine;

public class WaterNotTargetedFacingState : AbstractPlayerVisualsRotationState
{
    public WaterNotTargetedFacingState(PlayerVisualsRotationController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
    }

    public override void FixedUpdate()
    {
        PlayerController playerController = Controller.PlayerController;
        if (playerController == null)
        {
            return;
        }

        if (!playerController.IsInWater())
        {
            Controller.SetState(Controller.IsTargeted
                ? Controller.SurfaceTargetedState
                : Controller.SurfaceNotTargetedState);
            return;
        }

        Vector3 moveInputRaw = playerController.GetMove3D();
        if (moveInputRaw == Vector3.zero)
        {
            return;
        }

        Controller.RotateToFacing3D(playerController.RotateInputByCamera3D(moveInputRaw));
    }

    public override void OnExit()
    {
    }
}
