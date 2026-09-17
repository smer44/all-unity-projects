using UnityEngine;

public class RestrictedLookAtCameraState : AbstractCameraState
{
    private readonly float distanceToPivot = 1.5f;
    private readonly float lookSensitivity = 0.1f;
    private readonly float minPitch = -80f;
    private readonly float maxPitch = 80f;
    private readonly float firstAngle = 30f;
    private readonly float secondAngle = 60f;
    private readonly float returnToUnrestrictedAngleSpeed = 90f;
    private readonly float positionSharpness = 12f;
    private readonly float rotationSharpness = 12f;

    private float yaw;
    private float pitch;

    public RestrictedLookAtCameraState(CameraController controller) : base(controller, true)
    {
    }

    public override void OnEnter()
    {
        InitializeCursor();
        CacheLookAngles();
        ApplyCameraOrbit();
    }

    public override void Update()
    {
        if (CameraPivot == null || CameraPosition == null)
        {
            return;
        }

        float inputScale = GetInputScale();
        Vector2 lookDelta = ReadLookDelta() * inputScale;
        yaw += lookDelta.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - lookDelta.y * lookSensitivity, minPitch, maxPitch);

        ClampToBlockedAngle();
        ReturnTowardsUnrestrictedAngle();
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

    private float GetInputScale()
    {
        float currentAngle = GetAngleFromPivotForward(GetLookRotation() * Vector3.forward);
        float minAngle = Mathf.Min(firstAngle, secondAngle);
        float maxAngle = Mathf.Max(firstAngle, secondAngle);

        if (currentAngle <= minAngle)
        {
            return 1f;
        }

        if (currentAngle >= maxAngle)
        {
            return 0f;
        }

        float t = Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }

    private void ClampToBlockedAngle()
    {
        ApplyAngleLimit(Mathf.Max(firstAngle, secondAngle));
    }

    private void ReturnTowardsUnrestrictedAngle()
    {
        Quaternion lookRotation = GetLookRotation();
        Vector3 currentForward = lookRotation * Vector3.forward;
        float currentAngle = GetAngleFromPivotForward(currentForward);
        float targetFreeAngle = Mathf.Min(firstAngle, secondAngle);

        if (currentAngle <= targetFreeAngle)
        {
            return;
        }

        float targetAngle = Mathf.Max(
            targetFreeAngle,
            currentAngle - returnToUnrestrictedAngleSpeed * Time.deltaTime);

        SetLookDirectionAtAngle(currentForward, targetAngle);
    }

    private void ApplyAngleLimit(float maxAngle)
    {
        Quaternion lookRotation = GetLookRotation();
        Vector3 currentForward = lookRotation * Vector3.forward;
        float currentAngle = GetAngleFromPivotForward(currentForward);

        if (currentAngle <= maxAngle)
        {
            return;
        }

        SetLookDirectionAtAngle(currentForward, maxAngle);
    }

    private void SetLookDirectionAtAngle(Vector3 currentForward, float angle)
    {
        Vector3 referenceForward = GetPivotForward();
        Vector3 limitedForward = Vector3.RotateTowards(
            referenceForward,
            currentForward.normalized,
            angle * Mathf.Deg2Rad,
            0f);

        Quaternion limitedRotation = Quaternion.LookRotation(limitedForward, GetPivotUp());
        Vector3 limitedEuler = limitedRotation.eulerAngles;
        yaw = limitedEuler.y;
        pitch = Mathf.Clamp(NormalizeAngle(limitedEuler.x), minPitch, maxPitch);
    }

    private float GetAngleFromPivotForward(Vector3 lookForward)
    {
        return Vector3.Angle(GetPivotForward(), lookForward);
    }

    private Vector3 GetPivotForward()
    {
        return CameraPivot != null ? CameraPivot.forward : Vector3.forward;
    }

    private Vector3 GetPivotUp()
    {
        return CameraPivot != null ? CameraPivot.up : Vector3.up;
    }

    private Quaternion GetLookRotation()
    {
        return Quaternion.Euler(pitch, yaw, 0f);
    }

    private void ApplyCameraOrbit()
    {
        Quaternion lookRotation = GetLookRotation();
        Vector3 targetPosition = CameraPivot.position - lookRotation * Vector3.forward * distanceToPivot;
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

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
