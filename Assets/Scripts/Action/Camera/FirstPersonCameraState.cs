public class FirstPersonCameraState : LookAtCameraState
{
    protected override float DistanceToPivot => -0.5f;

    public FirstPersonCameraState(CameraController controller) : base(controller)
    {
    }
}
