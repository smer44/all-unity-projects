using UnityEngine;

public class LookAtFlyingNormalizingCameraState : AbstractCameraState
{
    private const float NormalizeDuration = 0.5f;

    private Quaternion startRotation;
    private Quaternion horizontalRotation;
    private float orbitDistance;
    private float elapsed;

    public LookAtFlyingNormalizingCameraState(CameraController controller) : base(controller, true)
    {
    }

    public override void OnEnter()
    {
        elapsed = 0f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        startRotation = CameraPosition != null ? CameraPosition.rotation : Quaternion.identity;
        horizontalRotation = GetHorizontalRotation();
        orbitDistance = CameraPosition != null && CameraPivot != null
            ? Vector3.Dot(CameraPivot.position - CameraPosition.position, startRotation * Vector3.forward)
            : 2f;
    }

    public override void Update()
    {
        Update(Time.deltaTime);
    }

    public void Update(float deltaTime)
    {
        if (Controller.CurrentState != this)
        {
            return;
        }

        elapsed += Mathf.Max(0f, deltaTime);
        float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / NormalizeDuration));
        // Quaternion interpolation follows the shortest rotation arc, including
        // across Euler angle wraparound and from an upside-down camera.
        Quaternion rotation = Quaternion.Slerp(startRotation, horizontalRotation, progress);
        if (CameraPosition != null && CameraPivot != null)
        {
            CameraPosition.SetPositionAndRotation(
                // Leaving first person for a downward evade also restores the third-person distance.
                CameraPivot.position - rotation * Vector3.forward * Mathf.Lerp(orbitDistance, 2f, progress),
                rotation);
        }

        if (elapsed >= NormalizeDuration)
        {
            Controller.SetState(Controller.LookAtFlyingCameraState);
        }
    }

    private Quaternion GetHorizontalRotation()
    {
        if (CameraPosition != null && CameraPivot != null)
        {
            // A first-person camera sits in front of the pivot but still looks forward.
            Vector3 horizontalForward = Vector3.ProjectOnPlane(startRotation * Vector3.forward, Vector3.up);
            if (horizontalForward.sqrMagnitude > 0.0001f)
            {
                return Quaternion.LookRotation(horizontalForward, Vector3.up);
            }
        }

        // Directly above/below the target, yaw is unconstrained. Project onto
        // yaw-only quaternions to choose the closest upright orientation.
        float yawLengthSquared = startRotation.y * startRotation.y + startRotation.w * startRotation.w;
        return yawLengthSquared > 0.0001f
            ? new Quaternion(0f, startRotation.y, 0f, startRotation.w).normalized
            : Quaternion.identity;
    }
}
