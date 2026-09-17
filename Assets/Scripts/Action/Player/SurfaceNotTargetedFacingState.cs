using UnityEngine;

public class SurfaceNotTargetedFacingState : AbstractPlayerVisualsRotationState
{
    public SurfaceNotTargetedFacingState(PlayerVisualsRotationController controller) : base(controller)
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

        if (playerController.IsInWater())
        {
            Controller.SetState(Controller.WaterNotTargetedState);
            return;
        }

        if (Controller.IsTargeted)
        {
            Controller.SetState(Controller.SurfaceTargetedState);
            return;
        }

        Vector2 moveInputRaw = playerController.GetMove2D();
        if (moveInputRaw == Vector2.zero)
        {
            return;
        }

        Controller.RotateToFacing(playerController.RotateInputByCamera(moveInputRaw));
    }

    public override void OnExit()
    {
    }
}
