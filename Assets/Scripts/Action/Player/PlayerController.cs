using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// Controls player locomotion and base-layer animation. Ground attack visuals
/// are controlled independently by UpperBodyVisualsController.
/// </summary>
[RequireComponent(typeof(UpperBodyVisualsController))]
public class PlayerController : MonoBehaviour
{
    // GunPivot is the second entry in HandWeaponSwitch.prefab's gameObjects list.
    private const int GunWeaponIndex = 1;

    public enum DisplacementApplyMode
    {
        Simple,
        Inertia
    }

    private AbstractPlayerState currentState;
    private string flyingAttackAnimationName;
    private float flyingAttackElapsedTime;
    public Animator animator;

    //[SerializeField] private CameraController playerCameraController;
    [SerializeField] private DirectionPointer directionPointer;
    [SerializeField] private AbstractUnitControls buttonControls;
    [SerializeField] private PlayerVisualsRotationController visualsRotationController;
    [SerializeField] private UpperBodyVisualsController upperBodyVisualsController;

    public Transform visualsPivot;
    [SerializeField] private Transform[] verticalRotationBones;
    [SerializeField] private Rigidbody playerBody;
    [SerializeField] private Collider playerCollider;


    [SerializeField] private TogglerOfGameObjectKeySwitch handWeaponSwitch;
    //[SerializeField] public float moveSpeed = 7f;
    //[SerializeField] public float JumpMoveSpeed = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float minVerticalSpeed = -200f;
    [SerializeField] private float maxVerticalSpeed = 200f;
    [SerializeField] private int moveAndSlideIterations = 4;

    //[SerializeField] public float BackflipRotationSpeed = 25f;

    //[SerializeField] public float BackflipMovementSpeed = 15f;

    [SerializeField] private float JumpImpulse = 5f;

    [SerializeField] private bool BattleReady = false;
    [SerializeField] private int aerialJumpAmount = 1;
    [SerializeField, Min(0f)] private float flyingMoveAcceleration = 40f;
    public float FlyingMoveAcceleration => flyingMoveAcceleration;
    [SerializeField, Min(0f)] private float flyingMoveFrictionModifier = 0.8f;
    public float FlyingMoveFrictionModifier => flyingMoveFrictionModifier;
    [SerializeField, Min(0f)] private float flyingDashAcceleration = 300f;
    public float FlyingDashAcceleration => flyingDashAcceleration;
    [SerializeField, Min(0f)] private float flyingDashFrictionModifier = 3f;
    public float FlyingDashFrictionModifier => flyingDashFrictionModifier;
    [Tooltip("Speed-proportional friction coefficient while in idle flight.")]
    [SerializeField, Min(0f)] private float flyingIdleFrictionModifier = 200f;
    public float FlyingIdleFrictionModifier => flyingIdleFrictionModifier;
    [Tooltip("Minimum speed lost per second while in idle flight.")]
    [SerializeField, Min(0f)] private float flyingIdleMinimumFrictionMagnitude = 200f;
    public float FlyingIdleMinimumFrictionMagnitude => flyingIdleMinimumFrictionMagnitude;
    [SerializeField, Min(0f)] private float flyingDashLockDuration = 1.5f;
    public float FlyingDashLockDuration => flyingDashLockDuration;
    [SerializeField] private DisplacementApplyMode displacementApplyMode = DisplacementApplyMode.Simple;
    public DisplacementApplyMode CurrentDisplacementApplyMode => displacementApplyMode;

    public IdleState IdleState { get; private set; }

    public WalkState WalkState { get; private set; }

    public InteractState InteractState { get; private set; }

    public GroundMeleeMoveState GroundMeleeMoveState { get; private set; }

    public AirHitState AirHitState { get; private set; }

    public BlockingState BlockingState { get; private set; }

    public IdlePauseState IdlePauseState { get; private set; }

    public RunState RunState { get; private set; }
    public JumpUpState JumpUpState { get; private set; }
    public AerialJumpUpState AerialJumpUpState { get; private set; }
    public FallState FallState { get; private set; }
    public LandingState LandingState { get; private set; }

    public SlideState SlideState { get; private set; }
    public SwimIdleState SwimIdleState { get; private set; }
    public SwimMoveState SwimMoveState { get; private set; }
    public FlyingIdleState FlyingIdleState { get; private set; }
    public FlyingMoveState FlyingMoveState { get; private set; }
    public FlyingDashState FlyingDashState { get; private set; }
    public AerialEvadeState AerialEvadeState { get; private set; }
    public AerialEvadeDownwardsState AerialEvadeDownwardsState { get; private set; }

    public bool IsAerialEvading => currentState is AerialEvadeState;
    public bool IsFlyingIdle => currentState is FlyingIdleState;
    public bool IsFlyingMoving => currentState is FlyingMoveState;
    public bool IsFlyingDashing => currentState is FlyingDashState;
    public bool IsFlying => IsFlyingIdle || IsFlyingMoving || IsFlyingDashing || IsAerialEvading;
    public bool IsTargeted => visualsRotationController != null && visualsRotationController.IsTargeted;
    public bool IsAiming => PlayerCameraController != null
        && (PlayerCameraController.CurrentState is FirstPersonCameraState
            || PlayerCameraController.CurrentState is FirstPersonFlyingCameraState);
    private bool CanAim => currentState != null && currentState.AllowsAiming
        && IsGunSelected() && buttonControls != null && buttonControls.IsAimButtonPressed();
    private CameraController flightCameraController;
    private AbstractCameraState cameraStateBeforeFlight;

    public AbstractPlayerState RunOrWalkState => isRunMode ? RunState : WalkState;
    public AbstractPlayerState GroundMovementState => upperBodyVisualsController != null
        && upperBodyVisualsController.IsMeleeAttacking ? GroundMeleeMoveState : RunOrWalkState;
    public bool AllowsUpperBodyAttacks => currentState != null && currentState.AllowsUpperBodyAttacks;

    public float MoveSpeed => isRunMode ? RunState.MoveSpeed : WalkState.MoveSpeed;
    //private BackFlipState backFlipState;
    //private ForwardEvadeState forwardEvadeState;
    //private SideEvadeState sideEvadeState;


    private Vector3 localVelocity;
    public float localVerticalSpeedAccumulator;
    private bool isRunMode;
    public bool isJumpEnabled;
    private int aerialJumpCounter;
    public bool IsGrounded { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsBackFlip { get; private set; }
    public bool IsForwardEvade { get; private set; }
    public bool IsSideEvade { get; private set; }

    public bool IsStartingInteraction { get; private set; }

    public float SideEvadeDirection { get; private set; }
    public Vector2 MoveInputRaw { get; private set; }
    public Vector2 MoveInputRotated { get; private set; }
    public Vector3 MoveInputRaw3D { get; private set; }
    public Vector3 MoveInputRotated3D { get; private set; }
    public Vector3 FlyingMoveFacingDirection { get; private set; }
    // World velocity in units/second, retained between aerial states and corrected
    // by the collision solver after each physics step.
    public Vector3 FlyingVelocity { get; private set; }

    public GameObject InteractObject { get; private set; }
    private AbstractInteractable focusedInteractable;
    private Vector3 startPosition;
    private Vector3 oldParentPosition;
    private Transform cachedParentForSpeed;
    private readonly HashSet<Collider> waterTriggers = new HashSet<Collider>();
    private readonly RaycastHit[] moveAndSlideHitBuffer = new RaycastHit[8];
    private readonly Collider[] interactDetectionBuffer = new Collider[32];

    private List<Collider> toRemove = new List<Collider>();
    private int waterLayer = -1;
    private Vector3 previousAppliedDisplacement;

    public float skin = 0.05f;


    //public Rigidbody PlayerBody => playerBody;
    public CameraController PlayerCameraController => directionPointer as CameraController;
    public AbstractUnitControls ButtonControls => buttonControls;
    public TogglerOfGameObjectKeySwitch HandWeaponSwitch => handWeaponSwitch;
    public PlayerVisualsRotationController VisualsRotationController => visualsRotationController;
    public UpperBodyVisualsController UpperBodyVisualsController => upperBodyVisualsController;
    public Transform Direction => GetFacingDirectionTransform();
    public Rigidbody PlayerBody => playerBody;
    private Transform GroundParent;
    public RaycastHit BestGround { get; private set; }

    private void Awake()
    {
        if (playerBody == null)
        {
            playerBody = GetComponent<Rigidbody>();
        }

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>();
        }

        if (directionPointer == null)
        {
            directionPointer = GetComponentInChildren<DirectionPointer>();
        }

        if (handWeaponSwitch == null)
        {
            handWeaponSwitch = GetComponentInChildren<TogglerOfGameObjectKeySwitch>(true);
        }

        if (buttonControls == null)
        {
            buttonControls = ScriptableObject.CreateInstance<Action3DButtonControls>();
        }
        else
        {
            buttonControls = Instantiate(buttonControls);
        }

        buttonControls.Initialize(this);

        if (visualsRotationController == null)
        {
            visualsRotationController = GetComponent<PlayerVisualsRotationController>();
        }

        if (visualsRotationController == null)
        {
            visualsRotationController = gameObject.AddComponent<PlayerVisualsRotationController>();
        }

        if (upperBodyVisualsController == null || upperBodyVisualsController.gameObject != gameObject)
            upperBodyVisualsController = GetComponent<UpperBodyVisualsController>();
        if (upperBodyVisualsController == null)
            upperBodyVisualsController = gameObject.AddComponent<UpperBodyVisualsController>();
        upperBodyVisualsController.Initialize(this);

        waterLayer = LayerMask.NameToLayer("Water");

        //Initialize states: 
        IdleState = new IdleState(this);
        IdlePauseState = new IdlePauseState(this);
        WalkState = new WalkState(this);

        InteractState = new InteractState(this);
        GroundMeleeMoveState = new GroundMeleeMoveState(this);
        AirHitState = new AirHitState(this);
        BlockingState = new BlockingState(this);

        RunState = new RunState(this);
        JumpUpState = new JumpUpState(this);
        AerialJumpUpState = new AerialJumpUpState(this);
        FallState = new FallState(this);

        SlideState = new SlideState(this);
        SwimIdleState = new SwimIdleState(this);
        SwimMoveState = new SwimMoveState(this);
        FlyingIdleState = new FlyingIdleState(this);
        FlyingMoveState = new FlyingMoveState(this);
        FlyingDashState = new FlyingDashState(this);
        AerialEvadeState = new AerialEvadeState(this);
        AerialEvadeDownwardsState = new AerialEvadeDownwardsState(this);

        //landingState = new LandingState(this);
        //backFlipState = new BackFlipState(this);
        //forwardEvadeState = new ForwardEvadeState(this);
        //sideEvadeState = new SideEvadeState(this);

    }

    public void PauseControls()
    {
        SetState(IdlePauseState);
    }

    public void UnPauseControls()
    {
        SetState(IdleState);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        startPosition = playerBody != null ? playerBody.position : transform.position;
        
        if (directionPointer == null)
        {
            Debug.LogWarning($"{nameof(PlayerController)} on {name} has no {nameof(DirectionPointer)} assigned.");
        }

        UpdateOldParentPosition();
        SetState(IdleState);
    }

    // Update is called once per frame
    private void Update()
    {
        buttonControls?.UpdateControls(Time.deltaTime);
        UpdateRunWalkMode();
        UpdateFlightToggle();
        UpdateFlyingDash();
        //camera controller does update by its own.

        GizmoDisplayer.DebugHalfCircleGismo(
            playerCollider,
            visualsPivot != null ? visualsPivot.forward : transform.forward,
            GetReferenceUp(),
            1f);
        currentState?.Update();
        UpdateFocusedInteractable();
    }

    private void FixedUpdate()
    {
        FixedUpdateV1();
    }

    private void LateUpdate()
    {
        currentState?.LateUpdate();
    }


    private void FixedUpdateV1()
    {
        // Inertia coasts at the stored velocity until the state accelerates or brakes.
        // Simple movement starts with no displacement each tick.
        localVelocity = displacementApplyMode == DisplacementApplyMode.Inertia
            ? FlyingVelocity * Time.fixedDeltaTime
            : Vector3.zero;

        //CheckResetToStartPosition();

        // 2) Let the current locomotion state compute input / gravity / jump
        // effects for this tick. Those methods accumulate displacement for this
        // fixed step into localVelocity rather than moving the Rigidbody directly.
        currentState?.FixedUpdate();
        if (!IsFlying && visualsRotationController != null && visualsRotationController.isActiveAndEnabled)
        {
            visualsRotationController.FixedUpdateController();
        }

        if (playerBody != null)
        {
            // 3) Read how much motion comes from the current support object
            // (moving platform, rigidbody ground, etc). This carry motion is
            // independent from the player's own movement input.
            var parentWorldDisplacement = GetParentWorldDisplacement();

            // 4) Snapshot the player's own displacement that the state logic
            // produced in step 2.
            var selfWorldDisplacement = localVelocity;

            // 5) Prepare an extra displacement that only exists to push the
            // player out of overlaps before the real movement is solved.
            var depenetrationDisplacement = Vector3.zero;

            //Debug.Log($"parentWorldSpeed {parentWorldDisplacement / Time.fixedDeltaTime}");
            //Debug.Log($"totalWorldSpeed  {selfWorldDisplacement / Time.fixedDeltaTime}");

            if (playerCollider is CapsuleCollider capsule)
            {
                // 6) If the capsule starts this frame intersecting something,
                // compute a push-out vector. The parent displacement is passed in
                // so depenetration is solved relative to the support motion too.
                KinematicCalc.PushOutOfOverlaps(
                    ref depenetrationDisplacement,
                    capsule,
                    parentWorldDisplacement);
            }

            // 7) "Support" motion is everything that should happen before the
            // player's own sweep: platform carry plus overlap correction.
            var supportDisplacement = parentWorldDisplacement + depenetrationDisplacement;

            // 8) Solve the player's intended motion with iterative move-and-slide.
            // This attempts the self displacement, clips against obstacles, and
            // returns the reachable movement after sliding along hit surfaces.
            var slideDisplacement = KinematicCalc.MoveAndSlideIterations(
                playerCollider as CapsuleCollider,
                playerBody != null ? playerBody.position : transform.position,
                selfWorldDisplacement,
                moveAndSlideIterations,
                moveAndSlideHitBuffer,
                skin,
                supportDisplacement);

            // 9) Final displacement for this tick is support/carry motion plus
            // the resolved self movement.
            var totalDisplacement = supportDisplacement + slideDisplacement;

            // Inertia has already been integrated before the sweep. Retain only
            // reachable self-motion; carry and push-out must not become momentum.
            if (displacementApplyMode == DisplacementApplyMode.Inertia)
            {
                FlyingVelocity = slideDisplacement / Time.fixedDeltaTime;
            }

            // 10) Apply exactly the collision-resolved displacement.
            ApplyTotalDisplacement(totalDisplacement);
            //Debug.Log($"Vertical speed : {totalDisplacement.y / Time.fixedDeltaTime}");
        }

        if (IsFlying && visualsRotationController != null && visualsRotationController.isActiveAndEnabled)
        {
            visualsRotationController.FixedUpdateController();
        }

        // 11) Cache the current parent transform position so next fixed step can
        // measure fresh parent carry motion against this frame's result.
        UpdateOldParentPosition();

    }

    private void CheckResetToStartPosition()
    {
        var currentY = playerBody != null ? playerBody.position.y : transform.position.y;
        if (currentY < -100f)
        {
            ResetToStartPosition();
        }

    }

    private void ResetToStartPosition()
    {
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
            //playerBody.position = startPosition;
            playerBody.MovePosition(startPosition);
        }
        else
        {
            transform.position = startPosition;
        }

        localVelocity = Vector3.zero;
        localVerticalSpeedAccumulator = 0f;
        ResetDisplacementApplyState();
        IsGrounded = false;
        waterTriggers.Clear();
    }


    public void UpdateMoveInputRotated()
    {
        MoveInputRaw = GetMove2D();
        MoveInputRotated = RotateInputByCamera(MoveInputRaw);
    }

    public void UpdateMoveInputRotated3D()
    {
        MoveInputRaw3D = GetMove3D();
        MoveInputRotated3D = RotateInputByCamera3D(MoveInputRaw3D);
    }

    public bool IsJumpPressedOnGround()
    {
        return isJumpEnabled && CheckIsGrounded() && IsJumpPressed();
    }

    public bool IsWASDMoving()
    {
        var moveInput = GetMove2D();
        return moveInput.x != 0f || moveInput.y != 0f;
    }

    public bool IsWASDStaying()
    {
        return !IsWASDMoving();
    }

    public bool IsSwimmingInputPressed()
    {
        return GetMove3D() != Vector3.zero;
    }


    public bool IsJumpPressed()
    {
        return buttonControls != null && buttonControls.IsJumpPressed();
    }

    public bool IsInteractButtonPressed()
    {
        return buttonControls != null && buttonControls.IsInteractButtonPressed();
    }

    public bool IsCancelButtonPressed()
    {
        return buttonControls != null && buttonControls.IsCancelButtonPressed();
    }

    public bool IsPunchButtonPresssed()
    {
        return BattleReady && buttonControls != null && buttonControls.IsPunchButtonPresssed();
    }

    public void SwitchToAttackState()
    {
        upperBodyVisualsController?.RequestAttack();
    }

    public bool IsGunSelected()
    {
        return GetActiveWeaponIndex() == GunWeaponIndex;
    }

    public void UpdateAiming()
    {
        CameraController camera = PlayerCameraController;
        if (camera == null)
        {
            return;
        }

        if (!CanAim)
        {
            StopAiming();
            return;
        }

        camera.SetState(IsFlying
            ? (AbstractCameraState)camera.FirstPersonFlyingCameraState
            : camera.FirstPersonCameraState);
    }

    public void StopAiming()
    {
        if (IsAiming)
        {
            CameraController camera = PlayerCameraController;
            camera.SetState(IsFlying
                ? (AbstractCameraState)camera.LookAtFlyingCameraState
                : camera.LookAtCameraState);
        }
    }

    public int GetActiveWeaponIndex()
    {
        return handWeaponSwitch != null ? handWeaponSwitch.GetActiveIndex() : 0;
    }

    public void PlayFlyingLocomotionAnimation(string animationName)
    {
        // Moving between hover and flight must not interrupt an attack in progress.
        if (animator != null && flyingAttackAnimationName == null)
        {
            animator.speed = 1f;
            animator.CrossFadeInFixedTime(animationName, 0.15f);
        }
    }

    public void UpdateFlyingAttackAnimation(string locomotionAnimationName)
    {
        if (animator == null)
        {
            flyingAttackAnimationName = null;
            return;
        }

        bool attackFinished = false;
        if (flyingAttackAnimationName != null)
        {
            flyingAttackElapsedTime += Time.fixedDeltaTime;
            // Match the grounded sword's follow-up window.
            if (flyingAttackAnimationName == "ForwardSwordAttack"
                && IsPunchButtonPresssed()
                && flyingAttackElapsedTime >= 0.75f && flyingAttackElapsedTime <= 1.25f)
            {
                PlayFlyingAttackAnimation("Stab2");
                return;
            }

            var animationState = animator.GetCurrentAnimatorStateInfo(0);
            if (animator.IsInTransition(0)
                || !animationState.IsName(flyingAttackAnimationName)
                || animationState.normalizedTime < 1f)
            {
                return;
            }

            flyingAttackAnimationName = null;
            attackFinished = true;
        }

        if (IsPunchButtonPresssed())
        {
            switch (GetActiveWeaponIndex())
            {
                case GunWeaponIndex:
                    PlayFlyingAttackAnimation("Shoot");
                    break;
                case 2:
                    PlayFlyingAttackAnimation("ForwardSwordAttack");
                    break;
                default:
                    PlayFlyingAttackAnimation("Punch");
                    break;
            }
        }
        else if (attackFinished)
        {
            PlayFlyingLocomotionAnimation(locomotionAnimationName);
        }
    }

    private void PlayFlyingAttackAnimation(string animationName)
    {
        // Only the animation changes; the flying state continues to own physics and facing.
        flyingAttackAnimationName = animationName;
        flyingAttackElapsedTime = 0f;
        animator.speed = 1.5f;
        animator.CrossFadeInFixedTime(animationName, 0.10f, 0, 0f);
    }

    public bool IsBlocking()
    {
        return BattleReady && !IsGunSelected() && buttonControls != null && buttonControls.IsBlockButtonPressed();
    }

    public bool IsEvadeModifierPressed()
    {
        return buttonControls != null && buttonControls.IsEvadeModifierPressed();
    }

    private void UpdateRunWalkMode()
    {
        if (buttonControls == null)
        {
            return;
        }

        if (buttonControls.WasRunWalkTogglePressed())
        {
            isRunMode = !isRunMode;
        }
    }

    public void SetState(AbstractPlayerState newState)
    {
        if (newState == null || newState == currentState)
        {
            return;
        }

        bool wasFlying = IsFlying;
        currentState?.OnExit();
        if (newState != FlyingIdleState && newState != FlyingMoveState && newState != FlyingDashState)
        {
            flyingAttackAnimationName = null;
        }
        currentState = newState;
        if (!currentState.AllowsUpperBodyAttacks && upperBodyVisualsController != null)
            upperBodyVisualsController.SetState(upperBodyVisualsController.DefaultUpperBodyState);
        if (!currentState.AllowsAiming)
        {
            StopAiming();
        }

        displacementApplyMode = IsFlying ? DisplacementApplyMode.Inertia : DisplacementApplyMode.Simple;
        if (IsFlying && !wasFlying)
        {
            BeginFlight();
        }
        else if (wasFlying && !IsFlying)
        {
            EndFlight();
        }
        //Debug.Log($"PlayerController.SetState {newState}");
        currentState.OnEnter();
    }

    private void UpdateFlightToggle()
    {
        if (currentState == IdlePauseState || currentState == InteractState)
        {
            return;
        }

        if (buttonControls != null && buttonControls.WasFlightTogglePressed())
        {
            if (IsFlyingDashing && FlyingDashState.IsDirectionLocked)
                return;

            SetState(IsFlying ? (AbstractPlayerState)IdleState : FlyingIdleState);
        }
    }

    private void UpdateFlyingDash()
    {
        if ((currentState == FlyingIdleState || currentState == FlyingMoveState)
            && buttonControls != null && buttonControls.WasFlyingDashPressed())
        {
            UpdateFlyingMoveInput();
            // Space counts as active flight even if pressed in the same frame as dash.
            // Otherwise, hovering retains its backwards dash entry.
            FlyingDashState.Begin(currentState == FlyingIdleState && MoveInputRaw3D.y == 0f
                ? Vector3.back : MoveInputRaw3D);
        }
    }

    private void BeginFlight()
    {
        StopAllMotion();
        ClearGroundParent();
        IsGrounded = false;
        if (playerBody != null)
        {
            playerBody.useGravity = false;
            if (!playerBody.isKinematic)
            {
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
            }
        }

        flightCameraController = PlayerCameraController;
        if (flightCameraController != null)
        {
            // Aiming is input-driven; never restore a cached first-person state after release.
            cameraStateBeforeFlight = IsAiming
                ? flightCameraController.LookAtCameraState
                : flightCameraController.CurrentState;
            if (!CanAim)
            {
                flightCameraController.SetState(flightCameraController.LookAtFlyingCameraState);
            }
            // An aiming locomotion state's OnEnter switches directly to first-person flight.
        }
    }

    private void EndFlight()
    {
        ToGroundKinematicks();
        if (flightCameraController != null && !CanAim)
        {
            flightCameraController.SetState(cameraStateBeforeFlight ?? flightCameraController.LookAtCameraState);
        }

        flightCameraController = null;
        cameraStateBeforeFlight = null;
    }

    public void UpdateFlyingMoveInput()
    {
        MoveInputRaw3D = buttonControls != null ? buttonControls.GetFlyingMove3D() : Vector3.zero;
        MoveInputRaw = new Vector2(MoveInputRaw3D.x, MoveInputRaw3D.z);
        MoveInputRotated3D = CameraFacingCalc.RotateFlyingInput(MoveInputRaw3D, Direction);
        // Cache camera-relative WASD separately so Space affects motion, not moving-flight facing.
        FlyingMoveFacingDirection = CameraFacingCalc.RotateFlyingInput(MoveInputRaw, Direction);
    }


    public bool UpdateInteractObject()
    {
        //if (TryGetDetectedInteractable(out var interactable, distance, height))
        if (TryGetDetectedInteractableHalfSphere(out var interactable, 1f))
        {
            InteractObject = interactable.gameObject;
            return true;
        }

        InteractObject = null;
        return false;
    }


    private void UpdateFocusedInteractable(float radius = 1f)
    {
        AbstractInteractable detectedInteractable = null;
        if (TryGetDetectedInteractableHalfSphere(out var interactable, radius))
        {
            detectedInteractable = interactable;
        }

        if (focusedInteractable == detectedInteractable)
        {
            return;
        }

        if (focusedInteractable != null)
        {
            Debug.Log($"OnFocusExit called for {focusedInteractable}");
            focusedInteractable.OnFocusExit();
        }

        focusedInteractable = detectedInteractable;

        if (focusedInteractable != null)
        {
            Debug.Log($"OnFocusEnter called for {focusedInteractable}");
            focusedInteractable.OnFocusEnter();
        }
    }


    private bool TryGetDetectedInteractableBox(out AbstractInteractable interactable, float distance = 0.5f, float height = 0.5f)
    {
        interactable = null;
        if (playerCollider == null)
        {
            return false;
        }

        var forward = visualsPivot != null ? visualsPivot.forward : transform.forward;
        var origin = ColliderSpatialUtil.GetColliderCenterPoint(playerCollider);
        var halfExtents = GetInteractionBoxHalfExtents(playerCollider, height);
        var orientation = Quaternion.LookRotation(forward, GetReferenceUp());
        var projectedDistance = ColliderSpatialUtil.GetProjectedFromForwardMiddlePoint(playerCollider, forward, distance);
        var castOrigin = origin + forward * halfExtents.z;
        var castDistance = Mathf.Max(0f, projectedDistance - (halfExtents.z * 2f));
        var hits = Physics.BoxCastAll(
            castOrigin,
            halfExtents,
            forward,
            orientation,
            castDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        return ColliderQueryUtil.TryGetDetectedComponentFromHits(hits, playerCollider, out interactable);
    }


    private bool TryGetDetectedInteractableHalfSphere(out AbstractInteractable interactable, float radius = 1f)
    {
        interactable = null;
        if (playerCollider == null)
        {
            return false;
        }

        var referenceUp = GetReferenceUp();
        var forward = FacingCalc.GetReferenceForwardByUp(
            referenceUp,
            visualsPivot != null ? visualsPivot.forward : transform.forward);

        var origin = ColliderSpatialUtil.GetForwardMiddlePoint(playerCollider, forward);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            radius,
            interactDetectionBuffer,
            ~0,
            QueryTriggerInteraction.Ignore);

        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            var candidateCollider = interactDetectionBuffer[i];
            if (!ColliderQueryUtil.TryGetHalfSphereCandidateComponent(
                candidateCollider,
                playerCollider,
                origin,
                forward,
                radius,
                out AbstractInteractable candidateInteractable,
                out float sqrDistance))
            {
                continue;
            }
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                interactable = candidateInteractable;
            }
        }

        return interactable != null;
    }
    private Vector3 GetInteractionBoxHalfExtents(Collider colliderToUse, float height)
    {
        float halfDepth = Mathf.Max(skin * 0.5f, 0.01f);
        var colliderHalfExtents = ColliderSpatialUtil.GetColliderHalfExtents(colliderToUse);
        return new Vector3(Mathf.Max(colliderHalfExtents.x, colliderHalfExtents.z), height * 0.5f, halfDepth);
    }

    public Vector2 RotateInputByCamera(Vector2 input)
    {
        return CameraFacingCalc.RotateInput(input, GetFacingDirectionTransform());
    }

    public Vector3 RotateInputByCamera3D(Vector3 input)
    {
        return CameraFacingCalc.RotateInput(input, GetFacingDirectionTransform());
    }

    private Transform GetFacingDirectionTransform()
    {
        return directionPointer != null ? directionPointer.GetDirection() : null;
    }

    public Vector2 GetMove2D()
    {
        return buttonControls != null ? buttonControls.GetMove2D() : Vector2.zero;
    }

    public Vector3 GetMove3D()
    {
        return buttonControls != null ? buttonControls.GetMove3D() : Vector3.zero;
    }

    public void UpdateLook()
    {
        //playerCameraController?.UpdateLook();
    }

    public float GetCameraVerticalAngleDegrees()
    {
        CameraController cameraController = PlayerCameraController;
        if (cameraController == null)
        {
            return 0f;
        }

        Transform cameraDirection = cameraController.GetDirection();
        if (cameraDirection == null)
        {
            return 0f;
        }

        Vector3 forward = cameraDirection.forward;
        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.Asin(Mathf.Clamp(forward.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    public void AddVerticalRotationAngleToBones(float angleDegrees)
    {
        if (verticalRotationBones == null)
        {
            return;
        }

        foreach (Transform bone in verticalRotationBones)
        {
            AnimationChanger.AddVerticalRotationAngle(bone, angleDegrees);
        }
    }


    public void MoveWASDKinematic(Vector2 inputRotated, float speed)
    {
        MoveWASDKinematic(new Vector3(inputRotated.x, 0f, inputRotated.y), speed);
    }

    // Simple: input * speed is velocity. Inertia: input * speed is acceleration
    // in units/second squared, integrated once into the persistent world velocity.
    public void MoveWASDKinematic(Vector3 inputRotated, float speed)
    {
        if (displacementApplyMode == DisplacementApplyMode.Inertia)
        {
            SetFlyingVelocity(KinematicCalc.IntegrateVelocity(
                FlyingVelocity, inputRotated * speed, Time.fixedDeltaTime, float.PositiveInfinity));
            return;
        }

        localVelocity += inputRotated * (speed * Time.fixedDeltaTime);
    }

    // Apply the state's final world velocity to this physics step's displacement.
    public void SetFlyingVelocity(Vector3 velocity)
    {
        FlyingVelocity = velocity;
        localVelocity = velocity * Time.fixedDeltaTime;
    }

    public void ResetFlyingVelocity()
    {
        StopAllMotion();
    }

    private Vector3 GetWorldDisplacementFromLocalVelocity()
    {
        // This is only the player's relative motion. Parent carry is applied separately.
        return ToWorldDelta(localVelocity);
    }

    public Vector3 GetParentWorldDisplacementTransform()
    {
        var currentParent = GroundParent;
        if (currentParent == null)
        {
            return Vector3.zero;
        }

        if (cachedParentForSpeed != currentParent)
        {
            cachedParentForSpeed = currentParent;
            oldParentPosition = currentParent.position;
            return Vector3.zero;
        }

        return currentParent.position - oldParentPosition;
    }

    public Vector3 GetParentWorldDisplacement()
    {
        return GetParentWorldDisplacementRigidBody();
    }



    public Vector3 GetParentWorldDisplacementRigidBody()
    {
        var currentParent = GroundParent;
        if (currentParent == null || Time.fixedDeltaTime <= Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        Rigidbody groundBody = null;
        if (BestGround.collider != null && BestGround.collider.transform == currentParent)
        {
            groundBody = BestGround.rigidbody;
        }

        if (groundBody == null)
        {
            groundBody = currentParent.GetComponent<Rigidbody>();
        }

        if (groundBody == null)
        {
            groundBody = currentParent.GetComponentInParent<Rigidbody>();
        }

        if (groundBody == null)
        {
            return Vector3.zero;
        }

        var samplePoint = playerBody.position; //playerBody != null ? playerBody.position : transform.position;
        return groundBody.GetPointVelocity(samplePoint) * Time.fixedDeltaTime;
    }


    private void UpdateOldParentPosition()
    {
        cachedParentForSpeed = GroundParent;
        oldParentPosition = cachedParentForSpeed != null ? cachedParentForSpeed.position : Vector3.zero;
    }

    private Vector3 ToWorldDelta(Vector3 localDelta)
    {
        var referenceFrame = GroundParent;
        return referenceFrame != null ? referenceFrame.TransformVector(localDelta) : localDelta;
    }
    /*/
    private Vector3 ToLocalDelta(Vector3 worldDelta)
    {
        var referenceFrame = GroundParent;
        return referenceFrame != null ? referenceFrame.InverseTransformVector(worldDelta) : worldDelta;
    }
    /*/

    public Vector3 GetReferenceUp()
    {
        //var referenceFrame = GroundParent;
        //return referenceFrame != null ? referenceFrame.up : Vector3.up;
        return Vector3.up;
    }

    private void ApplyTotalDisplacement(Vector3 totalDisplacement)
    {
        KinematicCalc.SimpleDisplacementApply(playerBody, totalDisplacement, ref previousAppliedDisplacement);
    }

    private void ResetDisplacementApplyState()
    {
        previousAppliedDisplacement = Vector3.zero;
        FlyingVelocity = Vector3.zero;
    }

    public void DoJumpImpulse()
    {
        localVerticalSpeedAccumulator += JumpImpulse;

    }

    public void ResetAerialJumpCounter()
    {
        aerialJumpCounter = Mathf.Max(0, aerialJumpAmount);
    }

    public bool TryConsumeAerialJump()
    {
        if (aerialJumpCounter <= 0)
        {
            return false;
        }

        aerialJumpCounter--;
        return true;
    }


    public void VerticalSpeedOnFloor()
    {
        KinematicCalc.SnapToGround2(
            ref localVelocity,
            playerCollider,
            BestGround,
            GroundParent,
            GetReferenceUp());
        localVerticalSpeedAccumulator = 0f;
    }

    public void VerticalSpeedInAir()
    {
        localVerticalSpeedAccumulator += gravity * Time.fixedDeltaTime;
        localVerticalSpeedAccumulator = Mathf.Clamp(localVerticalSpeedAccumulator, minVerticalSpeed, maxVerticalSpeed);
        localVelocity.y = localVerticalSpeedAccumulator * Time.fixedDeltaTime;
    }

    public void StopAllMotion()
    {
        localVelocity = Vector3.zero;
        localVerticalSpeedAccumulator = 0f;
        ResetDisplacementApplyState();

        if (playerBody == null)
        {
            return;
        }

        //playerBody.linearVelocity = Vector3.zero;
        //playerBody.angularVelocity = Vector3.zero;
    }

    public void ToSwimKinematics()
    {
        StopAllMotion();
        ClearGroundParent();

        if (playerBody == null)
        {
            return;
        }

        playerBody.useGravity = false;
        //playerBody.constraints &= ~RigidbodyConstraints.FreezeRotationX;
    }


    /// <summary>
    /// Changes player mode to ground kinematicks
    /// keeping body standing straight and 
    /// expecting react on floor.
    /// </summary>
    public void ToGroundKinematicks()
    {
        StopAllMotion();

        if (playerBody != null)
        {
            playerBody.useGravity = true;
            var currentEuler = playerBody.rotation.eulerAngles;
            var groundedRotation = Quaternion.Euler(0f, currentEuler.y, currentEuler.z);
            playerBody.rotation = groundedRotation;
            playerBody.constraints |= RigidbodyConstraints.FreezeRotationX;
        }

        if (visualsPivot != null)
        {
            var visualsEuler = visualsPivot.rotation.eulerAngles;
            visualsPivot.rotation = Quaternion.Euler(0f, visualsEuler.y, 0f);
        }
    }

    public bool IsInWater()
    {
        toRemove.Clear();
        foreach (var t in waterTriggers)
        {
            if (t == null || !t.enabled || !IsWaterTrigger(t))
            {
                toRemove.Add(t);
            }
        }

        foreach (var t in toRemove) waterTriggers.Remove(t);
        return waterTriggers.Count > 0;
    }

    public bool CanJumpOutOfWater(float upperPart = 0.5f)
    {
        return ColliderSpatialUtil.CheckUpperHalfBoundsSphere(
            playerCollider,
            waterLayer,
            GetReferenceUp(),
            upperPart);
    }

    public void KeepFloorSpeed()
    {
        localVerticalSpeedAccumulator += GetParentWorldDisplacement().y / Time.fixedDeltaTime;
    }

    public void ClearGroundParent()
    {
        GroundParent = null;
        BestGround = default;

    }

    public bool CheckIsGrounded()
    {
        if (playerCollider == null)
        {
            IsGrounded = false;
            //BestGround = default;
            return false;
        }

        var bounds = playerCollider.bounds;


        var parentWorldDisplacement = GetParentWorldDisplacement();
        var deeperfloor = parentWorldDisplacement.y;
        var distance = bounds.extents.y + skin + 0.1f + 1f * Mathf.Max(Mathf.Abs(deeperfloor), Mathf.Abs(localVerticalSpeedAccumulator * Time.fixedDeltaTime));
        var referenceUp = GetReferenceUp();

        if (KinematicCalc.TryGetBestGroundHit(
            playerCollider,
            parentWorldDisplacement,
            referenceUp,
            distance,
            out var bestGroundHit))
        {
            IsGrounded = true;
            BestGround = bestGroundHit;
            GroundParent = BestGround.collider.gameObject.transform;
        }
        else
        {
            IsGrounded = false;
            BestGround = default;
        }

        //Debug.Log($"CheckIsGrounded  : {IsGrounded} , distance : {distance}");
        return IsGrounded;
    }

    private void OnTriggerEnter(Collider other)
    {
        TrackWaterTrigger(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        TrackWaterTrigger(other, true);
    }

    private void OnTriggerExit(Collider other)
    {
        TrackWaterTrigger(other, false);
    }

    private void OnDisable()
    {
        waterTriggers.Clear();
        InteractObject = null;

        if (focusedInteractable != null)
        {
            focusedInteractable.OnFocusExit();
            focusedInteractable = null;
        }
    }

    private void TrackWaterTrigger(Collider other, bool isInside)
    {
        if (!IsWaterTrigger(other))
        {
            return;
        }

        if (isInside)
        {
            waterTriggers.Add(other);
        }
        else
        {
            waterTriggers.Remove(other);
        }
    }

    private bool IsWaterTrigger(Collider other)
    {
        return other != null
            && other.isTrigger
            && other.gameObject.layer == waterLayer
            && other.gameObject.CompareTag("Water");
    }

}
