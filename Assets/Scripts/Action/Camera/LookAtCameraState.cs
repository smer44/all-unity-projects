using UnityEngine;

public class LookAtCameraState : AbstractCameraState
{
    protected virtual float DistanceToPivot => 2f;
    private readonly float lookSensitivity = 0.1f;
    private readonly float minPitch = -80f;
    private readonly float maxPitch = 80f;
    private readonly float distanceSharpness = 12f;
    private readonly float rotationSharpness = 12f;

    private float yaw;
    private float pitch;
    private float orbitDistance;
    private float smoothTimeRemaining;

    public LookAtCameraState(CameraController controller) : base(controller, false)
    {
    }

    public override void OnEnter()
    {
        smoothTimeRemaining = Controller.LookAtSmoothDuration;
        InitializeCursor();
        CacheLookAngles();
        orbitDistance = CameraPosition != null && CameraPivot != null
            // Preserve the current signed distance when RMB reverses an unfinished blend.
            ? Vector3.Dot(CameraPivot.position - CameraPosition.position, CameraPosition.forward)
            : DistanceToPivot;
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

        Vector2 lookDelta = ReadLookDelta();
        yaw += lookDelta.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - lookDelta.y * lookSensitivity, minPitch, maxPitch);
        ApplyCameraOrbit(deltaTime);
    }

    private void InitializeCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void CacheLookAngles()
    {
        if (CameraPosition == null)
        {
            return;
        }

        Vector3 euler = CameraPosition.eulerAngles;
        yaw = euler.y;
        pitch = Mathf.Clamp(NormalizeAngle(euler.x), minPitch, maxPitch);
    }

    private Vector2 ReadLookDelta()
    {
        return Controller.ButtonControls != null ? Controller.ButtonControls.GetMouseMove2D() : Vector2.zero;
    }

    private void ApplyCameraOrbit(float deltaTime)
    {
        if (CameraPivot == null || CameraPosition == null)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.Euler(pitch, yaw, 0f);
        if (smoothTimeRemaining > 0f)
        {
            orbitDistance = Mathf.Lerp(orbitDistance, DistanceToPivot, 1f - Mathf.Exp(-distanceSharpness * deltaTime));
            lookRotation = CameraFacingCalc.ExpLerpRotate(CameraPosition.rotation, lookRotation, rotationSharpness, deltaTime);

            smoothTimeRemaining = Mathf.Max(0f, smoothTimeRemaining - deltaTime);
        }
        else
        {
            orbitDistance = DistanceToPivot;
        }

        // Follow the pivot directly even during entry/exit smoothing. Derive position
        // from the displayed rotation so strafing and mouse look cannot pull the
        // player off the camera's center line while the distance blends.
        CameraPosition.SetPositionAndRotation(
            CameraPivot.position - lookRotation * Vector3.forward * orbitDistance,
            lookRotation);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
