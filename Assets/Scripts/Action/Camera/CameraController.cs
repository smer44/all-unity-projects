using UnityEngine;

[DisallowMultipleComponent]
public class CameraController : DirectionPointer
{
    public enum CameraStateKind
    {
        Free,
        LookAt,
        RestrictedLookAt,
        Locked,
        LookAtFlying,
        LookAtFlyingNormalizing,
        FirstPerson,
        FirstPersonFlying
    }

    [SerializeField] private CameraStateKind initialState = CameraStateKind.LookAt;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private AbstractUnitControls buttonControls;
    [SerializeField, Min(0f)] private float lookAtSmoothDuration = 1f;

    private AbstractCameraState currentState;

    public Transform CameraPivotTransform => cameraPivot;
    public AbstractUnitControls ButtonControls => buttonControls;
    public float LookAtSmoothDuration => lookAtSmoothDuration;

    public AbstractCameraState CurrentState => currentState;
    public FreeCameraController FreeCameraState { get; private set; }
    public LookAtCameraState LookAtCameraState { get; private set; }
    public LookAtFlyingCameraState LookAtFlyingCameraState { get; private set; }
    public FirstPersonCameraState FirstPersonCameraState { get; private set; }
    public FirstPersonFlyingCameraState FirstPersonFlyingCameraState { get; private set; }
    public LookAtFlyingNormalizingCameraState LookAtFlyingNormalizingCameraState { get; private set; }
    public RestrictedLookAtCameraState RestrictedLookAtCameraState { get; private set; }
    public LockedCameraState LockedCameraState { get; private set; }
    public Transform CameraPivot => currentState != null ? currentState.CameraPivot : null;
    public Transform CameraPosition => currentState != null ? currentState.CameraPosition : null;

    public override Transform GetDirection()
    {
        return cameraPosition;
    }

    private void Awake()
    {
        if (buttonControls == null)
        {
            buttonControls = ScriptableObject.CreateInstance<Action3DButtonControls>();
        }

        FreeCameraState = new FreeCameraController(this);
        LookAtCameraState = new LookAtCameraState(this);
        LookAtFlyingCameraState = new LookAtFlyingCameraState(this);
        FirstPersonCameraState = new FirstPersonCameraState(this);
        FirstPersonFlyingCameraState = new FirstPersonFlyingCameraState(this);
        LookAtFlyingNormalizingCameraState = new LookAtFlyingNormalizingCameraState(this);
        RestrictedLookAtCameraState = new RestrictedLookAtCameraState(this);
        LockedCameraState = new LockedCameraState(this);
    }

    private void Start()
    {
        if (currentState == null)
        {
            SetState(GetInitialState());
        }
    }

    public void Initialize(MonoBehaviour controller)
    {
        SetState(GetInitialState());
    }


    public void SetState(AbstractCameraState newState)
    {
        if (newState == null || newState == currentState)
        {
            return;
        }

        currentState?.OnExit();
        currentState = newState;
        currentState.OnEnter();
    }

    public void SetState(CameraStateKind stateKind)
    {
        SetState(GetState(stateKind));
    }

    public void SetCameraPivot(Transform newCameraPivot)
    {
        cameraPivot = newCameraPivot;
    }

    [ContextMenu("Camera State/Set Free")]
    public void SetFreeState()
    {
        SetState(CameraStateKind.Free);
    }

    [ContextMenu("Camera State/Set Look At")]
    public void SetLookAtState()
    {
        SetState(CameraStateKind.LookAt);
    }

    [ContextMenu("Camera State/Set Look At Flying")]
    public void SetLookAtFlyingState()
    {
        SetState(CameraStateKind.LookAtFlying);
    }

    [ContextMenu("Camera State/Set Look At Flying Normalizing")]
    public void SetLookAtFlyingNormalizingState()
    {
        SetState(CameraStateKind.LookAtFlyingNormalizing);
    }

    [ContextMenu("Camera State/Set Restricted Look At")]
    public void SetRestrictedLookAtState()
    {
        SetState(CameraStateKind.RestrictedLookAt);
    }

    [ContextMenu("Camera State/Set Locked")]
    public void SetLockedState()
    {
        SetState(CameraStateKind.Locked);
    }

    public void Update()
    {
        currentState?.Update();
    }

    private AbstractCameraState GetInitialState()
    {
        return GetState(initialState);
    }

    private AbstractCameraState GetState(CameraStateKind stateKind)
    {
        return stateKind switch
        {
            CameraStateKind.Free => FreeCameraState,
            CameraStateKind.LookAtFlying => LookAtFlyingCameraState,
            CameraStateKind.FirstPerson => FirstPersonCameraState,
            CameraStateKind.FirstPersonFlying => FirstPersonFlyingCameraState,
            CameraStateKind.LookAtFlyingNormalizing => LookAtFlyingNormalizingCameraState,
            CameraStateKind.RestrictedLookAt => RestrictedLookAtCameraState,
            CameraStateKind.Locked => LockedCameraState,
            _ => LookAtCameraState
        };
    }
}
