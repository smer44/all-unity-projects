using UnityEngine;

public class AerialEvadeDownwardsState : AerialEvadeState
{
    private const float MaxExitSpeed = 5f;

    protected override float ExitSpeedLimit => MaxExitSpeed;

    public AerialEvadeDownwardsState(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        CameraController camera = Controller.PlayerCameraController;
        if (camera != null)
        {
            camera.SetState(camera.LookAtFlyingNormalizingCameraState);
        }
    }

    protected override Vector3 GetMovementDirection()
    {
        return Vector3.down;
    }
}
