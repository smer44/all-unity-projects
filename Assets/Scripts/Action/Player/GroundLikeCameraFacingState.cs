using UnityEngine;

public class GroundLikeCameraFacingState : AbstractPlayerVisualsRotationState
{
    public GroundLikeCameraFacingState(PlayerVisualsRotationController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
    }

    public override void FixedUpdate()
    {
        // Camera-facing visuals update once per rendered frame in LateUpdate.
    }

    public override void LateUpdate()
    {
        LateUpdate(Time.deltaTime);
    }

    public void LateUpdate(float deltaTime)
    {
        PlayerController player = Controller.PlayerController;
        if (player == null || player.visualsPivot == null || player.PlayerCameraController == null)
        {
            return;
        }

        Transform cameraTransform = player.PlayerCameraController.GetDirection();
        if (cameraTransform == null)
        {
            return;
        }

        // Keep the character upright while following the camera's horizontal heading.
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // The camera updates in Update. Follow its latest heading after animation,
        // using render delta time so physics tick timing cannot cause visible steps.
        Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);
        float blend = 1f - Mathf.Exp(-Controller.RotationSpeed * deltaTime);
        player.visualsPivot.rotation = Quaternion.Slerp(player.visualsPivot.rotation, targetRotation, blend);
    }

    public override void OnExit()
    {
    }
}
