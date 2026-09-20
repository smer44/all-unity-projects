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
    public FlyingNotTargetedFacingState FlyingNotTargetedState { get; private set; }
    public FlyingTargetedFacingState FlyingTargetedState { get; private set; }
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
        FlyingNotTargetedState = new FlyingNotTargetedFacingState(this);
        FlyingTargetedState = new FlyingTargetedFacingState(this);
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

        bool isFlyingState = currentState == FlyingNotTargetedState || currentState == FlyingTargetedState;
        if (playerController != null && playerController.IsFlying)
        {
            if (!isFlyingState || playerController.IsFlyingDashing)
            {
                SetState(GetDefaultState());
            }
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
            return IsTargeted && !playerController.IsFlyingDashing ? FlyingTargetedState : FlyingNotTargetedState;
        }

        if (playerController != null && playerController.IsInWater())
        {
            return WaterNotTargetedState;
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
