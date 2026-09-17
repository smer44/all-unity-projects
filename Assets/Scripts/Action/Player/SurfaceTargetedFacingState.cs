public class SurfaceTargetedFacingState : AbstractPlayerVisualsRotationState
{
    public SurfaceTargetedFacingState(PlayerVisualsRotationController controller) : base(controller)
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

        if (!Controller.IsTargeted)
        {
            Controller.SetState(Controller.SurfaceNotTargetedState);
            return;
        }

        if (Controller.TryGetPlanarTargetFacing(out var facing))
        {
            Controller.RotateToFacing(facing);
        }
    }

    public override void OnExit()
    {
    }
}
