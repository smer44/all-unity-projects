using UnityEngine;

public static class CameraFacingCalc
{
    public static Quaternion RotateOrbitLocal(Quaternion rotation, Vector2 lookDelta, float sensitivity)
    {
        // Post-multiplication turns around the camera's own axes, retaining roll
        // and allowing pitch to continue through any number of full turns.
        return (rotation
            * Quaternion.AngleAxis(lookDelta.x * sensitivity, Vector3.up)
            * Quaternion.AngleAxis(-lookDelta.y * sensitivity, Vector3.right)).normalized;
    }

    public static Vector3 RotateFlyingInput(Vector2 input, Transform cameraDirection)
    {
        Vector3 localDirection = new Vector3(input.x, 0f, input.y);
        return cameraDirection != null ? cameraDirection.rotation * localDirection : localDirection;
    }

    public static Vector2 RotateInput(Vector2 input, Transform rotationTarget)
    {
        if (rotationTarget == null)
        {
            return input;
        }

        float yawDegrees = rotationTarget.eulerAngles.y;
        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3 rotated = rotation * new Vector3(input.x, 0f, input.y);
        return new Vector2(rotated.x, rotated.z);
    }

    public static Vector3 RotateInput(Vector3 input, Transform rotationTarget)
    {
        if (rotationTarget == null)
        {
            return input;
        }

        float yawDegrees = rotationTarget.eulerAngles.y;
        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3 rotatedPlanar = rotation * new Vector3(input.x, 0f, input.z);
        return new Vector3(rotatedPlanar.x, input.y, rotatedPlanar.z);
    }

    public static Vector3 ExpLerpMove(Vector3 currentPosition, Vector3 targetPosition, float sharpness, float deltaTime)
    {
        float t = 1f - Mathf.Exp(-sharpness * deltaTime);
        return Vector3.Lerp(currentPosition, targetPosition, t);
    }

    public static Quaternion ExpLerpRotate(Quaternion currentRotation, Quaternion targetRotation, float sharpness, float deltaTime)
    {
        float t = 1f - Mathf.Exp(-sharpness * deltaTime);
        return Quaternion.Slerp(currentRotation, targetRotation, t);
    }

    public static bool IsPositionClose(Vector3 currentPosition, Vector3 targetPosition, float epsilon)
    {
        return Vector3.Distance(currentPosition, targetPosition) <= epsilon;
    }

    public static bool IsRotationClose(Quaternion currentRotation, Quaternion targetRotation, float epsilonDegrees)
    {
        return Quaternion.Angle(currentRotation, targetRotation) <= epsilonDegrees;
    }
}
