using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class DebugCameraSwitcher : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;

    private void Awake()
    {
        if (cameraController == null)
        {
            cameraController = GetComponent<CameraController>();
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || cameraController == null
            || cameraController.CurrentState is LookAtFlyingCameraState
            || cameraController.CurrentState is LookAtFlyingNormalizingCameraState)
        {
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame)
        {
            cameraController.SetState(cameraController.FreeCameraState);
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            cameraController.SetState(cameraController.LookAtCameraState);
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            cameraController.SetState(cameraController.RestrictedLookAtCameraState);
        }

        if (keyboard.tKey.wasPressedThisFrame)
        {
            cameraController.SetState(cameraController.LockedCameraState);
        }
    }
}
