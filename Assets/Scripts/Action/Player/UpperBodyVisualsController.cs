using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
[AddComponentMenu("Animation/Upper Body Visuals Controller")]
public sealed class UpperBodyVisualsController : MonoBehaviour
{
    private const string UpperBodyLayerName = "UpperBodyLayer";
    [SerializeField] private PlayerController playerController;
    [SerializeField, Min(0f)] private float punchDuration = 0.7f;
    [SerializeField, Min(0f)] private float stabDuration = 1.2f;
    [SerializeField, Min(0f)] private float stabFollowUpDuration = 0.8f;
    [SerializeField, Min(0f)] private float shootDuration = 0.9f;

    public PlayerController PlayerController => playerController;
    public AbstractUpperBodyState CurrentState { get; private set; }
    public DefaultUpperBodyState DefaultUpperBodyState { get; private set; }
    public PunchUpperBodyState PunchUpperBodyState { get; private set; }
    public StabUpperBodyState StabUpperBodyState { get; private set; }
    public ShootTweakState ShootTweakState { get; private set; }
    public bool IsAttacking => CurrentState != null && CurrentState != DefaultUpperBodyState;
    public bool IsMeleeAttacking => CurrentState != null && CurrentState.IsMelee;
    public float PunchDuration => Mathf.Max(0f, punchDuration);
    public float StabDuration => Mathf.Max(0f, stabDuration);
    public float StabFollowUpDuration => Mathf.Max(0f, stabFollowUpDuration);
    public float ShootDuration => Mathf.Max(0f, shootDuration);

    private void Awake() => Initialize(GetComponent<PlayerController>());

    public void Initialize(PlayerController owner)
    {
        playerController = owner;
        if (DefaultUpperBodyState != null)
        {
            if (CurrentState == DefaultUpperBodyState)
                DefaultUpperBodyState.OnEnter();
            return;
        }
        DefaultUpperBodyState = new DefaultUpperBodyState(this);
        PunchUpperBodyState = new PunchUpperBodyState(this);
        StabUpperBodyState = new StabUpperBodyState(this);
        ShootTweakState = new ShootTweakState(this);
        SetState(DefaultUpperBodyState);
    }

    public void SetState(AbstractUpperBodyState newState)
    {
        if (newState == null || newState == CurrentState)
            return;
        CurrentState?.OnExit();
        CurrentState = newState;
        CurrentState.OnEnter();
    }

    public void RequestAttack()
    {
        if (!enabled || playerController == null || !playerController.AllowsUpperBodyAttacks
            || !playerController.IsPunchButtonPresssed())
            return;
        AbstractUpperBodyState attack = GetSelectedAttackState();
        if (IsAttacking && CurrentState != attack)
            SetState(DefaultUpperBodyState);
        if (!IsAttacking)
            SetState(attack);
        else
            CurrentState.OnAttackRequested();
    }

    private AbstractUpperBodyState GetSelectedAttackState()
    {
        switch (playerController.GetActiveWeaponIndex())
        {
            case 1: return ShootTweakState;
            case 2: return StabUpperBodyState;
            default: return PunchUpperBodyState;
        }
    }

    private void Update()
    {
        CancelInvalidAttack();
        CurrentState?.Update();
    }

    private void FixedUpdate() => FixedUpdateController();

    public void FixedUpdateController()
    {
        CancelInvalidAttack();
        CurrentState?.FixedUpdate();
    }

    private void LateUpdate() => CurrentState?.LateUpdate();

    private void CancelInvalidAttack()
    {
        if (IsAttacking && (playerController == null || !playerController.AllowsUpperBodyAttacks
            || CurrentState != GetSelectedAttackState()))
            SetState(DefaultUpperBodyState);
    }

    private void OnDisable()
    {
        SetState(DefaultUpperBodyState);
        SetUpperBodyLayerWeight(0f);
    }

    public void SetUpperBodyLayerWeight(float weight)
    {
        Animator animator = playerController != null ? playerController.animator : null;
        if (animator == null || animator.runtimeAnimatorController == null)
            return;
        int layer = animator.GetLayerIndex(UpperBodyLayerName);
        if (layer >= 0)
            animator.SetLayerWeight(layer, weight);
    }

    public void PlayAttackAnimation(string animationName)
    {
        Animator animator = playerController != null ? playerController.animator : null;
        if (animator == null || animator.runtimeAnimatorController == null)
            return;
        int layer = animator.GetLayerIndex(UpperBodyLayerName);
        if (layer >= 0)
        {
            animator.SetLayerWeight(layer, 1f);
            // Layer state speeds control the attack without accelerating base-layer locomotion.
            animator.Play(animationName, layer, 0f);
        }
    }
}
