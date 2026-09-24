using UnityEngine;

public class PlayerVisualsRotationController : MonoBehaviour
{
    private AbstractPlayerVisualsRotationState currentState;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private float rotationSpeed = 5f;

    public GameObject target;

    public SurfaceNotTargetedFacingState SurfaceNotTargetedState { get; private set; }
    public WaterNotTargetedFacingState WaterNotTargetedState { get; private set; }
    public SurfaceTargetedFacingState SurfaceTargetedState { get; private set; }
    public GroundLikeCameraFacingState GroundLikeCameraFacingState { get; private set; }
    public FlyingNonTargetedFacingDownwardState FlyingNonTargetedDownwardState { get; private set; }
    public FlyingNonTargetedFacingForwardState FlyingNonTargetedForwardState { get; private set; }
    public FlyingTargetedFacingDownwardState FlyingTargetedDownwardState { get; private set; }
    public FlyingTargetedFacingForwardState FlyingTargetedForwardState { get; private set; }
    public FreezeRotationState FreezeRotationState { get; private set; }

    public PlayerController PlayerController => playerController;
    public float RotationSpeed => rotationSpeed;
    public bool IsTargeted => target != null;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        SurfaceNotTargetedState = new SurfaceNotTargetedFacingState(this);
        WaterNotTargetedState = new WaterNotTargetedFacingState(this);
        SurfaceTargetedState = new SurfaceTargetedFacingState(this);
        GroundLikeCameraFacingState = new GroundLikeCameraFacingState(this);
        FlyingNonTargetedDownwardState = new FlyingNonTargetedFacingDownwardState(this);
        FlyingNonTargetedForwardState = new FlyingNonTargetedFacingForwardState(this);
        FlyingTargetedDownwardState = new FlyingTargetedFacingDownwardState(this);
        FlyingTargetedForwardState = new FlyingTargetedFacingForwardState(this);
        FreezeRotationState = new FreezeRotationState(this);
    }

    private void Start()
    {
        if (currentState == null)
        {
            SetState(GetDefaultState());
        }
    }

    private void LateUpdate()
    {
        currentState?.LateUpdate();
    }

    public void FixedUpdateController()
    {
        // A timed freeze takes priority over automatic surface/flight transitions.
        if (currentState == FreezeRotationState && currentState != null)
        {
            currentState.FixedUpdate();
            return;
        }

        UpdateAimingState();

        bool isFlyingState = currentState == FlyingNonTargetedDownwardState || currentState == FlyingNonTargetedForwardState
            || currentState == FlyingTargetedDownwardState || currentState == FlyingTargetedForwardState;
        if (playerController != null && playerController.IsFlying)
        {
            // Apply target and shooting changes before updating facing, including while hovering.
            SetState(GetDefaultState());
        }
        else if (currentState == null || isFlyingState)
        {
            SetState(GetDefaultState());
        }

        currentState?.FixedUpdate();
    }

    public void SetState(AbstractPlayerVisualsRotationState newState)
    {
        if (newState == null || newState == currentState)
        {
            return;
        }

        currentState?.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }

    public void UpdateAimingState()
    {
        if (currentState == FreezeRotationState && currentState != null)
        {
            return;
        }

        AbstractPlayerVisualsRotationState defaultState = GetDefaultState();
        if (defaultState == GroundLikeCameraFacingState || currentState == GroundLikeCameraFacingState)
        {
            SetState(defaultState);
        }
    }

    public void EnterFreezeRotationState(float duration)
    {
        if (duration <= 0f)
        {
            ExitFreezeRotationState();
            return;
        }

        FreezeRotationState.Configure(currentState, duration);
        SetState(FreezeRotationState);
    }

    public void ExitFreezeRotationState()
    {
        if (currentState == FreezeRotationState && currentState != null)
        {
            SetState(FreezeRotationState.PreviousState ?? GetDefaultState());
        }
    }

    public AbstractPlayerVisualsRotationState GetDefaultState()
    {
        if (playerController != null && playerController.IsFlying)
        {
            if (playerController.IsFlyingDashing)
                return FlyingNonTargetedDownwardState;

            bool shootingWhileMoving = playerController.IsFlyingMoving && playerController.IsFlyingShooting;
            if (IsTargeted)
                return shootingWhileMoving ? FlyingTargetedForwardState : FlyingTargetedDownwardState;

            return shootingWhileMoving ? FlyingNonTargetedForwardState : FlyingNonTargetedDownwardState;
        }

        if (playerController != null && playerController.IsInWater())
        {
            return WaterNotTargetedState;
        }

        if (playerController != null && playerController.IsGroundAiming)
        {
            return GroundLikeCameraFacingState;
        }

        return IsTargeted ? SurfaceTargetedState : SurfaceNotTargetedState;
    }

    public void RotateToFacing(Vector2 rotatedFacing)
    {
        if (playerController == null)
        {
            return;
        }

        FacingCalc.RotateToFacing(
            playerController.visualsPivot,
            rotatedFacing,
            playerController.GetReferenceUp(),
            rotationSpeed);
    }

    public void RotateToFacing3D(Vector3 rotatedFacing)
    {
        if (playerController == null)
        {
            return;
        }

        FacingCalc.RotateToFacing3D(
            playerController.visualsPivot,
            rotatedFacing,
            playerController.PlayerBody,
            rotationSpeed);
    }

    public void RotateToFacing3D(Vector3 rotatedFacing, Vector3 referenceUp)
    {
        if (playerController == null)
        {
            return;
        }

        FacingCalc.RotateToFacing3D(
            playerController.visualsPivot,
            rotatedFacing,
            referenceUp,
            rotationSpeed);
    }

    public void RotateToFacing3Dv2(Vector3 rotatedFacing, Vector3 cameraForward, Vector3 cameraUp, bool faceForward = false)
    {
        if (playerController == null || playerController.visualsPivot == null || rotatedFacing.sqrMagnitude < 0.0001f)
            return;

        Vector3 cameraBack = -cameraForward;
        float alignment = Mathf.Abs(Vector3.Dot(rotatedFacing.normalized, cameraForward));
        Vector3 referenceUp = Vector3.Slerp(cameraBack, cameraUp, alignment).normalized;

        if (faceForward)
        {
            // Exchange axes from the same desired flight orientation: its down
            // becomes shooting forward, and its movement-facing forward becomes up.
            Quaternion movementRotation = Quaternion.LookRotation(rotatedFacing.normalized, referenceUp);
            referenceUp = rotatedFacing.normalized;
            rotatedFacing = movementRotation * Vector3.down;
        }

        FacingCalc.RotateToFacing3D(
            playerController.visualsPivot,
            rotatedFacing,
            referenceUp,
            rotationSpeed);
    }


    public void RotateToFacing3DTargetedUpwardsChange(Vector3 rotatedFacing, Vector3 targetPosition, Vector3 cameraUp)
    {
        if (playerController == null || playerController.visualsPivot == null || rotatedFacing.sqrMagnitude < 0.0001f)
            return;

        Transform visuals = playerController.visualsPivot;
        Vector3 directionToTarget = targetPosition - visuals.position;
        // Forward must stay along movement. Roll points down toward the target's
        // projection onto the plane perpendicular to that forward direction.
        RotateToFacing3DWithStableUp(visuals, rotatedFacing, -directionToTarget, cameraUp);
    }

    public void RotateToFacing3DTargetedForward(Vector3 movementDirection, Vector3 targetPosition, Vector3 cameraUp)
    {
        if (playerController == null || playerController.visualsPivot == null)
            return;

        Transform visuals = playerController.visualsPivot;
        Vector3 directionToTarget = targetPosition - visuals.position;
        if (directionToTarget.sqrMagnitude < 0.0001f)
            return;

        // Targeting owns forward; movement determines roll through the up axis.
        RotateToFacing3DWithStableUp(visuals, directionToTarget, movementDirection, cameraUp);
    }

    private void RotateToFacing3DWithStableUp(Transform visuals, Vector3 facing, Vector3 desiredUp, Vector3 cameraUp)
    {
        Vector3 forward = facing.normalized;
        Vector3 referenceUp = Vector3.ProjectOnPlane(desiredUp.normalized, forward);
        if (referenceUp.sqrMagnitude < 0.0001f)
        {
            // Parallel or missing directions cannot determine roll. Keep the current
            // up axis where possible, then fall back to the camera or a world axis.
            referenceUp = Vector3.ProjectOnPlane(visuals.up, forward);
            if (referenceUp.sqrMagnitude < 0.0001f)
                referenceUp = FacingCalc.GetReferenceForwardByUp(forward, cameraUp);
        }

        FacingCalc.RotateToFacing3D(visuals, forward, referenceUp.normalized, rotationSpeed);
    }



    public bool TryGetPlanarTargetFacing(out Vector2 facing)
    {
        facing = Vector2.zero;
        if (!TryGetTargetFacing3D(out Vector3 toTarget))
        {
            return false;
        }

        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        facing = new Vector2(toTarget.x, toTarget.z);
        return true;
    }

    public bool TryGetTargetFacing3D(out Vector3 facing)
    {
        facing = Vector3.zero;
        if (playerController == null || target == null)
        {
            return false;
        }

        Transform visualsPivot = playerController.visualsPivot;
        Vector3 origin = visualsPivot != null
            ? visualsPivot.position
            : playerController.transform.position;

        facing = target.transform.position - origin;
        return facing.sqrMagnitude > Mathf.Epsilon;
    }
}
