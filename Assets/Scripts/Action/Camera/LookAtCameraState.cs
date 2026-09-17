using System.Collections;
using UnityEngine;

public class LookAtCameraState : AbstractCameraState
{
    protected virtual float DistanceToPivot => 2f;
    private readonly float lookSensitivity = 0.1f;
    private readonly float minPitch = -80f;
    private readonly float maxPitch = 80f;
    private readonly float positionSharpness = 12f;
    private readonly float rotationSharpness = 12f;

    private float yaw;
    private float pitch;
    private Coroutine smoothRoutine;

    public LookAtCameraState(CameraController controller) : base(controller, false)
    {
    }

    public override void OnEnter()
    {
        Smooth = true;
        RestartSmoothRoutine();
        InitializeCursor();
        CacheLookAngles();
        ApplyCameraOrbit();
    }

    public override void OnExit()
    {
        StopSmoothRoutine();
    }

    public override void Update()
    {
        if (CameraPivot == null || CameraPosition == null)
        {
            return;
        }

        Vector2 lookDelta = ReadLookDelta();
        yaw += lookDelta.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - lookDelta.y * lookSensitivity, minPitch, maxPitch);
        ApplyCameraOrbit();
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

    private void ApplyCameraOrbit()
    {
        if (CameraPivot == null || CameraPosition == null)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPosition = CameraPivot.position - lookRotation * Vector3.forward * DistanceToPivot;
        ApplyPositionAndRotation(targetPosition, lookRotation);
    }

    private void ApplyPositionAndRotation(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (Smooth)
        {
            CameraPosition.position = CameraFacingCalc.ExpLerpMove(
                CameraPosition.position,
                targetPosition,
                positionSharpness,
                Time.deltaTime);

            CameraPosition.rotation = CameraFacingCalc.ExpLerpRotate(
                CameraPosition.rotation,
                targetRotation,
                rotationSharpness,
                Time.deltaTime);

            return;
        }

        CameraPosition.position = targetPosition;
        CameraPosition.rotation = targetRotation;
    }

    private void RestartSmoothRoutine()
    {
        StopSmoothRoutine();
        if (Controller.LookAtSmoothDuration <= 0f)
        {
            Smooth = false;
            return;
        }

        smoothRoutine = Controller.StartCoroutine(DisableSmoothAfterDelay());
    }

    private void StopSmoothRoutine()
    {
        if (smoothRoutine == null)
        {
            return;
        }

        Controller.StopCoroutine(smoothRoutine);
        smoothRoutine = null;
    }

    private IEnumerator DisableSmoothAfterDelay()
    {
        yield return new WaitForSeconds(Controller.LookAtSmoothDuration);
        Smooth = false;
        smoothRoutine = null;
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
