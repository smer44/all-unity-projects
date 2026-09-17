using UnityEngine;

public class FreeCameraController : AbstractCameraState
{
    private readonly float lookSensitivity = 0.1f;
    private readonly float minPitch = -80f;
    private readonly float maxPitch = 80f;
    private readonly float moveSpeed = 10f;
    private readonly float positionSharpness = 12f;
    private readonly float rotationSharpness = 12f;

    private float yaw;
    private float pitch;

    public FreeCameraController(CameraController controller) : base(controller, false)
    {
    }

    public override Transform CameraPivot => CameraPosition;
    public override Transform CameraPosition => Controller.GetDirection();

    public override void OnEnter()
    {
        InitializeCursor();
        CacheLookAngles();
    }

    public override void Update()
    {
        UpdateRotation();
        UpdatePosition();
    }

    private void UpdateRotation()
    {
        Transform rotationTarget = CameraPosition;
        if (rotationTarget == null)
        {
            return;
        }

        Vector2 lookDelta = GetMouseDelta();
        if (lookDelta.sqrMagnitude > Mathf.Epsilon)
        {
            yaw += lookDelta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - lookDelta.y * lookSensitivity, minPitch, maxPitch);
        }

        ApplyRotation(rotationTarget, Quaternion.Euler(pitch, yaw, 0f));
    }

    private void UpdatePosition()
    {
        Transform movementTarget = CameraPosition;
        if (movementTarget == null)
        {
            return;
        }

        Vector3 moveInputRaw = GetMove3D();
        Vector3 moveInputRotated = CameraFacingCalc.RotateInput(moveInputRaw, movementTarget);
        MoveCamera(movementTarget, moveInputRotated);
    }

    private void InitializeCursor()
    {
        Debug.Log("FreeCameraController.InitializeCursor");
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void CacheLookAngles()
    {
        Transform rotationTarget = CameraPosition;
        if (rotationTarget == null)
        {
            return;
        }

        Vector3 euler = rotationTarget.eulerAngles;
        yaw = euler.y;
        pitch = Mathf.Clamp(NormalizeAngle(euler.x), minPitch, maxPitch);
    }

    private Vector2 GetMouseDelta()
    {
        return Controller.ButtonControls != null ? Controller.ButtonControls.GetMouseMove2D() : Vector2.zero;
    }

    private Vector3 GetMove3D()
    {
        return Controller.ButtonControls != null ? Controller.ButtonControls.GetMove3D() : Vector3.zero;
    }

    private void MoveCamera(Transform movementTarget, Vector3 inputRotated)
    {
        if (inputRotated.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 targetPosition = movementTarget.position + inputRotated * (moveSpeed * Time.deltaTime);
        ApplyPosition(movementTarget, targetPosition);
    }

    private void ApplyPosition(Transform movementTarget, Vector3 targetPosition)
    {
        if (Smooth)
        {
            movementTarget.position = CameraFacingCalc.ExpLerpMove(
                movementTarget.position,
                targetPosition,
                positionSharpness,
                Time.deltaTime);
            return;
        }

        movementTarget.position = targetPosition;
    }

    private void ApplyRotation(Transform rotationTarget, Quaternion targetRotation)
    {
        if (Smooth)
        {
            rotationTarget.rotation = CameraFacingCalc.ExpLerpRotate(
                rotationTarget.rotation,
                targetRotation,
                rotationSharpness,
                Time.deltaTime);
            return;
        }

        rotationTarget.rotation = targetRotation;
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
