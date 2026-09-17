using UnityEngine;

public class LockedCameraState : AbstractCameraState
{
    private readonly float distanceToPivot = 1.5f;
    private readonly float positionSharpness = 12f;
    private readonly float rotationSharpness = 12f;
    private readonly float positionEpsilon = 0.01f;
    private readonly float rotationEpsilonDegrees = 0.5f;

    public LockedCameraState(CameraController controller) : base(controller, true)
    {
    }

    public override void OnEnter()
    {
        Smooth = true;
        ApplyLockedTransform();
    }

    public override void Update()
    {
        ApplyLockedTransform();
    }

    private void ApplyLockedTransform()
    {
        if (CameraPivot == null || CameraPosition == null)
        {
            return;
        }

        Quaternion targetRotation = CameraPivot.rotation;
        Vector3 targetPosition = CameraPivot.position - CameraPivot.forward * distanceToPivot;
        ApplyPositionAndRotation(targetPosition, targetRotation);
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

            if (CameraFacingCalc.IsPositionClose(CameraPosition.position, targetPosition, positionEpsilon)
                && CameraFacingCalc.IsRotationClose(CameraPosition.rotation, targetRotation, rotationEpsilonDegrees))
            {
                Smooth = false;
            }

            return;
        }

        CameraPosition.position = targetPosition;
        CameraPosition.rotation = targetRotation;
    }
}
