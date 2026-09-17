using UnityEngine;

public static class FacingCalc
{
    public static void RotateToFacing3D(
        Transform visualsPivot, Vector3 movementDirection, Vector3 referenceUp, float speed)
    {
        if (visualsPivot == null || movementDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // Align the same forward axis used by swimming with the converted world
        // movement. Reference up controls roll without changing that direction.
        Quaternion rotation = Quaternion.LookRotation(movementDirection.normalized, referenceUp);
        visualsPivot.rotation = Quaternion.Slerp(
            visualsPivot.rotation, rotation, speed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Projects the supplied forward vector onto the plane defined by referenceUp
    /// and returns a normalized planar heading. If the projection collapses, it
    /// falls back to stable world axes so callers still get a usable direction.
    /// </summary>
    public static Vector3 GetReferenceForwardByUp(Vector3 referenceUp, Vector3 forward)
    {
        Vector3 up = referenceUp.sqrMagnitude <= Mathf.Epsilon ? Vector3.up : referenceUp.normalized;
        Vector3 referenceForward = Vector3.ProjectOnPlane(forward, up);
        if (referenceForward.sqrMagnitude <= Mathf.Epsilon)
        {
            referenceForward = Vector3.ProjectOnPlane(Vector3.forward, up);
        }

        if (referenceForward.sqrMagnitude <= Mathf.Epsilon)
        {
            referenceForward = Vector3.ProjectOnPlane(Vector3.right, up);
        }

        return referenceForward.normalized;
    }

    /// <summary>
    /// Rotates a visuals pivot toward a planar facing direction.
    /// The input is expected in world-space XZ coordinates, and the rotation is
    /// applied smoothly using Slerp so character turning stays continuous.
    /// </summary>
    public static void RotateToFacing(
        Transform visualsPivot,
        Vector2 rotatedFacing,
        Vector3 referenceUp,
        float speed)
    {
        if (visualsPivot == null)
        {
            return;
        }

        Vector3 direction = new Vector3(rotatedFacing.x, 0f, rotatedFacing.y);
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // LookRotation builds the target orientation from the desired forward axis
        // and the supplied up axis, so callers are not locked to Vector3.up.
        Quaternion targetRotation = Quaternion.LookRotation(direction, referenceUp);
        visualsPivot.rotation = Quaternion.Slerp(
            visualsPivot.rotation,
            targetRotation,
            speed * Time.fixedDeltaTime
        );
    }

    /// <summary>
    /// Rotates a visuals pivot toward a full 3D facing direction.
    /// Yaw is derived from the horizontal projection, while pitch is derived from
    /// the vertical component, allowing swimming or flying visuals to tilt up/down.
    /// </summary>
    public static void RotateToFacing3D(
        Transform visualsPivot,
        Vector3 rotatedFacing,
        Rigidbody playerBody,
        float speed)
    {
        if (visualsPivot == null || rotatedFacing.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 direction = rotatedFacing.normalized;
        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
        float yaw = visualsPivot.rotation.eulerAngles.y;
        if (flatDirection.sqrMagnitude >= 0.0001f)
        {
            // Keep yaw stable when aiming mostly vertical by only replacing it when
            // the horizontal projection is large enough to define a heading.
            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        float pitch = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
        Quaternion yawRotation = Quaternion.AngleAxis(yaw, Vector3.up);
        // Apply pitch around the yaw-rotated local right axis so the character tilts
        // relative to its current heading instead of the global world axes.
        Quaternion pitchRotation = Quaternion.AngleAxis(-pitch, yawRotation * Vector3.right);
        Quaternion targetVisualRotation = pitchRotation * yawRotation;
        visualsPivot.rotation = Quaternion.Slerp(
            visualsPivot.rotation,
            targetVisualRotation,
            speed * Time.fixedDeltaTime
        );

        if (playerBody == null)
        {
            return;
        }

        // Body rotation handling can be added here later if the Rigidbody should
        // track pitch as well as the visuals pivot.
    }
}
