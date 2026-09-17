public class FirstPersonFlyingCameraState : LookAtFlyingCameraState
{
    protected override float DistanceToPivot => -0.5f;

    public FirstPersonFlyingCameraState(CameraController controller) : base(controller)
    {
    }
}
