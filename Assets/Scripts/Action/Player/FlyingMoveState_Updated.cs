using UnityEngine;

public class FlyingMoveStateOld : AbstractPlayerState
{
    public override bool AllowsAiming => true;

    public const string AnimationName = "FlyingMove";//"FlyingMovev2";

    // The targeting rotation controller already knows the selected target, but the
    // target Transform itself is not exposed through PlayerController. While targeted,
    // let that controller produce its normal target-facing forward during FixedUpdate,
    // capture that direction in LateUpdate, then replace the final visual rotation with
    // the flight-oriented one calculated below.
    private Vector3 cachedTargetDirection;
    private bool hasCachedTargetDirection;
    private bool captureTargetDirectionOnLateUpdate;

    public FlyingMoveStateOld(PlayerController controller) : base(controller)
    {
    }

    public override void OnEnter()
    {
        cachedTargetDirection = Vector3.zero;
        hasCachedTargetDirection = false;
        captureTargetDirectionOnLateUpdate = false;

        Controller.UpdateAiming();
        Controller.ResetAerialJumpCounter();
        Controller.PlayFlyingLocomotionAnimation(AnimationName);
    }

    public override void Update()
    {
        Controller.UpdateAiming();
    }

    public override void FixedUpdate()
    {
        Controller.UpdateFlyingMoveInput();

        // PlayerController runs PlayerVisualsRotationController after the state's
        // FixedUpdate. Mark this tick so LateUpdate can capture its fresh target-facing
        // direction before we apply our own flight-facing rotation.
        captureTargetDirectionOnLateUpdate = Controller.IsTargeted;

        if (Controller.IsEvadeModifierPressed())
        {
            Controller.SetState(Controller.MoveInputRaw3D != Vector3.zero
                ? (AbstractPlayerState)Controller.AerialEvadeState
                : Controller.AerialEvadeDownwardsState);
            return;
        }

        if (Controller.MoveInputRaw3D == Vector3.zero)
        {
            Controller.SetState(Controller.FlyingIdleState);
            // Begin coasting in this physics step, without a frame of zero motion.
            Controller.FlyingIdleState.FixedUpdate();
            return;
        }

        Controller.UpdateFlyingAttackAnimation("FlyingMove");

        Controller.SetFlyingVelocity(CalculateFlyingVelocity());
    }

    public override void LateUpdate()
    {
        UpdateVisualFacing();
    }

    /// <summary>
    /// Places the player's visual forward along the current flight input and chooses
    /// roll by trying to point the visual's down axis toward the preferred reference.
    ///
    /// Untargeted: preferred down points away from the camera (camera forward).
    /// Targeted:   preferred down points toward the selected target.
    ///
    /// The preferred down direction is projected onto the plane perpendicular to the
    /// flight direction, so diagonal movement keeps as much of that preference as
    /// geometrically possible. If the preferred direction is parallel to flight
    /// (straight toward/away from camera or target), camera-down is used instead.
    /// </summary>
    private void UpdateVisualFacing()
    {
        Transform visuals = Controller.visualsPivot;
        Transform cameraDirection = Controller.Direction;

        if (visuals == null || cameraDirection == null)
        {
            captureTargetDirectionOnLateUpdate = false;
            return;
        }

        Vector3 movementDirection = Controller.MoveInputRotated3D;
        if (movementDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            captureTargetDirectionOnLateUpdate = false;
            return;
        }

        movementDirection.Normalize();

        bool isTargeted = Controller.IsTargeted;

        // At this point PlayerController's FixedUpdate has already allowed the normal
        // visuals rotation controller to face the selected target. Capture that forward
        // before replacing the final visual rotation. On render frames without a physics
        // tick, retain the last captured target direction rather than reading back our
        // own flight-facing forward.
        if (isTargeted && (captureTargetDirectionOnLateUpdate || !hasCachedTargetDirection))
        {
            Vector3 targetDirection = visuals.forward;
            if (targetDirection.sqrMagnitude > Mathf.Epsilon)
            {
                cachedTargetDirection = targetDirection.normalized;
                hasCachedTargetDirection = true;
            }
        }

        captureTargetDirectionOnLateUpdate = false;

        if (!isTargeted)
        {
            UpdateUntargetedVisualFacing(visuals, movementDirection, cameraDirection);
            return;
        }

        // Targeted mode keeps the previous rule: forward follows movement while down
        // points toward the selected target whenever that direction can define roll.
        Vector3 preferredDown = hasCachedTargetDirection
            ? cachedTargetDirection
            : cameraDirection.forward;

        Vector3 visualDown = Vector3.ProjectOnPlane(preferredDown, movementDirection);
        Vector3 cameraDown = Vector3.ProjectOnPlane(-cameraDirection.up, movementDirection);

        if (visualDown.sqrMagnitude <= 0.000001f)
            visualDown = cameraDown;

        visualDown = GetStablePerpendicularDown(
            visualDown,
            movementDirection,
            cameraDirection);

        visuals.rotation = Quaternion.LookRotation(movementDirection, -visualDown);
    }


    /// <summary>
    /// Returns how sharply movement passes through the camera plane.
    /// 0 = movement lies in the camera plane (left/right/up/down).
    /// 1 = movement is perpendicular to that plane (camera forward/backward).
    /// The value is linear in angular degrees from 0 to 90.
    /// </summary>
    private float CalculateAngleToCamera(Vector3 movementDirection, Transform cameraDirection)
    {
        if (movementDirection.sqrMagnitude <= Mathf.Epsilon || cameraDirection == null)
            return 0f;

        Vector3 cameraForward = cameraDirection.forward;
        if (cameraForward.sqrMagnitude <= Mathf.Epsilon)
            return 0f;

        float angleToCameraForward = Vector3.Angle(
            movementDirection.normalized,
            cameraForward.normalized);

        // Forward = 0 degrees and backward = 180 degrees relative to camera forward.
        // Both are 90 degrees away from the camera plane. Directions inside the plane
        // are 90 degrees from camera forward and therefore 0 degrees from the plane.
        float angleToCameraPlane = Mathf.Abs(90f - angleToCameraForward);
        return Mathf.Clamp01(angleToCameraPlane / 90f);
    }


     private void UpdateUntargetedVisualFacing(
        Transform visuals,
        Vector3 movementDirection,
        Transform cameraDirection)
    {
        float angleToCamera = CalculateAngleToCamera(movementDirection, cameraDirection);

        Vector3 awayFromCameraDown = Vector3.ProjectOnPlane(
            cameraDirection.forward,
            movementDirection);

        Vector3 cameraDown = Vector3.ProjectOnPlane(
            -cameraDirection.up,
            movementDirection);

        bool hasAwayFromCameraDown = awayFromCameraDown.sqrMagnitude > 0.000001f;
        bool hasCameraDown = cameraDown.sqrMagnitude > 0.000001f;

        // At the exact singular endpoints only one of the two references can define
        // roll. These cases are also where angleToCamera is exactly 0 or 1.
        if (!hasAwayFromCameraDown)
        {
            cameraDown = GetStablePerpendicularDown(
                cameraDown,
                movementDirection,
                cameraDirection);
            visuals.rotation = Quaternion.LookRotation(movementDirection, -cameraDown);
            return;
        }

        if (!hasCameraDown)
        {
            awayFromCameraDown = GetStablePerpendicularDown(
                awayFromCameraDown,
                movementDirection,
                cameraDirection);
            visuals.rotation = Quaternion.LookRotation(movementDirection, -awayFromCameraDown);
            return;
        }

        awayFromCameraDown.Normalize();
        cameraDown.Normalize();

        Quaternion awayFromCameraRotation = Quaternion.LookRotation(
            movementDirection,
            -awayFromCameraDown);

        Quaternion cameraDownRotation = Quaternion.LookRotation(
            movementDirection,
            -cameraDown);

        visuals.rotation = Quaternion.Slerp(
            awayFromCameraRotation,
            cameraDownRotation,
            angleToCamera);
    }   

    private Vector3 GetStablePerpendicularDown(
        Vector3 downDirection,
        Vector3 movementDirection,
        Transform cameraDirection)
    {
        if (downDirection.sqrMagnitude <= 0.000001f)
        {
            downDirection = Vector3.Cross(movementDirection, cameraDirection.right);
            if (downDirection.sqrMagnitude <= 0.000001f)
                downDirection = Vector3.Cross(movementDirection, Vector3.right);
            if (downDirection.sqrMagnitude <= 0.000001f)
                downDirection = Vector3.Cross(movementDirection, Vector3.forward);
        }

        return downDirection.normalized;
    }

    private Vector3 CalculateFlyingVelocity()
    {
        Vector3 velocity = Controller.FlyingVelocity;
        Vector3 thrust = Controller.MoveInputRotated3D * Controller.FlyingMoveAcceleration;
        Vector3 friction = velocity * Controller.FlyingMoveFrictionModifier;
        // At full input, thrust balances friction at acceleration / friction modifier.
        return velocity + (thrust - friction) * Time.fixedDeltaTime;
    }

    public override void OnExit()
    {
        captureTargetDirectionOnLateUpdate = false;
    }
}
