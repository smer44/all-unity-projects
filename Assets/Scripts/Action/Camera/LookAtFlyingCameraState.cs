using UnityEngine;

public class LookAtFlyingCameraState : AbstractCameraState
{
    protected virtual float DistanceToPivot => 2f;
    private const float LookSensitivity = 0.1f;
    private const float Sharpness = 12f;

    private Quaternion orbitRotation;
    private float orbitDistance;
    private float smoothTimeRemaining;

    public LookAtFlyingCameraState(CameraController controller) : base(controller, false)
    {
    }

    public override void OnEnter()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        orbitRotation = CameraPosition != null ? CameraPosition.rotation : Quaternion.identity;
        orbitDistance = CameraPosition != null && CameraPivot != null
            // Keep the sign when switching directly from a first-person camera.
            ? Vector3.Dot(CameraPivot.position - CameraPosition.position, orbitRotation * Vector3.forward)
            : DistanceToPivot;
        smoothTimeRemaining = Controller.LookAtSmoothDuration;
        ApplyCameraOrbit(0f);
    }

    public override void Update()
    {
        Update(Time.deltaTime);
    }

    public void Update(float deltaTime)
    {
        if (CameraPivot == null || CameraPosition == null)
        {
            return;
        }

        Vector2 lookDelta = Controller.ButtonControls != null
            ? Controller.ButtonControls.GetMouseMove2D()
            : Vector2.zero;
        orbitRotation = CameraFacingCalc.RotateOrbitLocal(orbitRotation, lookDelta, LookSensitivity);
        ApplyCameraOrbit(deltaTime);
    }

    private void ApplyCameraOrbit(float deltaTime)
    {
        if (CameraPivot == null || CameraPosition == null)
        {
            return;
        }

        if (smoothTimeRemaining > 0f)
        {
            orbitDistance = Mathf.Lerp(
                orbitDistance, DistanceToPivot, 1f - Mathf.Exp(-Sharpness * deltaTime));
            smoothTimeRemaining = Mathf.Max(0f, smoothTimeRemaining - deltaTime);
        }
        else
        {
            orbitDistance = DistanceToPivot;
        }

        // Smooth only the entry distance. Apply rotation immediately so mouse
        // input always turns around the visible camera axes, even during entry.
        CameraPosition.SetPositionAndRotation(
            CameraPivot.position - orbitRotation * Vector3.forward * orbitDistance,
            orbitRotation);
    }
}
